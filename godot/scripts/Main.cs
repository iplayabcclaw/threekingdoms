using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class Main : Control
{
    private readonly ScenarioData _scenario = ScenarioLoader.LoadFirstRivalry();
    private readonly Dictionary<string, Control> _screens = [];
    private readonly Dictionary<string, Button> _navigationButtons = [];
    private GameRuntime _runtime = null!;
    private WorldMapView _worldMap = null!;
    private CityManagementView _cityManagement = null!;
    private ExpeditionView _expedition = null!;
    private BattleView _battleView = null!;
    private Control _current = null!;
    private ColorRect _fade = null!;
    private Label _toast = null!;
    private PanelContainer? _eventPanel;
    private AudioStreamPlayer _music = null!;
    private bool _soundEnabled = true;
    private bool _battleMusicActive;
    private bool _reduceMotion;
    private int _trackIndex;
    private bool _smokeMode;
    private bool _transitioning;
    private double _autoTimer;

    public override void _Ready()
    {
        var userArgs = OS.GetCmdlineUserArgs();
        if (userArgs.Contains("--runtime-self-test"))
        {
            RuntimeSelfTest.Run(_scenario);
            GetTree().Quit();
            return;
        }
        var battleVisualTest = userArgs.Contains("--battle-visual-test");
        var uiVisualTest = userArgs.Contains("--ui-visual-test");
        _smokeMode = userArgs.Contains("--smoke-test") || battleVisualTest || uiVisualTest;
        Theme = GameTheme.Create();
        if (_smokeMode) StartGame(new NewGameOptions(), battleVisualTest, uiVisualTest);
        else ShowNewGameSetup();
    }

    private void ShowNewGameSetup()
    {
        var setup = new NewGameSetupView();
        setup.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(setup);
        setup.Initialize(_scenario);
        setup.StartRequested += options =>
        {
            RemoveChild(setup);
            setup.QueueFree();
            StartGame(options, false, false);
        };
    }

    private void StartGame(NewGameOptions options, bool battleVisualTest, bool uiVisualTest)
    {
        _runtime = new GameRuntime(_scenario, options);
        _runtime.Notice += ShowNotice;
        _runtime.Changed += OnGameChanged;

        var sceneHost = new Control { Name = "PreloadedSceneHost", MouseFilter = MouseFilterEnum.Pass };
        sceneHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(sceneHost);
        _worldMap = GD.Load<PackedScene>("res://scenes/world_map.tscn").Instantiate<WorldMapView>();
        _cityManagement = GD.Load<PackedScene>("res://scenes/city_management.tscn").Instantiate<CityManagementView>();
        AddScreen(sceneHost, "world", _worldMap);
        AddScreen(sceneHost, "city", _cityManagement);
        _expedition = new ExpeditionView();
        AddFeature(sceneHost, "expedition", _expedition, "军事出征", "配置混合兵种与出战武将；军团抵达后进入战前布阵和正式战斗画面。");
        AddFeature(sceneHost, "diplomacy", new DiplomacyView(), "纵横捭阖", "通商带来持续收益，停战约束双方出征，俘虏交换需要双方均有可释放武将。");
        AddFeature(sceneHost, "talent", new TalentView(), "人才府", "登庸、劝降、策反、任命与武将调动共用完整武将状态。");
        AddFeature(sceneHost, "ai", new AiCouncilView(), "军师府 · AI 托管", "分别控制内政、人才、外交和军事代理，也可启动全势力自动演进。");
        _battleView = new BattleView();
        AddFeature(sceneHost, "battle", _battleView, "沙场演武", "布置前中后军与左右翼，实时点选或框选战斗队，下达移动、集火和固守军令。");
        AddFeature(sceneHost, "save", new SaveView(), "存档管理", "3 个循环自动档与 10 个手动档，保存完整局势、事件、军团、外交与设置。");

        _worldMap.Initialize(_runtime);
        _cityManagement.Initialize(_runtime);
        _worldMap.OpenCityRequested += OpenCity;
        _worldMap.ExpeditionRequested += OpenExpedition;
        _worldMap.ArmyInterceptRequested += OpenArmyIntercept;
        _worldMap.ArmyMovementFinished += OnArmyMovementFinished;
        _expedition.ArmyMovementRequested += ShowArmyMovement;
        _cityManagement.BackRequested += () => Navigate("world");
        _current = _worldMap;
        foreach (var screen in _screens.Values) screen.Visible = screen == _current;

        BuildNavigation();
        BuildOverlays();
        if (!_smokeMode) BuildAudio();
        ShowNotice("游戏已载入。", false);
        if (battleVisualTest) CallDeferred(nameof(RunBattleVisualTest));
        else if (uiVisualTest) CallDeferred(nameof(RunUiVisualTest));
        else if (_smokeMode) CallDeferred(nameof(QuitSmoke));
    }

    public override void _Process(double delta)
    {
        if (_runtime is null) return;
        if (!_runtime.State.AutoEvolution.Enabled || _runtime.State.PendingEvent is not null || _runtime.State.PendingBattle is not null || _runtime.State.Outcome != "ongoing") return;
        _autoTimer += delta;
        var interval = _runtime.State.AutoEvolution.Speed switch { "slow" => 1.5, "fast" => .18, _ => .65 };
        if (_autoTimer < interval) return;
        _autoTimer = 0;
        _runtime.EndTurn();
    }

    private void AddScreen(Control host, string id, Control screen)
    {
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); host.AddChild(screen); _screens[id] = screen;
    }

    private void AddFeature(Control host, string id, FeatureScreen screen, string title, string subtitle)
    {
        AddScreen(host, id, screen); screen.Initialize(_runtime, title, subtitle);
    }

    private void BuildNavigation()
    {
        var panel = new PanelContainer { AnchorLeft = 0, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 14, OffsetTop = -64, OffsetRight = -14, OffsetBottom = -5, ZIndex = 80, MouseFilter = MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(9)); AddChild(panel);
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 6); margin.AddThemeConstantOverride("margin_right", 6); margin.AddThemeConstantOverride("margin_top", 5); margin.AddThemeConstantOverride("margin_bottom", 5); panel.AddChild(margin);
        var row = new HFlowContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; row.AddThemeConstantOverride("h_separation", 6); row.AddThemeConstantOverride("v_separation", 5); margin.AddChild(row);
        foreach (var item in new[] { ("world", "天下"), ("city", "内政"), ("talent", "人才"), ("diplomacy", "外交"), ("expedition", "出征"), ("battle", "战报"), ("ai", "军师"), ("save", "存档") })
        {
            var button = GameTheme.Button(item.Item2); button.CustomMinimumSize = new Vector2(88, 40); button.ToggleMode = true; var id = item.Item1; button.Pressed += () => { if (id == "city") _cityManagement.ShowOverview(); Navigate(id); }; _navigationButtons[id] = button; row.AddChild(button);
        }
        var endTurn = GameTheme.Button("结束本月"); endTurn.CustomMinimumSize = new Vector2(130, 40); endTurn.AddThemeColorOverride("font_color", GameTheme.OnAccent); endTurn.AddThemeColorOverride("font_hover_color", GameTheme.OnAccent); endTurn.AddThemeColorOverride("font_pressed_color", GameTheme.OnAccent); endTurn.AddThemeColorOverride("font_focus_color", GameTheme.OnAccent); endTurn.AddThemeStyleboxOverride("normal", GameTheme.Box(GameTheme.Cinnabar, new Color(GameTheme.Bronze, .82f), 6, 1, 14, 8)); endTurn.AddThemeStyleboxOverride("hover", GameTheme.Box(Color.FromHtml("#b95b4a"), GameTheme.Cinnabar, 6, 2, 13, 7)); endTurn.Pressed += () => _runtime.EndTurn(); row.AddChild(endTurn);
        var sound = GameTheme.Button("声音开"); sound.CustomMinimumSize = new Vector2(80, 40); sound.Pressed += () => { _soundEnabled = !_soundEnabled; sound.Text = _soundEnabled ? "声音开" : "声音关"; if (_music is not null) { if (_soundEnabled) _music.Play(); else _music.Stop(); } }; row.AddChild(sound);
        var motion = GameTheme.Button("动效开"); motion.CustomMinimumSize = new Vector2(80, 40); motion.Pressed += () => { _reduceMotion = !_reduceMotion; motion.Text = _reduceMotion ? "动效关" : "动效开"; _battleView?.SetReducedMotion(_reduceMotion); }; row.AddChild(motion);
        var quit = GameTheme.Button("退出"); quit.CustomMinimumSize = new Vector2(65, 40); quit.Pressed += () => GetTree().Quit(); row.AddChild(quit);
        UpdateNavigationState();
    }

    private void BuildOverlays()
    {
        _toast = new Label { AnchorLeft = .5f, AnchorTop = 1, AnchorRight = .5f, AnchorBottom = 1, OffsetLeft = -410, OffsetRight = 410, OffsetTop = -116, OffsetBottom = -72, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ZIndex = 90, MouseFilter = MouseFilterEnum.Ignore };
        _toast.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.PanelRaised, .97f), new Color(GameTheme.Bronze, .66f), 7)); _toast.AddThemeColorOverride("font_color", GameTheme.Paper); AddChild(_toast);
        _fade = new ColorRect { Name = "SceneFade", Color = new Color(GameTheme.Ink, 0), MouseFilter = MouseFilterEnum.Ignore, ZIndex = 100 }; _fade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_fade);
    }

    private void BuildAudio()
    {
        _music = new AudioStreamPlayer { VolumeDb = -13 };
        AddChild(_music);
        _music.Finished += () =>
        {
            if (_battleMusicActive) PlayBattleTrack();
            else { _trackIndex = (_trackIndex + 1) % AssetPaths.AmbientMusic.Count; PlayTrack(); }
        };
        UpdateMusicForGameState();
    }

    private void PlayTrack()
    {
        _music.Stream = GD.Load<AudioStream>(AssetPaths.AmbientMusic[_trackIndex]);
        if (_soundEnabled && _music.Stream is not null) _music.Play();
    }

    private void PlayBattleTrack()
    {
        _music.Stream = GD.Load<AudioStream>(AssetPaths.BattleMusic);
        if (_soundEnabled && _music.Stream is not null) _music.Play();
    }

    private void UpdateMusicForGameState()
    {
        if (_music is null || _runtime is null) return;
        var shouldPlayBattleMusic = _runtime.State.PendingBattle is not null;
        if (shouldPlayBattleMusic == _battleMusicActive && _music.Stream is not null) return;
        _battleMusicActive = shouldPlayBattleMusic;
        if (_battleMusicActive) PlayBattleTrack();
        else PlayTrack();
    }

    private async void QuitSmoke()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit();
    }

    private async void RunBattleVisualTest()
    {
        var fieldSource = _runtime.State.Cities
            .Where(city => city.OwnerFactionId == _runtime.State.PlayerFactionId && city.Garrison >= 7000)
            .First(city => _runtime.State.Roads.Any(road => (road.FromCityId == city.Id && _runtime.City(road.ToCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId) || (road.ToCityId == city.Id && _runtime.City(road.FromCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId)));
        var enemyCity = _runtime.State.Cities.First(city => city.OwnerFactionId != _runtime.State.PlayerFactionId && _runtime.State.Officers.Any(officer => officer.InitialState.FactionId == city.OwnerFactionId && officer.InitialState.CityId == city.Id && officer.InitialState.Status == "serving"));
        var enemyCommander = _runtime.State.Officers.First(officer => officer.InitialState.FactionId == enemyCity.OwnerFactionId && officer.InitialState.CityId == enemyCity.Id && officer.InitialState.Status == "serving");
        var enemyArmy = new ArmyData { Id = "visual-enemy-army", FactionId = enemyCity.OwnerFactionId!, SourceCityId = enemyCity.Id, TargetCityId = fieldSource.Id, CommanderId = enemyCommander.Profile.Id, Soldiers = 3700, Food = 6000, Training = 70, Morale = 72, Composition = new Dictionary<string, int> { ["infantry"] = 2200, ["archers"] = 1000, ["cavalry"] = 500 }, Status = "marching", RemainingDays = 12, TotalDays = 60 };
        enemyCommander.InitialState.Status = "deployed"; enemyCommander.InitialState.ArmyId = enemyArmy.Id; _runtime.State.Armies.Add(enemyArmy);
        var fieldCommander = _runtime.PlayerOfficers().Where(item => item.InitialState.CityId == fieldSource.Id && item.InitialState.Status == "serving").OrderByDescending(item => _runtime.EffectiveAbility(item, "leadership", "military")).First();
        _runtime.CreateExpedition(fieldSource.Id, fieldSource.Id, fieldCommander.Profile.Id, 4000, 6000, "standard", "encirclement", [], new Dictionary<string, int> { ["infantry"] = 2500, ["spears"] = 750, ["archers"] = 750 }, [], enemyArmy.Id);
        Navigate("battle");
        await ToSignal(GetTree().CreateTimer(.7), SceneTreeTimer.SignalName.Timeout);
        _runtime.ConfigurePendingBattle("goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["spears"] = "spear-wall", ["archers"] = "rear-double" });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var planningImage = GetViewport().GetTexture().GetImage();
        var planningError = planningImage.SavePng("/tmp/three-kingdoms-battle-planning-test.png");
        _runtime.StartPendingBattle();
        _battleView.SetPlaybackForVisualTest(4.8);
        var volley = _runtime.State.PendingBattle?.Timeline.FirstOrDefault(item => item.Action == "volley");
        if (volley is not null) _battleView.SetPlaybackForVisualTest(volley.Start + volley.Duration * .52);
        await ToSignal(GetTree().CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var fieldImage = GetViewport().GetTexture().GetImage();
        var fieldError = fieldImage.SavePng("/tmp/three-kingdoms-battle-field-test.png");
        _runtime.CompletePendingBattle();

        var source = _runtime.State.Cities
            .Where(city => city.OwnerFactionId == _runtime.State.PlayerFactionId && city.Garrison >= 3000)
            .First(city => _runtime.State.Roads.Any(road => (road.FromCityId == city.Id && _runtime.City(road.ToCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId) || (road.ToCityId == city.Id && _runtime.City(road.FromCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId)));
        var road = _runtime.State.Roads.First(item => (item.FromCityId == source.Id && _runtime.City(item.ToCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId) || (item.ToCityId == source.Id && _runtime.City(item.FromCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId));
        var targetId = road.FromCityId == source.Id ? road.ToCityId : road.FromCityId;
        var commander = _runtime.PlayerOfficers().Where(item => item.InitialState.CityId == source.Id && item.InitialState.Status == "serving").OrderByDescending(item => _runtime.EffectiveAbility(item, "leadership", "military")).First();
        _runtime.CreateExpedition(source.Id, targetId, commander.Profile.Id, 3000, 5000, "standard", "arrow-volley", [], new Dictionary<string, int> { ["infantry"] = 2000, ["spears"] = 500, ["archers"] = 500 });
        var army = _runtime.State.Armies.Last();
        while (_runtime.State.PendingBattle is null && army.Status == "marching") { _runtime.State.Turn++; _runtime.MarchArmy(army.Id); }
        Navigate("battle");
        _runtime.ConfigurePendingBattle("goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["spears"] = "spear-wall", ["archers"] = "rear-double" });
        _runtime.StartPendingBattle();
        _battleView.SetPlaybackForVisualTest(9.0);
        await ToSignal(GetTree().CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var siegeImage = GetViewport().GetTexture().GetImage();
        var siegeError = siegeImage.SavePng("/tmp/three-kingdoms-battle-siege-test.png");
        _runtime.CompletePendingBattle();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var resultImage = GetViewport().GetTexture().GetImage();
        var resultError = resultImage.SavePng("/tmp/three-kingdoms-battle-result-test.png");
        GD.Print($"[BattleVisualTest] planning={planningError} field={fieldError} siege={siegeError} result={resultError} size={siegeImage.GetWidth()}x{siegeImage.GetHeight()}");
        GetTree().Quit();
    }

    private async void RunUiVisualTest()
    {
        _reduceMotion = true;
        _worldMap.ShowWholeMapForVisualTest();
        var enemy = _runtime.State.Cities.First(city => city.OwnerFactionId != _runtime.State.PlayerFactionId);
        _worldMap.SelectCityForVisualTest(enemy.Id);
        await ToSignal(GetTree().CreateTimer(.8), SceneTreeTimer.SignalName.Timeout);
        await SaveUiFrame("world");

        var armySource = _runtime.State.Cities.First(city => city.OwnerFactionId == _runtime.State.PlayerFactionId && city.Garrison >= 3000 && _runtime.State.Roads.Any(road => road.TravelDays > 30 && ((road.FromCityId == city.Id && _runtime.City(road.ToCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId) || (road.ToCityId == city.Id && _runtime.City(road.FromCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId))));
        var armyRoad = _runtime.State.Roads.First(road => road.TravelDays > 30 && ((road.FromCityId == armySource.Id && _runtime.City(road.ToCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId) || (road.ToCityId == armySource.Id && _runtime.City(road.FromCityId)?.OwnerFactionId != _runtime.State.PlayerFactionId)));
        var armyTargetId = armyRoad.FromCityId == armySource.Id ? armyRoad.ToCityId : armyRoad.FromCityId;
        var armyCommander = _runtime.PlayerOfficers().First(item => item.InitialState.CityId == armySource.Id && item.InitialState.Status == "serving");
        _runtime.CreateExpedition(armySource.Id, armyTargetId, armyCommander.Profile.Id, 3000, 5000, "standard", "steady-advance", [], new Dictionary<string, int> { ["infantry"] = 2500, ["archers"] = 500 });
        _worldMap.SelectArmyForVisualTest(_runtime.State.Armies.Last().Id);
        await SaveUiFrame("army-control");

        var enemyArmyCity = _runtime.City(armyTargetId)!;
        var enemyArmyCommander = _runtime.State.Officers.First(item => item.InitialState.FactionId == enemyArmyCity.OwnerFactionId && item.InitialState.CityId == enemyArmyCity.Id && item.InitialState.Status == "serving");
        var interceptTarget = new ArmyData { Id = "ui-enemy-army", FactionId = enemyArmyCity.OwnerFactionId!, SourceCityId = enemyArmyCity.Id, TargetCityId = armySource.Id, CommanderId = enemyArmyCommander.Profile.Id, Soldiers = 3700, Food = 5000, Composition = new Dictionary<string, int> { ["infantry"] = 2200, ["archers"] = 1000, ["cavalry"] = 500 }, RouteRoadIds = [armyRoad.Id], RemainingDays = Math.Max(1, armyRoad.TravelDays - 20), TotalDays = armyRoad.TravelDays, Status = "marching" };
        enemyArmyCommander.InitialState.Status = "deployed"; enemyArmyCommander.InitialState.ArmyId = interceptTarget.Id; _runtime.State.Armies.Add(interceptTarget);
        _worldMap.SelectArmyForVisualTest(interceptTarget.Id);
        await SaveUiFrame("army-intercept");
        _expedition.PresetArmyTarget(interceptTarget.Id);
        Navigate("expedition");
        await SaveUiFrame("intercept-expedition");

        _expedition.PresetTarget(enemy.Id);
        Navigate("expedition");
        await SaveUiFrame("expedition");

        _cityManagement.ShowOverview();
        Navigate("city");
        await SaveUiFrame("city");
        var cityForVisualTest = _runtime.State.Cities.First(item => item.OwnerFactionId == _runtime.State.PlayerFactionId);
        _cityManagement.ShowCity(cityForVisualTest);
        _cityManagement.ShowPageForVisualTest("buildings");
        await SaveUiFrame("city-buildings");
        _cityManagement.ShowPageForVisualTest("governance");
        await SaveUiFrame("city-governance");

        Navigate("talent");
        var talent = (TalentView)_screens["talent"];
        foreach (var tab in new[] { "overview", "recruitment", "march", "office" })
        {
            talent.ShowTabForVisualTest(tab);
            await SaveUiFrame($"talent-{tab}");
        }
        foreach (var id in new[] { "diplomacy", "ai", "save" })
        {
            Navigate(id);
            await SaveUiFrame(id);
        }
        GD.Print("[UiVisualTest] world/expedition/city/city-buildings/city-governance/talent-tabs/diplomacy/ai/save screenshots saved");
        GetTree().Quit();
    }

    private async System.Threading.Tasks.Task SaveUiFrame(string id)
    {
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng($"/tmp/three-kingdoms-ui-{id}.png");
        GD.Print($"[UiVisualTest] {id}={error} size={image.GetWidth()}x{image.GetHeight()}");
    }

    private void OpenCity(string cityId)
    {
        var city = _runtime.City(cityId); if (city is null) return; _cityManagement.ShowCity(city); Navigate("city");
    }

    private void OpenExpedition(string targetCityId)
    {
        _expedition.PresetTarget(targetCityId);
        Navigate("expedition");
    }

    private void OpenArmyIntercept(string targetArmyId)
    {
        _expedition.PresetArmyTarget(targetArmyId);
        Navigate("expedition");
    }

    private void ShowArmyMovement(string armyId, int fromRemainingDays)
    {
        _worldMap.QueueArmyMovement(armyId, fromRemainingDays);
        Navigate("world");
    }

    private void OnArmyMovementFinished()
    {
        if (_runtime.State.PendingBattle is not null) Navigate("battle");
    }

    private void Navigate(string id)
    {
        if (!_screens.TryGetValue(id, out var next)) return;
        SwitchTo(next);
    }

    private void UpdateNavigationState()
    {
        foreach (var entry in _navigationButtons) entry.Value.ButtonPressed = _screens.TryGetValue(entry.Key, out var screen) && screen == _current;
    }

    private async void SwitchTo(Control next)
    {
        if (_transitioning || next == _current) return;
        _transitioning = true; _fade.MouseFilter = MouseFilterEnum.Stop;
        var cover = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic); cover.TweenProperty(_fade, "color:a", 1f, _reduceMotion ? .001f : .08f); await ToSignal(cover, Tween.SignalName.Finished);
        _current.Visible = false; next.Visible = true; _current = next;
        UpdateNavigationState();
        if (next is FeatureScreen feature) feature.Refresh();
        var reveal = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic); reveal.TweenProperty(_fade, "color:a", 0f, _reduceMotion ? .001f : .14f); await ToSignal(reveal, Tween.SignalName.Finished);
        _fade.MouseFilter = MouseFilterEnum.Ignore; _transitioning = false;
    }

    private void OnGameChanged()
    {
        UpdateMusicForGameState();
        if (_runtime.State.PendingEvent is not null) ShowEvent(); else CloseEvent();
        if (_runtime.State.PendingBattle is not null && _runtime.State.Armies.FirstOrDefault(item => item.Id == _runtime.State.PendingBattle.ArmyId)?.FactionId != _runtime.State.PlayerFactionId && _screens.TryGetValue("battle", out var battle) && _current != battle) Navigate("battle");
        if (_runtime.State.Outcome != "ongoing" && !string.IsNullOrEmpty(_runtime.State.OutcomeMessage)) ShowNotice(_runtime.State.OutcomeMessage, true);
    }

    private void ShowEvent()
    {
        if (_eventPanel is not null) return;
        var pending = _runtime.State.PendingEvent!; var definition = _runtime.State.Events.FirstOrDefault(item => item.Id == pending.DefinitionId); if (definition is null) return;
        _eventPanel = new PanelContainer { AnchorLeft = .5f, AnchorTop = .5f, AnchorRight = .5f, AnchorBottom = .5f, OffsetLeft = -330, OffsetTop = -210, OffsetRight = 330, OffsetBottom = 210, ZIndex = 95, MouseFilter = MouseFilterEnum.Stop };
        _eventPanel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(12)); AddChild(_eventPanel);
        UiOrnaments.AttachInkCorners(_eventPanel, 260, .11f);
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 16); _eventPanel.AddChild(layout);
        var title = new Label { Text = $"{definition.Title} · {RuntimeCityName(pending.CityId)}", CustomMinimumSize = new Vector2(0, 54) }; title.AddThemeFontSizeOverride("font_size", 27); title.AddThemeColorOverride("font_color", GameTheme.Paper); layout.AddChild(title);
        var description = new Label { Text = definition.Description, CustomMinimumSize = new Vector2(0, 95), AutowrapMode = TextServer.AutowrapMode.WordSmart }; description.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(description);
        foreach (var choice in definition.Choices) { var button = GameTheme.Button($"{choice.Label}　{choice.Description}"); button.CustomMinimumSize = new Vector2(0, 58); var id = choice.Id; button.Pressed += () => _runtime.ChooseEvent(id); layout.AddChild(button); }
    }

    private string RuntimeCityName(string id) => _runtime.City(id)?.Name ?? "天下";
    private void CloseEvent() { if (_eventPanel is null) return; _eventPanel.QueueFree(); _eventPanel = null; }
    private void ShowNotice(string message) => ShowNotice(message, false);
    private void ShowNotice(string message, bool persistent)
    {
        if (_toast is null) return; _toast.Text = message; _toast.Modulate = Colors.White;
        if (!persistent) { var tween = CreateTween(); tween.TweenInterval(2.8); tween.TweenProperty(_toast, "modulate:a", 0f, .45f); }
    }
}
