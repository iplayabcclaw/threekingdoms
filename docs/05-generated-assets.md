# 生图资产说明

## 1. 生成方式

首批三张背景图片、骑兵行军序列帧、地域城池与关隘图片、全部 112 名武将全身肖像使用 Codex 内置生图能力生成，生成日期为 2026-07-13。它们是项目概念和首版界面资产，不含正式 UI、文字、数值和商标。

背景源图为 1672×941 PNG；精灵和地图标记源图根据 atlas 布局采用方形或横向画布。正式运行版本保留 PNG 源文件，同时生成透明 WebP 派生文件。

## 2. 资产清单

### `world-map-bg-v1.png`

- 用途：天下地图背景。
- 焦点：中央河流和多座城池。
- UI 安全区：左右边缘与上方暗部适合覆盖面板。
- 程序覆盖：城池热点、路线、旗色、军团、筛选和资源栏。
- 限制：图片中的建筑仅作氛围，不能直接等同于剧本城市坐标。

提示词摘要：汉末古代中国地理沙盘，山河、平原、道路与城池，写实与水墨结合，16:9 斜俯视，暖色黄昏，无文字、无 UI、无战斗。

### `world-map-bg-v2.png`

- 用途：新版天下地图物理地形层，与代码中的中国轮廓遮罩、城池坐标和自然领地边界对齐。
- 构图：西北旱地与山系、北方平原、川蜀盆地、荆襄水网、江淮平原和东南海岸形成清晰的区域差异。
- 水系：以黄河、长江及南方支流为主要地形引导，城池、道路、关隘、行政线和文字仍完全由程序叠加。
- 显示方式：使用 `100% 100%` 与地图归一化坐标严格对齐，再由 `PLAYABLE_TERRITORY_BOUNDARY` 裁成汉地轮廓；海域由程序单独绘制。
- 生成方式：Codex 内置 imagegen，2026-07-13，1672×941 PNG。

提示词摘要：三国历史策略游戏中国地形图，16:9 斜俯视，按西北、华北、川蜀、荆襄、江淮、江南与岭南配置山脉、平原和水系，写实手绘结合水墨氛围；无城池、道路、行政线、文字、旗帜、UI 和现代元素。

### `world-map-bg-v4.png`

- 用途：当前 Godot 天下地图正式底图，解决全国缩放后城池与高饱和地形融为一体的问题。
- 生成方式：Codex 内置 imagegen，2026-07-14；以 `world-map-bg-v3.png` 为编辑参考进行样式迁移，保留山河、海岸与整体构图，输出 1672×941 PNG。
- 视觉规则：暖白宣纸底、淡灰黑水墨山岭、低对比水系与地纹；不包含城池、道路、势力边界、文字、旗帜、UI 或水印。
- 程序覆盖：城池、城名、旗色、关隘、道路、军团和筛选信息均由 Godot 原生节点绘制。

提示词摘要：保持原地图陆地、海岸、河流、山脉和镜头构图不变，改为明亮、克制、低对比的中国淡墨黑白地图，暖白纸色、浅灰山水，让彩色城池标记和路线在缩小后仍显著可辨；无城市、道路、文字、旗帜、UI、边框和水印。

### `council-hall-bg-v1.png`

- 用途：议政、城市概览或势力中枢背景。
- 焦点：中央桌案和远处城市。
- UI 安全区：左右立柱和下部暗区。
- 程序覆盖：城市数据、政策、行动和武将卡片。

提示词摘要：汉代木构议政厅，桌案、竹简、铜灯与远处城池，晨光，克制的写实历史策略画面，无人物、无文字、无 UI。

### `expedition-result-bg-v1.png`

- 用途：出征结果和战报背景。
- 焦点：远处城墙、营地与暮色天空。
- UI 安全区：较暗的前景和两侧区域。
- 程序覆盖：双方评分、损失、关键因素、战报和继续按钮。

