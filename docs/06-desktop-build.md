# Godot 桌面构建说明

项目使用 Godot 4.7 .NET 直接生成桌面版本，不再通过网页或 Tauri 包装。

## 开发环境

- Godot 4.7 Mono/.NET 版。
- .NET 8 SDK。
- 对应目标平台的 Godot export templates。

仓库 `.tools/` 中已有本地 Godot/.NET 时，启动脚本会优先使用它们；也可以设置 `GODOT_BIN`。

## 本地检查

```bash
./tools/run-godot.sh check
./tools/run-godot.sh smoke
./tools/run-godot.sh test
```

## 导出准备

1. 用 `./tools/run-godot.sh editor` 打开工程。
2. 在 Godot 的“项目 → 导出”中安装模板并建立 macOS、Windows 预设。
3. 为预设配置 `godot/assets/app-icons/` 中的对应图标。
4. 确认 C# 程序集、`data/` 与运行时资源均被导出。
5. 分别在 macOS 与 Windows 实机验证新建游戏、存档、退出、重启、读档和音频。

正式发布前仍需按目标平台完成签名、公证或安装包制作；这些平台凭据不放入仓库。
