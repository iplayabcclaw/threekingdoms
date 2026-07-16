using Godot;

namespace ThreeKingdomsSimulator.Godot;

public abstract partial class FeatureScreen : Control
{
    protected GameRuntime Runtime = null!;
    protected VBoxContainer Body = null!;
    protected Label Notice = null!;
    protected ScrollContainer ContentScroll = null!;

    public void Initialize(GameRuntime runtime, string title, string subtitle)
    {
        Runtime = runtime;
        var background = new ColorRect { Color = GameTheme.Backdrop }; background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(background);
        var header = new PanelContainer { MouseFilter = MouseFilterEnum.Stop }; header.SetAnchorsPreset(LayoutPreset.TopWide); header.OffsetBottom = 86; header.AddThemeStyleboxOverride("panel", GameTheme.HeaderBox()); AddChild(header);
        UiOrnaments.AttachInkCorners(header, 185, .055f);
        var headerMargin = new MarginContainer(); headerMargin.AddThemeConstantOverride("margin_left", 26); headerMargin.AddThemeConstantOverride("margin_right", 26); headerMargin.AddThemeConstantOverride("margin_top", 10); headerMargin.AddThemeConstantOverride("margin_bottom", 10); header.AddChild(headerMargin);
        var headerRow = new HBoxContainer(); headerRow.AddThemeConstantOverride("separation", 14); headerMargin.AddChild(headerRow);
        var accent = new ColorRect { Color = GameTheme.Gold, CustomMinimumSize = new Vector2(3, 0), MouseFilter = MouseFilterEnum.Ignore }; headerRow.AddChild(accent);
        var titles = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; titles.AddThemeConstantOverride("separation", 0); headerRow.AddChild(titles);
        var heading = new Label { Text = title, CustomMinimumSize = new Vector2(0, 40), VerticalAlignment = VerticalAlignment.Center }; heading.AddThemeFontSizeOverride("font_size", 29); heading.AddThemeColorOverride("font_color", GameTheme.Paper); titles.AddChild(heading);
        var sub = new Label { Text = subtitle, CustomMinimumSize = new Vector2(0, 25), VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart }; sub.AddThemeColorOverride("font_color", GameTheme.Muted); titles.AddChild(sub);
        var seal = new Label { Text = "山河逐鹿", CustomMinimumSize = new Vector2(112, 42), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; seal.AddThemeFontSizeOverride("font_size", 15); seal.AddThemeColorOverride("font_color", GameTheme.Gold); seal.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.Cinnabar, .16f), new Color(GameTheme.Cinnabar, .8f), 4, 1, 10, 4)); headerRow.AddChild(seal);
        ContentScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; ContentScroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); ContentScroll.OffsetLeft = 24; ContentScroll.OffsetTop = 102; ContentScroll.OffsetRight = -24; ContentScroll.OffsetBottom = -74; AddChild(ContentScroll);
        Body = new VBoxContainer { CustomMinimumSize = new Vector2(0, 650), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; Body.AddThemeConstantOverride("separation", 16); ContentScroll.AddChild(Body);
        Notice = new Label { CustomMinimumSize = new Vector2(0, 42), AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center }; Notice.AddThemeColorOverride("font_color", GameTheme.Gold); Notice.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.Bronze, .07f), new Color(GameTheme.GoldDim, .4f), 5, 1, 14, 5)); Body.AddChild(Notice);
        runtime.Changed += Refresh;
        runtime.Notice += message => { if (Notice is not null) Notice.Text = message; };
        Build(); Refresh();
    }

    protected abstract void Build();
    public abstract void Refresh();

    protected static HFlowContainer Row() { var row = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddThemeConstantOverride("h_separation", 12); row.AddThemeConstantOverride("v_separation", 10); return row; }
    protected static Label Text(string value, float width = 180) => new() { Text = value, CustomMinimumSize = new Vector2(width, 40), VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    protected static OptionButton Choice(float width = 260) => new() { CustomMinimumSize = new Vector2(width, 42), MouseDefaultCursorShape = CursorShape.PointingHand };
    protected static SpinBox Number(string suffix, double min, double max, double step, double value) => new() { Suffix = suffix, MinValue = min, MaxValue = max, Step = step, Value = value, CustomMinimumSize = new Vector2(150, 40) };
    protected static string Selected(OptionButton option) => option.Selected < 0 ? string.Empty : option.GetItemMetadata(option.Selected).AsString();
    protected static void AddChoice(OptionButton option, string label, string value) { option.AddItem(label); option.SetItemMetadata(option.ItemCount - 1, value); }
    protected static PanelContainer Panel(Control child)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", GameTheme.PanelBox());
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 13);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 13);
        margin.AddChild(child);
        panel.AddChild(margin);
        return panel;
    }
}

public partial class TalentView : FeatureScreen
{
    private OptionButton _candidate = null!, _actor = null!, _appointment = null!, _marchOfficer = null!, _marchTargetCity = null!, _promotionOfficer = null!, _officeTrack = null!;
    private Label _overviewSummary = null!, _progression = null!, _marchSource = null!, _marchPreview = null!, _recruitmentMethod = null!, _recruitmentPreview = null!, _recruitmentSummary = null!;
    private Button _recruitButton = null!, _marchButton = null!;
    private VBoxContainer _overviewTable = null!, _marchTable = null!, _officeTree = null!;
    private GridContainer _portraitGrid = null!;
    private readonly Dictionary<string, VBoxContainer> _talentPages = [];
    private readonly Dictionary<string, Button> _talentTabs = [];
    private readonly Dictionary<string, string> _courtDraftSelections = [];
    private string _activeTalentTab = "overview";

    protected override void Build()
    {
        BuildTalentTabs();
        BuildOverviewPage(CreateTalentPage("overview"));
        BuildRecruitmentPage(CreateTalentPage("recruitment"));
        BuildMarchPage(CreateTalentPage("march"));
        BuildOfficePage(CreateTalentPage("office"));
        ShowTalentTab("overview");
    }

    private void BuildTalentTabs()
    {
        var tabs = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; tabs.AddThemeConstantOverride("separation", 10);
        foreach (var item in new[] { ("overview", "总览"), ("recruitment", "招募"), ("march", "调动"), ("office", "官职") })
        {
            var button = GameTheme.Button(item.Item2); button.CustomMinimumSize = new Vector2(150, 44); button.Pressed += () => ShowTalentTab(item.Item1); tabs.AddChild(button); _talentTabs[item.Item1] = button;
        }
        Body.AddChild(tabs);
    }

    private VBoxContainer CreateTalentPage(string id)
    {
        var page = new VBoxContainer { CustomMinimumSize = new Vector2(0, 600), SizeFlagsHorizontal = SizeFlags.ExpandFill }; page.AddThemeConstantOverride("separation", 14); Body.AddChild(page); _talentPages[id] = page; return page;
    }

    private void BuildOverviewPage(VBoxContainer page)
    {
        _overviewSummary = Text("", 1200); _overviewSummary.AddThemeColorOverride("font_color", GameTheme.Gold); page.AddChild(Panel(_overviewSummary));
        _overviewTable = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _overviewTable.AddThemeConstantOverride("separation", 3); page.AddChild(_overviewTable);
    }

    private void BuildRecruitmentPage(VBoxContainer page)
    {
        var recruit = Row(); recruit.AddChild(Text("人才招募", 100)); _candidate = Choice(300); _actor = Choice(280); _recruitmentMethod = Text("", 180); _appointment = Choice(150);
        foreach (var item in new[] { ("strategist", "军师"), ("civil", "文官"), ("general", "武将"), ("reserve", "待命") }) AddChoice(_appointment, item.Item2, item.Item1);
        recruit.AddChild(_candidate); recruit.AddChild(_actor); recruit.AddChild(_recruitmentMethod); recruit.AddChild(_appointment);
        _recruitButton = GameTheme.Button("执行招募"); _recruitButton.Pressed += () => Runtime.RecruitOfficer(Selected(_candidate), Selected(_actor), Selected(_appointment)); recruit.AddChild(_recruitButton); page.AddChild(Panel(recruit));
        _candidate.ItemSelected += _ => RefreshRecruitmentPreview(); _actor.ItemSelected += _ => RefreshRecruitmentPreview();
        _recruitmentPreview = Text("", 1200); _recruitmentPreview.CustomMinimumSize = new Vector2(0, 58); _recruitmentPreview.AddThemeColorOverride("font_color", GameTheme.Gold); page.AddChild(Panel(_recruitmentPreview));
        _recruitmentSummary = Text("", 1200); _recruitmentSummary.AddThemeColorOverride("font_color", GameTheme.Muted); page.AddChild(Panel(_recruitmentSummary));
        var heading = new Label { Text = "已发现的在野武将", CustomMinimumSize = new Vector2(0, 38), VerticalAlignment = VerticalAlignment.Center }; heading.AddThemeFontSizeOverride("font_size", 21); heading.AddThemeColorOverride("font_color", GameTheme.Paper); page.AddChild(heading);
        _portraitGrid = new GridContainer { Columns = 7, CustomMinimumSize = new Vector2(0, 350) }; _portraitGrid.AddThemeConstantOverride("h_separation", 10); _portraitGrid.AddThemeConstantOverride("v_separation", 12); page.AddChild(_portraitGrid);
    }

    private void BuildMarchPage(VBoxContainer page)
    {
        var march = Row(); march.AddChild(Text("武将调动", 100)); _marchOfficer = Choice(220); march.AddChild(_marchOfficer); march.AddChild(Text("从", 30)); _marchSource = Text("—", 100); march.AddChild(_marchSource); march.AddChild(Text("至", 30)); _marchTargetCity = Choice(230); march.AddChild(_marchTargetCity);
        _marchButton = GameTheme.Button("确认调动"); _marchButton.Pressed += () => Runtime.TransferOfficer(Selected(_marchOfficer), Selected(_marchTargetCity)); march.AddChild(_marchButton); page.AddChild(Panel(march));
        _marchOfficer.ItemSelected += _ => RefreshMarchRoute(); _marchTargetCity.ItemSelected += _ => RefreshMarchPreview();
        _marchPreview = Text("", 1200); _marchPreview.CustomMinimumSize = new Vector2(0, 58); _marchPreview.AddThemeColorOverride("font_color", GameTheme.Muted); page.AddChild(Panel(_marchPreview));
        _marchTable = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _marchTable.AddThemeConstantOverride("separation", 3); page.AddChild(_marchTable);
    }