提示词摘要：战役结束后的古代营地与城墙，雨后暮色、营火、补给车和归营队伍，无正在发生的战斗、无血腥、无文字、无 UI。

### `cavalry-march-sprite-source-v1.png`

- 用途：天下地图行军军团的骑兵奔跑动画源图。
- 规格：4×2 排列的 8 帧侧向完整奔跑循环，统一人物、马匹、盔甲、旗帜、比例与基线。
- 运行版本：去除纯色背景后生成 `godot/assets/runtime/cavalry-march-sprite-v1.webp`，由 Godot 精灵帧播放。
- 动画预览：`generated/cavalry-march-preview-v1.webp`，透明背景、8 帧循环。
- 路线表现：军团按当前路段切线旋转，向左行军时自动镜像；围城时暂停奔跑帧。
- 限制：无文字、无 UI、无投影、无尘土、无商标和水印。

提示词摘要：汉末披甲骑兵与深栗色战马，半写实精细手绘策略游戏素材，8 帧完整奔跑循环，严格侧视、统一比例和基线，纯绿色抠图背景。

### 战斗地形、地域城堡与五兵种动画

- 野战背景：平原、丘陵、水路、山地 4 套，均为 16:9 横向战场，中部预留两军推进和接战区域。
- 攻城背景：中原、西凉、江南、南蛮 4 套；依据世界地图城池图标使用的地域家族自动选择。每套图明确包含近景外城墙与城门、中景民居或军营、远景内城主殿，右侧为攻城军展开区域。
- 兵种动画：步兵、枪兵、弓兵、骑兵、攻城兵 5 套；每套为 4×2 的 8 帧图集，前四帧负责行进，后四帧分别表现盾击、刺击、射箭、骑兵冲击和冲车撞门。
- 武将动画：所有武将均为骑乘单位，不存在步行武将；通用猛将、统帅、军师 3 套，刘备、关羽、张飞、吕布专属 4 套，共 7 套 4×2 八帧图集。前四帧表现马上待机、行进和挥令，后四帧表现骑将冲锋、挥砍/突刺和收势。
- 源文件：`generated/battle/` 下保留背景 PNG、绿幕源图和去绿幕后的透明 PNG；骑兵同时复用既有 `cavalry-march-sprite-source-v1.png`。
- 运行版本：`runtime/battle/backgrounds/` 下为 1600×900 WebP；`runtime/battle/troops/` 与 `runtime/battle/officers/` 下为 1024×512 透明 WebP。
- 派生脚本：`tools/build-battle-assets.py` 负责背景裁切压缩、逐帧边界识别、人物等比例归一和 WebP 输出；绿幕源先通过 imagegen 技能自带的柔边去色工具清理绿色溢色。

地形提示词摘要：汉末三国历史策略游戏战斗全景，按开阔平原、黄土丘陵、浅河渡口和险峻山道分别构图，电影级手绘写实与克制水墨气氛，敌军左侧、我军右侧，中部开阔，无人物、文字、UI、旗帜和现代元素。

城堡提示词摘要：参考对应地域城池图标的材料、屋顶和防御形态，生成中原夯土砖城、西凉边塞、江南水城和南蛮山寨四类攻城全景；外城墙、城门、内城三层清晰，城池位于左侧、右侧预留攻城军区域，无军队、文字、UI、日式或欧式城堡和奇幻元素。

兵种提示词摘要：东汉末年步兵、枪兵、弓兵或木制冲车，半写实精细手绘游戏精灵，严格侧视、统一人物/器械/比例/基线，4×2 八帧，前四帧行进、后四帧对应兵种攻击动作，纯绿色抠图背景，无文字、UI、阴影、水印和现代元素。

