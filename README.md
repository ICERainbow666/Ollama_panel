# Ollama 控制面板

一个轻量级的 WPF 桌面应用，用于管理 Ollama 服务的启动和停止。

## 功能

- **一键启停** — 启动/停止 Ollama 服务（`ollama serve`）
- **系统托盘** — 最小化或关闭窗口后常驻托盘，双击恢复
- **状态监控** — 实时显示运行状态（绿灯运行中 / 红灯已停止）及 API 版本
- **日志输出** — 实时显示 Ollama 的控制台输出
- **开机自启** — 可选开机自动启动 Ollama 服务
- **路径自定义** — 支持通过浏览按钮选择 Ollama 安装路径
- **设置持久化** — 所有设置保存到注册表，重启不丢失

## 环境要求

- Windows 10 / 11
- 已安装 [Ollama](https://ollama.com)
- 开发需要 .NET 10 SDK

## 使用方式

### 直接运行

下载 `Ollama_panel.exe`，双击即可运行，无需安装。

### 从源码构建

```bash
dotnet build
dotnet run
```

### 打包发布

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

发布后的单文件 exe 位于 `dist/` 目录。

## 使用说明

1. 启动后在设置中确认 Ollama 路径是否正确（默认：`C:\Users\用户名\AppData\Local\Programs\Ollama\ollama.exe`）
2. 点击「启动 Ollama」按钮
3. 关闭窗口会最小化到系统托盘，托盘右键可退出
4. 勾选「开机自启」可在系统启动时自动运行 Ollama 服务

## 技术栈

- C# / .NET 10
- WPF（UI）
- Windows Forms NotifyIcon（系统托盘）
- Windows Registry（设置存储）

零第三方依赖。