    private void BuildOfficePage(VBoxContainer page)
    {
        var office = Row(); office.AddChild(Text("官阶与俸禄", 110)); _promotionOfficer = Choice(300); _officeTrack = Choice(140); AddChoice(_officeTrack, "文职序列", "civil"); AddChoice(_officeTrack, "武职序列", "military"); office.AddChild(_promotionOfficer); office.AddChild(_officeTrack);
        var promote = GameTheme.Button("晋升/转序"); promote.Pressed += () => Runtime.PromoteOfficer(Selected(_promotionOfficer), Selected(_officeTrack)); office.AddChild(promote);
        var demote = GameTheme.Button("降职"); demote.Pressed += () => Runtime.DemoteOfficer(Selected(_promotionOfficer)); office.AddChild(demote);
        var repay = GameTheme.Button("补发欠俸"); repay.Pressed += () => Runtime.PaySalaryArrears(Selected(_promotionOfficer)); office.AddChild(repay); page.AddChild(Panel(office));
        var progressionBox = new VBoxContainer(); progressionBox.AddThemeConstantOverride("separation", 8);
        var progressionTitle = new Label { Text = "成长与官职", CustomMinimumSize = new Vector2(0, 30), VerticalAlignment = VerticalAlignment.Center }; progressionTitle.AddThemeFontSizeOverride("font_size", 19); progressionTitle.AddThemeColorOverride("font_color", GameTheme.Paper); progressionBox.AddChild(progressionTitle);
        _progression = new Label { CustomMinimumSize = new Vector2(0, 112), AutowrapMode = TextServer.AutowrapMode.WordSmart }; _progression.AddThemeColorOverride("font_color", GameTheme.Muted); _progression.AddThemeConstantOverride("line_spacing", 4); progressionBox.AddChild(_progression); page.AddChild(Panel(progressionBox));
        _promotionOfficer.ItemSelected += _ => RefreshProgression();
        var courtTitle = new Label { Text = "朝堂官职树", CustomMinimumSize = new Vector2(0, 44), VerticalAlignment = VerticalAlignment.Center }; courtTitle.AddThemeFontSizeOverride("font_size", 22); courtTitle.AddThemeColorOverride("font_color", GameTheme.Paper); page.AddChild(courtTitle);
        var courtHelp = Text("主公统领三系主官，每系下辖三席。职位决定势力加成方向，任职者能力决定基础强度，个人与名将特性会转化为额外势力光环。", 1200); courtHelp.AddThemeColorOverride("font_color", GameTheme.Muted); page.AddChild(courtHelp);
        _officeTree = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _officeTree.AddThemeConstantOverride("separation", 8); page.AddChild(_officeTree);
        page.MoveChild(courtTitle, 0); page.MoveChild(courtHelp, 1); page.MoveChild(_officeTree, 2);
    }
    public override void Refresh()
    {
        if (_candidate is null) return;
        FillOfficerChoice(_actor, Runtime.PlayerOfficers().Where(item => item.InitialState.Status == "serving"), item => $"{item.Profile.Name} · 魅{Runtime.EffectiveAbility(item, "charisma", "civil")} 智{Runtime.EffectiveAbility(item, "intelligence", "civil")}");
        FillOfficerChoice(_marchOfficer, Runtime.PlayerOfficers().Where(item => item.InitialState.Status == "serving" && string.IsNullOrEmpty(item.InitialState.ArmyId) && item.InitialState.Appointment != "ruler" && Runtime.City(item.InitialState.CityId)?.OwnerFactionId == Runtime.State.PlayerFactionId), item => item.Profile.Name);
        FillOfficerChoice(_promotionOfficer, Runtime.PlayerOfficers().Where(item => item.InitialState.Appointment != "ruler"), item => $"等级{item.InitialState.Level} {item.Profile.Name} · {OfficerProgressionRules.OfficeName(item.InitialState.OfficeTrack, item.InitialState.OfficeRank)} · 俸{OfficerProgressionRules.Salary(item)}");
        var candidateList = RecruitmentCandidates().ToList();
        FillOfficerChoice(_candidate, candidateList, item => $"{item.Profile.Name} · {OfficerStatusLabel(item.InitialState.Status)} · {Runtime.Faction(item.InitialState.FactionId)?.ShortName ?? Runtime.City(item.InitialState.CityId)?.Name ?? "在野"} · 忠{item.InitialState.Loyalty}");
        RefreshRecruitmentPreview();
        RefreshMarchRoute();
        RefreshProgression();
        RefreshOverview();
        RebuildCandidateGrid(candidateList);
        RefreshMarchTable();
        RebuildOfficeTree();
    }

    private void ShowTalentTab(string id)
    {
        if (!_talentPages.ContainsKey(id)) return;
        _activeTalentTab = id;
        foreach (var item in _talentPages) item.Value.Visible = item.Key == id;
        foreach (var item in _talentTabs)
        {
            item.Value.AddThemeStyleboxOverride("normal", item.Key == id ? GameTheme.ButtonBox("focus") : GameTheme.ButtonBox("normal"));
            item.Value.AddThemeColorOverride("font_color", item.Key == id ? GameTheme.Gold : GameTheme.Paper);
        }
        ContentScroll.ScrollVertical = 0;
    }

    public void ShowTabForVisualTest(string id) => ShowTalentTab(id);

    private void RefreshOverview()
    {
        ClearChildren(_overviewTable);
        var officers = Runtime.PlayerOfficers().OrderBy(item => item.InitialState.Appointment == "ruler" ? 0 : 1).ThenBy(item => item.InitialState.CityId).ThenByDescending(item => item.InitialState.OfficeRank).ToList();
        var courtCount = officers.Count(item => !string.IsNullOrEmpty(item.InitialState.CourtOfficeId));
        _overviewSummary.Text = $"本势力武将 {officers.Count} 人　·　在任 {officers.Count(item => item.InitialState.Status == "serving")}　·　调动中 {officers.Count(item => item.InitialState.Status == "marching")}　·　出征 {officers.Count(item => item.InitialState.Status == "deployed")}　·　朝堂任职 {courtCount}/12　·　已发 {Runtime.State.MonthlySalaryPaid} / 应发 {Runtime.State.MonthlySalaryDue}";
        var widths = new float[] { 100, 130, 80, 90, 175, 100, 230, 70, 90 };
        _overviewTable.AddChild(TableRow(["武将", "所在 / 行程", "状态", "职责", "官阶 / 朝堂", "等级 / 功勋", "五维", "忠诚", "月俸"], widths, true, 0));
        for (var index = 0; index < officers.Count; index++)
        {
            var officer = officers[index];
            var court = officer.InitialState.Appointment == "ruler" ? "主公" : OfficerProgressionRules.CourtOfficeName(officer.InitialState.CourtOfficeId);
            var office = officer.InitialState.Appointment == "ruler" ? court : $"{OfficerProgressionRules.OfficeName(officer.InitialState.OfficeTrack, officer.InitialState.OfficeRank)} / {court}";
            var stats = $"统{Runtime.PermanentAbility(officer, "leadership")} 武{Runtime.PermanentAbility(officer, "might")} 智{Runtime.PermanentAbility(officer, "intelligence")} 政{Runtime.PermanentAbility(officer, "politics")} 魅{Runtime.PermanentAbility(officer, "charisma")}";
            var location = officer.InitialState.Status == "marching" ? $"{Runtime.City(officer.InitialState.CityId)?.Name}→{Runtime.City(officer.InitialState.TravelTargetCityId)?.Name}" : Runtime.City(officer.InitialState.CityId)?.Name ?? "—";
            _overviewTable.AddChild(TableRow([officer.Profile.Name, location, OfficerStatusLabel(officer.InitialState.Status), GameRuntime.AppointmentLabel(officer.InitialState.Appointment), office, $"{officer.InitialState.Level} / {officer.InitialState.Merit}", stats, officer.InitialState.Loyalty.ToString(), OfficerProgressionRules.Salary(officer).ToString()], widths, false, index));
        }
    }

    private void RebuildCandidateGrid(List<ScenarioOfficerData> candidates)
    {
        ClearChildren(_portraitGrid);
        var free = candidates.Where(item => item.InitialState.Status == "free").OrderByDescending(item => Runtime.PermanentAbility(item, "charisma")).ThenByDescending(item => Runtime.PermanentAbility(item, "intelligence")).ToList();
        var captiveCount = candidates.Count(item => item.InitialState.Status == "captive");
        var subversionCount = candidates.Count(item => Runtime.RecruitmentMethod(item.Profile.Id) == "subversion");
        _recruitmentSummary.Text = $"已发现的在野武将 {free.Count} 人　·　可劝降俘虏 {captiveCount} 人　·　可策反敌将 {subversionCount} 人（忠诚<{GameRuntime.SubversionLoyaltyLimit}）。选择目标或执行者即可查看预计成功率；策反消耗1000金。";
        _portraitGrid.CustomMinimumSize = new Vector2(0, 350);
        if (free.Count == 0)
        {
            var empty = Text("当前没有已发现的在野武将，请先在城市人才命令中搜索。", 900); empty.AddThemeColorOverride("font_color", GameTheme.Muted); _portraitGrid.AddChild(empty); return;
        }
        foreach (var officer in free) _portraitGrid.AddChild(CandidateCard(officer));
    }

