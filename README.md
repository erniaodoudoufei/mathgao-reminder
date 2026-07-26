# MathGao Reminder

面向课堂使用的 Windows 提醒工具，可设置提醒时间、显示倒计时，并在到点后展示全屏提醒。

## 功能

- 设置日期、时间和提醒内容
- 桌面悬浮状态与倒计时
- 到点全屏提醒
- 托盘运行与主窗口快速恢复
- 单实例运行，重复启动时唤醒已打开的窗口

## 运行环境

- Windows 10/11
- .NET 8 Desktop Runtime

## 本地运行

```powershell
dotnet run
```

## 构建

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained false
```

## 界面预览

![启动后页面](启动后页面.png)

![提醒倒计时](提醒倒计时15秒.png)

![全屏提醒](全屏提醒.png)