骑将提示词摘要：东汉末年武将全程骑乘同一匹战马，严格侧视4×2八帧，完整显示马腿、骑手和兵器；通用骑将按猛将、统帅、军师区分，刘备双股剑、关羽青龙偃月刀、张飞蛇矛、吕布方天画戟使用专属身份、服色、坐骑与动作，无任何下马或步行帧。

### 地域城池与关隘标记

- 城池体系：中原、华北、东北、江南、华南、西凉、南蛮 7 类，每类 2 种，共 14 张。
- 关隘体系：山隘、北地边关、临水关城、南方林岭关塞，共 4 张。
- 运行规格：全部为 512×512 透明 WebP，位于 `runtime/map-markers/`。
- 当前剧本分配：33 座城按州域映射地域风格，并在同地域内稳定分配两个变体；潼关、雁门关、虎牢关、博望坡、剑门关、武关稳定使用现有关隘风格资产。
- 交互覆盖：势力徽记、危险状态、选中动效、城名、兵力和地图模式信息由 Godot 原生节点绘制。
- 源图：`city-*-source-v1.png` 与 `pass-markers-source-v1.png`；总览为 `map-marker-preview-v1.png`。
- 派生脚本：`tools/build-map-markers.py`，负责 atlas 拆分、统一尺寸、透明角校验与 WebP 输出。

提示词摘要：汉末三国历史策略游戏地图标记，35 度斜俯视、半写实精细手绘、统一比例和暖色光照；按中原、华北、东北、江南、华南、西凉、南中建筑材料与防御形态制作差异化城池；关隘包含山岭、北疆、临水和南方林岭类型；纯洋红抠图背景，无文字、人物、军队、现代建筑、日式城堡和奇幻元素。

### 全量武将全身肖像

- 武将：194 年剧本中的全部 112 名武将，人物 ID 与图片文件一一绑定。
- 风格：原创人物面孔、历史写实、东汉末年服饰与甲胄、克制的水墨氛围背景，不使用影视演员形象。
- 年龄：依据剧本出生年按 `194 - birthYear` 计算，并分别表现少年、青年、中年、年长中年和老年；例如庞统约 15 岁、法正约 18 岁、韩遂约 49 岁、士燮约 57 岁。
- 源图：位于 `generated/officer-portraits/`，保留生成时的 PNG 分辨率与 `-source-v1` 版本后缀。
- 运行规格：统一为 384×576 WebP，位于 `runtime/officer-portraits/`，用于名册缩略图和人物详情页。
- 总览：`generated/officer-portrait-preview-01-v1.webp` 至 `-04-v1.webp`，每页最多 28 人，并标注姓名和 194 年时年龄。
- 派生脚本：`tools/build-officer-portraits.py`，从项目武将数据读取清单，负责缺图检查、等比例补幅、WebP 压缩和分组总览生成。
- 当前状态：已通过 `officerPortraitPath(officerId)` 接入武将界面，任意剧本武将 ID 都会稳定映射到对应运行图片。

提示词摘要：三国名将原创全身肖像，写实电影级历史人物质感，完整头脚与武器，东汉末年甲胄和服饰，暗色克制背景，按人物性格与阵营区分体态、配色和气质；无影视演员相貌、无文字、无 UI、无卡牌边框、无奇幻盔甲。

## 3. 使用规则

- 图片通过资产清单引用，不在组件中散落硬编码路径。
- 场景背景显示时使用 `cover`，并允许通过焦点坐标调整裁切；地图标记使用 `contain` 保留完整轮廓。
- 背景上统一增加可调暗角和局部渐变，保证文字可读。
- 正式信息全部由 Godot 原生文本、控件和绘制节点呈现。
- 新版本使用 `-v2`、`-v3` 后缀，不覆盖旧版本。
- 删除资产前先搜索引用并更新清单。

## 4. 后续生图计划

在首页与交互结构稳定后再生成：

1. 城池繁荣、普通、疲敝三种状态背景。
2. 春夏秋冬地图氛围变体。
3. 事件插画模板。
4. 兵种与设施卡片插画。