    private Control CandidateCard(ScenarioOfficerData officer)
    {
        var faction = Runtime.Faction(officer.InitialState.FactionId);
        var border = faction is null ? GameTheme.Bronze : Color.FromHtml(faction.Color);
        var card = new PanelContainer { CustomMinimumSize = new Vector2(180, 338) }; card.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.PanelRaised, .98f), new Color(border, .68f), 8, 2));
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 5); card.AddChild(layout);
        var portrait = new TextureRect { Texture = GD.Load<Texture2D>(AssetPaths.OfficerPortrait(officer.Profile.Id)), CustomMinimumSize = new Vector2(168, 228), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; layout.AddChild(portrait);
        var name = new Label { Text = $"等级{officer.InitialState.Level} · {officer.Profile.Name} · {officer.Profile.CourtesyName}", HorizontalAlignment = HorizontalAlignment.Center }; name.AddThemeFontSizeOverride("font_size", 17); name.AddThemeColorOverride("font_color", GameTheme.Paper); layout.AddChild(name);
        var stats = new Label { Text = $"统{Runtime.PermanentAbility(officer, "leadership")} 武{Runtime.PermanentAbility(officer, "might")} 智{Runtime.PermanentAbility(officer, "intelligence")}\n政{Runtime.PermanentAbility(officer, "politics")} 魅{Runtime.PermanentAbility(officer, "charisma")} · {OfficerProgressionRules.OfficeName(officer.InitialState.OfficeTrack, officer.InitialState.OfficeRank)}", HorizontalAlignment = HorizontalAlignment.Center }; stats.AddThemeFontSizeOverride("font_size", 11); stats.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(stats);
        var select = GameTheme.Button("选为招募目标"); select.CustomMinimumSize = new Vector2(0, 32); select.Pressed += () => { SelectByMetadata(_candidate, officer.Profile.Id); RefreshRecruitmentPreview(); }; layout.AddChild(select);
        return card;
    }

    private void RefreshMarchTable()
    {
        ClearChildren(_marchTable);
        var widths = new float[] { 120, 220, 150, 180, 110, 150, 80 };
        _marchTable.AddChild(TableRow(["武将", "所在 / 路线", "职责", "官阶", "状态", "行程", "疲劳"], widths, true, 0));
        var officers = Runtime.PlayerOfficers().Where(item => item.InitialState.Appointment != "ruler").OrderBy(item => item.InitialState.CityId).ThenBy(item => item.Profile.Name).ToList();
        for (var index = 0; index < officers.Count; index++)
        {
            var officer = officers[index];
            var route = officer.InitialState.Status == "marching" ? $"{Runtime.City(officer.InitialState.CityId)?.Name} → {Runtime.City(officer.InitialState.TravelTargetCityId)?.Name}" : Runtime.City(officer.InitialState.CityId)?.Name ?? "—";
            var progress = officer.InitialState.Status == "marching" ? $"已行{officer.InitialState.TravelTotalDays - officer.InitialState.TravelRemainingDays} / 共{officer.InitialState.TravelTotalDays}日" : "—";
            _marchTable.AddChild(TableRow([officer.Profile.Name, route, GameRuntime.AppointmentLabel(officer.InitialState.Appointment), OfficerProgressionRules.OfficeName(officer.InitialState.OfficeTrack, officer.InitialState.OfficeRank), OfficerStatusLabel(officer.InitialState.Status), progress, officer.InitialState.Fatigue.ToString()], widths, false, index));
        }
    }

    private void RebuildOfficeTree()
    {
        var appointedIds = Runtime.PlayerOfficers().Where(item => !string.IsNullOrEmpty(item.InitialState.CourtOfficeId)).Select(item => item.Profile.Id).ToHashSet();
        foreach (var draft in _courtDraftSelections.ToList())
        {
            var office = OfficerProgressionRules.CourtOffice(draft.Key);
            var candidate = Runtime.Officer(draft.Value);
            if (appointedIds.Contains(draft.Value) || office is null || candidate is null || candidate.InitialState.Status != "serving" || candidate.InitialState.OfficeTrack != office.Track || candidate.InitialState.OfficeRank < office.MinimumRank) _courtDraftSelections.Remove(draft.Key);
        }
        ClearChildren(_officeTree);
        var ruler = Runtime.PlayerOfficers().FirstOrDefault(item => item.InitialState.Appointment == "ruler");
        var rulerCenter = new CenterContainer { CustomMinimumSize = new Vector2(0, 105), SizeFlagsHorizontal = SizeFlags.ExpandFill }; rulerCenter.AddChild(RulerCard(ruler)); _officeTree.AddChild(rulerCenter);
        var stem = new Label { Text = "│", HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(0, 28) }; stem.AddThemeColorOverride("font_color", GameTheme.Gold); _officeTree.AddChild(stem);
        var branches = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; branches.AddThemeConstantOverride("separation", 14); _officeTree.AddChild(branches);
        foreach (var mainOffice in OfficerProgressionRules.CourtOffices.Where(item => item.Tier == 1))
        {
            var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1, CustomMinimumSize = new Vector2(400, 0) }; column.AddThemeConstantOverride("separation", 8); column.AddChild(CourtOfficeCard(mainOffice));
            foreach (var subordinate in OfficerProgressionRules.CourtOffices.Where(item => item.ParentId == mainOffice.Id)) column.AddChild(CourtOfficeCard(subordinate));
            branches.AddChild(column);
        }
    }

    private Control RulerCard(ScenarioOfficerData? ruler)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(380, 100) }; panel.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.Cinnabar, .24f), GameTheme.Gold, 9, 2, 16, 10));
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 4); panel.AddChild(layout);
        var title = new Label { Text = "主公", HorizontalAlignment = HorizontalAlignment.Center }; title.AddThemeFontSizeOverride("font_size", 22); title.AddThemeColorOverride("font_color", GameTheme.Gold); layout.AddChild(title);
        var name = new Label { Text = ruler is null ? "暂无君主" : $"{ruler.Profile.Name} · {Runtime.Faction(Runtime.State.PlayerFactionId)?.Name}", HorizontalAlignment = HorizontalAlignment.Center }; name.AddThemeColorOverride("font_color", GameTheme.Paper); layout.AddChild(name);
        var effect = new Label { Text = "统领百官与三军 · 不领俸禄", HorizontalAlignment = HorizontalAlignment.Center }; effect.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(effect); return panel;
    }

    private Control CourtOfficeCard(CourtOfficeDefinition office)
    {
        var holder = Runtime.PlayerOfficers().FirstOrDefault(item => item.InitialState.CourtOfficeId == office.Id);
        var previewHolder = holder;
        if (previewHolder is null && _courtDraftSelections.TryGetValue(office.Id, out var draftId)) previewHolder = Runtime.Officer(draftId);
        var border = office.Tier == 1 ? GameTheme.Gold : GameTheme.Bronze;
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(400, office.Tier == 1 ? 190 : 178) }; panel.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.PanelRaised, .96f), border, 8, office.Tier == 1 ? 2 : 1, 12, 9));
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 4); panel.AddChild(layout);
        var titleRow = new HBoxContainer(); layout.AddChild(titleRow);
        var title = new Label { Text = office.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill }; title.AddThemeFontSizeOverride("font_size", office.Tier == 1 ? 19 : 17); title.AddThemeColorOverride("font_color", office.Tier == 1 ? GameTheme.Gold : GameTheme.Paper); titleRow.AddChild(title);
        var salary = new Label { Text = $"津贴 +{office.SalaryAllowance}金/月" }; salary.AddThemeColorOverride("font_color", GameTheme.Gold); titleRow.AddChild(salary);
        var current = new Label { Text = holder is not null ? $"现任：{holder.Profile.Name} · 总俸{OfficerProgressionRules.Salary(holder)}金" : previewHolder is not null ? $"任命预览：{previewHolder.Profile.Name} · 任后总俸{OfficerProgressionRules.Salary(previewHolder) + office.SalaryAllowance}金" : "当前空缺" }; current.AddThemeColorOverride("font_color", previewHolder is null ? GameTheme.Muted : GameTheme.Paper); layout.AddChild(current);
        var influence = previewHolder is null ? office.Effect : OfficerProgressionRules.CourtInfluenceSummary(previewHolder, office);
        var effect = new Label { Text = $"{influence}\n门槛：{OfficerProgressionRules.OfficeName(office.Track, office.MinimumRank)}", AutowrapMode = TextServer.AutowrapMode.WordSmart }; effect.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(effect);
        var controls = new HBoxContainer(); controls.AddThemeConstantOverride("separation", 8); layout.AddChild(controls);
        if (holder is null)
        {
            var choice = Choice(240);
            AddChoice(choice, "请选择武将", "");
            var reservedByOtherOffices = _courtDraftSelections.Where(item => item.Key != office.Id && !string.IsNullOrEmpty(item.Value)).Select(item => item.Value).ToHashSet();
            foreach (var officer in Runtime.PlayerOfficers().Where(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && string.IsNullOrEmpty(item.InitialState.CourtOfficeId) && !reservedByOtherOffices.Contains(item.Profile.Id) && item.InitialState.OfficeTrack == office.Track && item.InitialState.OfficeRank >= office.MinimumRank).OrderByDescending(item => item.InitialState.OfficeRank).ThenByDescending(item => item.InitialState.Merit))
            {
                var traits = OfficerProgressionRules.AllTraits(officer).Take(2).ToList();
                AddChoice(choice, $"{officer.Profile.Name} · {OfficerProgressionRules.OfficeName(officer.InitialState.OfficeTrack, officer.InitialState.OfficeRank)}{(traits.Count == 0 ? "" : $" · {string.Join('、', traits)}")}", officer.Profile.Id);
            }
            if (_courtDraftSelections.TryGetValue(office.Id, out var draftOfficerId)) SelectByMetadata(choice, draftOfficerId);
            choice.ItemSelected += _ =>
            {
                var selectedOfficerId = Selected(choice);
                if (string.IsNullOrEmpty(selectedOfficerId)) _courtDraftSelections.Remove(office.Id);
                else
                {
                    foreach (var otherOfficeId in _courtDraftSelections.Where(item => item.Key != office.Id && item.Value == selectedOfficerId).Select(item => item.Key).ToList()) _courtDraftSelections.Remove(otherOfficeId);
                    _courtDraftSelections[office.Id] = selectedOfficerId;
                }
                RebuildOfficeTree();
            };
            controls.AddChild(choice);
            var appoint = GameTheme.Button("任命"); appoint.CustomMinimumSize = new Vector2(82, 38); appoint.Disabled = string.IsNullOrEmpty(Selected(choice)); appoint.Pressed += () =>
            {
                var selectedOfficerId = Selected(choice);
                if (string.IsNullOrEmpty(selectedOfficerId)) return;
                _courtDraftSelections.Remove(office.Id);
                if (!Runtime.AppointCourtOffice(selectedOfficerId, office.Id)) { _courtDraftSelections[office.Id] = selectedOfficerId; RebuildOfficeTree(); }
            }; controls.AddChild(appoint);
        }
        else
        {
            var locked = new Label { Text = "职位已占用，需先卸任才能重新任命", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center }; locked.AddThemeColorOverride("font_color", GameTheme.Muted); controls.AddChild(locked);
            var vacate = GameTheme.Button("卸任"); vacate.CustomMinimumSize = new Vector2(82, 38); vacate.Pressed += () => Runtime.VacateCourtOffice(holder.Profile.Id); controls.AddChild(vacate);
        }
        return panel;
    }

    private static Control TableRow(string[] values, float[] widths, bool header, int index)
    {
        var panel = new PanelContainer();
        var background = header ? new Color(GameTheme.Bronze, .20f) : index % 2 == 0 ? new Color(GameTheme.PanelRaised, .92f) : new Color(GameTheme.Panel, .92f);
        panel.AddThemeStyleboxOverride("panel", GameTheme.Box(background, new Color(GameTheme.GoldDim, header ? .72f : .28f), 4, 1, 10, 3));
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 10); panel.AddChild(row);
        for (var i = 0; i < values.Length; i++)
        {
            var label = new Label { Text = values[i], CustomMinimumSize = new Vector2(widths[i], header ? 34 : 31), VerticalAlignment = VerticalAlignment.Center, TooltipText = values[i] }; label.AddThemeColorOverride("font_color", header ? GameTheme.Gold : GameTheme.Paper); if (header) label.AddThemeFontSizeOverride("font_size", 14); row.AddChild(label);
        }
        return panel;
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren()) { node.RemoveChild(child); child.QueueFree(); }
    }
    private IEnumerable<ScenarioOfficerData> RecruitmentCandidates() => Runtime.State.Officers.Where(item => Runtime.IsRecruitmentCandidate(item.Profile.Id));
    private static string OfficerStatusLabel(string status) => status switch
    {
        "serving" => "任职",
        "free" => "在野",
        "captive" => "俘虏",
        "deployed" => "出征",
        "marching" => "调动中",
        "wounded" => "负伤",
        _ => "未知",
    };
    private void RefreshRecruitmentPreview()
    {
        if (_recruitmentPreview is null) return;
        var candidate = Runtime.Officer(Selected(_candidate));
        var actor = Runtime.Officer(Selected(_actor));
        var method = candidate is null ? string.Empty : Runtime.RecruitmentMethod(candidate.Profile.Id);
        _recruitmentMethod.Text = $"方式：{GameRuntime.RecruitmentMethodLabel(method)}";
        if (candidate is null || actor is null || string.IsNullOrEmpty(method))
        {
            _recruitmentPreview.Text = "当前没有可执行的招募组合。";
            _recruitButton.Disabled = true;
            return;
        }
        var chance = Runtime.RecruitmentChance(candidate.Profile.Id, actor.Profile.Id);
        var cost = method == "subversion" ? "；消耗1000金" : "；无额外花费";
        var blocked = method == "subversion" && Runtime.State.Resources.Gold < 1000;
        _recruitmentPreview.Text = $"{actor.Profile.Name}将以「{GameRuntime.RecruitmentMethodLabel(method)}」尝试招募{candidate.Profile.Name}　·　预计成功率 {chance}%{cost}" + (blocked ? "\n当前不可执行：势力府库不足1000金。" : "");
        _recruitButton.Disabled = blocked;
    }
    private void RefreshProgression()
    {
        if (_progression is null) return;
        var officer = Runtime.Officer(Selected(_promotionOfficer));
        _progression.Text = officer is null ? "请选择一名己方武将查看成长、官职和俸禄。" : Runtime.OfficerProgressionSummary(officer);
    }
    private void RefreshMarchRoute()
    {
        if (_marchSource is null) return;
        var officer = Runtime.Officer(Selected(_marchOfficer));
        _marchSource.Text = officer is null ? "—" : Runtime.City(officer.InitialState.CityId)?.Name ?? "未知";
        var targets = Runtime.State.Cities
            .Where(item => item.OwnerFactionId == Runtime.State.PlayerFactionId && item.Id != officer?.InitialState.CityId)
            .Select(item => (City: item, Days: officer is null ? 0 : Runtime.OfficerTransferDays(officer.Profile.Id, item.Id)))
            .Where(item => item.Days > 0)
            .ToList();
        Fill(_marchTargetCity, targets, item => $"{item.City.Name} · {item.Days}日", item => item.City.Id);
        RefreshMarchPreview();
    }
    private void RefreshMarchPreview()
    {
        if (_marchPreview is null) return;
        var officer = Runtime.Officer(Selected(_marchOfficer));
        var target = Runtime.City(Selected(_marchTargetCity));
        var days = officer is null || target is null ? 0 : Runtime.OfficerTransferDays(officer.Profile.Id, target.Id);
        _marchButton.Disabled = officer is null || target is null || days <= 0;
        _marchPreview.Text = days <= 0
            ? "当前武将没有可以调往的己方目标城。"
            : $"{officer!.Profile.Name}将从{Runtime.City(officer.InitialState.CityId)?.Name}调往{target!.Name}，路程{days}日；确认后进入调动状态，每次月结推进30日，预计{(int)Math.Ceiling(days / 30d)}个月到达。";
    }
    private static void FillOfficerChoice(OptionButton option, IEnumerable<ScenarioOfficerData> items, Func<ScenarioOfficerData, string> label)
    {
        var previous = Selected(option); option.Clear();
        foreach (var item in items) { option.AddItem(label(item)); option.SetItemMetadata(option.ItemCount - 1, item.Profile.Id); }
        SelectByMetadata(option, previous);
    }
    private static void SelectByMetadata(OptionButton option, string value) { for (var i = 0; i < option.ItemCount; i++) if (option.GetItemMetadata(i).AsString() == value) { option.Select(i); return; } }
    private static void Fill<T>(OptionButton option, IEnumerable<T> items, Func<T, string> label, Func<T, string> value) { var previous = Selected(option); option.Clear(); foreach (var item in items) AddChoice(option, label(item), value(item)); for (var i = 0; i < option.ItemCount; i++) if (option.GetItemMetadata(i).AsString() == previous) option.Select(i); }
}

