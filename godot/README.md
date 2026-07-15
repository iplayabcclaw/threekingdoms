# Godot 工程

该目录是游戏的唯一正式实现，使用 Godot 4.7 .NET 与 C#，不依赖网页前端、Node.js 或 Tauri。

## 目录

- `scripts/`：运行时、领域规则、存档和界面代码。
- `scenes/`：主场景、天下地图和城池管理场景。
- `data/`：Godot 直接读取的剧本 JSON。
- `assets/runtime/`：游戏运行时直接加载的优化资源。
- `assets/generated/`：生成图片的源文件与预览图，继续保留用于后续加工。
- `assets/audio/`：背景音乐源文件。
- `assets/app-icons/`：桌面与移动平台应用图标。

## 本地命令

在仓库根目录运行：

```bash
./tools/run-godot.sh check
./tools/run-godot.sh smoke
./tools/run-godot.sh test
./tools/run-godot.sh run
./tools/run-godot.sh editor
```

如需使用系统安装的 Godot Mono：

```bash
GODOT_BIN=/path/to/godot-mono ./tools/run-godot.sh run
```
