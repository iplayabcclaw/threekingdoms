using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class NewGameSetupView : Control
{
    public event Action<NewGameOptions>? StartRequested;

    private ScenarioData _scenario = null!;
    private OptionButton _faction = null!;
    private OptionButton _difficulty = null!;
    private OptionButton _autoSave = null!;
    private Label _factionDetails = null!;
    private Label _ruleDetails = null!;

    public void Initialize(ScenarioData scenario)
    {
        _scenario = scenario;
        Build();
        RefreshPreview();
    }

    private void Build()
    {
        var background = new ColorRect { Color = GameTheme.Backdrop };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var panel = new PanelContainer
        {
            AnchorLeft = .5f,
            AnchorTop = .5f,
            AnchorRight = .5f,
            AnchorBottom = .5f,
            OffsetLeft = -520,
            OffsetTop = -360,
            OffsetRight = 520,
            OffsetBottom = 360,
        };
        panel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(14));
        AddChild(panel);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 16);
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 30); margin.AddThemeConstantOverride("margin_right", 30); margin.AddThemeConstantOverride("margin_top", 22); margin.AddThemeConstantOverride("margin_bottom", 24); margin.AddChild(layout); panel.AddChild(margin);

        var title = new Label { Text = "三国：山河逐鹿", CustomMinimumSize = new Vector2(0, 58), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 38);
        title.AddThemeColorOverride("font_color", GameTheme.Paper);
        layout.AddChild(title);

        var subtitle = new Label { Text = $"{_scenario.Name} · {_scenario.Year}年{_scenario.Month}月 · 选择你的势力与开局规则", HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeColorOverride("font_color", GameTheme.Muted);
        layout.AddChild(subtitle);

        var selectors = new GridContainer { Columns = 2 };
        selectors.AddThemeConstantOverride("h_separation", 18);
        selectors.AddThemeConstantOverride("v_separation", 14);
        selectors.AddChild(FieldLabel("玩家势力"));
        _faction = Choice();
        foreach (var faction in _scenario.Factions)
        {
            var cityCount = _scenario.Cities.Count(city => city.OwnerFactionId == faction.Id);
            _faction.AddItem($"{faction.Name} · {faction.RulerName} · {cityCount}城");
            _faction.SetItemMetadata(_faction.ItemCount - 1, faction.Id);
        }
        SelectMetadata(_faction, _scenario.PlayerFactionId);
        selectors.AddChild(_faction);

        selectors.AddChild(FieldLabel("游戏难度"));
        _difficulty = Choice();
        AddChoice(_difficulty, "标准 · 原始资源", "standard");
        AddChoice(_difficulty, "宽松 · 初始钱粮装备 +20%", "relaxed");
        AddChoice(_difficulty, "艰难 · 初始钱粮装备 -15%", "hard");
        selectors.AddChild(_difficulty);

        selectors.AddChild(FieldLabel("自动存档"));
        _autoSave = Choice();
        AddChoice(_autoSave, "每月", "monthly");
        AddChoice(_autoSave, "每三个月", "quarterly");
        AddChoice(_autoSave, "每年", "yearly");
        AddChoice(_autoSave, "关闭", "off");
        selectors.AddChild(_autoSave);
        layout.AddChild(selectors);

        _factionDetails = DetailLabel(120);
        layout.AddChild(Panel(_factionDetails));
        _ruleDetails = DetailLabel(92);
        layout.AddChild(Panel(_ruleDetails));

        var start = GameTheme.Button("执掌势力　·　开始游戏");
        start.CustomMinimumSize = new Vector2(0, 54);
        start.AddThemeFontSizeOverride("font_size", 18);
        start.AddThemeColorOverride("font_color", GameTheme.OnAccent);
        start.AddThemeColorOverride("font_hover_color", GameTheme.OnAccent);
        start.AddThemeColorOverride("font_pressed_color", GameTheme.OnAccent);
        start.AddThemeColorOverride("font_focus_color", GameTheme.OnAccent);
        start.AddThemeStyleboxOverride("normal", GameTheme.Box(GameTheme.Jade, new Color(GameTheme.Bronze, .72f), 7, 1, 18, 10));
        start.AddThemeStyleboxOverride("hover", GameTheme.Box(Color.FromHtml("#5b8177"), GameTheme.Jade, 7, 2, 17, 9));
        start.Pressed += Start;
        layout.AddChild(start);

        _faction.ItemSelected += _ => RefreshPreview();
        _difficulty.ItemSelected += _ => RefreshPreview();
        _autoSave.ItemSelected += _ => RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_faction is null || _faction.Selected < 0) return;
        var factionId = Selected(_faction);
        var faction = _scenario.Factions.First(item => item.Id == factionId);
        var cities = _scenario.Cities.Where(city => city.OwnerFactionId == factionId).ToList();
        var officers = _scenario.Officers.Where(officer => officer.InitialState.FactionId == factionId && officer.InitialState.Alive).ToList();
        var resources = GameSession.PreviewInitialResources(_scenario, factionId, Selected(_difficulty));
        _factionDetails.Text = $"{faction.Name}　君主 {faction.RulerName}\n初始城池：{string.Join('、', cities.Select(city => city.Name))}\n人才 {officers.Count} 人　势力府库：金 {resources.Gold:N0} / 粮 {resources.Food:N0} / 装备 {resources.Equipment:N0} / 威望 {resources.Prestige:N0}";
        _ruleDetails.Text = $"胜利：统一天下立即获胜；或控制至少{GameSession.StrategicVictoryCityCount}城并连续维持{GameSession.StrategicVictoryRequiredMonths}个月。　失败：失去全部城池。\n难度：{DifficultyLabel(Selected(_difficulty))}　自动存档：{AutoSaveLabel(Selected(_autoSave))}";
    }

    private void Start()
    {
        StartRequested?.Invoke(new NewGameOptions
        {
            PlayerFactionId = Selected(_faction),
            Difficulty = Selected(_difficulty),
            AutoSaveFrequency = Selected(_autoSave),
        });
    }

    private static Label FieldLabel(string text)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(180, 44), VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeColorOverride("font_color", GameTheme.Paper);
        return label;
    }

    private static OptionButton Choice() => new() { CustomMinimumSize = new Vector2(720, 44), MouseDefaultCursorShape = CursorShape.PointingHand };
    private static Label DetailLabel(float height) => new() { CustomMinimumSize = new Vector2(0, height), AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center };

    private static PanelContainer Panel(Control child)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", GameTheme.PanelBox(8));
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 16); margin.AddThemeConstantOverride("margin_right", 16); margin.AddThemeConstantOverride("margin_top", 8); margin.AddThemeConstantOverride("margin_bottom", 8); margin.AddChild(child); panel.AddChild(margin);
        return panel;
    }

    private static void AddChoice(OptionButton option, string label, string value)
    {
        option.AddItem(label);
        option.SetItemMetadata(option.ItemCount - 1, value);
    }

    private static string Selected(OptionButton option) => option.Selected < 0 ? string.Empty : option.GetItemMetadata(option.Selected).AsString();

    private static void SelectMetadata(OptionButton option, string value)
    {
        for (var index = 0; index < option.ItemCount; index++)
        {
            if (option.GetItemMetadata(index).AsString() == value) { option.Select(index); return; }
        }
    }

    private static string DifficultyLabel(string value) => value switch { "relaxed" => "宽松", "hard" => "艰难", _ => "标准" };
    private static string AutoSaveLabel(string value) => value switch { "quarterly" => "每三个月", "yearly" => "每年", "off" => "关闭", _ => "每月" };
}