public partial class DiplomacyView : FeatureScreen
{
    private OptionButton _faction = null!, _type = null!; private SpinBox _gift = null!; private Label _relations = null!, _incoming = null!, _preview = null!; private Button _send = null!, _accept = null!, _reject = null!;
    protected override void Build()
    {
        var row = Row(); row.AddChild(Text("外交交涉", 100)); _faction = Choice(300); _type = Choice(190); _gift = Number("金赠礼", 0, 100000, 250, 0);
        foreach (var item in new[] { ("trade", "通商"), ("truce", "停战"), ("captive-exchange", "交换俘虏") }) AddChoice(_type, item.Item2, item.Item1);
        row.AddChild(_faction); row.AddChild(_type); row.AddChild(_gift); _send = GameTheme.Button("派遣使者"); _send.Pressed += () => Runtime.ProposeDiplomacy(Selected(_faction), Selected(_type), (int)_gift.Value); row.AddChild(_send); Body.AddChild(Panel(row));
        _faction.ItemSelected += _ => RefreshPreview(); _type.ItemSelected += _ => RefreshPreview(); _gift.ValueChanged += _ => RefreshPreview();
        _preview = Text("", 1220); _preview.CustomMinimumSize = new Vector2(0, 64); Body.AddChild(Panel(_preview));
        var incomingRow = Row(); _incoming = Text("暂无待回应的 AI 外交提案。", 850); incomingRow.AddChild(_incoming); _accept = GameTheme.Button("接受提案"); _accept.Pressed += () => Runtime.RespondToAiProposal(true); incomingRow.AddChild(_accept); _reject = GameTheme.Button("拒绝提案"); _reject.Pressed += () => Runtime.RespondToAiProposal(false); incomingRow.AddChild(_reject); Body.AddChild(Panel(incomingRow));
        _relations = new Label { CustomMinimumSize = new Vector2(0, 540), AutowrapMode = TextServer.AutowrapMode.WordSmart }; _relations.AddThemeColorOverride("font_color", GameTheme.Muted); Body.AddChild(Panel(_relations));
    }
    public override void Refresh()
    {
        if (_faction is null) return; var old = Selected(_faction); _faction.Clear();
        foreach (var faction in Runtime.State.Factions.Where(item => item.Id != Runtime.State.PlayerFactionId)) AddChoice(_faction, faction.Name, faction.Id); Restore(_faction, old);
        _gift.MaxValue = Runtime.State.Resources.Gold;
        var pending = Runtime.State.AiDiplomaticProposals.FirstOrDefault(item => item.Status == "pending");
        _incoming.Text = pending is null ? "暂无待回应的 AI 外交提案。" : $"{Runtime.Faction(pending.FromFactionId)?.Name}主动提出{GameRuntime.DiplomacyLabel(pending.Type)}，期限{pending.DurationMonths}个月。";
        _accept.Disabled = pending is null; _reject.Disabled = pending is null;
        _relations.Text = "天下外交态势\n\n" + string.Join("\n\n", Runtime.State.Diplomacy.Select(relation => { var faction = Runtime.Faction(relation.FactionId); var treaty = relation.Treaties.Count == 0 ? "无条约" : string.Join('、', relation.Treaties.Select(item => $"{GameRuntime.DiplomacyLabel(item.Key)}余{item.Value}月")); return $"{faction?.Name}　{GameRuntime.AttitudeLabel(relation.Relation)}　关系 {relation.Relation:+#;-#;0}　信任 {relation.Trust}　{treaty}"; }));
        RefreshPreview();
    }
    private void RefreshPreview()
    {
        if (_preview is null || _faction.Selected < 0 || _type.Selected < 0) return;
        var factionId = Selected(_faction); var type = Selected(_type); var gift = (int)_gift.Value;
        var relation = Runtime.State.Diplomacy.FirstOrDefault(item => item.FactionId == factionId);
        var blocked = relation?.LastProposalTurn == Runtime.State.Turn ? "本月已经向该势力交涉" : type is ("trade" or "truce") && Runtime.HasTreaty(factionId, type) ? $"现有{GameRuntime.DiplomacyLabel(type)}尚未到期" : type == "captive-exchange" && !Runtime.CanExchangeCaptives(factionId) ? "双方目前没有可成对交换的俘虏" : null;
        _send.Disabled = blocked is not null;
        _preview.Text = $"预计成功率 {Runtime.DiplomacyChance(factionId, type, gift)}%　·　{GameRuntime.DiplomacyEffect(type)}" + (blocked is null ? "" : $"\n当前不可执行：{blocked}");
    }
    private static void Restore(OptionButton option, string value) { for (var i = 0; i < option.ItemCount; i++) if (option.GetItemMetadata(i).AsString() == value) option.Select(i); }
}

public partial class ExpeditionView : FeatureScreen
{
    public event Action<string, int>? ArmyMovementRequested;

