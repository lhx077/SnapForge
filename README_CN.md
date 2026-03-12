# SnapForge

**SnapForge** - 一款轻量级、开源的 Windows 截图标注工具，采用独特的 TUI + GUI 混合架构。

## 功能特性

- **区域截图** - 全屏覆盖层，拖拽选择截图区域
- **标注工具** - 箭头、矩形、椭圆、直线、自由画笔、文字、马赛克
- **TUI 控制面板** - 基于 Terminal.Gui 的仪表板界面，Tokyo Night 配色
- **终端模式** - 类似 bash/cmd 的全屏终端界面
- **全局热键** - 可配置的系统级快捷键（默认：PrintScreen）
- **系统托盘** - 后台运行，托盘图标和右键菜单
- **开机自启** - 可选的 Windows 开机启动
- **JSON 配置** - 所有设置存储在可读的配置文件中
- **剪贴板 + 文件** - 复制到剪贴板和/或保存到磁盘

## 快速开始

### 环境要求

- Windows 10 1809+ / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 构建运行

```bash
cd src
dotnet build
dotnet run
```

### 发布构建

```bash
cd src
dotnet publish -c Release -r win-x64 --self-contained
```

## 使用指南

### TUI 面板模式

启动 SnapForge 后进入 TUI 控制面板，使用底部按钮或命令输入框：

| 快捷键 | 操作 |
|--------|------|
| F1 | 截图 |
| F2 | 帮助 |
| F3 | 设置 |
| F4 | 关于 |
| F5 | 退出 |

在面板中按任意字母键会自动聚焦到命令输入框。

### TUI 终端模式

输入 `mode` 命令在面板模式和终端模式之间切换。终端模式提供无边框的全屏命令行界面。

### 命令列表

```
capture          截图
mode             切换面板/终端模式
config           显示当前配置
set <键> <值>    设置配置项
get <键>         获取配置项
hotkey           打开热键录制窗口
autostart on|off 启用/禁用开机自启
about            关于信息
clear            清除日志
exit             退出
help             显示帮助
```

### `set` / `get` 支持的配置键

| 键名 | 别名 | 说明 |
|------|------|------|
| `hotkey` | - | 全局热键 |
| `savedirectory` | `dir` | 截图保存目录 |
| `imageformat` | `format` | 图片格式：`png`、`jpg`、`bmp` |
| `defaultcolor` | `color` | 默认标注颜色（十六进制） |
| `defaultpenwidth` | `penwidth` | 画笔宽度（像素） |
| `defaultfontsize` | `fontsize` | 文字大小 |
| `mosaicblocksize` | `mosaic` | 马赛克块大小 |

### 标注编辑器

截图后自动打开标注编辑器，支持以下工具：

| 工具 | 快捷键 | 说明 |
|------|--------|------|
| 箭头 | A | 绘制箭头 |
| 矩形 | R | 绘制矩形 |
| 椭圆 | E | 绘制椭圆 |
| 直线 | L | 绘制直线 |
| 画笔 | F | 自由画笔 |
| 文字 | T | 添加文字标注 |
| 马赛克 | M | 区域马赛克 |

**编辑器操作：**
- **Ctrl+Z** - 撤销
- **Ctrl+S** - 保存到文件
- **Ctrl+C** - 复制到剪贴板
- **Esc** - 取消/关闭
- **鼠标滚轮** - 调整画笔宽度

### 系统托盘

右键托盘图标：
- 截图
- 打开主面板
- 设置
- 退出

左键点击托盘图标打开 TUI 面板。

## 配置文件

配置文件路径：`%APPDATA%\SnapForge\config.json`

```json
{
  "SaveDirectory": "C:\\Users\\你的用户名\\Pictures",
  "ImageFormat": "png",
  "JpegQuality": 90,
  "AutoStart": false,
  "MinimizeToTray": true,
  "PlaySound": false,
  "CopyToClipboard": true,
  "Hotkey": "PrintScreen",
  "MosaicBlockSize": 12,
  "DefaultPenWidth": 2,
  "DefaultColor": "#FF0000",
  "DefaultFontSize": 16,
  "DefaultFontFamily": "Microsoft YaHei"
}
```

### 配置项说明

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SaveDirectory` | string | `~/Pictures` | 截图保存目录 |
| `ImageFormat` | string | `"png"` | 输出格式：`png`、`jpg`、`bmp` |
| `JpegQuality` | int | `90` | JPEG 压缩质量 (1-100) |
| `AutoStart` | bool | `false` | 开机自启 |
| `MinimizeToTray` | bool | `true` | 最小化到托盘 |
| `PlaySound` | bool | `false` | 截图后播放提示音 |
| `CopyToClipboard` | bool | `true` | 截图后复制到剪贴板 |
| `Hotkey` | string | `"PrintScreen"` | 全局热键组合 |
| `MosaicBlockSize` | int | `12` | 马赛克块大小 |
| `DefaultPenWidth` | int | `2` | 默认画笔宽度 |
| `DefaultColor` | string | `"#FF0000"` | 默认标注颜色 |
| `DefaultFontSize` | int | `16` | 文字标注字号 |
| `DefaultFontFamily` | string | `"Microsoft YaHei"` | 文字标注字体 |

### 热键格式

使用 `+` 组合修饰键和按键：

```
PrintScreen           单键
Ctrl+Shift+X          修饰键组合
Alt+F9                Alt + 功能键
Ctrl+Alt+S            多修饰键
```

支持的修饰键：`Ctrl`、`Shift`、`Alt`、`Win`

## 技术栈

- **.NET 8.0** - 运行时
- **Terminal.Gui 2.x** - TUI 框架（Tokyo Night TrueColor 主题）
- **WinForms** - GUI 标注编辑器 & 热键录制
- **System.Drawing** - 图像处理与渲染

## 项目结构

```
ScreenshotPlugin/
+-- src/
|   +-- Program.cs                 # 入口 & 主循环
|   +-- App/
|   |   +-- Config.cs              # JSON 配置管理
|   |   +-- TrayIcon.cs            # 系统托盘 & 全局热键
|   |   +-- GlobalHotkey.cs        # Win32 热键注册
|   |   +-- AutoStartManager.cs    # 开机自启注册表
|   +-- TUI/
|   |   +-- TuiMainWindow.cs       # TUI 面板 & 终端
|   +-- GUI/
|   |   +-- ScreenshotOverlay.cs   # 截图覆盖层
|   |   +-- AnnotationEditor.cs    # 标注编辑器
|   |   +-- Tools/
|   |       +-- AnnotationTool.cs  # 工具定义
|   |       +-- AnnotationRenderer.cs # 绘制渲染器
|   +-- ScreenshotPlugin.csproj
+-- LICENSE                        # Apache-2.0
+-- README.md                      # English README
+-- README_CN.md                   # 中文 README
```

## 许可证

基于 [Apache License 2.0](LICENSE) 开源。

Copyright 2026 lhx077

## 作者

**lhx077** - [GitHub](https://github.com/lhx077)

---

<p align="center">
  <b>SnapForge</b> - 截图 * 标注 * 分享
</p>
