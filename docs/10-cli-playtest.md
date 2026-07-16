# CLI 试玩说明

## 目标

CLI 是 Godot 正式玩法的无界面入口。它直接调用 `GameRuntime`、`BattleCalculator` 与 `SaveService`，不复制内政、外交或战斗判定，适合 AI 和开发者连续试玩、复现问题及做回归检查。

## 会话与查询

```bash
./tools/game-cli.sh reset [--faction 势力名或ID] [--difficulty standard|relaxed|hard]
./tools/game-cli.sh status
./tools/game-cli.sh factions [--difficulty standard|relaxed|hard]
./tools/game-cli.sh map
./tools/game-cli.sh cities [--all]
./tools/game-cli.sh city <城池>
./tools/game-cli.sh officers [--all] [--city <城池>]
./tools/game-cli.sh officer <武将>
./tools/game-cli.sh armies [--all]
./tools/game-cli.sh reports [条数]
./tools/game-cli.sh log [条数]
./tools/game-cli.sh catalog
./tools/game-cli.sh coverage
./tools/game-cli.sh preview end-turn
./tools/game-cli.sh preview develop <城> <武将> <城务>
```

默认状态位于 `.playtest/cli-session.json`。成功的玩法操作会自动保存，查询不会改动局势。设置 `THREE_KINGDOMS_CLI_STATE` 可切换到另一个会话文件。

## 内政与人才

```bash
./tools/game-cli.sh develop <城> <武将> agriculture|commerce|patrol|defense|recruit|train|search|relief
./tools/game-cli.sh build <城> <武将> <设施ID> [地块编号，从1开始]
./tools/game-cli.sh maintain <城> <设施实例ID> upgrade|repair
./tools/game-cli.sh govern <城> manual|delegated <方针> <定位>

./tools/game-cli.sh talent status
./tools/game-cli.sh talent candidates
./tools/game-cli.sh talent appoint <武将> governor|strategist|civil|general|reserve
./tools/game-cli.sh talent transfer <武将> <目标城>
./tools/game-cli.sh talent recruit <候选人> <执行者> <任命>
./tools/game-cli.sh talent promote <武将> civil|military
./tools/game-cli.sh talent demote <武将>
./tools/game-cli.sh talent court-appoint <武将> <朝堂职位ID>
./tools/game-cli.sh talent court-vacate <武将>
./tools/game-cli.sh talent pay-arrears <武将>
```

设施 ID 可通过 `city <城>` 查看现有实例；`catalog` 会列出设施、阵型、兵种军令、战术、朝堂职位与特殊部队的所有有效 ID。

## 军事、外交、事件与回合

```bash
./tools/game-cli.sh expedition-options <出发城> [--soldiers 兵力]
./tools/game-cli.sh expedition <起点> <目标> <主将> <兵力> <军粮> \
  [--deputies 武将1,武将2] \
  [--composition infantry=2000,spears=500,archers=500] \
  [--special 特殊部队ID=500] \
  [--target-army 敌军ID]
./tools/game-cli.sh march <军团ID或唯一主将名>
./tools/game-cli.sh intercept <我军> <敌军>
./tools/game-cli.sh withdraw <军团> <己方城>

./tools/game-cli.sh diplomacy list
./tools/game-cli.sh diplomacy preview <势力> trade|truce|captive-exchange [赠礼]
./tools/game-cli.sh diplomacy propose <势力> trade|truce|captive-exchange [赠礼]
./tools/game-cli.sh diplomacy respond accept|reject
./tools/game-cli.sh event show
./tools/game-cli.sh event choose <选项ID>
./tools/game-cli.sh end-turn
```

随机事件沿用正式游戏规则：结算进入第 4、8、12……回合时，从当前满足条件且不在冷却中的事件里随机抽取一个。事件出现后，`end-turn` 会和界面按钮一样被阻止，必须先执行 `event show` 查看选项，再执行 `event choose <选项ID>`。

## 战斗

```bash
./tools/game-cli.sh battle show
./tools/game-cli.sh battle configure <阵型ID> \
  [--orders infantry=shield-line,spears=spear-wall,archers=rear-double] \
  [--stance 姿态ID] [--tactic 战术ID]
./tools/game-cli.sh battle start
./tools/game-cli.sh battle advance <秒>
./tools/game-cli.sh battle command <命令> <我方编组ID,...|all> [--target <敌方编组ID>] [--x N --y N]
./tools/game-cli.sh battle finish
./tools/game-cli.sh battle auto
```

`battle auto` 会从战前状态直接开始并完成战斗；`battle command` 用于测试 `attack`、`move`、`auto`、`hold`、`defend-gate`、`inner-city`、`sortie`、`reserve-line` 等实时军令。

## 屏幕点击覆盖

`coverage` 会输出“屏幕按钮 → CLI 命令”的机器可读映射。当前所有改变游戏局势的点击都有对应命令；下拉选择、列表选择等表单点击由命令参数表达，地图与人才等屏幕上的可见信息由 `map`、`talent status`、`expedition-options`、`diplomacy preview`、`battle show` 和 `reports` 提供。

页面导航、关闭弹窗、镜头缩放、背景音乐、动效开关和退出游戏不会改变玩法局势，因此不建立持久化命令；AI 可用对应查询代替页面导航，结束 CLI 进程代替退出按钮。

## 存档与机器读取

```bash
./tools/game-cli.sh save <1-10>
./tools/game-cli.sh saves
./tools/game-cli.sh load manual|auto <槽位>
./tools/game-cli.sh --json status
```

CLI 使用隔离的 `.playtest/home` 作为 Godot 用户目录，因此手动档和自动档也只属于 CLI 试玩环境。`--json` 可放在命令参数任意位置，失败时返回非零退出码并输出结构化错误。