    private OptionButton _source = null!, _target = null!, _commander = null!, _deputy1 = null!, _deputy2 = null!, _specialTroop = null!; private SpinBox _food = null!, _infantry = null!, _spears = null!, _archers = null!, _cavalry = null!, _siege = null!, _specialCount = null!; private VBoxContainer _armyList = null!; private Label _compositionTotal = null!; private string _presetTargetId = string.Empty, _presetArmyTargetId = string.Empty; private bool _refreshingOfficerChoices;
    protected override void Build()
    {
        ContentScroll.OffsetBottom = -154;
        var row = Row(); row.AddChild(Text("行军命令", 92)); _source = Choice(220); _target = Choice(220); _commander = Choice(260); _food = Number("粮", 100, 100000, 100, 5000);
        foreach (var control in new Control[] { _source, _target, _commander, _food }) row.AddChild(control);
        var battlePlanHint = Text("姿态、阵型和战术在接战后的战前军议中选择。", 620); battlePlanHint.AddThemeColorOverride("font_color", GameTheme.Muted); row.AddChild(battlePlanHint);
        Body.AddChild(Panel(row));
        var officers = Row(); officers.AddChild(Text("出战武将", 100)); _deputy1 = Choice(260); _deputy2 = Choice(260); officers.AddChild(_deputy1); officers.AddChild(_deputy2); var officerHint = Text("主将在上方选择；本栏可配置至多两名副将。", 650); officerHint.AddThemeColorOverride("font_color", GameTheme.Muted); officers.AddChild(officerHint);
        Body.AddChild(Panel(officers));
        var composition = Row(); composition.AddChild(Text("实际阵型", 92)); _infantry = Number("步", 0, 1000000, 500, 2500); _spears = Number("枪", 0, 1000000, 500, 1000); _archers = Number("弓", 0, 1000000, 500, 500); _cavalry = Number("骑", 0, 1000000, 500, 0); _siege = Number("器械", 0, 1000000, 500, 0); foreach (var control in CompositionControls()) { control.CustomMinimumSize = new Vector2(132, 40); control.ValueChanged += _ => RefreshCapacity(); composition.AddChild(control); } var full = GameTheme.Button("全城出击"); full.Pressed += FillWholeGarrison; composition.AddChild(full); var recommended = GameTheme.Button("推荐编制"); recommended.Pressed += ApplyRecommendedComposition; composition.AddChild(recommended); _compositionTotal = Text("合计 4,000", 210); composition.AddChild(_compositionTotal); Body.AddChild(Panel(composition));
        var elite = Row(); elite.AddChild(Text("特殊部队", 92)); _specialTroop = Choice(280); _specialCount = Number("精锐人数", 0, 100000, 500, 0); elite.AddChild(_specialTroop); elite.AddChild(_specialCount); var eliteHint = Text("计入对应基础兵种；达到军团20%时可触发专属特性。", 620); eliteHint.AddThemeColorOverride("font_color", GameTheme.Muted); elite.AddChild(eliteHint); var elitePanel = Panel(elite); elitePanel.Visible = false; Body.AddChild(elitePanel);
        _specialTroop.ItemSelected += _ => RefreshCapacity(); _specialCount.ValueChanged += _ => RefreshCapacity();
        _source.ItemSelected += _ => RefreshSource();
        _commander.ItemSelected += _ => RefreshCommanders(); _deputy1.ItemSelected += _ => RefreshCommanders(); _deputy2.ItemSelected += _ => RefreshCommanders();
        _armyList = new VBoxContainer { CustomMinimumSize = new Vector2(0, 540) }; _armyList.AddThemeConstantOverride("separation", 10); Body.AddChild(Panel(_armyList));

        var actionBar = new PanelContainer { AnchorLeft = 0, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 24, OffsetTop = -144, OffsetRight = -24, OffsetBottom = -76, ZIndex = 45, MouseFilter = MouseFilterEnum.Stop };
        actionBar.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(8)); AddChild(actionBar);
        var actionMargin = new MarginContainer(); actionMargin.AddThemeConstantOverride("margin_left", 16); actionMargin.AddThemeConstantOverride("margin_right", 12); actionMargin.AddThemeConstantOverride("margin_top", 7); actionMargin.AddThemeConstantOverride("margin_bottom", 7); actionBar.AddChild(actionMargin);
        var actionRow = new HBoxContainer(); actionRow.AddThemeConstantOverride("separation", 16); actionMargin.AddChild(actionRow);
        var actionHint = new Label { Text = "确认出发城、目标、主将、兵种与军粮后下令出征", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center }; actionHint.AddThemeColorOverride("font_color", GameTheme.Muted); actionRow.AddChild(actionHint);
        var send = GameTheme.Button("确认出征"); send.CustomMinimumSize = new Vector2(190, 48); send.AddThemeFontSizeOverride("font_size", 17); send.AddThemeColorOverride("font_color", GameTheme.OnAccent); send.AddThemeColorOverride("font_hover_color", GameTheme.OnAccent); send.AddThemeColorOverride("font_pressed_color", GameTheme.OnAccent); send.AddThemeStyleboxOverride("normal", GameTheme.Box(GameTheme.Cinnabar, new Color(GameTheme.Bronze, .82f), 7, 1, 18, 10)); send.AddThemeStyleboxOverride("hover", GameTheme.Box(Color.FromHtml("#b95b4a"), GameTheme.Cinnabar, 7, 2, 17, 9)); send.Pressed += LaunchExpedition; actionRow.AddChild(send);
    }
    public override void Refresh()
    {
        if (_source is null) return; var previous = Selected(_source); _source.Clear(); foreach (var city in Runtime.State.Cities.Where(item => item.OwnerFactionId == Runtime.State.PlayerFactionId)) AddChoice(_source, $"{city.Name} · {city.Garrison:N0}兵", city.Id); Restore(_source, previous); RefreshCommanders();
        previous = string.IsNullOrEmpty(_presetTargetId) ? Selected(_target) : _presetTargetId; _target.Clear();
        var interceptTarget = Runtime.State.Armies.FirstOrDefault(item => item.Id == _presetArmyTargetId && item.FactionId != Runtime.State.PlayerFactionId && item.Status is "marching" or "besieging");
        if (interceptTarget is not null)
        {
            var commander = Runtime.Officer(interceptTarget.CommanderId)?.Profile.Name ?? "未知主将";
            AddChoice(_target, $"拦截 {Runtime.Faction(interceptTarget.FactionId)?.ShortName}·{commander}军（{interceptTarget.Soldiers:N0}兵）", interceptTarget.Id);
            _target.Disabled = true;
        }
        else
        {
            _presetArmyTargetId = string.Empty;
            foreach (var city in Runtime.State.Cities.Where(item => item.OwnerFactionId != Runtime.State.PlayerFactionId)) AddChoice(_target, $"{city.Name} · {Runtime.Faction(city.OwnerFactionId)?.ShortName}", city.Id);
            Restore(_target, previous);
            _target.Disabled = false;
        }
        _presetTargetId = string.Empty;
        RefreshSourceLimits();
        RefreshSpecialTroops();
        RefreshCapacity();
        RebuildArmyList();
    }

    private void LaunchExpedition()
    {
        var deputies = new[] { Selected(_deputy1), Selected(_deputy2) }.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var before = Runtime.State.Armies.Count;
        var composition = new Dictionary<string, int> { ["infantry"] = (int)_infantry.Value, ["spears"] = (int)_spears.Value, ["archers"] = (int)_archers.Value, ["cavalry"] = (int)_cavalry.Value, ["siege"] = (int)_siege.Value };
        var specialTroops = string.IsNullOrEmpty(Selected(_specialTroop)) || _specialCount.Value <= 0 ? new Dictionary<string, int>() : new Dictionary<string, int> { [Selected(_specialTroop)] = (int)_specialCount.Value };
        var soldiers = composition.Values.Sum();
        var targetArmy = Runtime.State.Armies.FirstOrDefault(item => item.Id == _presetArmyTargetId);
        var targetCityId = targetArmy?.TargetCityId ?? Selected(_target);
        if (!Runtime.CreateExpedition(Selected(_source), targetCityId, Selected(_commander), soldiers, (int)_food.Value, deputies, composition, specialTroops, targetArmy?.Id ?? string.Empty)) return;
        var army = Runtime.State.Armies.Skip(before).First();
        _presetArmyTargetId = string.Empty;
        ArmyMovementRequested?.Invoke(army.Id, army.TotalDays);
    }

    private void RebuildArmyList()
    {
        foreach (var child in _armyList.GetChildren()) { _armyList.RemoveChild(child); child.QueueFree(); }
        var title = Text("己方军团 · 行军命令在当前回合立即执行；撤兵请在天下地图点击对应军团", 1100); title.AddThemeColorOverride("font_color", GameTheme.Paper); _armyList.AddChild(title);
        var armies = Runtime.State.Armies.Where(item => item.FactionId == Runtime.State.PlayerFactionId).OrderByDescending(item => item.Id).ToList();
        if (armies.Count == 0)
        {
            var empty = Text("尚无出征军团。", 900); empty.AddThemeColorOverride("font_color", GameTheme.Muted); _armyList.AddChild(empty); return;
        }
        foreach (var army in armies)
        {
            var row = Row();
            var commander = Runtime.Officer(army.CommanderId)?.Profile.Name ?? "未知主将";
            var progress = Math.Max(0, army.TotalDays - army.RemainingDays);
            var composition = string.Join('、', army.Composition.Select(item => $"{BattleCatalog.TroopName(item.Key)}{item.Value:N0}"));
            var elites = army.SpecialTroops.Count == 0 ? "" : "；精锐" + string.Join('、', army.SpecialTroops.Select(item => $"{OfficerProgressionRules.SpecialTroops.GetValueOrDefault(item.Key)?.Name ?? item.Key}{item.Value:N0}"));
            var target = string.IsNullOrEmpty(army.TargetArmyId) ? Runtime.City(army.TargetCityId)?.Name : $"拦截{Runtime.Officer(Runtime.State.Armies.FirstOrDefault(item => item.Id == army.TargetArmyId)?.CommanderId ?? string.Empty)?.Profile.Name ?? "敌军"}";
            var summary = Text($"{commander}军　{Runtime.City(army.SourceCityId)?.Name} → {target}\n兵力 {army.Soldiers:N0}（{composition}{elites}）　军粮 {army.Food:N0}　士气{army.Morale}　疲劳{army.Fatigue}　行程 {progress}/{army.TotalDays}日　状态 {army.Status}", 1040);
            summary.AddThemeColorOverride("font_color", GameTheme.Muted); row.AddChild(summary);
            var canAdvance = army.Status is "marching" or "besieging";
            var march = GameTheme.Button(!canAdvance ? "行军结束" : army.LastMarchTurn >= Runtime.State.Turn ? "本回合已行动" : army.Status == "besieging" ? "继续攻城" : "本回合行军");
            march.CustomMinimumSize = new Vector2(170, 48); march.Disabled = !canAdvance || army.LastMarchTurn >= Runtime.State.Turn;
            var armyId = army.Id; march.Pressed += () => March(armyId); row.AddChild(march); _armyList.AddChild(row);
        }
    }

    private void March(string armyId)
    {
        var army = Runtime.State.Armies.First(item => item.Id == armyId);
        var before = army.Status == "besieging" ? 0 : army.RemainingDays;
        if (Runtime.MarchArmy(armyId)) ArmyMovementRequested?.Invoke(armyId, before);
    }
    public void PresetTarget(string targetCityId)
    {
        _presetArmyTargetId = string.Empty;
        _presetTargetId = targetCityId;
        if (_target is not null) { Refresh(); Restore(_target, targetCityId); }
    }

    public void PresetArmyTarget(string targetArmyId)
    {
        _presetTargetId = string.Empty;
        _presetArmyTargetId = targetArmyId;
        if (_target is not null) Refresh();
    }

    private SpinBox[] CompositionControls() => [_infantry, _spears, _archers, _cavalry, _siege];

    private int CompositionTotal() => CompositionControls().Sum(control => (int)control.Value);

    private void RefreshSource()
    {
        RefreshCommanders();
        RefreshSourceLimits();
        RefreshSpecialTroops();
        RefreshCapacity();
    }

    private void RefreshSourceLimits()
    {
        var source = Runtime.City(Selected(_source));
        if (source is null) return;
        foreach (var control in CompositionControls()) control.MaxValue = source.Garrison;
        _food.MaxValue = Math.Max(100, Runtime.State.Resources.Food);
        _food.Value = Math.Min(_food.Value, _food.MaxValue);
        if (CompositionTotal() > source.Garrison) ApplyRecommendedComposition();
    }

    private void FillWholeGarrison() => Compose(Runtime.City(Selected(_source))?.Garrison ?? 0);

    private void ApplyRecommendedComposition()
    {
        var source = Runtime.City(Selected(_source));
        if (source is null) return;
        var current = CompositionTotal();
        Compose(Math.Min(source.Garrison, current >= 1000 ? current : Math.Min(5000, source.Garrison)));
    }

    private void Compose(int total)
    {
        total = Math.Max(0, total);
        var archers = total >= 1000 ? Math.Max(500, total / 4 / 500 * 500) : 0;
        _siege.Value = 0; _archers.Value = archers; _spears.Value = 0; _cavalry.Value = 0; _infantry.Value = Math.Max(0, total - archers);
        RefreshCapacity();
    }
    private void RefreshCapacity()
    {
        if (_compositionTotal is null) return; var total = CompositionTotal(); var source = Runtime.City(Selected(_source));
        var special = OfficerProgressionRules.SpecialTroops.GetValueOrDefault(Selected(_specialTroop));
        if (special is null) { _specialCount.MaxValue = 0; _specialCount.Value = 0; }
        else { var baseCount = special.BaseTroopType switch { "cavalry" => (int)_cavalry.Value, "infantry" => (int)_infantry.Value, "spears" => (int)_spears.Value, "archers" => (int)_archers.Value, _ => (int)_siege.Value }; _specialCount.MaxValue = baseCount; if (_specialCount.Value > baseCount) _specialCount.Value = baseCount / 500 * 500; }
        _compositionTotal.Text = $"合计 {total:N0} / 城中 {source?.Garrison ?? 0:N0}"; _compositionTotal.AddThemeColorOverride("font_color", source is not null && total > source.Garrison ? GameTheme.Danger : GameTheme.Paper);
    }
    private void RefreshCommanders()
    {
        if (_commander is null || _refreshingOfficerChoices) return;
        _refreshingOfficerChoices = true;
        try
        {
            var eligible = Runtime.PlayerOfficers().Where(item => item.InitialState.CityId == Selected(_source) && item.InitialState.Status == "serving").ToList();
            var eligibleIds = eligible.Select(item => item.Profile.Id).ToHashSet();
            var commanderId = Selected(_commander);
            var deputy1Id = Selected(_deputy1);
            var deputy2Id = Selected(_deputy2);
            if (!eligibleIds.Contains(commanderId)) commanderId = string.Empty;
            if (!eligibleIds.Contains(deputy1Id) || deputy1Id == commanderId) deputy1Id = string.Empty;
            if (!eligibleIds.Contains(deputy2Id) || deputy2Id == commanderId || deputy2Id == deputy1Id) deputy2Id = string.Empty;

            _commander.Clear();
            foreach (var officer in eligible.Where(item => item.Profile.Id != deputy1Id && item.Profile.Id != deputy2Id))
                AddChoice(_commander, $"等级{officer.InitialState.Level} {officer.Profile.Name} · 统{Runtime.EffectiveAbility(officer, "leadership", "military")} 武{Runtime.EffectiveAbility(officer, "might", "military")}", officer.Profile.Id);
            Restore(_commander, commanderId);
            if (_commander.Selected < 0 && _commander.ItemCount > 0) _commander.Select(0);
            commanderId = Selected(_commander);

            FillDeputy(_deputy1, eligible, deputy1Id, commanderId, deputy2Id);
            deputy1Id = Selected(_deputy1);
            FillDeputy(_deputy2, eligible, deputy2Id, commanderId, deputy1Id);
        }
        finally
        {
            _refreshingOfficerChoices = false;
        }
        RefreshCapacity();
    }
    private void RefreshSpecialTroops()
    {
        if (_specialTroop is null) return; var old = Selected(_specialTroop); _specialTroop.Clear(); AddChoice(_specialTroop, "不编特殊部队", "");
        foreach (var item in OfficerProgressionRules.SpecialTroops.Values.Where(item => item.FactionIds.Contains(Runtime.State.PlayerFactionId))) AddChoice(_specialTroop, $"{item.Name} · {BattleCatalog.TroopName(item.BaseTroopType)} · {item.Description}", item.Id);
        Restore(_specialTroop, old); if (_specialTroop.Selected < 0) _specialTroop.Select(0);
    }
    private static void FillDeputy(OptionButton option, IEnumerable<ScenarioOfficerData> officers, string selectedId, string commanderId, string otherDeputyId) { option.Clear(); AddChoice(option, "不设副将", ""); foreach (var officer in officers.Where(item => item.Profile.Id != commanderId && item.Profile.Id != otherDeputyId)) AddChoice(option, $"副将 · {officer.Profile.Name}", officer.Profile.Id); Restore(option, selectedId); if (option.Selected < 0) option.Select(0); }
    private static void Restore(OptionButton option, string value) { for (var i = 0; i < option.ItemCount; i++) if (option.GetItemMetadata(i).AsString() == value) option.Select(i); }
}

public partial class BattleView : FeatureScreen
{
    public event Action? BattleCompleted;

    private BattlefieldCanvas _battlefield = null!;
    private HBoxContainer _officers = null!;
    private PanelContainer _planningPanel = null!, _battlefieldPanel = null!, _commandPanel = null!, _officersPanel = null!;
    private OptionButton _formation = null!, _infantryOrder = null!, _spearOrder = null!, _archerOrder = null!, _cavalryOrder = null!, _siegeOrder = null!, _speed = null!, _battleStance = null!, _battleTactic = null!;
    private Label _tacticBrief = null!;
    private Label _commandStatus = null!;
    private PanelContainer _traitTooltip = null!;
    private Label _traitTooltipText = null!;
    private Button _start = null!, _skip = null!, _attackTarget = null!, _defendGate = null!, _innerCity = null!, _sortie = null!, _reserveLine = null!;
    private string _battleId = string.Empty;
    private double _playback;
    private double _playbackSpeed = 1;
    private bool _finishing;

