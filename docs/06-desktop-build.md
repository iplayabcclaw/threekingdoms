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

## Windows 导出准备

1. 安装 Godot 4.7 .NET 与 .NET 8 SDK。
2. 在 Godot 的“编辑器 → 管理导出模板”中安装同版本的 Windows x86_64 export template 与 ICU Data。
3. 仓库已经提交 `godot/export_presets.cfg`，无需在另一台电脑手工新建 Windows 预设。
4. Windows PowerShell 执行 `.\tools\build-windows.cmd`；macOS/Linux 交叉导出执行 `./tools/build-windows.sh`。
5. 产物生成到仓库根目录的 `build/windows/`。

构建脚本会先编译 C#，再使用 `Windows Desktop` release 预设导出。预设显式包含剧本 JSON，并使用 `godot/assets/app-icons/icon.ico` 作为 Windows 图标。`godot/assets/generated/` 仅包含原始加工素材，不会进入导出产物。

完成导出后，需要在 Windows 实机验证新建游戏、存档、退出、重启、读档和音频。

正式发布前仍需按目标平台完成签名、公证或安装包制作；这些平台凭据不放入仓库。
