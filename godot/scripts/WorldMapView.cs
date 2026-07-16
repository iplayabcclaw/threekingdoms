using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class WorldMapView : Control
{
    [Signal]
    public delegate void OpenCityRequestedEventHandler(string cityId);

    [Signal]
    public delegate void ExpeditionRequestedEventHandler(string targetCityId);

    [Signal]
    public delegate void ArmyInterceptRequestedEventHandler(string targetArmyId);

    public event Action? ArmyMovementFinished;

    private readonly Dictionary<string, CityData> _citiesById = [];
    private readonly Dictionary<string, FactionData> _factionsById = [];
    private readonly Dictionary<string, Button> _cityButtons = [];
    private readonly Dictionary<string, Label> _cityLabels = [];
    private readonly Dictionary<string, Label> _cityFlagLabels = [];
    private readonly Dictionary<string, ShaderMaterial> _cityFlagMaterials = [];
    private readonly Dictionary<string, Node2D> _armyMarkers = [];
    private readonly Queue<(string ArmyId, int FromRemainingDays, Vector2? Start, Vector2? End, bool RemoveAfter)> _queuedArmyMovements = [];
    private readonly List<(Control Control, Vector2 MapPoint, Vector2 ScreenOffset)> _fixedMarkers = [];
    private GameRuntime _runtime = null!;
    private ScenarioData _scenario = null!;
    private Control _mapViewport = null!;
    private Node2D _mapRoot = null!;
    private Node2D _armyLayer = null!;
    private Label _cityName = null!;
    private Label _cityDetails = null!;
    private Label _performance = null!;
    private Button _openCity = null!;
    private Button _expedition = null!;
    private PanelContainer _armyPanel = null!;
    private Label _armyName = null!;
    private Label _armyDetails = null!;
    private OptionButton _retreatCity = null!;
    private OptionButton _armyInterceptChoice = null!;
    private Button _armyAdvance = null!;
    private Button _armyIntercept = null!;
    private Button _armyRedirectIntercept = null!;
    private Button _armyRetreat = null!;
    private Label _date = null!;
    private Label _resources = null!;
    private OptionButton _factionFilter = null!;
    private CityData? _selectedCity;
    private Vector2 _mapSize = new(1672, 941);
    private Vector2 _panOffset;
    private float _baseScale = 1.0f;
    private const float RegionalZoom = 3.2f;
    private float _zoom = RegionalZoom;
    private bool _dragging;
    private bool _initialFocusPending = true;
    private long _transformUpdates;
    private int _contentBuilds;
    private bool _playingArmyMovement;
    private string? _playingArmyId;
    private string? _selectedArmyId;

    public void Initialize(GameRuntime runtime)
    {
        _runtime = runtime;
        _scenario = new ScenarioData
        {
            Name = runtime.State.ScenarioName,
            PlayerFactionId = runtime.State.PlayerFactionId,
            Factions = runtime.State.Factions,
            Cities = runtime.State.Cities,
            Roads = runtime.State.Roads,
            Passes = runtime.State.Passes,
            Resources = runtime.State.Resources,
            Year = runtime.State.Year,
            Month = runtime.State.Month,
        };
        foreach (var city in runtime.State.Cities)
        {
            _citiesById[city.Id] = city;
        }
        foreach (var faction in runtime.State.Factions)
        {
            _factionsById[faction.Id] = faction;
        }

        BuildInterface();
        BuildStaticMapOnce();
        SelectCity(runtime.State.Cities.Find(item => item.OwnerFactionId == runtime.State.PlayerFactionId) ?? runtime.State.Cities[0]);
        runtime.Changed += Refresh;
        CallDeferred(nameof(LayoutMap));
    }

    private void BuildInterface()
    {
        var background = new ColorRect { Color = GameTheme.Backdrop };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        _mapViewport = new Control
        {
            Name = "MapViewport",
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.Drag,
        };
        _mapViewport.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _mapViewport.OffsetTop = 74;
        _mapViewport.OffsetBottom = -44;
        _mapViewport.GuiInput += OnMapInput;
        AddChild(_mapViewport);

        _mapRoot = new Node2D { Name = "MapTransformRoot" };
        _mapViewport.AddChild(_mapRoot);
        _mapViewport.Resized += LayoutMap;

        BuildTopBar();
        BuildCityPanel();
        BuildArmyPanel();
        BuildBottomStatus();
        BuildMapControls();
    }

    private void BuildTopBar()
    {
        var top = new Panel { MouseFilter = MouseFilterEnum.Stop };
        top.SetAnchorsPreset(LayoutPreset.TopWide);
        top.OffsetBottom = 74;
        top.AddThemeStyleboxOverride("panel", GameTheme.HeaderBox());
        AddChild(top);

        var title = new Label
        {
            Text = "三国：山河逐鹿",
            Position = new Vector2(26, 11),
            Size = new Vector2(310, 48),
            VerticalAlignment = VerticalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 27);
        title.AddThemeColorOverride("font_color", GameTheme.Paper);
        top.AddChild(title);

        _date = new Label
        {
            Position = new Vector2(345, 11),
            Size = new Vector2(520, 48),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _date.AddThemeColorOverride("font_color", GameTheme.Muted);
        top.AddChild(_date);

        _resources = new Label
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -540,
            OffsetRight = -26,
            OffsetTop = 11,
            OffsetBottom = 59,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = "前者为当前势力府库；后者为结束本月后的预计净变化（不含尚未决出的战斗与事件选择）",
        };
        _resources.AddThemeColorOverride("font_color", GameTheme.Bronze);
        top.AddChild(_resources);
        Refresh();
    }

    private void BuildCityPanel()
    {
        var panel = new Panel
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -350,
            OffsetRight = -22,
            OffsetTop = 96,
            OffsetBottom = 392,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 20,
        };
        panel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(9));
        AddChild(panel);
        UiOrnaments.AttachInkCorners(panel, 170, .08f);

        var caption = new Label { Text = "当前城池", Position = new Vector2(20, 15), Size = new Vector2(280, 28) };
        caption.AddThemeColorOverride("font_color", GameTheme.Gold);
        panel.AddChild(caption);

        _cityName = new Label { Position = new Vector2(20, 42), Size = new Vector2(280, 45) };
        _cityName.AddThemeFontSizeOverride("font_size", 29);
        _cityName.AddThemeColorOverride("font_color", GameTheme.Paper);
        panel.AddChild(_cityName);

        _cityDetails = new Label
        {
            Position = new Vector2(20, 88),
            Size = new Vector2(288, 88),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _cityDetails.AddThemeColorOverride("font_color", GameTheme.Muted);
        panel.AddChild(_cityDetails);

        _openCity = GameTheme.Button("进入城池内政");
        _openCity.Position = new Vector2(20, 182);
        _openCity.Size = new Vector2(288, 42);
        _openCity.Pressed += () =>
        {
            if (_selectedCity is not null)
            {
                EmitSignal(SignalName.OpenCityRequested, _selectedCity.Id);
            }
        };
        panel.AddChild(_openCity);

        _expedition = GameTheme.Button("出征此城");
        _expedition.Position = new Vector2(20, 232);
        _expedition.Size = new Vector2(288, 42);
        _expedition.Pressed += () =>
        {
            if (_selectedCity is not null)
            {
                EmitSignal(SignalName.ExpeditionRequested, _selectedCity.Id);
            }
        };
        panel.AddChild(_expedition);
    }

    private void BuildArmyPanel()
    {
        _armyPanel = new PanelContainer
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -420,
            OffsetRight = -22,
            OffsetTop = 96,
            OffsetBottom = 590,
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
            ZIndex = 30,
        };
        _armyPanel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(10));
        AddChild(_armyPanel);
        UiOrnaments.AttachInkCorners(_armyPanel, 190, .08f);
        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" }) margin.AddThemeConstantOverride(side, 14);
        _armyPanel.AddChild(margin);
        var layout = new VBoxContainer(); layout.AddThemeConstantOverride("separation", 8); margin.AddChild(layout);

        var header = new HBoxContainer();
        var caption = new Label { Text = "军团指挥", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        caption.AddThemeFontSizeOverride("font_size", 18); caption.AddThemeColorOverride("font_color", GameTheme.Gold); header.AddChild(caption);
        var close = new Button { Text = "×", Flat = true, CustomMinimumSize = new Vector2(38, 34), FocusMode = FocusModeEnum.None, TooltipText = "关闭军团面板" };
        close.AddThemeFontSizeOverride("font_size", 22); close.AddThemeColorOverride("font_color", GameTheme.Paper); close.Pressed += () => { _selectedArmyId = null; _armyPanel.Visible = false; }; header.AddChild(close); layout.AddChild(header);

        _armyName = new Label { CustomMinimumSize = new Vector2(0, 36), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _armyName.AddThemeFontSizeOverride("font_size", 23); _armyName.AddThemeColorOverride("font_color", GameTheme.Paper); layout.AddChild(_armyName);
        _armyDetails = new Label { CustomMinimumSize = new Vector2(0, 70), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _armyDetails.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(_armyDetails);

        _armyAdvance = GameTheme.Button("继续前进"); _armyAdvance.CustomMinimumSize = new Vector2(0, 42); _armyAdvance.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; _armyAdvance.Pressed += AdvanceSelectedArmy; layout.AddChild(_armyAdvance);
        _armyIntercept = GameTheme.Button("出击拦截"); _armyIntercept.CustomMinimumSize = new Vector2(0, 42); _armyIntercept.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; _armyIntercept.Pressed += InterceptSelectedArmy; layout.AddChild(_armyIntercept);
        var interceptOrder = new HBoxContainer(); interceptOrder.AddThemeConstantOverride("separation", 8);
        _armyInterceptChoice = new OptionButton { CustomMinimumSize = new Vector2(0, 42), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; interceptOrder.AddChild(_armyInterceptChoice);
        _armyRedirectIntercept = GameTheme.Button("改令拦截"); _armyRedirectIntercept.CustomMinimumSize = new Vector2(130, 42); _armyRedirectIntercept.Pressed += RedirectArmyToIntercept; interceptOrder.AddChild(_armyRedirectIntercept); layout.AddChild(interceptOrder);
        var hint = new Label { Text = "出征当回合已行动；进入下一回合后，每支军团可选择一次继续前进或撤兵。", CustomMinimumSize = new Vector2(0, 34), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        hint.AddThemeFontSizeOverride("font_size", 12); hint.AddThemeColorOverride("font_color", GameTheme.Muted); layout.AddChild(hint);
        var retreat = new HBoxContainer(); retreat.AddThemeConstantOverride("separation", 8);
        _retreatCity = new OptionButton { CustomMinimumSize = new Vector2(0, 42), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; retreat.AddChild(_retreatCity);
        _armyRetreat = GameTheme.Button("撤兵"); _armyRetreat.CustomMinimumSize = new Vector2(92, 42); _armyRetreat.Pressed += WithdrawSelectedArmy; retreat.AddChild(_armyRetreat); layout.AddChild(retreat);
    }

    private void BuildBottomStatus()
    {
        var bottom = new Panel
        {
            AnchorTop = 1,
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetTop = -44,
            MouseFilter = MouseFilterEnum.Stop,
        };
        bottom.AddThemeStyleboxOverride("panel", GameTheme.HeaderBox());
        AddChild(bottom);

        var hint = new Label
        {
            Text = "按住鼠标左键拖动 · 滚轮缩放 · 城池和道路节点只创建一次",
            Position = new Vector2(20, 0),
            Size = new Vector2(600, 44),
            VerticalAlignment = VerticalAlignment.Center,
        };
        hint.AddThemeColorOverride("font_color", GameTheme.Muted);
        bottom.AddChild(hint);

        _performance = new Label
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            OffsetLeft = -610,
            OffsetRight = -20,
            OffsetBottom = 44,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _performance.AddThemeColorOverride("font_color", GameTheme.Success);
        bottom.AddChild(_performance);
        UpdatePerformanceLabel();
    }

    private void BuildMapControls()
    {
        var panel = new PanelContainer { Position = new Vector2(18, 86), Size = new Vector2(426, 52), MouseFilter = MouseFilterEnum.Stop, ZIndex = 20 };
        panel.AddThemeStyleboxOverride("panel", GameTheme.RaisedBox(7));
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 5); margin.AddThemeConstantOverride("margin_right", 5); margin.AddThemeConstantOverride("margin_top", 4); margin.AddThemeConstantOverride("margin_bottom", 4); panel.AddChild(margin);
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 8); margin.AddChild(row);
        _factionFilter = new OptionButton { CustomMinimumSize = new Vector2(210, 40) }; _factionFilter.AddItem("显示全部势力"); _factionFilter.SetItemMetadata(0, "all");
        foreach (var faction in _runtime.State.Factions) { _factionFilter.AddItem(faction.Name); _factionFilter.SetItemMetadata(_factionFilter.ItemCount - 1, faction.Id); }
        _factionFilter.ItemSelected += _ => Refresh(); row.AddChild(_factionFilter);

        var zoomOut = GameTheme.Button("－");
        zoomOut.CustomMinimumSize = new Vector2(42, 40);
        zoomOut.TooltipText = "缩小战略地图";
        zoomOut.Pressed += () => ZoomAt(_mapViewport.Size / 2.0f, 1.0f / 1.25f);
        row.AddChild(zoomOut);

        var zoomIn = GameTheme.Button("＋");
        zoomIn.CustomMinimumSize = new Vector2(42, 40);
        zoomIn.TooltipText = "放大战略地图";
        zoomIn.Pressed += () => ZoomAt(_mapViewport.Size / 2.0f, 1.25f);
        row.AddChild(zoomIn);

        var overview = GameTheme.Button("天下全图");
        overview.CustomMinimumSize = new Vector2(96, 40);
        overview.TooltipText = "缩到最小比例，查看完整天下";
        overview.Pressed += ShowWholeMap;
        row.AddChild(overview);
        AddChild(panel);
    }

    private void BuildStaticMapOnce()
    {
        _contentBuilds += 1;
        var texture = GD.Load<Texture2D>("res://assets/world-map-bg-v4.png");
        if (texture is not null)
        {
            _mapSize = texture.GetSize();
            _mapRoot.AddChild(new Sprite2D
            {
                Name = "MapBackground",
                Texture = texture,
                Centered = true,
                Modulate = Colors.White,
            });
        }

        foreach (var road in _scenario.Roads)
        {
            if (!_citiesById.TryGetValue(road.FromCityId, out var from) || !_citiesById.TryGetValue(road.ToCityId, out var to))
            {
                continue;
            }

            var line = new Line2D
            {
                Name = road.Id,
                Width = road.Kind == "trunk" ? 2.4f : 1.4f,
                DefaultColor = road.Terrain == "river"
                    ? new Color(0.22f, 0.39f, 0.46f, 0.84f)
                    : new Color(0.34f, 0.25f, 0.16f, road.Kind == "trunk" ? 0.82f : 0.58f),
                Antialiased = true,
                ZIndex = 2,
            };
            line.AddPoint(ToMapPoint(from.Position));
            foreach (var waypoint in road.Waypoints)
            {
                line.AddPoint(ToMapPoint(waypoint));
            }
            line.AddPoint(ToMapPoint(to.Position));
            _mapRoot.AddChild(line);
        }

        foreach (var city in _scenario.Cities)
        {
            var faction = _factionsById.GetValueOrDefault(city.OwnerFactionId);
            var button = new Button
            {
                Name = city.Id,
                Position = ToMapPoint(city.Position) - new Vector2(55, 87),
                Size = new Vector2(110, 174),
                TooltipText = $"{city.Name} · {faction?.Name ?? "未知势力"}",
                Flat = true,
                MouseDefaultCursorShape = CursorShape.PointingHand,
                ZIndex = 4,
            };
            button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            button.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
            button.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
            button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

            var flagRoot = BuildFactionFlag(city, faction, _cityButtons.Count);
            button.AddChild(flagRoot);

            var cityTexture = GD.Load<Texture2D>(AssetPaths.CityMarker(city.Region, city.Id));
            var cityTextureSize = cityTexture?.GetSize() ?? Vector2.One;
            var shadow = new Polygon2D
            {
                Polygon = EllipsePoints(new Vector2(55, 148), new Vector2(36, 7), 24),
                Color = new Color(.10f, .075f, .045f, .28f),
                ZIndex = -1,
            };
            button.AddChild(shadow);
            var illustration = new Sprite2D
            {
                Texture = cityTexture,
                Position = new Vector2(55, 110),
                Scale = Vector2.One * (84.0f / Mathf.Max(cityTextureSize.X, cityTextureSize.Y)),
                Centered = true,
            };
            button.AddChild(illustration);
            var markerLabel = new Label
            {
                Text = city.Name,
                Position = new Vector2(0, 146),
                Size = new Vector2(110, 28),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            markerLabel.AddThemeFontSizeOverride("font_size", 15);
            markerLabel.AddThemeConstantOverride("outline_size", 3);
            markerLabel.AddThemeColorOverride("font_color", GameTheme.Paper);
            markerLabel.AddThemeColorOverride("font_outline_color", new Color(GameTheme.PanelRaised, .96f));
            button.AddChild(markerLabel);
            button.Pressed += () => SelectCity(city);
            _cityButtons[city.Id] = button;
            _cityLabels[city.Id] = markerLabel;
            _mapRoot.AddChild(button);
            _fixedMarkers.Add((button, ToMapPoint(city.Position), new Vector2(0, -23)));
        }

        foreach (var pass in _scenario.Passes)
        {
            var passTexture = GD.Load<Texture2D>(AssetPaths.PassMarker(pass.Id));
            var passTextureSize = passTexture?.GetSize() ?? Vector2.One;
            var marker = new Control
            {
                Name = pass.Id,
                Position = ToMapPoint(pass.Position) - new Vector2(25, 25),
                Size = new Vector2(50, 50),
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 3,
            };
            marker.AddChild(new Sprite2D
            {
                Texture = passTexture,
                Position = new Vector2(25, 25),
                Scale = Vector2.One * (50.0f / Mathf.Max(passTextureSize.X, passTextureSize.Y)),
                Centered = true,
            });
            _mapRoot.AddChild(marker);
            _fixedMarkers.Add((marker, ToMapPoint(pass.Position), Vector2.Zero));
            var label = new Label { Text = pass.Name, Position = ToMapPoint(pass.Position) + new Vector2(22, -10), Size = new Vector2(90, 24), MouseFilter = MouseFilterEnum.Ignore, ZIndex = 3 };
            label.AddThemeFontSizeOverride("font_size", 11); label.AddThemeColorOverride("font_color", GameTheme.Paper); _mapRoot.AddChild(label);
            _fixedMarkers.Add((label, ToMapPoint(pass.Position), new Vector2(62, 0)));
        }

        _armyLayer = new Node2D { Name = "ArmyLayer", ZIndex = 12 };
        _mapRoot.AddChild(_armyLayer);
        RefreshArmyMarkers();

        UpdatePerformanceLabel();
    }

    public void QueueArmyMovement(string armyId, int fromRemainingDays)
    {
        _queuedArmyMovements.Enqueue((armyId, fromRemainingDays, null, null, false));
        if (!_playingArmyMovement) CallDeferred(nameof(PlayQueuedArmyMovements));
    }

    private void QueueArmyWithdrawal(string armyId, Vector2 start, Vector2 end)
    {
        _queuedArmyMovements.Enqueue((armyId, 0, start, end, true));
        if (!_playingArmyMovement) CallDeferred(nameof(PlayQueuedArmyMovements));
    }

    private async void PlayQueuedArmyMovements()
    {
        if (_playingArmyMovement) return;
        _playingArmyMovement = true;
        while (_queuedArmyMovements.Count > 0)
        {
            while (!IsVisibleInTree()) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var movement = _queuedArmyMovements.Dequeue();
            var army = _runtime.State.Armies.FirstOrDefault(item => item.Id == movement.ArmyId);
            if (army is null) continue;
            _playingArmyId = army.Id;
            var marker = GetOrCreateArmyMarker(army);
            var start = movement.Start ?? ArmyMapPosition(army, movement.FromRemainingDays);
            var end = movement.End ?? ArmyMapPosition(army, army.RemainingDays);
            marker.Position = start;
            var sprite = marker.GetNode<AnimatedSprite2D>("MarchSprite");
            sprite.FlipH = end.X < start.X;
            sprite.Play();
            FocusMapPoint((start + end) / 2.0f);
            var tween = CreateTween().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(marker, "position", end, movement.RemoveAfter ? 1.35f : 1.15f);
            await ToSignal(tween, Tween.SignalName.Finished);
            marker.Position = end;
            if (movement.RemoveAfter || army.Status is not "marching" and not "besieging" and not "retreating")
            {
                await ToSignal(GetTree().CreateTimer(.45), SceneTreeTimer.SignalName.Timeout);
                _armyMarkers.Remove(army.Id);
                marker.QueueFree();
            }
            _playingArmyId = null;
        }
        _playingArmyMovement = false;
        ArmyMovementFinished?.Invoke();
    }

    private void RefreshArmyMarkers()
    {
        if (_armyLayer is null) return;
        var visibleIds = _runtime.State.Armies.Where(item => item.Status is "marching" or "besieging" or "retreating" || item.Id == _playingArmyId).Select(item => item.Id).ToHashSet();
        foreach (var armyId in _armyMarkers.Keys.Where(id => !visibleIds.Contains(id)).ToList())
        {
            _armyMarkers[armyId].QueueFree();
            _armyMarkers.Remove(armyId);
        }
        foreach (var army in _runtime.State.Armies.Where(item => item.Status is "marching" or "besieging" or "retreating"))
        {
            var marker = GetOrCreateArmyMarker(army);
            if (army.Id != _playingArmyId) marker.Position = ArmyMapPosition(army, army.RemainingDays);
        }
        UpdateArmyMarkerScale(_baseScale * _zoom);
        RefreshSelectedArmyPanel();
    }

    private Node2D GetOrCreateArmyMarker(ArmyData army)
    {
        if (_armyMarkers.TryGetValue(army.Id, out var existing))
        {
            existing.GetNode<Label>("ArmyLabel").Text = ArmyMarkerText(army);
            existing.GetNode<AnimatedSprite2D>("MarchSprite").Play();
            return existing;
        }
        var marker = new Node2D { Name = army.Id, ZIndex = 12 };
        var sheet = GD.Load<Texture2D>("res://assets/runtime/cavalry-march-sprite-v1.webp");
        var frames = new SpriteFrames();
        frames.SetAnimationSpeed("default", 8);
        frames.SetAnimationLoop("default", true);
        var frameSize = sheet.GetSize() / new Vector2(4, 2);
        for (var row = 0; row < 2; row++)
        for (var column = 0; column < 4; column++)
            frames.AddFrame("default", new AtlasTexture { Atlas = sheet, Region = new Rect2(column * frameSize.X, row * frameSize.Y, frameSize.X, frameSize.Y) });
        var sprite = new AnimatedSprite2D { Name = "MarchSprite", SpriteFrames = frames, Position = new Vector2(0, -28), Scale = Vector2.One * .18f, Frame = 0 };
        marker.AddChild(sprite); sprite.Play();
        var label = new Label { Name = "ArmyLabel", Text = ArmyMarkerText(army), Position = new Vector2(-90, 28), Size = new Vector2(180, 42), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeStyleboxOverride("normal", GameTheme.Box(new Color(GameTheme.PanelRaised, .94f), new Color(GameTheme.Bronze, .66f), 5));
        label.AddThemeFontSizeOverride("font_size", 13); label.AddThemeColorOverride("font_color", GameTheme.Paper); marker.AddChild(label);
        var select = new Button { Name = "SelectArmy", Position = new Vector2(-90, -74), Size = new Vector2(180, 144), Flat = true, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand, TooltipText = "选择军团并下达本回合命令", ZIndex = 4 };
        foreach (var state in new[] { "normal", "hover", "pressed", "focus" }) select.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        var armyId = army.Id; select.Pressed += () => SelectArmy(armyId); marker.AddChild(select);
        _armyLayer.AddChild(marker);
        _armyMarkers[army.Id] = marker;
        return marker;
    }

    private void SelectArmy(string armyId)
    {
        _selectedArmyId = armyId;
        RefreshSelectedArmyPanel();
        _armyPanel.Visible = true;
    }

    private void RefreshSelectedArmyPanel()
    {
        if (_armyPanel is null || string.IsNullOrEmpty(_selectedArmyId)) return;
        var army = _runtime.State.Armies.FirstOrDefault(item => item.Id == _selectedArmyId);
        if (army is null || army.Status is not "marching" and not "besieging" and not "retreating")
        {
            _selectedArmyId = null;
            _armyPanel.Visible = false;
            return;
        }
        var commander = _runtime.Officer(army.CommanderId)?.Profile.Name ?? "未知主将";
        var faction = _runtime.Faction(army.FactionId)?.ShortName ?? "未知";
        var progress = Math.Max(0, army.TotalDays - army.RemainingDays);
        _armyName.Text = $"{faction} · {commander}军";
        _armyDetails.Text = $"{_runtime.City(army.SourceCityId)?.Name} → {_runtime.City(army.TargetCityId)?.Name}\n兵力 {army.Soldiers:N0}　军粮 {army.Food:N0}　士气 {army.Morale}\n行程 {progress}/{army.TotalDays}日　{ArmyStatusName(army.Status)}";
        var controllable = army.FactionId == _runtime.State.PlayerFactionId;
        var canAct = controllable && (army.Status is "marching" or "besieging") && army.LastMarchTurn < _runtime.State.Turn;
        _armyAdvance.Disabled = !canAct;
        _armyAdvance.Text = !controllable ? "敌军不可控制" : army.Status == "retreating" ? "败军正在撤退" : canAct ? army.Status == "besieging" ? "继续攻城" : "继续前进" : "本回合已行动";
        _armyAdvance.Visible = controllable;
        _armyIntercept.Visible = !controllable;
        _armyIntercept.Disabled = controllable || _runtime.State.PendingBattle is not null;
        _armyIntercept.Text = _runtime.State.PendingBattle is null ? "从城内另组新军拦截" : "当前已有战斗待处理";
        RefreshArmyInterceptChoices(army, controllable);
        _retreatCity.Clear();
        foreach (var city in _runtime.State.Cities.Where(item => item.OwnerFactionId == army.FactionId).OrderBy(item => item.Name))
        {
            _retreatCity.AddItem($"退回 {city.Name}");
            _retreatCity.SetItemMetadata(_retreatCity.ItemCount - 1, city.Id);
        }
        _retreatCity.Disabled = !canAct;
        _armyRetreat.Disabled = !canAct || _retreatCity.ItemCount == 0;
    }

    private void AdvanceSelectedArmy()
    {
        if (string.IsNullOrEmpty(_selectedArmyId)) return;
        var army = _runtime.State.Armies.FirstOrDefault(item => item.Id == _selectedArmyId);
        if (army is null) return;
        var before = army.Status == "besieging" ? 0 : army.RemainingDays;
        if (_runtime.MarchArmy(army.Id)) QueueArmyMovement(army.Id, before);
    }

    private void InterceptSelectedArmy()
    {
        if (string.IsNullOrEmpty(_selectedArmyId)) return;
        var army = _runtime.State.Armies.FirstOrDefault(item => item.Id == _selectedArmyId);
        if (army is null || army.FactionId == _runtime.State.PlayerFactionId) return;
        EmitSignal(SignalName.ArmyInterceptRequested, army.Id);
        _armyPanel.Visible = false;
    }

    private void RefreshArmyInterceptChoices(ArmyData selectedArmy, bool selectedIsPlayerArmy)
    {
        _armyInterceptChoice.Clear();
        var candidates = _runtime.State.Armies
            .Where(item => item.Status is "marching" or "besieging")
            .Where(item => selectedIsPlayerArmy ? item.FactionId != _runtime.State.PlayerFactionId : item.FactionId == _runtime.State.PlayerFactionId)
            .OrderBy(item => item.RemainingDays)
            .ToList();
        var enabledIndex = -1;
        var firstReason = string.Empty;
        foreach (var candidate in candidates)
        {
            var interceptor = selectedIsPlayerArmy ? selectedArmy : candidate;
            var target = selectedIsPlayerArmy ? candidate : selectedArmy;
            var commander = _runtime.Officer(candidate.CommanderId)?.Profile.Name ?? "未知主将";
            var valid = _runtime.CanOrderArmyIntercept(interceptor.Id, target.Id, out var reason);
            _armyInterceptChoice.AddItem($"{_runtime.Faction(candidate.FactionId)?.ShortName}·{commander}军（{candidate.Soldiers:N0}兵）");
            var index = _armyInterceptChoice.ItemCount - 1;
            _armyInterceptChoice.SetItemMetadata(index, candidate.Id);
            _armyInterceptChoice.SetItemDisabled(index, !valid);
            if (valid && enabledIndex < 0) enabledIndex = index;
            if (!valid && string.IsNullOrEmpty(firstReason)) firstReason = reason;
        }
        if (_armyInterceptChoice.ItemCount == 0)
        {
            _armyInterceptChoice.AddItem(selectedIsPlayerArmy ? "没有可拦截的敌军" : "没有可调遣的在途己军");
            _armyInterceptChoice.SetItemMetadata(0, string.Empty);
            _armyInterceptChoice.SetItemDisabled(0, true);
        }
        if (enabledIndex >= 0) _armyInterceptChoice.Select(enabledIndex);
        _armyInterceptChoice.Disabled = enabledIndex < 0 || _runtime.State.PendingBattle is not null;
        _armyRedirectIntercept.Disabled = _armyInterceptChoice.Disabled;
        _armyRedirectIntercept.Text = selectedIsPlayerArmy
            ? selectedArmy.LastMarchTurn >= _runtime.State.Turn ? "改令（下回合）" : "改令并截击"
            : "调现有军拦截";
        _armyRedirectIntercept.TooltipText = _runtime.State.PendingBattle is not null ? "当前已有战斗待处理。" : enabledIndex < 0 ? firstReason : "两军在共同道路上的行军区间相遇时会立即进入野战。";
    }

    private void RedirectArmyToIntercept()
    {
        if (string.IsNullOrEmpty(_selectedArmyId) || _armyInterceptChoice.Selected < 0) return;
        var selected = _runtime.State.Armies.FirstOrDefault(item => item.Id == _selectedArmyId);
        var choiceId = _armyInterceptChoice.GetItemMetadata(_armyInterceptChoice.Selected).AsString();
        var choice = _runtime.State.Armies.FirstOrDefault(item => item.Id == choiceId);
        if (selected is null || choice is null) return;
        var interceptor = selected.FactionId == _runtime.State.PlayerFactionId ? selected : choice;
        var target = selected.FactionId == _runtime.State.PlayerFactionId ? choice : selected;
        var before = interceptor.RemainingDays;
        if (!_runtime.OrderArmyIntercept(interceptor.Id, target.Id)) return;
        if (interceptor.RemainingDays != before || interceptor.Status == "awaiting-battle") QueueArmyMovement(interceptor.Id, before);
        _selectedArmyId = null;
        _armyPanel.Visible = false;
    }

    private void WithdrawSelectedArmy()
    {
        if (string.IsNullOrEmpty(_selectedArmyId) || _retreatCity.Selected < 0) return;
        var army = _runtime.State.Armies.FirstOrDefault(item => item.Id == _selectedArmyId);
        var destinationId = _retreatCity.GetItemMetadata(_retreatCity.Selected).AsString();
        var destination = City(destinationId);
        if (army is null || destination is null) return;
        var start = ArmyMapPosition(army, army.RemainingDays);
        var end = ToMapPoint(destination.Position);
        _playingArmyId = army.Id;
        if (!_runtime.WithdrawArmy(army.Id, destinationId))
        {
            _playingArmyId = null;
            return;
        }
        QueueArmyWithdrawal(army.Id, start, end);
        _selectedArmyId = null;
        _armyPanel.Visible = false;
    }

    private string ArmyMarkerText(ArmyData army) => $"{_runtime.Officer(army.CommanderId)?.Profile.Name ?? "军团"} · {Compact(army.Soldiers)}兵";

    private static string ArmyStatusName(string status) => status switch { "marching" => "行军中", "besieging" => "围城中", "retreating" => "败退中", "awaiting-battle" => "等待交战", _ => status };

    private Vector2 ArmyMapPosition(ArmyData army, int remainingDays)
    {
        if (army.Status == "besieging" || army.Status == "awaiting-battle" && remainingDays == 0) return City(army.TargetCityId) is { } siegeTarget ? ToMapPoint(siegeTarget.Position) : Vector2.Zero;
        var elapsed = Math.Clamp(army.TotalDays - remainingDays, 0, army.TotalDays);
        var currentCityId = army.SourceCityId;
        foreach (var roadId in army.RouteRoadIds)
        {
            var road = _runtime.State.Roads.FirstOrDefault(item => item.Id == roadId);
            if (road is null) continue;
            var forward = road.FromCityId == currentCityId;
            var nextCityId = forward ? road.ToCityId : road.FromCityId;
            var startCity = City(currentCityId);
            var endCity = City(nextCityId);
            if (startCity is null || endCity is null) continue;
            var points = new List<Vector2> { ToMapPoint(startCity.Position) };
            var waypoints = forward ? road.Waypoints : road.Waypoints.AsEnumerable().Reverse();
            points.AddRange(waypoints.Select(ToMapPoint));
            points.Add(ToMapPoint(endCity.Position));
            if (elapsed <= road.TravelDays) return PointAlongPolyline(points, road.TravelDays == 0 ? 1 : elapsed / (float)road.TravelDays);
            elapsed -= road.TravelDays;
            currentCityId = nextCityId;
        }
        return City(army.TargetCityId) is { } target ? ToMapPoint(target.Position) : Vector2.Zero;
    }

    private static Vector2 PointAlongPolyline(List<Vector2> points, float progress)
    {
        progress = Mathf.Clamp(progress, 0, 1);
        var total = 0.0f;
        for (var index = 1; index < points.Count; index++) total += points[index - 1].DistanceTo(points[index]);
        if (total <= .001f) return points[^1];
        var remaining = total * progress;
        for (var index = 1; index < points.Count; index++)
        {
            var length = points[index - 1].DistanceTo(points[index]);
            if (remaining <= length) return points[index - 1].Lerp(points[index], length <= .001f ? 1 : remaining / length);
            remaining -= length;
        }
        return points[^1];
    }

    private void FocusMapPoint(Vector2 point)
    {
        _zoom = Mathf.Max(_zoom, 2.2f);
        _panOffset = -point * (_baseScale * _zoom);
        ApplyMapTransform();
    }

    private Node2D BuildFactionFlag(CityData city, FactionData? faction, int animationIndex)
    {
        var root = new Node2D
        {
            Name = $"{city.Id}-flag",
            Position = new Vector2(118, 58),
            ZIndex = 1,
        };
        var flagTexture = GD.Load<Texture2D>(AssetPaths.FactionFlag());
        var flagSize = flagTexture?.GetSize() ?? Vector2.One;
        var shader = GD.Load<Shader>("res://shaders/faction_flag.gdshader");
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("faction_color", faction is null ? Color.FromHtml("#777777") : Color.FromHtml(faction.Color));
        material.SetShaderParameter("flag_phase", animationIndex * .71f);
        root.AddChild(new Sprite2D
        {
            Texture = flagTexture,
            Scale = Vector2.One * (78.0f / Mathf.Max(flagSize.X, flagSize.Y)),
            Centered = true,
            Material = material,
        });

        var factionLabel = new Label
        {
            Text = faction?.ShortName ?? "城",
            Position = new Vector2(-15, -29),
            Size = new Vector2(42, 38),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        factionLabel.AddThemeFontSizeOverride("font_size", 25);
        factionLabel.AddThemeConstantOverride("outline_size", 5);
        factionLabel.AddThemeColorOverride("font_color", GameTheme.OnAccent);
        factionLabel.AddThemeColorOverride("font_outline_color", new Color(.08f, .06f, .035f, .96f));
        root.AddChild(factionLabel);
        _cityFlagLabels[city.Id] = factionLabel;
        _cityFlagMaterials[city.Id] = material;
        return root;
    }

    private static Vector2[] EllipsePoints(Vector2 center, Vector2 radius, int segments)
    {
        var points = new Vector2[segments];
        for (var index = 0; index < segments; index++)
        {
            var angle = Mathf.Tau * index / segments;
            points[index] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
        }
        return points;
    }

    private void OnMapInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                _dragging = mouseButton.Pressed;
                _mapViewport.MouseDefaultCursorShape = _dragging ? CursorShape.Move : CursorShape.Drag;
                _mapViewport.AcceptEvent();
                break;
            case InputEventMouseButton mouseButton when mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp:
                ZoomAt(mouseButton.Position, 1.12f);
                _mapViewport.AcceptEvent();
                break;
            case InputEventMouseButton mouseButton when mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown:
                ZoomAt(mouseButton.Position, 1.0f / 1.12f);
                _mapViewport.AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                _panOffset += motion.Relative;
                ApplyMapTransform();
                _transformUpdates += 1;
                UpdatePerformanceLabel();
                _mapViewport.AcceptEvent();
                break;
        }
    }

    private void ZoomAt(Vector2 pointer, float factor)
    {
        var previous = _zoom;
        _zoom = Mathf.Clamp(_zoom * factor, 1.0f, 4.0f);
        if (Mathf.IsEqualApprox(previous, _zoom))
        {
            return;
        }

        var center = _mapViewport.Size / 2.0f;
        var relative = pointer - center - _panOffset;
        _panOffset -= relative * (_zoom / previous - 1.0f);
        ApplyMapTransform();
        _transformUpdates += 1;
        UpdatePerformanceLabel();
    }

    private void LayoutMap()
    {
        if (_mapViewport is null || _mapViewport.Size.X <= 1 || _mapViewport.Size.Y <= 1)
        {
            return;
        }

        _baseScale = Mathf.Min(_mapViewport.Size.X / _mapSize.X, _mapViewport.Size.Y / _mapSize.Y);
        if (_initialFocusPending && _selectedCity is not null)
        {
            _zoom = RegionalZoom;
            _panOffset = -ToMapPoint(_selectedCity.Position) * (_baseScale * _zoom);
            _initialFocusPending = false;
        }
        ApplyMapTransform();
    }

    private void ApplyMapTransform()
    {
        var scale = _baseScale * _zoom;
        var scaledSize = _mapSize * scale;
        var limit = new Vector2(
            Mathf.Max(0, (scaledSize.X - _mapViewport.Size.X) / 2.0f),
            Mathf.Max(0, (scaledSize.Y - _mapViewport.Size.Y) / 2.0f));
        _panOffset = new Vector2(
            Mathf.Clamp(_panOffset.X, -limit.X, limit.X),
            Mathf.Clamp(_panOffset.Y, -limit.Y, limit.Y));
        _mapRoot.Position = _mapViewport.Size / 2.0f + _panOffset;
        _mapRoot.Scale = Vector2.One * scale;
        UpdateFixedMarkerTransforms(scale);
        UpdateArmyMarkerScale(scale);
    }

    private void UpdateArmyMarkerScale(float mapScale)
    {
        if (mapScale <= 0) return;
        foreach (var marker in _armyMarkers.Values) marker.Scale = Vector2.One / mapScale;
    }

    private void UpdateFixedMarkerTransforms(float mapScale)
    {
        // Keep overview markers compact so nearby cities never present a large,
        // overlapping click target. Regional focus still enlarges them enough
        // to read while the increased zoom creates more space between cities.
        var overviewScale = Mathf.Lerp(.36f, .78f, Mathf.Clamp((_zoom - 1.0f) / 1.8f, 0, 1));
        foreach (var marker in _fixedMarkers)
        {
            marker.Control.Scale = Vector2.One * (overviewScale / mapScale);
            marker.Control.Position = marker.MapPoint + (marker.ScreenOffset - marker.Control.Size * overviewScale / 2.0f) / mapScale;
        }
    }

    private void ShowWholeMap()
    {
        _zoom = 1.0f;
        _panOffset = Vector2.Zero;
        ApplyMapTransform();
        _transformUpdates += 1;
        UpdatePerformanceLabel();
    }

    public void ShowWholeMapForVisualTest() => ShowWholeMap();

    public void SelectCityForVisualTest(string cityId)
    {
        if (_citiesById.TryGetValue(cityId, out var city)) SelectCity(city);
    }

    public void SelectArmyForVisualTest(string armyId) => SelectArmy(armyId);

    private void SelectCity(CityData city)
    {
        _selectedCity = city;
        foreach (var (cityId, label) in _cityLabels)
        {
            label.AddThemeColorOverride("font_color", cityId == city.Id ? GameTheme.Bronze : GameTheme.Paper);
        }
        var faction = _factionsById.GetValueOrDefault(city.OwnerFactionId);
        _cityName.Text = $"{city.Name} · {city.Region}";
        _cityDetails.Text = $"{faction?.Name ?? "未知势力"}　太守 {city.GovernorName}\n驻军 {city.Garrison:N0}　人口 {city.Population:N0}\n农业 {city.Agriculture}　商业 {city.Commerce}　民心 {city.PublicSupport}";
        var isPlayerCity = city.OwnerFactionId == _runtime.State.PlayerFactionId;
        _openCity.Visible = isPlayerCity;
        _openCity.Disabled = !isPlayerCity;
        _expedition.Visible = !isPlayerCity;
        _expedition.Disabled = isPlayerCity || _runtime.HasTreaty(city.OwnerFactionId, "truce");
        _expedition.TooltipText = _expedition.Disabled && !isPlayerCity ? "停战协定期间不能出征" : "进入出征准备并自动选择该城为目标";
    }

    public string SelectedCityId => _selectedCity?.Id ?? StatePlayerCity()?.Id ?? string.Empty;

    public void Refresh()
    {
        if (_runtime is null || _date is null) return;
        var state = _runtime.State;
        var owned = state.Cities.Count(city => city.OwnerFactionId == state.PlayerFactionId);
        var playerFaction = state.Factions.FirstOrDefault(item => item.Id == state.PlayerFactionId);
        _date.Text = $"{state.ScenarioName} · {state.Year}年{state.Month}月 · 第{state.Turn}回合\n{playerFaction?.ShortName ?? "己"}方 {owned}城 · 九城归心 {state.NineCityControlMonths}/{GameSession.StrategicVictoryRequiredMonths}月";
        var delta = _runtime.PreviewEndTurnResourceDelta();
        _resources.Text = $"金 {state.Resources.Gold:N0} {FormatDelta(delta.Gold)}　　粮 {state.Resources.Food:N0} {FormatDelta(delta.Food)}\n" +
            $"威望 {state.Resources.Prestige:N0} {FormatDelta(delta.Prestige)}";
        foreach (var city in state.Cities)
        {
            _citiesById[city.Id] = city;
            if (!_cityButtons.TryGetValue(city.Id, out var button)) continue;
            if (!_cityLabels.TryGetValue(city.Id, out var markerLabel)) continue;
            var faction = _factionsById.GetValueOrDefault(city.OwnerFactionId);
            markerLabel.Text = city.Name;
            markerLabel.AddThemeColorOverride("font_color", city.Id == _selectedCity?.Id ? GameTheme.Bronze : GameTheme.Paper);
            button.TooltipText = $"{city.Name} · {faction?.Name ?? "未知势力"}";
            if (_cityFlagLabels.TryGetValue(city.Id, out var flagLabel)) flagLabel.Text = faction?.ShortName ?? "城";
            if (_cityFlagMaterials.TryGetValue(city.Id, out var flagMaterial)) flagMaterial.SetShaderParameter("faction_color", faction is null ? Color.FromHtml("#777777") : Color.FromHtml(faction.Color));
            var filter = _factionFilter is null || _factionFilter.Selected < 0 ? "all" : _factionFilter.GetItemMetadata(_factionFilter.Selected).AsString();
            button.Visible = filter == "all" || filter == city.OwnerFactionId;
        }
        RefreshArmyMarkers();
        if (_selectedCity is not null) SelectCity(City(_selectedCity.Id) ?? _selectedCity);
    }

    private CityData? City(string id) => _runtime.State.Cities.FirstOrDefault(item => item.Id == id);
    private CityData? StatePlayerCity() => _runtime.State.Cities.FirstOrDefault(item => item.OwnerFactionId == _runtime.State.PlayerFactionId);
    private static string FormatDelta(int value) => value.ToString("+#,0;-#,0;0");
    private string DiplomacyValue(string factionId)
    {
        if (factionId == _runtime.State.PlayerFactionId) return "己方";
        var relation = _runtime.State.Diplomacy.FirstOrDefault(item => item.FactionId == factionId);
        return relation is null ? "未知" : $"关系{relation.Relation:+#;-#;0}";
    }

    private Vector2 ToMapPoint(MapPosition point)
    {
        return new Vector2((point.X / 100.0f - 0.5f) * _mapSize.X, (point.Y / 100.0f - 0.5f) * _mapSize.Y);
    }

    private void UpdatePerformanceLabel()
    {
        if (_performance is null)
        {
            return;
        }
        _performance.Text = $"视野 {_zoom:0.0}×　地图 transform 更新 {_transformUpdates:N0} 次　内容节点构建 {_contentBuilds} 次";
    }

    private static string Compact(int value)
    {
        return value >= 10_000 ? $"{value / 10_000.0f:0.#}万" : value.ToString("N0");
    }
}