    protected override void Build()
    {
        Body.AddThemeConstantOverride("separation", 10);
        var planning = new VBoxContainer(); planning.AddThemeConstantOverride("separation", 8);
        var planSummary = Row(); planSummary.AddChild(Text("阵型与军略", 92)); _formation = Choice(126); foreach (var item in new[] { ("goose", "雁行阵"), ("wedge", "锋矢阵"), ("crane", "鹤翼阵"), ("shield", "盾阵"), ("siege-array", "攻城阵") }) AddChoice(_formation, item.Item2, item.Item1); planSummary.AddChild(_formation);
        _infantryOrder = Choice(125); _spearOrder = Choice(125); _archerOrder = Choice(125); _cavalryOrder = Choice(125); _siegeOrder = Choice(135);
        FillOrder(_infantryOrder, "步", [("shield-line", "盾墙"), ("loose-line", "疏阵"), ("assault-column", "突击纵队")]);
        FillOrder(_spearOrder, "枪", [("spear-wall", "密集枪阵"), ("support-line", "支援横阵"), ("spear-column", "推进纵阵")]);
        FillOrder(_archerOrder, "弓", [("rear-double", "后排双列"), ("wing-fire", "翼侧射列"), ("skirmish", "前出散射")]);
        FillOrder(_cavalryOrder, "骑", [("wing-column", "两翼纵队"), ("cavalry-wedge", "锋矢冲锋"), ("reserve", "中央预备")]);
        FillOrder(_siegeOrder, "械", [("protected-siege", "保护列"), ("gate-column", "攻门列"), ("wall-pressure", "压墙列")]);
        _battleStance = Choice(112); AddChoice(_battleStance, "稳健", "cautious"); AddChoice(_battleStance, "标准", "standard"); AddChoice(_battleStance, "激进", "aggressive"); planSummary.AddChild(_battleStance);
        _battleTactic = Choice(160); FillTactics(_battleTactic); planSummary.AddChild(_battleTactic);
        _formation.ItemSelected += _ => PreviewBattlePlan();
        foreach (var option in new[] { _infantryOrder, _spearOrder, _archerOrder, _cavalryOrder, _siegeOrder }) option.ItemSelected += _ => PreviewBattlePlan();
        _tacticBrief = Text("选择主战术查看收益、风险和兵种条件。", 760); planSummary.AddChild(_tacticBrief);
        _battleStance.ItemSelected += _ => RefreshTacticBrief(); _battleTactic.ItemSelected += _ => RefreshTacticBrief();
        planning.AddChild(planSummary);
        var troopOrders = Row(); troopOrders.AddChild(Text("兵种阵位", 92));
        foreach (var option in new[] { _infantryOrder, _spearOrder, _archerOrder, _cavalryOrder, _siegeOrder }) troopOrders.AddChild(option);
        planning.AddChild(troopOrders);
        _planningPanel = Panel(planning); Body.AddChild(_planningPanel);

        _officers = new HBoxContainer { CustomMinimumSize = new Vector2(0, 100), Alignment = BoxContainer.AlignmentMode.Center }; _officers.AddThemeConstantOverride("separation", 8); _officersPanel = Panel(_officers); Body.AddChild(_officersPanel);
        _battlefield = new BattlefieldCanvas();
        _battlefield.CustomMinimumSize = new Vector2(0, 560);
        _battlefield.FriendlySelectionChanged += _ => UpdateCommandStatus();
        _battlefield.EnemySelectionChanged += _ => UpdateCommandStatus();
        _battlefield.AttackCommandIssued += (groups, target) => Runtime.IssueBattleCommand(groups, "attack", target);
        _battlefield.MoveCommandIssued += (groups, point) => Runtime.IssueBattleCommand(groups, "move", destinationX: point.X, destinationY: point.Y);
        _battlefieldPanel = Panel(_battlefield); Body.AddChild(_battlefieldPanel);
        BuildCommandPanel();
        Body.MoveChild(_commandPanel, _battlefieldPanel.GetIndex());
        BuildTraitTooltip();
        SetProcess(true);
    }