武将肖像已完成全量风格检查；后续新增场景仍需先验证样张，再扩展批量资产。

## 5. 完整生成规格

### 天下地图

```text
Use case: stylized-concept
Asset type: game main-screen world map background
Primary request: a premium strategy-game world map inspired by the geography of ancient China during the Three Kingdoms era
Style/medium: refined painterly realism blended with Chinese ink-wash atmosphere
Composition/framing: 16:9 wide oblique top-down view with calm UI-safe edges
Constraints: no interface, no text, no numbers, no logos, no watermark, no battle, no fantasy magic
```

### 议政厅

```text
Use case: stylized-concept
Asset type: game council and city-management screen background
Primary request: an exquisite ancient Han dynasty warlord council hall interior overlooking a prosperous city
Style/medium: refined painterly realism and grounded Han dynasty material culture
Composition/framing: 16:9 wide interior with central strategy table and side UI-safe areas
Constraints: no people, no interface, no text, no logos, no watermark, no fantasy magic
```

### 出征战报

```text
Use case: stylized-concept
Asset type: game expedition result screen background
Primary request: a Three Kingdoms campaign aftermath panorama behind a numerical battle-result report
Style/medium: cinematic painterly realism with restrained Chinese ink-wash atmosphere
Composition/framing: 16:9 wide scene with dark foreground and calm areas for result cards
Constraints: no battle in progress, no gore, no UI, no text, no logos, no watermark, no fantasy magic
```

### 地域城池与关隘

```text
Use case: stylized-concept
Asset type: transparent historical strategy-game map marker atlas
Primary request: two distinct fortified settlements for one specified Three Kingdoms regional family, or four distinct fortified pass types
Style/medium: exquisite hand-painted historical realism matching the oblique top-down world map
Composition/framing: identical 35-degree oblique top-down camera, one complete marker per cell, unified scale and baseline
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background with clean gutters
Constraints: historically plausible late Eastern Han architecture; no text, people, armies, cast shadow, logos or watermark
Avoid: modern buildings, Japanese castles, European castles, fantasy architecture, cropped silhouettes
```

## 6. 生成音乐资产

### `shan-he-wei-ding-v1.mp3`

- 曲名：《山河未定》。
- 用途：天下地图、城池经营及其他主要界面的全局循环背景音乐。
- 生成日期：2026-07-13。
- 规格：MP3，44.1 kHz，双声道，256 kbps，约 4 分 22 秒。
- 播放：作为双曲目播放列表第一首，结束后切换至《烽火长歌》；默认音量 22%，受声音总开关控制。
- 风格：古琴与洞箫主奏，琵琶、低鼓、编钟和少量二胡点缀，沉稳苍凉而克制。
- 限制：纯器乐，无人声、无现代电子音、无密集战鼓和夸张电影管弦乐。

生成提示词摘要：三国时代中国风策略游戏背景音乐，约 72 BPM，以古琴、洞箫和五声音阶表现汉末乱世与辽阔山河，适合长时间思考和循环播放。

### `feng-huo-chang-ge-v1.mp3`

- 曲名：《烽火长歌》。
- 用途：全局背景音乐播放列表第二首，增强兵荒马乱、军阵推进和战争史诗氛围。
- 生成日期：2026-07-13。
- 规格：MP3，44.1 kHz，双声道，256 kbps，约 3 分 59 秒。
- 播放：已停用，不进入运行时播放列表；因成品出现不符合要求的人声质感，由 v2 替换。
- 风格：古琴、埙、洞箫和二胡奏出悠长乱世主题，中段逐渐加入琵琶、大鼓、军阵战鼓、编钟、低音合奏和浑厚号角。
- 限制：纯器乐，无人声、无现代电子音、无摇滚鼓组，避免持续高强度轰鸣。

