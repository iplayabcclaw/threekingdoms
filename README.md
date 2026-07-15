# 三国：山河逐鹿

一款以东汉末年为背景的单机势力经营策略游戏。项目现已统一为 **Godot 4.7 .NET / C#** 实现，仓库中的 React、Vite、Tauri 和 HTML 旧版本已移除。

## 项目入口

- Godot 工程：`godot/project.godot`
- C# 代码：`godot/scripts/`
- 场景：`godot/scenes/`
- 剧本数据：`godot/data/`
- 游戏资源：`godot/assets/`
- 资源处理工具：`tools/`

## 运行与验证

仓库自带启动脚本，会优先使用 `.tools/` 中的 Godot/.NET；也可以通过 `GODOT_BIN` 指定系统中的 Godot Mono 可执行文件。

```bash
./tools/run-godot.sh check   # 编译 C#
./tools/run-godot.sh smoke   # 无窗口启动检查
./tools/run-godot.sh test    # 领域命令、回合、事件与存读档自测
./tools/run-godot.sh run     # 启动游戏
./tools/run-godot.sh editor  # 打开 Godot 编辑器
```

## Windows 一键打包

另一台电脑克隆仓库后，安装 **Godot 4.7 .NET**、**.NET 8 SDK**，并在 Godot 中安装同版本的 **Windows x86_64 export template** 与 **ICU Data**。如果 Godot 可执行文件不在 `PATH`，先设置 `GODOT_BIN`。

Windows PowerShell：

```powershell
$env:GODOT_BIN = "C:\Tools\Godot\Godot_v4.7-stable_mono_win64.exe"
.\tools\build-windows.cmd
```

macOS/Linux 交叉导出 Windows：

```bash
GODOT_BIN=/path/to/godot-mono ./tools/build-windows.sh
```

产物位于 `build/windows/`。导出预设会把游戏运行需要的 JSON、字体、音乐和 `assets/runtime/` 一并打包；`assets/generated/` 原始加工素材不参与运行和导出。

## 文档

- [产品需求](docs/01-requirements.md)
- [Godot 实现设计](docs/02-implementation.md)
- [C# 与 Godot 代码规范](docs/03-code-standards.md)
- [视觉与交互设计](docs/04-design-system.md)
- [生成资源说明](docs/05-generated-assets.md)
- [桌面构建说明](docs/06-desktop-build.md)
- [战斗系统优化需求](docs/07-battle-system-requirements.md)
- [内政系统深化需求](docs/08-domestic-system-requirements.md)
- [武将成长、特性与官职系统需求](docs/09-officer-progression-system-requirements.md)

## 当前内容

- 33 城、16 势力、112 武将、45 条道路、6 座关隘和 40 个随机事件。
- 天下地图、内政、人才、外交、出征、战报、军师和存档界面。
- 城池经营、人才登用、外交提案、路线行军、攻城结算、AI 演进与胜负判定。
- 3 个循环自动档和 10 个手动档。
- 图片源文件、运行时 WebP、音乐和应用图标均保存在 `godot/assets/`。