    private void BuildTraitTooltip()
    {
        _traitTooltip = new PanelContainer { Visible = false, ZIndex = 120, MouseFilter = MouseFilterEnum.Ignore };
        _traitTooltip.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.PanelRaised, .99f), new Color(GameTheme.Bronze, .88f), 9, 2));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16); margin.AddThemeConstantOverride("margin_right", 16); margin.AddThemeConstantOverride("margin_top", 12); margin.AddThemeConstantOverride("margin_bottom", 12);
        _traitTooltip.AddChild(margin);
        _traitTooltipText = new Label { CustomMinimumSize = new Vector2(560, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _traitTooltipText.AddThemeColorOverride("font_color", GameTheme.Paper); _traitTooltipText.AddThemeFontSizeOverride("font_size", 14); margin.AddChild(_traitTooltipText);
        AddChild(_traitTooltip);
    }

    private void ShowTraitTooltip(string text)
    {
        if (_traitTooltip is null || string.IsNullOrWhiteSpace(text)) return;
        _traitTooltipText.Text = text;
        _traitTooltip.Visible = true;
        _traitTooltip.ResetSize();
        CallDeferred(nameof(PositionTraitTooltip));
    }

    private void HideTraitTooltip()
    {
        if (_traitTooltip is not null) _traitTooltip.Visible = false;
    }

    private void PositionTraitTooltip()
    {
        if (_traitTooltip is null || !_traitTooltip.Visible) return;
        var mouse = GetLocalMousePosition();
        var x = Math.Clamp(mouse.X + 18, 12, Math.Max(12, Size.X - _traitTooltip.Size.X - 12));
        var y = Math.Clamp(mouse.Y + 20, 12, Math.Max(12, Size.Y - _traitTooltip.Size.Y - 76));
        _traitTooltip.Position = new Vector2(x, y);
    }

    private void BuildCommandPanel()
    {
        var commands = Row();
        _speed = Choice(86); AddChoice(_speed, "速度 ×1", "1"); AddChoice(_speed, "速度 ×2", "2"); _speed.ItemSelected += _ => _playbackSpeed = double.TryParse(Selected(_speed), out var speed) ? speed : 1; commands.AddChild(_speed);
        _skip = GameTheme.Button("跳过演算"); _skip.Pressed += SkipBattle; commands.AddChild(_skip);
        _commandStatus = Text("未选择军团", 300); commands.AddChild(_commandStatus);
        var centerCamera = GameTheme.Button("镜头归中"); centerCamera.Pressed += _battlefield.ResetCameraView; commands.AddChild(centerCamera);
        var zoomOut = GameTheme.Button("战场－"); zoomOut.Pressed += _battlefield.ZoomOut; commands.AddChild(zoomOut);
        var zoomIn = GameTheme.Button("战场＋"); zoomIn.Pressed += _battlefield.ZoomIn; commands.AddChild(zoomIn);
        var selectAll = GameTheme.Button("全选我军"); selectAll.Pressed += _battlefield.SelectAllPlayerGroups; commands.AddChild(selectAll);
        _attackTarget = GameTheme.Button("攻击目标"); _attackTarget.Pressed += _battlefield.IssueAttackOnSelectedEnemy; commands.AddChild(_attackTarget);
        var auto = GameTheme.Button("自由进攻"); auto.Pressed += () => IssueSelectedCommand("auto"); commands.AddChild(auto);
        var hold = GameTheme.Button("原地固守"); hold.Pressed += () => IssueSelectedCommand("hold"); commands.AddChild(hold);
        _defendGate = GameTheme.Button("增援城门"); _defendGate.Pressed += () => IssueSelectedCommand("defend-gate"); commands.AddChild(_defendGate);
        _innerCity = GameTheme.Button("退守内城"); _innerCity.Pressed += () => IssueSelectedCommand("inner-city"); commands.AddChild(_innerCity);
        _sortie = GameTheme.Button("出城突袭"); _sortie.Pressed += () => IssueSelectedCommand("sortie"); commands.AddChild(_sortie);
        _reserveLine = GameTheme.Button("保存预备队"); _reserveLine.Pressed += () => IssueSelectedCommand("reserve-line"); commands.AddChild(_reserveLine);
        commands.AddChild(Text("页面滚轮滚动　Ctrl+滚轮缩放战场　中键拖动", 330));
        _commandPanel = Panel(commands); _commandPanel.Visible = false; Body.AddChild(_commandPanel);
    }

    private async void ResetBattleScrollAfterLayout()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ContentScroll.ScrollVertical = 0;
    }

    private void IssueSelectedCommand(string command)
    {
        Runtime.IssueBattleCommand(_battlefield.SelectedGroupIds, command);
    }

    private void UpdateCommandStatus()
    {
        if (_commandStatus is null) return;
        var pending = Runtime?.State.PendingBattle;
        var selected = _battlefield.SelectedGroupIds.Count;
        var enemy = pending?.Groups.FirstOrDefault(item => item.Id == _battlefield.SelectedEnemyId);
        _commandStatus.Text = selected == 0
            ? "未选择军团：左键点击或拖框选择我军"
            : enemy is null
                ? $"已选择 {selected} 支我军　·　右键地面移动，右键敌军进攻"
                : $"已选择 {selected} 支我军　·　目标：{BattleCatalog.TroopName(enemy.TroopType)} {enemy.FinalSoldiers:N0}人";
        if (_attackTarget is not null) _attackTarget.Disabled = selected == 0 || enemy is null || enemy.FinalSoldiers <= 0;
    }

    public override void Refresh()
    {
        if (_battlefield is null) return;
        var pending = Runtime.State.PendingBattle;
        _planningPanel.Visible = pending?.Status == "planning";
        _commandPanel.Visible = pending?.Status == "running";
        _officersPanel.Visible = pending is not null;
        _battlefieldPanel.Visible = pending is not null;
        Notice.Visible = pending?.Status != "running";
        if (pending is not null)
        {
            RefreshTroopOrderVisibility(pending);
            if (_battleId != pending.Id)
            {
                _battleId = pending.Id; _playback = 0; _finishing = false;
                ContentScroll.ScrollVertical = 0;
                ResetBattleScrollAfterLayout();
                var playerPlan = pending.PlayerSide == "attacker" ? pending.AttackerFormation : pending.DefenderFormation;
                Select(_formation, playerPlan.FormationId);
                Select(_infantryOrder, playerPlan.TroopOrders.GetValueOrDefault("infantry")?.OrderId ?? "shield-line");
                Select(_spearOrder, playerPlan.TroopOrders.GetValueOrDefault("spears")?.OrderId ?? "spear-wall");
                Select(_archerOrder, playerPlan.TroopOrders.GetValueOrDefault("archers")?.OrderId ?? "rear-double");
                Select(_cavalryOrder, playerPlan.TroopOrders.GetValueOrDefault("cavalry")?.OrderId ?? "wing-column");
                Select(_siegeOrder, playerPlan.TroopOrders.GetValueOrDefault("siege")?.OrderId ?? "protected-siege");
                Select(_battleStance, pending.PlayerSide == "attacker" ? pending.Stance : pending.DefenderStance);
                Select(_battleTactic, pending.PlayerSide == "attacker" ? pending.PrimaryTactic : pending.DefenderPrimaryTactic);
                RefreshTacticBrief();
            }
            _skip.Disabled = pending.Status != "running";
            var defenderCommands = pending.PlayerSide == "defender" && pending.BattleType == "siege" && pending.Status == "running";
            foreach (var button in new[] { _defendGate, _innerCity, _sortie, _reserveLine }) button.Visible = defenderCommands;
            _battlefield.SetBattle(pending); _battlefield.SetPlaybackTime(_playback);
            UpdateCommandStatus();
            RebuildOfficers(pending);
            RefreshTacticBrief();
        }
        else
        {
            _battleId = string.Empty;
            _battlefield.SetBattle(null);
            HideTraitTooltip();
        }
    }

    public override void _Process(double delta)
    {
        PositionTraitTooltip();
        if (Runtime is null) return;
        var pending = Runtime.State.PendingBattle;
        if (pending is null || pending.Status != "running" || _finishing) return;
        Runtime.AdvancePendingBattle(delta * _playbackSpeed);
        _playback = Math.Min(pending.Duration, pending.Elapsed + pending.SimulationAccumulator);
        _battlefield.SetPlaybackTime(_playback);
        var officerEvent = LatestActiveEvent(pending, item => !string.IsNullOrEmpty(item.OfficerId));
        foreach (var child in _officers.GetChildren().OfType<Control>()) child.Modulate = child.HasMeta("officer_id") && child.GetMeta("officer_id").AsString() == officerEvent?.OfficerId ? new Color(1.18f, 1.08f, .72f) : Colors.White;
        if (pending.Status != "resolved") return;
        _finishing = true;
        CompleteBattle();
    }

    private BattleTimelineEventData? LatestActiveEvent(PendingBattleData pending, Func<BattleTimelineEventData, bool> predicate)
    {
        BattleTimelineEventData? latest = null;
        for (var index = pending.Timeline.Count - 1; index >= 0; index--)
        {
            var item = pending.Timeline[index];
            if (item.Start < _playback - 1.5) break;
            if (!predicate(item) || item.Start > _playback || item.Start + Math.Max(.2, item.Duration) < _playback) continue;
            if (latest is null || item.Start > latest.Start) latest = item;
        }
        return latest;
    }

    public void SetPlaybackForVisualTest(double value)
    {
        SetProcess(false);
        var pending = Runtime?.State.PendingBattle;
        if (pending?.Status == "running" && value > pending.Elapsed) Runtime!.AdvancePendingBattle(value - pending.Elapsed);
        _playback = pending is null ? value : Math.Min(value, pending.Elapsed + pending.SimulationAccumulator);
        if (_battlefield is not null) _battlefield.SetPlaybackTime(_playback);
    }

    public void SetReducedMotion(bool value)
    {
        if (_battlefield is not null) _battlefield.SetReducedMotion(value);
    }

    private void StartBattle()
    {
        if (!Runtime.ConfigurePendingBattle(Selected(_formation), CurrentBattleOrders(), Selected(_battleStance), Selected(_battleTactic))) return;
        Runtime.StartPendingBattle();
    }

    private Dictionary<string, string> CurrentBattleOrders() => new()
    {
        ["infantry"] = Selected(_infantryOrder),
        ["spears"] = Selected(_spearOrder),
        ["archers"] = Selected(_archerOrder),
        ["cavalry"] = Selected(_cavalryOrder),
        ["siege"] = Selected(_siegeOrder),
    };

    private void RefreshTroopOrderVisibility(PendingBattleData pending)
    {
        var playerTroops = pending.Groups
            .Where(item => item.Side == pending.PlayerSide && item.InitialSoldiers > 0)
            .Select(item => item.TroopType)
            .ToHashSet();
        _infantryOrder.Visible = playerTroops.Contains("infantry");
        _spearOrder.Visible = playerTroops.Contains("spears");
        _archerOrder.Visible = playerTroops.Contains("archers");
        _cavalryOrder.Visible = playerTroops.Contains("cavalry");
        _siegeOrder.Visible = playerTroops.Contains("siege");
    }

    private void PreviewBattlePlan()
    {
        var pending = Runtime?.State.PendingBattle;
        if (pending is null || pending.Status != "planning" || _battlefield is null) return;
        BattleCalculator.Configure(pending, Selected(_formation), CurrentBattleOrders(), Selected(_battleStance), Selected(_battleTactic));
        _battlefield.SetBattle(pending);
        RefreshTacticBrief();
    }

    private void SkipBattle()
    {
        var pending = Runtime.State.PendingBattle; if (pending is null || pending.Status != "running") return;
        _finishing = true;
        CompleteBattle();
    }

    private void CompleteBattle()
    {
        if (Runtime.CompletePendingBattle()) BattleCompleted?.Invoke();
    }

    private void RebuildOfficers(PendingBattleData pending)
    {
        HideTraitTooltip();
        foreach (var child in _officers.GetChildren()) { _officers.RemoveChild(child); child.QueueFree(); }
        var enemy = pending.PlayerSide == "attacker" ? pending.DefenderOfficerIds : pending.AttackerOfficerIds;
        var player = pending.PlayerSide == "attacker" ? pending.AttackerOfficerIds : pending.DefenderOfficerIds;
        foreach (var id in enemy) _officers.AddChild(OfficerCard(id, pending, false));
        var battlefield = pending.BattleType == "field"
            ? $"敌左　·　{Runtime.City(pending.CityId)?.Region}野战　·　我右\n两军列阵，正面交锋"
            : $"敌左　·　{Runtime.City(pending.CityId)?.Name}　·　我右\n外墙{pending.WallBefore}%　城门{pending.GateBefore}%　内城{pending.InnerBefore}%";
        var center = Text(battlefield, 350); center.HorizontalAlignment = HorizontalAlignment.Center; center.AddThemeColorOverride("font_color", GameTheme.Gold); _officers.AddChild(center);
        foreach (var id in player) _officers.AddChild(OfficerCard(id, pending, true));
        _start = GameTheme.Button("下令开战\n全军出击");
        _start.CustomMinimumSize = new Vector2(140, 68);
        _start.AddThemeFontSizeOverride("font_size", 18);
        _start.AddThemeColorOverride("font_color", GameTheme.OnAccent);
        _start.AddThemeColorOverride("font_hover_color", GameTheme.OnAccent);
        _start.AddThemeColorOverride("font_pressed_color", GameTheme.OnAccent);
        _start.AddThemeStyleboxOverride("normal", GameTheme.Box(GameTheme.Cinnabar, new Color(GameTheme.Bronze, .82f), 8, 2, 18, 10));
        _start.AddThemeStyleboxOverride("hover", GameTheme.Box(Color.FromHtml("#b95b4a"), GameTheme.Cinnabar, 8, 2, 18, 10));
        _start.Pressed += StartBattle;
        _officers.AddChild(_start);
    }

    private Control OfficerCard(string officerId, PendingBattleData pending, bool player)
    {
        var officer = Runtime.Officer(officerId); var box = new VBoxContainer { CustomMinimumSize = new Vector2(82, 92) };
        box.SetMeta("officer_id", officerId);
        var concealed = !player && pending.Status == "planning" && !HasPreciseBattleIntel(pending);
        var portrait = new TextureRect { Texture = concealed ? null : GD.Load<Texture2D>(AssetPaths.OfficerPortrait(officerId)), CustomMinimumSize = new Vector2(76, 60), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; box.AddChild(portrait);
        var color = player ? GameTheme.Success : GameTheme.Danger;
        var name = new Label { Text = concealed ? "敌将未明" : officer?.Profile.Name, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        name.AddThemeFontSizeOverride("font_size", 11); name.AddThemeColorOverride("font_color", color); box.AddChild(name);
        var role = pending.OfficerRoles.GetValueOrDefault(officerId, "武将");
        var roleLabel = new Label { Text = concealed ? "情报不足" : role, HorizontalAlignment = HorizontalAlignment.Center, MouseDefaultCursorShape = CursorShape.Help, MouseFilter = MouseFilterEnum.Stop };
        roleLabel.AddThemeFontSizeOverride("font_size", 11); roleLabel.AddThemeColorOverride("font_color", color);
        if (!concealed && officer is not null)
        {
            var tooltip = OfficerRoleTooltip(officer, role, pending, player);
            roleLabel.MouseEntered += () => ShowTraitTooltip(tooltip);
            roleLabel.MouseExited += HideTraitTooltip;
        }
        box.AddChild(roleLabel);
        return box;
    }

    private static string OfficerRoleTooltip(ScenarioOfficerData officer, string role, PendingBattleData pending, bool player)
    {
        var sideIds = player
            ? pending.PlayerSide == "attacker" ? pending.AttackerOfficerIds : pending.DefenderOfficerIds
            : pending.PlayerSide == "attacker" ? pending.DefenderOfficerIds : pending.AttackerOfficerIds;
        var roleIndex = Math.Max(0, sideIds.IndexOf(officer.Profile.Id));
        var roleEffect = roleIndex switch
        {
            0 => "统率与魅力提高全军各阶段战力",
            1 => "武力与统率提高前线突破能力",
            _ => "智力与统率提高远程及计策执行能力",
        };
        var lines = new List<string>
        {
            role,
            $"职责功效：{roleEffect}，当前战力贡献 +{BattleCalculator.OfficerRoleContribution(officer, roleIndex):P1}",
        };
        var traits = OfficerProgressionRules.AllTraits(officer)
            .Select(id => OfficerProgressionRules.Traits.GetValueOrDefault(id))
            .Where(item => item is not null)
            .Cast<TraitDefinition>()
            .ToList();
        if (traits.Count == 0)
        {
            lines.Add("个人特性：无");
            return string.Join('\n', lines);
        }
        lines.Add("个人特性：");
        foreach (var trait in traits)
        {
            var effects = new List<string>();
            if (trait.DomesticModifier > 0) effects.Add($"内政 +{trait.DomesticModifier:P0}");
            if (trait.BattleModifier > 0) effects.Add($"战斗 +{trait.BattleModifier:P0}（{string.Join('、', trait.BattleStages)}）");
            if (!string.IsNullOrEmpty(trait.RequiredRoleToken)) effects.Add($"需担任{trait.RequiredRoleToken}");
            if (!string.IsNullOrEmpty(trait.RequiredTroopType)) effects.Add($"需{BattleCatalog.TroopName(trait.RequiredTroopType)}至少500人且占全军20%" );
            if (!string.IsNullOrEmpty(trait.RequiredSpecialTroopId) && OfficerProgressionRules.SpecialTroops.TryGetValue(trait.RequiredSpecialTroopId, out var special)) effects.Add($"需{special.Name}至少500人且占全军20%" );
            lines.Add($"· {trait.Name}（{trait.Quality}）：{trait.Description}{(effects.Count == 0 ? "" : $"［{string.Join("；", effects)}］")}");
        }
        return string.Join('\n', lines);
    }

    private void RefreshTacticBrief()
    {
        if (_tacticBrief is null) return;
        var pending = Runtime.State.PendingBattle;
        var composition = pending?.Groups
            .Where(item => item.Side == pending.PlayerSide)
            .GroupBy(item => item.TroopType)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.InitialSoldiers)) ?? [];
        var requirement = BattleCalculator.TacticRequirement(Selected(_battleTactic), composition);
        _tacticBrief.Text = $"{BattleCalculator.StanceEffectSummary(Selected(_battleStance))}　·　主战术：{BattleCalculator.TacticEffectSummary(Selected(_battleTactic))}\n" + (string.IsNullOrEmpty(requirement) ? "兵种条件满足，可以下令开战。" : requirement);
        _tacticBrief.AddThemeColorOverride("font_color", string.IsNullOrEmpty(requirement) ? GameTheme.Muted : GameTheme.Danger);
        if (_start is not null) _start.Disabled = pending?.Status != "planning" || !string.IsNullOrEmpty(requirement);
    }

    private bool HasPreciseBattleIntel(PendingBattleData pending)
    {
        var city = Runtime.City(pending.CityId);
        return pending.PlayerSide == "defender" || pending.BattleType == "field" || (city?.IntelligenceAge ?? 99) <= 1;
    }

    private static void FillTactics(OptionButton option)
    {
        foreach (var id in new[] { "steady-advance", "shield-wall", "feigned-retreat", "night-raid", "fire-attack", "encirclement", "arrow-volley", "cavalry-charge", "fortify-camp", "cut-supply", "siege-ladders", "undermine-walls" })
            AddChoice(option, BattleCatalog.TacticName(id), id);
    }

    private static void FillOrder(OptionButton option, string prefix, (string Id, string Label)[] values) { foreach (var item in values) AddChoice(option, $"{prefix}·{item.Label}", item.Id); }
    private static void Select(OptionButton option, string value) { for (var index = 0; index < option.ItemCount; index++) if (option.GetItemMetadata(index).AsString() == value) { option.Select(index); return; } }
}