生成提示词摘要：三国时期兵荒马乱、群雄征战题材的中国风史诗背景音乐，约 68 BPM，从孤城残阳与百姓流离逐渐推进至烽烟四起和铁骑压境，最后回落到战后苍茫山河。

### `feng-huo-chang-ge-v2.mp3`

- 曲名：《烽火长歌》v2。
- 用途：替换 v1，作为全局背景音乐播放列表第二首。
- 生成日期：2026-07-13。
- 规格：MP3，44.1 kHz，双声道，256 kbps，约 2 分 33 秒。
- 播放：已停用，不进入运行时播放列表；作为 v3 琵琶与古筝改编版的参考音频保留。
- 风格：参考经典三国历史剧片尾曲的苍茫回望与英雄兴亡气质，但使用原创旋律；以古琴、箫、琵琶、低音弦乐、大鼓、军鼓和编钟逐层推进。
- 限制：请求使用 `is_instrumental: true`，并明确排除演唱、吟唱、合唱、哼唱、念白、戏曲唱腔及任何人声采样；不复刻现有歌曲旋律。

生成提示词摘要：三国历史题材的中国风史诗纯器乐，以缓慢绵长的五声音阶主题表现英雄散尽、江山更替和百姓离乱，中段以沉稳鼓阵和编钟展开时代洪流，最后回到孤独的古琴与箫。

### `feng-huo-chang-ge-v3.mp3`

- 曲名：《烽火长歌》v3。
- 用途：替换 v2，作为全局背景音乐播放列表第二首。
- 生成日期：2026-07-13。
- 生成方式：使用 MiniMax `music-cover`，以 v2 为参考音频进行配器改编，而非重新文生音乐。
- 规格：MP3，44.1 kHz，双声道，256 kbps，约 2 分 31 秒。
- 播放：承接《山河未定》，结束后回到第一首，形成双曲目循环。
- 风格：尽量保持 v2 的主旋律、速度、段落结构、情绪推进与和声走向，仅使用琵琶和古筝重新配器。
- 限制：明确排除人声、吟唱、合唱、哼唱、念白、人声采样及鼓、弦乐、管乐、编钟、电子音等其他乐器。

生成提示词摘要：纯器乐中国风改编，琵琶承担主旋律和轮指，古筝承担铺陈、刮奏、泛音与和声，保留苍茫、悠远、悲壮的三国历史史诗感。

### `jin-ge-po-zhen-v1.mp3`

- 曲名：《金戈破阵》。
- 用途：战前布阵、实时交战与战斗结算前的专用背景音乐。
- 生成日期：2026-07-14。
- 生成方式：MiniMax Music 2.6，`is_instrumental: true`纯器乐模式。
- 规格：MP3，44.1 kHz，双声道，256 kbps，约 1 分 05 秒。
- 播放：存在待处理战斗时替换全局歌单并单曲循环；战斗完成后恢复《山河未定》与《烽火长歌》的双曲目歌单。
- 风格：约 108 BPM 的古风战阵配乐；琵琶以快速拨奏、扫弦、轮指和短促断奏表现奔袭与交锋，中国大鼓以低沉、分层的节奏推动对峙、冲锋和反击。
- 限制：纯器乐，无演唱、吟唱、合唱、哼唱、念白或人声采样；无现代电子音、摇滚鼓组、铜管和西式管弦乐。

生成提示词摘要：三国历史策略游戏古风战斗纯器乐，五声音阶，琵琶与中国大鼓主奏，数秒内进入战斗，中段呈现对峙、冲锋和反击，结尾保留适合循环的节奏延续感。

## 3. 第三方字体

- `godot/assets/fonts/NotoSansSC-VF.otf`：Noto Sans CJK Simplified Chinese 可变字体，来源于 `notofonts/noto-cjk` 官方仓库。
- `godot/assets/fonts/OFL-1.1.txt`：字体对应的 SIL Open Font License 1.1；字体可随游戏嵌入和分发。
