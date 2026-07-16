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

## 命令行试玩

游戏的内政、人才、外交、出征、实时战斗、事件、回合与存档操作都可通过 CLI 执行，并直接复用 Godot 游戏运行时的业务规则。CLI 试玩局默认保存在 `.playtest/cli-session.json`，每次成功操作后自动续存，不会污染正式游戏存档。

```bash
./tools/game-cli.sh help                  # 查看完整命令表
./tools/game-cli.sh reset                 # 以默认势力重新开局
./tools/game-cli.sh status                # 查看当前局势
./tools/game-cli.sh cities                # 列出己方城池及可用城务
./tools/game-cli.sh officers --city 小沛   # 按城池列出武将
./tools/game-cli.sh talent candidates      # 招募目标、方式及各执行者成功率
./tools/game-cli.sh diplomacy list         # 外交关系、条约及待回应提案
./tools/game-cli.sh event show             # 查看每4回合触发的待处理事件
./tools/game-cli.sh develop 小沛 关羽 train
./tools/game-cli.sh end-turn
```

城池、武将和势力参数可使用 ID 或唯一中文名。自动化试玩时加 `--json` 可获得结构化输出；通过 `THREE_KINGDOMS_CLI_STATE=/path/to/state.json` 可以并行维护多个互不干扰的试玩局。

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
- [CLI 试玩说明](docs/10-cli-playtest.md)
- [城池升级与建造位置扩展需求](docs/11-city-upgrade-system-requirements.md)

## 当前内容

- 33 城、16 势力、112 武将、45 条道路、6 座关隘和 40 个随机事件。
- 天下地图、内政、人才、外交、出征、战报、军师和存档界面。
- 城池经营、人才登用、外交提案、路线行军、攻城结算、敌对势力 AI 与胜负判定；玩家可逐城选择亲自治理或方针委任，但不提供全局托管与全 AI 演进。
- 3 个循环自动档和 10 个手动档。
- 图片源文件、运行时 WebP、音乐和应用图标均保存在 `godot/assets/`。
