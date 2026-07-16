using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class CityManagementView : Control
{
    [Signal] public delegate void BackRequestedEventHandler();

    private readonly Dictionary<string, List<Button>> _commandButtons = [];
    private readonly Dictionary<Button, string> _commandFocuses = [];
    private readonly Dictionary<string, Control> _pages = [];
    private readonly Dictionary<string, Button> _tabs = [];
    private GameRuntime _runtime = null!;
    private string _cityId = string.Empty, _page = "overview", _selectedSlot = string.Empty;
    private bool _overviewMode = true, _awaitingBuilder;
    private Button _back = null!, _previous = null!, _next = null!, _buildAction = null!, _upgradeAction = null!, _repairAction = null!, _cityUpgradeAction = null!, _cancelCityUpgradeAction = null!;
    private Label _title = null!, _subtitle = null!, _notice = null!, _buildingTitle = null!, _buildingInfo = null!, _cityUpgradeInfo = null!, _overviewResources = null!, _overviewDevelopment = null!, _overviewLedger = null!;
    private ScrollContainer _overview = null!;
    private VBoxContainer _overviewList = null!, _buildingSlots = null!;
    private Control _detail = null!;
    private OptionButton _officer = null!, _commandGroup = null!, _facilityChoice = null!, _builderChoice = null!, _cityUpgradeOfficer = null!, _governanceMode = null!, _policy = null!, _role = null!;
    private Control _builderRow = null!;
    private ConfirmationDialog _cityUpgradeDialog = null!, _cancelCityUpgradeDialog = null!;

    public void Initialize(GameRuntime runtime)
    {
        _runtime = runtime;
        BuildInterface();
        runtime.Changed += Refresh;
        runtime.Notice += message => { if (_notice is not null) _notice.Text = message; };
        ShowOverview();
    }

    public void ShowOverview()
    {
        _overviewMode = true;
        _overview.Visible = true;
        _detail.Visible = false;
        Refresh();
    }

    public void ShowCity(CityData city)
    {
        _cityId = city.Id;
        _overviewMode = false;
        _overview.Visible = false;
        _detail.Visible = true;
        SelectPage("overview");
        Refresh();
    }

    public void ShowPageForVisualTest(string page)
    {
        if (!_overviewMode && _pages.ContainsKey(page)) SelectPage(page);
    }

    private void Refresh()
    {
        if (_title is null) return;
        if (_overviewMode) RefreshOverview(); else RefreshCity();
    }

    private void RefreshOverview()
    {
        _title.Text = "势力内政";
        var cities = PlayerCities().ToList();
        var remaining = cities.Sum(city => city.ActionSlots);
        var capacity = cities.Sum(city => city.ActionCapacity);
        var treasury = _runtime.State.Resources;
        _subtitle.Text = $"势力府库：金 {treasury.Gold:N0}　粮 {treasury.Food:N0}　· {cities.Count}城 · 本月城务 {remaining}/{capacity}";
        _back.Text = "← 返回天下";
        _previous.Visible = false; _next.Visible = false;
        foreach (var child in _overviewList.GetChildren()) child.QueueFree();
        var hint = new Label { Text = "金粮由全势力共用。选择一座城池，查看太守、发展、建筑与治理方针。", CustomMinimumSize = new Vector2(0, 44), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        hint.AddThemeColorOverride("font_color", GameTheme.Bronze);
        _overviewList.AddChild(hint);
        foreach (var city in cities.OrderBy(CitySortScore).ThenBy(item => item.Name))
        {
            var forecast = _runtime.CityMonthlyForecast(city);
            var button = GameTheme.Button(
                $"{city.Name} Lv.{city.CityLevel}　{GameRuntime.CityStatusLabel(city.Status)}　太守 {city.GovernorName}　城务 {city.ActionSlots}/{city.ActionCapacity}\n" +
                $"月度贡献：金 {forecast.GoldIncome - forecast.GoldUpkeep:+#,0;-#,0;0}　粮 {forecast.FoodIncome - forecast.FoodUpkeep:+#,0;-#,0;0}　驻军 {city.Garrison:N0}　" +
                $"{GameRuntime.CityRoleLabel(city.CityRole)} / {GameRuntime.GovernancePolicyLabel(city.GovernancePolicy)}　· {_runtime.CityPrioritySummary(city)}");
            button.CustomMinimumSize = new Vector2(0, 78); button.Alignment = HorizontalAlignment.Left;
            var selectedCity = city; button.Pressed += () => ShowCity(selectedCity);
            _overviewList.AddChild(button);
        }
    }

    private void RefreshCity()
    {
        var city = _runtime.City(_cityId);
        if (city is null) { ShowOverview(); return; }
        var faction = _runtime.Faction(city.OwnerFactionId);
        var forecast = _runtime.CityMonthlyForecast(city);
        var treasury = _runtime.State.Resources;
        _title.Text = $"{city.Name}城 · Lv.{city.CityLevel}";
        _subtitle.Text = $"{city.Region} · {faction?.Name ?? "未知势力"} · 太守 {city.GovernorName} · {GameRuntime.CityStatusLabel(city.Status)} · 本月城务 {city.ActionSlots}/{city.ActionCapacity}";
        _back.Text = "← 返回天下"; _previous.Visible = true; _next.Visible = true;
        _overviewResources.Text = $"势力府库\n金　{treasury.Gold:N0}\n粮　{treasury.Food:N0}\n\n本城月度贡献\n金 {forecast.GoldIncome - forecast.GoldUpkeep:+#,0;-#,0;0}\n粮 {forecast.FoodIncome - forecast.FoodUpkeep:+#,0;-#,0;0}\n\n驻军 {city.Garrison:N0}　人口 {city.Population:N0}";
        _overviewDevelopment.Text = $"太守\n{city.GovernorName}\n\n农业　{city.Agriculture}　商业　{city.Commerce}\n治安　{city.PublicOrder}　民心　{city.PublicSupport}\n城防　{city.Defense}　文化　{city.Culture}\n训练　{city.Training}　疲敝　{city.Fatigue}\n\n当前要务：{_runtime.CityPrioritySummary(city)}";
        var ledger = city.LedgerEntries.TakeLast(5).Reverse().Select(item => $"第{item.Turn}月 · {item.Description}");
        var occupiedFacilitySlots = city.Facilities.Count + (city.ConstructionQueue?.Kind == "build" ? 1 : 0);
        _overviewLedger.Text = $"城池状态\n{GameRuntime.CityStatusLabel(city.Status)}　{GameRuntime.CityRoleLabel(city.CityRole)}\n城池 Lv.{city.CityLevel}　设施 {occupiedFacilitySlots}/{city.FacilitySlots}\n" +
            (city.ConstructionQueue is null ? "当前无工程" : $"在建：{GameRuntime.ConstructionLabel(city.ConstructionQueue)} · 余{city.ConstructionQueue.RemainingMonths}月") +
            $"\n\n最近台账\n{(ledger.Any() ? string.Join('\n', ledger) : "尚无月报")}";
        FillOfficers(city);
        FillBuildingSlots(city);
        RefreshCityUpgradePanel(city);
        SelectByMetadata(_governanceMode, city.GovernanceMode);
        SelectByMetadata(_policy, city.GovernancePolicy);
        SelectByMetadata(_role, city.CityRole);
        RefreshBuildingPanel(city);
        RefreshCommandButtons();
    }

    private void BuildInterface()
    {
        var background = new ColorRect { Color = GameTheme.Backdrop }; background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(background);
        var header = new Panel { ZIndex = 10 }; header.SetAnchorsPreset(LayoutPreset.TopWide); header.OffsetBottom = 86; header.AddThemeStyleboxOverride("panel", GameTheme.HeaderBox()); AddChild(header);
        UiOrnaments.AttachInkCorners(header, 185, .055f);
        _back = GameTheme.Button("← 返回天下"); _back.Position = new Vector2(22, 21); _back.Size = new Vector2(148, 42); _back.Pressed += Back; header.AddChild(_back);
        _title = new Label { Position = new Vector2(200, 10), Size = new Vector2(500, 40), VerticalAlignment = VerticalAlignment.Center }; _title.AddThemeFontSizeOverride("font_size", 29); _title.AddThemeColorOverride("font_color", GameTheme.Paper); header.AddChild(_title);
        _subtitle = new Label { Position = new Vector2(202, 47), Size = new Vector2(1040, 28) }; _subtitle.AddThemeColorOverride("font_color", GameTheme.Muted); header.AddChild(_subtitle);
        _previous = GameTheme.Button("上一城"); _previous.SetAnchorsPreset(LayoutPreset.TopRight); _previous.OffsetLeft = -246; _previous.OffsetTop = 21; _previous.OffsetRight = -144; _previous.OffsetBottom = 63; _previous.Pressed += () => StepCity(-1); header.AddChild(_previous);
        _next = GameTheme.Button("下一城"); _next.SetAnchorsPreset(LayoutPreset.TopRight); _next.OffsetLeft = -132; _next.OffsetTop = 21; _next.OffsetRight = -30; _next.OffsetBottom = 63; _next.Pressed += () => StepCity(1); header.AddChild(_next);

        _overview = new ScrollContainer(); _overview.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); _overview.OffsetLeft = 30; _overview.OffsetTop = 104; _overview.OffsetRight = -30; _overview.OffsetBottom = -76; AddChild(_overview);
        _overviewList = new VBoxContainer { CustomMinimumSize = new Vector2(1280, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill }; _overviewList.AddThemeConstantOverride("separation", 8); _overview.AddChild(_overviewList);

        _detail = new Control { MouseFilter = MouseFilterEnum.Ignore }; _detail.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_detail);
        var tabRow = new HBoxContainer(); tabRow.SetAnchorsPreset(LayoutPreset.TopWide); tabRow.OffsetLeft = 30; tabRow.OffsetTop = 100; tabRow.OffsetRight = -30; tabRow.OffsetBottom = 148; tabRow.AddThemeConstantOverride("separation", 8); _detail.AddChild(tabRow);
        AddTab(tabRow, "overview", "总览"); AddTab(tabRow, "buildings", "建筑"); AddTab(tabRow, "governance", "治理");
        BuildOverviewPage(); BuildBuildingsPage(); BuildGovernancePage();
        _notice = new Label { Text = "金粮为势力通用资源；城务与建设仍需本城可用武将及城务额度。", AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center };
        _notice.SetAnchorsPreset(LayoutPreset.BottomWide); _notice.OffsetLeft = 30; _notice.OffsetTop = -66; _notice.OffsetRight = -30; _notice.OffsetBottom = -20; _notice.AddThemeColorOverride("font_color", GameTheme.Bronze); _notice.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.Bronze, .07f), new Color(GameTheme.GoldDim, .38f), 5, 1, 12, 4)); _detail.AddChild(_notice);
        MoveChild(header, GetChildCount() - 1);
    }

    private void AddTab(HBoxContainer row, string id, string label)
    {
        var tab = GameTheme.Button(label); tab.CustomMinimumSize = new Vector2(132, 42); tab.Pressed += () => SelectPage(id); row.AddChild(tab); _tabs[id] = tab;
    }

    private Control AddPage(string id)
    {
        var page = new Control(); page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); page.OffsetLeft = 30; page.OffsetTop = 162; page.OffsetRight = -30; page.OffsetBottom = -84; _detail.AddChild(page); _pages[id] = page; return page;
    }

    private void SelectPage(string id)
    {
        _page = id;
        foreach (var entry in _pages) entry.Value.Visible = entry.Key == id;
        foreach (var entry in _tabs)
        {
            entry.Value.Disabled = entry.Key == id;
            if (entry.Key == id)
            {
                entry.Value.AddThemeStyleboxOverride("disabled", GameTheme.Box(GameTheme.Jade, Color.FromHtml("#375a52"), 6, 1, 14, 7));
                entry.Value.AddThemeColorOverride("font_disabled_color", GameTheme.OnAccent);
            }
            else entry.Value.AddThemeStyleboxOverride("normal", GameTheme.ButtonBox("normal"));
        }
        if (!_overviewMode) Refresh();
    }

    private void BuildOverviewPage()
    {
        var page = AddPage("overview");
        var cards = new HBoxContainer(); cards.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); cards.OffsetBottom = -106; cards.AddThemeConstantOverride("separation", 12); page.AddChild(cards);
        cards.AddChild(Card("势力府库与驻军", out _overviewResources, new Vector2(330, 0)));
        cards.AddChild(Card("太守与城池发展", out _overviewDevelopment, new Vector2(370, 0)));
        cards.AddChild(ScrollableCard("城池状态与台账", out _overviewLedger, new Vector2(400, 0)));
        var actions = new VBoxContainer(); actions.SetAnchorsPreset(LayoutPreset.BottomWide); actions.OffsetTop = -96; actions.AddThemeConstantOverride("separation", 6); page.AddChild(actions); BuildCommandRow(actions);
    }

    private void BuildCommandRow(VBoxContainer parent)
    {
        var row = FlowRow(8); row.AddChild(new Label { Text = "城务武将", CustomMinimumSize = new Vector2(82, 38), VerticalAlignment = VerticalAlignment.Center });
        _officer = new OptionButton { CustomMinimumSize = new Vector2(265, 38) }; _officer.ItemSelected += _ => RefreshCommandPreviews(); row.AddChild(_officer);
        _commandGroup = Choice(135, ("生产", "production"), ("民生", "civil"), ("军事", "military"), ("人才", "talent")); _commandGroup.ItemSelected += _ => RefreshCommandButtons(); row.AddChild(_commandGroup);
        foreach (var command in new[] { ("production", "agriculture", "劝课农桑"), ("production", "commerce", "振兴商业"), ("civil", "patrol", "整顿治安"), ("civil", "relief", "赈济百姓"), ("military", "defense", "修缮城防"), ("military", "recruit", "征募士卒"), ("military", "train", "操练兵马"), ("talent", "search", "寻访人才") })
        {
            var button = GameTheme.Button(command.Item3); button.CustomMinimumSize = new Vector2(116, 38); var focus = command.Item2; button.Pressed += () => _runtime.DevelopCity(_cityId, Selected(_officer), focus);
            if (!_commandButtons.TryGetValue(command.Item1, out var list)) { list = []; _commandButtons[command.Item1] = list; } list.Add(button); _commandFocuses[button] = focus; row.AddChild(button);
        }
        parent.AddChild(CommandPanel(row));
    }

    private void BuildBuildingsPage()
    {
        var page = AddPage("buildings");
        var layout = new HBoxContainer(); layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); layout.AddThemeConstantOverride("separation", 14); page.AddChild(layout);
        var board = new PanelContainer { CustomMinimumSize = new Vector2(720, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill }; board.AddThemeStyleboxOverride("panel", GameTheme.PanelBox(10)); layout.AddChild(board);
        UiOrnaments.AttachInkCorners(board, 250, .075f);
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 18); margin.AddThemeConstantOverride("margin_right", 18); margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14); board.AddChild(margin);
        var cityLayout = new VBoxContainer(); cityLayout.AddThemeConstantOverride("separation", 12); margin.AddChild(cityLayout);
        var upgradeCard = new PanelContainer(); upgradeCard.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.Panel, .78f), new Color(GameTheme.GoldDim, .65f), 7, 1, 12, 8)); cityLayout.AddChild(upgradeCard);
        var upgradeLayout = new VBoxContainer(); upgradeLayout.AddThemeConstantOverride("separation", 7); upgradeCard.AddChild(upgradeLayout);
        _cityUpgradeInfo = new Label { Text = "城池建设", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _cityUpgradeInfo.AddThemeColorOverride("font_color", GameTheme.Gold); upgradeLayout.AddChild(_cityUpgradeInfo);
        var upgradeRow = new HBoxContainer(); upgradeRow.AddThemeConstantOverride("separation", 8); upgradeLayout.AddChild(upgradeRow);
        _cityUpgradeOfficer = new OptionButton { CustomMinimumSize = new Vector2(220, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill }; upgradeRow.AddChild(_cityUpgradeOfficer);
        _cityUpgradeAction = GameTheme.Button("升级城池"); _cityUpgradeAction.CustomMinimumSize = new Vector2(130, 38); _cityUpgradeAction.Pressed += BeginCityUpgrade; upgradeRow.AddChild(_cityUpgradeAction);
        _cancelCityUpgradeAction = GameTheme.Button("取消升级"); _cancelCityUpgradeAction.CustomMinimumSize = new Vector2(110, 38); _cancelCityUpgradeAction.Pressed += BeginCancelCityUpgrade; upgradeRow.AddChild(_cancelCityUpgradeAction);
        var caption = new Label { Text = "城池示意 · 点击城内地块查看建筑", CustomMinimumSize = new Vector2(0, 38) }; caption.AddThemeFontSizeOverride("font_size", 20); caption.AddThemeColorOverride("font_color", GameTheme.Gold); cityLayout.AddChild(caption);
        var cityHint = new Label { Text = "城门与主街围绕各功能区展开；已建设施显示等级，空地可直接选择建造。", AutowrapMode = TextServer.AutowrapMode.WordSmart }; cityHint.AddThemeColorOverride("font_color", GameTheme.Muted); cityLayout.AddChild(cityHint);
        _buildingSlots = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; _buildingSlots.AddThemeConstantOverride("separation", 10); cityLayout.AddChild(_buildingSlots);

        var detail = new PanelContainer { CustomMinimumSize = new Vector2(430, 0) }; detail.AddThemeStyleboxOverride("panel", GameTheme.PanelBox(10)); layout.AddChild(detail);
        UiOrnaments.AttachInkCorners(detail, 215, .075f);
        var detailMargin = new MarginContainer(); detailMargin.AddThemeConstantOverride("margin_left", 18); detailMargin.AddThemeConstantOverride("margin_right", 18); detailMargin.AddThemeConstantOverride("margin_top", 14); detailMargin.AddThemeConstantOverride("margin_bottom", 14); detail.AddChild(detailMargin);
        var info = new VBoxContainer(); info.AddThemeConstantOverride("separation", 12); detailMargin.AddChild(info);
        _buildingTitle = new Label { Text = "选择一处地块" }; _buildingTitle.AddThemeFontSizeOverride("font_size", 22); _buildingTitle.AddThemeColorOverride("font_color", GameTheme.Gold); info.AddChild(_buildingTitle);
        _buildingInfo = new Label { Text = "点击左侧城内地块，查看建筑功效。", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0, 130) }; _buildingInfo.AddThemeColorOverride("font_color", GameTheme.Muted); info.AddChild(_buildingInfo);
        _facilityChoice = new OptionButton { CustomMinimumSize = new Vector2(0, 42) }; foreach (var entry in GameRuntime.FacilityCatalog) AddChoice(_facilityChoice, $"{entry.Value.Name} · 金{entry.Value.Gold} 粮{entry.Value.Food}", entry.Key); _facilityChoice.ItemSelected += _ => RefreshBuildingPanel(_runtime.City(_cityId)); info.AddChild(_facilityChoice);
        _buildAction = GameTheme.Button("建造此设施"); _buildAction.CustomMinimumSize = new Vector2(0, 46); _buildAction.Pressed += BeginOrConfirmBuild; info.AddChild(_buildAction);
        _builderRow = new HBoxContainer { Visible = false }; _builderRow.AddThemeConstantOverride("separation", 8); _builderRow.AddChild(new Label { Text = "负责武将", CustomMinimumSize = new Vector2(78, 38), VerticalAlignment = VerticalAlignment.Center }); _builderChoice = new OptionButton { CustomMinimumSize = new Vector2(0, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill }; _builderRow.AddChild(_builderChoice); info.AddChild(_builderRow);
        _upgradeAction = GameTheme.Button("升级已建设施"); _upgradeAction.Pressed += () => MaintainSelected(true); info.AddChild(_upgradeAction);
        _repairAction = GameTheme.Button("修缮已建设施"); _repairAction.Pressed += () => MaintainSelected(false); info.AddChild(_repairAction);

        _cityUpgradeDialog = new ConfirmationDialog { Title = "确认升级城池", OkButtonText = "确认开工", CancelButtonText = "返回" }; _cityUpgradeDialog.Confirmed += ConfirmCityUpgrade; AddChild(_cityUpgradeDialog);
        _cancelCityUpgradeDialog = new ConfirmationDialog { Title = "确认取消城池升级", OkButtonText = "取消并返还资源", CancelButtonText = "继续施工" }; _cancelCityUpgradeDialog.Confirmed += ConfirmCancelCityUpgrade; AddChild(_cancelCityUpgradeDialog);
    }

    private void BuildGovernancePage()
    {
        var page = AddPage("governance");
        var cards = new HBoxContainer(); cards.SetAnchorsPreset(LayoutPreset.TopWide); cards.OffsetBottom = 270; cards.AddThemeConstantOverride("separation", 14); page.AddChild(cards);
        cards.AddChild(StaticCard("治理说明", "亲自治理时城务均由玩家下令；方针委任会在月末让太守按所选方针使用本城剩余城务额度。城市定位会在转型完成后提供对应倾向。金粮为全势力共用。", new Vector2(520, 0)));
        cards.AddChild(StaticCard("方针含义", "休养：民心、治安与恢复优先\n通商：商业、治安优先\n屯田：农业、赈济优先\n备战：城防、征募、训练优先\n新土：治安、赈济、城防优先", new Vector2(520, 0)));
        var policyPanel = new VBoxContainer(); policyPanel.SetAnchorsPreset(LayoutPreset.TopWide); policyPanel.OffsetTop = 294; policyPanel.AddThemeConstantOverride("separation", 10); page.AddChild(policyPanel);
        var row = FlowRow(10); row.AddChild(new Label { Text = "治理方式", CustomMinimumSize = new Vector2(82, 40), VerticalAlignment = VerticalAlignment.Center });
        _governanceMode = Choice(150, ("亲自治理", "manual"), ("方针委任", "delegated")); _policy = Choice(165, ("均衡治理", "balanced"), ("休养生息", "recovery"), ("富民通商", "commerce"), ("屯田积粮", "agriculture"), ("强军固防", "military"), ("巩固新土", "integration")); _role = Choice(135, ("未定", "unassigned"), ("粮仓", "granary"), ("商埠", "market"), ("军镇", "garrison"), ("学府", "academy"), ("枢纽", "hub"));
        var apply = GameTheme.Button("应用治理方针"); apply.CustomMinimumSize = new Vector2(155, 40); apply.Pressed += ApplyGovernance;
        foreach (var control in new Control[] { _governanceMode, _policy, _role, apply }) row.AddChild(control); policyPanel.AddChild(CommandPanel(row));
    }

    private void FillBuildingSlots(CityData city)
    {
        if (_buildingSlots is null) return;
        foreach (var child in _buildingSlots.GetChildren()) child.QueueFree();
        var slots = GameRuntime.MaxFacilitySlots;
        var facilitiesBySlot = GameRuntime.FacilitiesBySlot(city);
        for (var index = 0; index < slots; index += 2)
        {
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 10); _buildingSlots.AddChild(row);
            for (var column = 0; column < 2 && index + column < slots; column++)
            {
                var slotIndex = index + column;
                var locked = slotIndex >= city.FacilitySlots;
                var item = facilitiesBySlot.GetValueOrDefault(slotIndex);
                var construction = city.ConstructionQueue is { Kind: "build" } queue && queue.TargetSlotIndex == slotIndex ? queue : null;
                var slotKey = $"slot-{slotIndex}";
                var unlockLevel = (slotIndex - GameRuntime.MinFacilitySlots + 1) * 2;
                var label = locked
                    ? $"🔒 第 {slotIndex + 1} 个位置\n城池 Lv.{unlockLevel} 解锁"
                    : construction is not null
                    ? $"{GameRuntime.FacilityName(construction.DefinitionId)}\n建造中 · 余{construction.RemainingMonths}月"
                    : item is null
                        ? $"＋ 空地 {slotIndex + 1}\n点击选择建筑"
                        : $"{GameRuntime.FacilityName(item.DefinitionId)}  Lv.{item.Level}\n状况 {item.Condition}%";
                var button = GameTheme.Button(label); button.CustomMinimumSize = new Vector2(0, 88); button.SizeFlagsHorizontal = SizeFlags.ExpandFill; button.AddThemeFontSizeOverride("font_size", 17); button.Disabled = locked;
                if (construction is not null) button.AddThemeColorOverride("font_color", GameTheme.Gold);
                if (slotKey == _selectedSlot) button.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.Gold, .22f), GameTheme.Gold, 8, 2, 14, 8));
                if (!locked) { var selected = slotKey; button.Pressed += () => { _selectedSlot = selected; _awaitingBuilder = false; Refresh(); }; }
                row.AddChild(button);
            }
        }
        if (string.IsNullOrEmpty(_selectedSlot) || SelectedSlotIndex() >= city.FacilitySlots) _selectedSlot = "slot-0";
    }

    private void RefreshBuildingPanel(CityData? city)
    {
        if (city is null || _buildingTitle is null) return;
        var slotIndex = SelectedSlotIndex();
        var facility = GameRuntime.FacilitiesBySlot(city).GetValueOrDefault(slotIndex);
        var construction = city.ConstructionQueue is { Kind: "build" } queue && queue.TargetSlotIndex == slotIndex ? queue : null;
        var isEmpty = facility is null && construction is null;
        _facilityChoice.Visible = isEmpty;
        _buildAction.Visible = isEmpty;
        _builderRow.Visible = isEmpty && _awaitingBuilder;
        _upgradeAction.Visible = facility is not null && facility.Level < 3;
        _repairAction.Visible = facility is not null;
        if (construction is not null)
        {
            _buildingTitle.Text = $"{GameRuntime.FacilityName(construction.DefinitionId)} · 建造中";
            _buildingInfo.Text = $"{GameRuntime.FacilityEffect(construction.DefinitionId)}\n\n状态：建造中\n进度：{construction.TotalMonths - construction.RemainingMonths}/{construction.TotalMonths}个月\n剩余：{construction.RemainingMonths}个月\n\n工程完成后会在当前地块转为正式设施。";
        }
        else if (isEmpty)
        {
            var selectedId = Selected(_facilityChoice); if (string.IsNullOrEmpty(selectedId) && _facilityChoice.ItemCount > 0) { _facilityChoice.Select(0); selectedId = Selected(_facilityChoice); }
            var definition = GameRuntime.FacilityCatalog.GetValueOrDefault(selectedId);
            _buildingTitle.Text = "空置地块";
            _buildingInfo.Text = definition is null ? "请选择要建设的设施。" : $"拟建：{definition.Name}\n{GameRuntime.FacilityEffect(selectedId)}\n\n费用：金 {definition.Gold:N0}　粮 {definition.Food:N0}\n工期：{definition.Months}个月\n\n建造会占用本城 1 点城务，并从势力府库扣除资源。";
            _buildAction.Text = _awaitingBuilder ? "确认委任并开工" : "建造此设施";
        }
        else
        {
            var builtFacility = facility!;
            _buildingTitle.Text = $"{GameRuntime.FacilityName(builtFacility.DefinitionId)} · Lv.{builtFacility.Level}";
            _buildingInfo.Text = $"{GameRuntime.FacilityEffect(builtFacility.DefinitionId)}\n\n当前状况：{builtFacility.Condition}%\n" + (city.ConstructionQueue is null ? (builtFacility.Level >= 3 ? "设施已满级，可进行修缮。" : "可选择升级或修缮。") : $"当前工程：{GameRuntime.ConstructionLabel(city.ConstructionQueue)}，余{city.ConstructionQueue.RemainingMonths}月。");
        }
    }

    private void RefreshCityUpgradePanel(CityData city)
    {
        var queue = city.ConstructionQueue;
        var cityUpgrade = queue is { Kind: "city-upgrade" } ? queue : null;
        _cancelCityUpgradeAction.Visible = cityUpgrade is not null;
        if (cityUpgrade is not null)
        {
            var completed = cityUpgrade.TotalMonths - cityUpgrade.RemainingMonths;
            var builder = _runtime.Officer(cityUpgrade.OfficerId)?.Profile.Name ?? "未指定";
            var pauseReason = _runtime.CityUpgradePauseReason(city);
            _cityUpgradeInfo.Text = $"城池建设 · Lv.{city.CityLevel} → Lv.{cityUpgrade.TargetCityLevel}\n进度 {completed}/{cityUpgrade.TotalMonths}个月 · 剩余{cityUpgrade.RemainingMonths}个月 · 负责 {builder}" + (string.IsNullOrEmpty(pauseReason) ? "" : $"\n{pauseReason}");
            _cityUpgradeOfficer.Visible = !string.IsNullOrEmpty(pauseReason);
            _cityUpgradeAction.Visible = !string.IsNullOrEmpty(pauseReason);
            _cityUpgradeAction.Text = "更换负责武将";
            _cancelCityUpgradeAction.Text = $"取消并返还 金{cityUpgrade.GoldCost:N0}/粮{cityUpgrade.FoodCost:N0}";
            return;
        }
        _cityUpgradeOfficer.Visible = city.CityLevel < GameRuntime.MaxCityLevel;
        _cityUpgradeAction.Visible = city.CityLevel < GameRuntime.MaxCityLevel;
        _cancelCityUpgradeAction.Visible = false;
        _cityUpgradeAction.Text = "升级城池";
        if (city.CityLevel >= GameRuntime.MaxCityLevel)
        {
            _cityUpgradeInfo.Text = $"城池建设 · Lv.{city.CityLevel} 已达最高等级\n建造位置 {city.FacilitySlots}/{GameRuntime.MaxFacilitySlots}";
            return;
        }
        var definition = GameRuntime.CityUpgradeForTargetLevel(city.CityLevel + 1)!;
        var unlock = definition.UnlocksFacilitySlot ? $" · 解锁第{GameRuntime.FacilitySlotsForCityLevel(definition.TargetLevel)}个位置" : $" · 下个位置 Lv.{GameRuntime.NextFacilitySlotUnlockLevel(city.CityLevel)} 解锁";
        var queueBlock = queue is null ? "" : $"\n需等待当前工程：{GameRuntime.ConstructionLabel(queue)}";
        _cityUpgradeInfo.Text = $"城池建设 · Lv.{city.CityLevel} → Lv.{definition.TargetLevel}　位置 {city.FacilitySlots}/{GameRuntime.MaxFacilitySlots}{unlock}\n农业+1　商业+1　城防+2　文化+1 · 金{definition.Gold:N0} 粮{definition.Food:N0} · {definition.Months}个月{queueBlock}";
        _cityUpgradeAction.Disabled = queue is not null;
    }

    private void BeginCityUpgrade()
    {
        var city = _runtime.City(_cityId);
        if (city is null) return;
        if (city.ConstructionQueue is { Kind: "city-upgrade" })
        {
            if (_runtime.ReassignCityUpgrade(city.Id, Selected(_cityUpgradeOfficer))) Refresh();
            return;
        }
        var definition = GameRuntime.CityUpgradeForTargetLevel(city.CityLevel + 1);
        if (definition is null) return;
        var unlock = definition.UnlocksFacilitySlot ? $"\n解锁：第{GameRuntime.FacilitySlotsForCityLevel(definition.TargetLevel)}个建造位置" : "";
        _cityUpgradeDialog.DialogText = $"{city.Name}将升级至 Lv.{definition.TargetLevel}\n农业 +1　商业 +1　城防 +2　文化 +1{unlock}\n\n消耗：金 {definition.Gold:N0}　粮 {definition.Food:N0}　本城城务 1\n工期：{definition.Months}个月\n负责武将：{SelectedOfficerName(_cityUpgradeOfficer)}";
        _cityUpgradeDialog.PopupCentered(new Vector2I(560, 330));
    }

    private void ConfirmCityUpgrade()
    {
        if (_runtime.UpgradeCity(_cityId, Selected(_cityUpgradeOfficer))) Refresh();
    }

    private void BeginCancelCityUpgrade()
    {
        var city = _runtime.City(_cityId);
        if (city?.ConstructionQueue is not { Kind: "city-upgrade" } queue) return;
        _cancelCityUpgradeDialog.DialogText = $"确认取消{city.Name}升至 Lv.{queue.TargetCityLevel} 的工程？\n\n将全额返还：金 {queue.GoldCost:N0}　粮 {queue.FoodCost:N0}\n本月已消耗的城务额度不返还，施工进度将清零。";
        _cancelCityUpgradeDialog.PopupCentered(new Vector2I(540, 280));
    }

    private void ConfirmCancelCityUpgrade()
    {
        if (_runtime.CancelCityUpgrade(_cityId)) Refresh();
    }

    private void BeginOrConfirmBuild()
    {
        if (!_awaitingBuilder) { _awaitingBuilder = true; Refresh(); _notice.Text = "请选择负责建设的武将，再确认开工。"; return; }
        if (_runtime.BuildFacility(_cityId, Selected(_builderChoice), Selected(_facilityChoice), SelectedSlotIndex())) Refresh();
    }

    private void MaintainSelected(bool upgrade)
    {
        var city = _runtime.City(_cityId);
        var facility = city is null ? null : GameRuntime.FacilitiesBySlot(city).GetValueOrDefault(SelectedSlotIndex());
        if (facility is null) return;
        _runtime.MaintainFacility(_cityId, facility.Id, upgrade);
    }

    private int SelectedSlotIndex() => _selectedSlot.StartsWith("slot-") && int.TryParse(_selectedSlot[5..], out var index) ? index : 0;

    private void ApplyGovernance() => _runtime.ConfigureCityGovernance(_cityId, Selected(_governanceMode), Selected(_policy), Selected(_role));

    private void FillOfficers(CityData city)
    {
        var previous = _officer.Selected >= 0 ? Selected(_officer) : string.Empty;
        var previousUpgrade = _cityUpgradeOfficer.Selected >= 0 ? Selected(_cityUpgradeOfficer) : string.Empty;
        _officer.Clear(); _builderChoice.Clear(); _cityUpgradeOfficer.Clear();
        foreach (var officer in _runtime.PlayerOfficers().Where(item => item.InitialState.CityId == city.Id && item.InitialState.Status == "serving"))
        {
            var used = city.MonthlyOfficerActionIds.Count(id => id == officer.Profile.Id);
            var label = $"Lv.{officer.InitialState.Level} {officer.Profile.Name} · 政{_runtime.EffectiveAbility(officer, "politics", "civil")} 智{_runtime.EffectiveAbility(officer, "intelligence", "civil")}{(used > 0 ? $" · 已行动{used}" : "")}";
            AddChoice(_officer, label, officer.Profile.Id); AddChoice(_builderChoice, label, officer.Profile.Id); AddChoice(_cityUpgradeOfficer, label, officer.Profile.Id);
        }
        SelectByMetadata(_officer, previous); SelectByMetadata(_builderChoice, previous); SelectByMetadata(_cityUpgradeOfficer, previousUpgrade); RefreshCommandPreviews();
    }

    private void RefreshCommandButtons()
    {
        if (_commandGroup is null) return;
        var group = Selected(_commandGroup); foreach (var entry in _commandButtons) foreach (var button in entry.Value) button.Visible = entry.Key == group; RefreshCommandPreviews();
    }

    private void RefreshCommandPreviews()
    {
        if (_officer is null || _notice is null || string.IsNullOrEmpty(_cityId)) return;
        foreach (var entry in _commandFocuses) entry.Key.TooltipText = _runtime.CityCommandPreview(_cityId, Selected(_officer), entry.Value);
        var visible = _commandFocuses.Keys.FirstOrDefault(button => button.Visible); if (visible is not null && _page == "overview") _notice.Text = visible.TooltipText.Replace('\n', '　');
    }

    private void Back() => EmitSignal(SignalName.BackRequested);
    private void StepCity(int direction) { var cities = PlayerCities().ToList(); if (cities.Count == 0) return; var index = cities.FindIndex(city => city.Id == _cityId); ShowCity(cities[(index + direction + cities.Count) % cities.Count]); }
    private IEnumerable<CityData> PlayerCities() => _runtime.State.Cities.Where(item => item.OwnerFactionId == _runtime.State.PlayerFactionId);
    private static int CitySortScore(CityData city) => city.Status switch { "shortage" => 0, "unrest" => 1, "integrating" => 2, "frontline" => 3, _ => 10 };
    private static string Selected(OptionButton option) => option.Selected < 0 ? string.Empty : option.GetItemMetadata(option.Selected).AsString();
    private string SelectedOfficerName(OptionButton option) => _runtime.Officer(Selected(option))?.Profile.Name ?? "未选择";
    private static void AddChoice(OptionButton option, string label, string value) { option.AddItem(label); option.SetItemMetadata(option.ItemCount - 1, value); }
    private static OptionButton Choice(float width, params (string Label, string Value)[] entries) { var option = new OptionButton { CustomMinimumSize = new Vector2(width, 38), MouseDefaultCursorShape = CursorShape.PointingHand }; foreach (var entry in entries) AddChoice(option, entry.Label, entry.Value); return option; }
    private static HFlowContainer FlowRow(int separation) { var row = new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; row.AddThemeConstantOverride("h_separation", separation); row.AddThemeConstantOverride("v_separation", 7); return row; }
    private static void SelectByMetadata(OptionButton option, string value) { for (var index = 0; index < option.ItemCount; index++) if (option.GetItemMetadata(index).AsString() == value) { option.Select(index); return; } if (option.ItemCount > 0) option.Select(0); }
    private static PanelContainer CommandPanel(Control child) { var panel = new PanelContainer(); panel.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.Panel, .74f), new Color(GameTheme.GoldDim, .55f), 6, 1)); var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 10); margin.AddThemeConstantOverride("margin_right", 10); margin.AddThemeConstantOverride("margin_top", 5); margin.AddThemeConstantOverride("margin_bottom", 5); margin.AddChild(child); panel.AddChild(margin); return panel; }
    private static PanelContainer Card(string heading, out Label body, Vector2 minimum) { var card = new PanelContainer { CustomMinimumSize = minimum, SizeFlagsHorizontal = SizeFlags.ExpandFill }; card.AddThemeStyleboxOverride("panel", GameTheme.PanelBox(10)); UiOrnaments.AttachInkCorners(card, 210, .07f); var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 12); var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 18); margin.AddThemeConstantOverride("margin_right", 18); margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14); margin.AddChild(layout); card.AddChild(margin); var title = new Label { Text = heading, CustomMinimumSize = new Vector2(0, 38) }; title.AddThemeFontSizeOverride("font_size", 20); title.AddThemeColorOverride("font_color", GameTheme.Gold); layout.AddChild(title); body = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeFontSizeOverride("font_size", 16); body.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(body); return card; }
    private static PanelContainer ScrollableCard(string heading, out Label body, Vector2 minimum)
    {
        var card = new PanelContainer { CustomMinimumSize = minimum, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", GameTheme.PanelBox(10));
        UiOrnaments.AttachInkCorners(card, 210, .07f);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18); margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14);
        card.AddChild(margin);
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 12); margin.AddChild(layout);
        var title = new Label { Text = heading, CustomMinimumSize = new Vector2(0, 38) };
        title.AddThemeFontSizeOverride("font_size", 20); title.AddThemeColorOverride("font_color", GameTheme.Gold); layout.AddChild(title);
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        layout.AddChild(scroll);
        body = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        body.AddThemeFontSizeOverride("font_size", 16); body.AddThemeColorOverride("font_color", GameTheme.Muted);
        scroll.AddChild(body);
        return card;
    }
    private static PanelContainer StaticCard(string heading, string text, Vector2 minimum) { var card = Card(heading, out var body, minimum); body.Text = text; return card; }
}
