# Xiaomi-Touchpad-Remapper

这个项目用于将 Xiaomi Book Pro 14 触控板重按触发的小爱截图动作重定向到 Windows 系统截图，效果等同于 `Win+Shift+S`。

当前实现方式不是直接读取触控板压感事件，而是重定向小米电脑管家在重按时调用的 `XiaoaiAgent.exe --sc` 启动链路。只有命中 `--sc` 参数时才会打开系统截图，其他参数会继续透传给原版 `XiaoaiAgent.exe`，尽量不影响正常功能。

## 文件说明

- `src/PressureToSnip.cs`
  截图中转程序。命中 `--sc` 时打开系统截图，否则转发给原版 `XiaoaiAgent.exe`。
- `src/ClientPathRemapCommon.cs`
  安装和恢复共用逻辑。通过修改
  `HKLM\SOFTWARE\Timi Personal Computing\Update\Clients\XiaoaiAgent\InstallPath`
  来重定向小米侧启动路径。
- `src/InstallXiaomiClientPathRemap.cs`
  安装程序入口。
- `src/RestoreXiaomiClientPathRemap.cs`
  恢复程序入口。
- `dist/PressureToSnip.exe`
  编译后的截图中转程序。
- `dist/InstallXiaomiClientPathRemap.exe`
  编译后的安装程序。
- `dist/RestoreXiaomiClientPathRemap.exe`
  编译后的恢复程序。

## 使用方法

安装：

```powershell
.\dist\InstallXiaomiClientPathRemap.exe
```

恢复：

```powershell
.\dist\RestoreXiaomiClientPathRemap.exe
```

程序会在需要时自动请求管理员权限。

## 工作原理

安装后，工具会：

- 读取小米注册表中的 `XiaoaiAgent` 安装目录
- 将原始安装目录记录到 `RemapperOriginalInstallPath`
- 把 `InstallPath` 改到 `%ProgramData%\MI\XiaomiTouchpadRemapper`
- 在该目录下放置一个伪装成 `XiaoaiAgent.exe` 的 `PressureToSnip.exe`

这样，当电脑管家因触控板重按启动 `XiaoaiAgent.exe --sc` 时，实际启动的是 `PressureToSnip.exe`：

- 如果参数包含 `--sc`，则调用 `explorer.exe ms-screenclip:`
- 如果不是 `--sc`，则继续启动原始安装目录中的 `XiaoaiAgent.exe`

## 构建

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:winexe /out:dist\PressureToSnip.exe src\PressureToSnip.cs
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:exe /out:dist\InstallXiaomiClientPathRemap.exe src\InstallXiaomiClientPathRemap.cs src\ClientPathRemapCommon.cs
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:exe /out:dist\RestoreXiaomiClientPathRemap.exe src\RestoreXiaomiClientPathRemap.cs src\ClientPathRemapCommon.cs
```

## 注意事项

- 这个方案依赖小米电脑管家相关服务仍然负责识别触控板重按。
- 如果后续小米更新了 `XiaoaiAgent` 的调用方式或参数，这个方案可能需要重新适配。
- 安装后如果小米相关组件更新，建议先恢复，再重新安装一次。
