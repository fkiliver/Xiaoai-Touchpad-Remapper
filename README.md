# Xiaomi-Touchpad-Remapper

这个项目用于将 Xiaomi Book Pro 14 触控板的重按动作重定向到 Windows 系统截图界面，效果等同于 `Win+Shift+S`。

当前实现方式是拦截由重按动作唤起的 `XiaoaiAgent.exe`，并将其改为调用系统截图入口。

## 文件说明

- `src/PressureToSnip.cs`
  一个很小的启动器，实际执行的是 `explorer.exe ms-screenclip:`

- `src/RemapCommon.cs`
  安装和恢复共用的逻辑

- `src/InstallXiaoaiRemap.cs`
  安装程序入口；普通权限启动即可，它会在需要时自动请求管理员权限

- `src/RestoreXiaoaiRemap.cs`
  恢复程序入口；普通权限启动即可，它会在需要时自动请求管理员权限

## 构建

使用 .NET Framework 自带的 C# 编译器生成三个可执行文件：

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:exe /out:PressureToSnip.exe src\PressureToSnip.cs
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:exe /out:InstallXiaoaiRemap.exe src\InstallXiaoaiRemap.cs src\RemapCommon.cs
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /t:exe /out:RestoreXiaoaiRemap.exe src\RestoreXiaoaiRemap.cs src\RemapCommon.cs
```

## 安装

直接运行：

```powershell
.\InstallXiaoaiRemap.exe
```

程序会自动弹出 UAC 提权窗口。确认后，会完成以下操作：

- 为 `XiaoaiAgent.exe` 写入 IFEO 重定向
- 结束当前正在运行的 `XiaoaiAgent.exe`

执行后，后续对 `XiaoaiAgent.exe` 的启动会被重定向为系统截图。

## 恢复

直接运行：

```powershell
.\RestoreXiaoaiRemap.exe
```

程序会自动弹出 UAC 提权窗口。确认后，会删除 IFEO 重定向，并结束当前正在运行的 `XiaoaiAgent.exe`。

## 原理说明

这个方案使用 Windows 的 `Image File Execution Options`（IFEO）机制，为 `XiaoaiAgent.exe` 配置 `Debugger`。

当系统或厂商服务尝试启动 `XiaoaiAgent.exe` 时，实际运行的是 `PressureToSnip.exe`；它再调用系统的 `ms-screenclip:` 协议，从而打开 Windows 截图界面。

`InstallXiaoaiRemap.exe` 和 `RestoreXiaoaiRemap.exe` 分别负责安装和移除这条重定向。

## 注意事项

- 安装和恢复都需要管理员权限，但程序会自动请求提权
- 这个方案会影响所有对 `XiaoaiAgent.exe` 的启动，不只是一种触发方式
- `PressureToSnip.exe` 需要与安装程序放在同一目录
- Windows 或厂商软件更新后，如果入口程序名发生变化，可能需要重新调整