public partial class BattleReportView : FeatureScreen
{
    public event Action? Confirmed;

    private PanelContainer _resultPanel = null!, _reportPanel = null!;
    private Label _empty = null!, _resultTitle = null!, _resultSubtitle = null!, _resultStats = null!, _resultNarrative = null!, _report = null!;
    private Button _confirm = null!;

    protected override void Build()
    {
        // 战报页不需要通用通知栏；空通知会在结果卡上方留下无意义的占位。
        Notice.Visible = false;
        _empty = new Label
        {
            Text = "尚无已结束的战斗。战斗完成后，本界面会展示刚刚结束的这一场战报。",
            CustomMinimumSize = new Vector2(0, 180),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _empty.AddThemeColorOverride("font_color", GameTheme.Muted);
        Body.AddChild(Panel(_empty));

        _resultPanel = new PanelContainer { CustomMinimumSize = new Vector2(0, 500), Visible = false };
        _resultPanel.AddThemeStyleboxOverride("panel", GameTheme.Box(new Color(GameTheme.PanelRaised, .98f), new Color(GameTheme.Bronze, .66f), 12, 2));
        UiOrnaments.AttachInkCorners(_resultPanel, 300, .13f);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 54); margin.AddThemeConstantOverride("margin_right", 54); margin.AddThemeConstantOverride("margin_top", 32); margin.AddThemeConstantOverride("margin_bottom", 32); _resultPanel.AddChild(margin);
        var layout = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center }; layout.AddThemeConstantOverride("separation", 14); margin.AddChild(layout);
        _resultTitle = new Label { Text = "胜　利", CustomMinimumSize = new Vector2(0, 92), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _resultTitle.AddThemeFontSizeOverride("font_size", 62); layout.AddChild(_resultTitle);
        _resultSubtitle = new Label { CustomMinimumSize = new Vector2(0, 40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _resultSubtitle.AddThemeFontSizeOverride("font_size", 22); _resultSubtitle.AddThemeColorOverride("font_color", GameTheme.Paper); layout.AddChild(_resultSubtitle);
        _resultStats = new Label { CustomMinimumSize = new Vector2(0, 78), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _resultStats.AddThemeFontSizeOverride("font_size", 18); _resultStats.AddThemeColorOverride("font_color", GameTheme.Bronze); layout.AddChild(_resultStats);
        _resultNarrative = new Label { CustomMinimumSize = new Vector2(0, 112), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _resultNarrative.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(_resultNarrative);
        _confirm = GameTheme.Button("确认战报，返回天下");
        _confirm.CustomMinimumSize = new Vector2(360, 58); _confirm.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; _confirm.AddThemeFontSizeOverride("font_size", 21);
        _confirm.AddThemeColorOverride("font_color", GameTheme.OnAccent); _confirm.AddThemeColorOverride("font_hover_color", GameTheme.OnAccent); _confirm.AddThemeColorOverride("font_pressed_color", GameTheme.OnAccent);
        _confirm.AddThemeStyleboxOverride("normal", GameTheme.Box(GameTheme.Cinnabar, new Color(GameTheme.Bronze, .88f), 8, 2, 22, 10));
        _confirm.AddThemeStyleboxOverride("hover", GameTheme.Box(Color.FromHtml("#b95b4a"), GameTheme.Cinnabar, 8, 2, 22, 10));
        _confirm.Pressed += () => Confirmed?.Invoke(); layout.AddChild(_confirm);
        Body.AddChild(_resultPanel);

        _report = new Label { CustomMinimumSize = new Vector2(0, 360), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _report.AddThemeColorOverride("font_color", GameTheme.Muted);
        _reportPanel = Panel(_report); _reportPanel.Visible = false; Body.AddChild(_reportPanel);
    }

    public override void Refresh()
    {
        if (_report is null) return;
        var report = Runtime.State.BattleReports.LastOrDefault();
        _empty.Visible = report is null;
        _resultPanel.Visible = report is not null;
        _reportPanel.Visible = report is not null;
        if (report is null) return;
        ShowBattleResult(report);
        _report.Text = "本场详细战报\n\n" + ReportText(report);
        _confirm.Text = Runtime.State.PendingBattle is null ? "确认战报，返回天下" : "确认战报，进入下一场战斗";
        ContentScroll.ScrollVertical = 0;
    }

    private void ShowBattleResult(BattleReportData report)
    {
        var undecided = report.Result == "stalemate";
        var won = !undecided && (report.PlayerSide == "attacker" ? report.Result == "victory" : report.Result == "defeat");
        _resultTitle.Text = undecided ? "战 事 未 决" : won ? "胜　利" : "失　败";
        _resultTitle.AddThemeColorOverride("font_color", undecided ? GameTheme.Bronze : won ? GameTheme.Success : GameTheme.Danger);
        _resultSubtitle.Text = report.BattleType == "field"
            ? $"第{report.Turn}回合 · {report.CityName}军团野战 · 双方收兵回营"
            : $"第{report.Turn}回合 · {report.CityName}攻防战 · {(report.CityCaptured ? "城池易手" : undecided ? "转入持续围城" : "城池未失")}";
        var playerBefore = report.PlayerSide == "attacker" ? report.AttackerBefore : report.DefenderBefore;
        var playerAfter = report.PlayerSide == "attacker" ? report.AttackerAfter : report.DefenderAfter;
        var playerLoss = report.PlayerSide == "attacker" ? report.AttackerLosses : report.DefenderLosses;
        var enemyBefore = report.PlayerSide == "attacker" ? report.DefenderBefore : report.AttackerBefore;
        var enemyAfter = report.PlayerSide == "attacker" ? report.DefenderAfter : report.AttackerAfter;
        var enemyLoss = report.PlayerSide == "attacker" ? report.DefenderLosses : report.AttackerLosses;
        var defenseLine = report.WallBefore == 0 && report.GateBefore == 0
            ? "野外交锋 · 双方军团正面接战"
            : $"外墙 {report.WallBefore}%→{report.WallAfter}%　城门 {report.GateBefore}%→{report.GateAfter}%　内城 {report.InnerBefore}%→{report.InnerAfter}%";
        _resultStats.Text = $"我军 {playerBefore:N0} → {playerAfter:N0}　损失 {playerLoss:N0}　　　　敌军 {enemyBefore:N0} → {enemyAfter:N0}　损失 {enemyLoss:N0}\n{defenseLine}";
        _resultNarrative.Text = report.Narrative;
    }

    private string ReportText(BattleReportData report)
    {
        var result = report.Result == "stalemate" ? "持续围城" : report.PlayerSide == "attacker" ? report.Result == "victory" ? "胜利" : "失利" : report.Result == "victory" ? "失利" : "胜利";
        var contributions = report.OfficerContributions.Count == 0 ? "" : "\n武将贡献：\n" + string.Join('\n', report.OfficerContributions.Select(item => $"· {Runtime.Officer(item.Key)?.Profile.Name}：{item.Value}"));
        var playerComposition = report.PlayerSide == "attacker" ? report.AttackerComposition : report.DefenderComposition;
        var composition = string.Join('、', playerComposition.Select(item => $"{BattleCatalog.TroopName(item.Key)}{item.Value:N0}"));
        var playerBefore = report.PlayerSide == "attacker" ? report.AttackerBefore : report.DefenderBefore; var playerAfter = report.PlayerSide == "attacker" ? report.AttackerAfter : report.DefenderAfter; var playerLoss = report.PlayerSide == "attacker" ? report.AttackerLosses : report.DefenderLosses;
        var enemyBefore = report.PlayerSide == "attacker" ? report.DefenderBefore : report.AttackerBefore; var enemyAfter = report.PlayerSide == "attacker" ? report.DefenderAfter : report.AttackerAfter; var enemyLoss = report.PlayerSide == "attacker" ? report.DefenderLosses : report.AttackerLosses;
        var primaryTactic = string.IsNullOrEmpty(report.PrimaryTactic) ? report.Tactic : report.PrimaryTactic;
        var phases = report.PhaseResults.Count == 0 ? "" : "\n阶段结算：\n" + string.Join('\n', report.PhaseResults.Select(phase => $"· {phase.Stage}：我军损失{(report.PlayerSide == "attacker" ? phase.AttackerLosses : phase.DefenderLosses):N0}，敌军损失{(report.PlayerSide == "attacker" ? phase.DefenderLosses : phase.AttackerLosses):N0}，士气{(report.PlayerSide == "attacker" ? phase.AttackerMorale : phase.DefenderMorale):F0}:{(report.PlayerSide == "attacker" ? phase.DefenderMorale : phase.AttackerMorale):F0}，溃散{(report.PlayerSide == "attacker" ? phase.AttackerRouted : phase.DefenderRouted)}:{(report.PlayerSide == "attacker" ? phase.DefenderRouted : phase.AttackerRouted)}，实时战力比{phase.PowerRatio:F2}"));
        var title = report.BattleType == "field" ? "军团野战" : "攻防战";
        var battlefield = report.BattleType == "field" ? "战场：野外接敌，无城墙与城门加成" : $"城防：外墙 {report.WallBefore}%→{report.WallAfter}%　城门 {report.GateBefore}%→{report.GateAfter}%　内城 {report.InnerBefore}%→{report.InnerAfter}%";
        return $"第{report.Turn}回合 · {report.CityName}{title} · {result}\n我军编成：{composition}\n我军 {playerBefore:N0} → {playerAfter:N0}（损失{playerLoss:N0}）　敌军 {enemyBefore:N0} → {enemyAfter:N0}（损失{enemyLoss:N0}）\n{battlefield}\n阵型：{BattleCatalog.FormationName(report.FormationId)}　姿态：{report.Stance}　战术：{BattleCatalog.TacticName(primaryTactic)}　城池易手：{(report.CityCaptured ? "是" : "否")}\n{report.Narrative}{phases}{contributions}";
    }
}

public partial class SaveView : FeatureScreen
{
    private GridContainer _grid = null!;
    protected override void Build() { _grid = new GridContainer { Columns = 5, CustomMinimumSize = new Vector2(0, 600) }; _grid.AddThemeConstantOverride("h_separation", 10); _grid.AddThemeConstantOverride("v_separation", 10); Body.AddChild(_grid); }
    public override void Refresh()
    {
        if (_grid is null) return; foreach (var child in _grid.GetChildren()) child.QueueFree(); var summaries = SaveService.List();
        for (var slot = 1; slot <= 10; slot++)
        {
            var current = summaries.FirstOrDefault(item => item.Kind == "manual" && item.Slot == slot); var box = new VBoxContainer { CustomMinimumSize = new Vector2(250, 130) }; box.AddChild(Text($"手动档 {slot}\n{(current is null ? "空" : $"{current.Year}年{current.Month}月 · 第{current.Turn}回合")}", 240)); var row = Row(); var captured = slot; var save = GameTheme.Button(current is null ? "保存" : "覆盖"); save.Pressed += () => { SaveService.WriteManual(Runtime.State, captured); Notice.Text = $"已保存到手动档{captured}。"; Refresh(); }; row.AddChild(save); var load = GameTheme.Button("载入"); load.Disabled = current is null; load.Pressed += () => { var state = SaveService.Load("manual", captured); if (state is not null) Runtime.Replace(state); }; row.AddChild(load); box.AddChild(row); _grid.AddChild(Panel(box));
        }
        for (var slot = 1; slot <= 3; slot++) { var current = summaries.FirstOrDefault(item => item.Kind == "auto" && item.Slot == slot); var box = new VBoxContainer { CustomMinimumSize = new Vector2(250, 110) }; box.AddChild(Text($"自动档 {slot}\n{(current is null ? "空" : $"{current.Year}年{current.Month}月 · 第{current.Turn}回合")}", 240)); var captured = slot; var load = GameTheme.Button("载入自动档"); load.Disabled = current is null; load.Pressed += () => { var state = SaveService.Load("auto", captured); if (state is not null) Runtime.Replace(state); }; box.AddChild(load); _grid.AddChild(Panel(box)); }
    }
}
