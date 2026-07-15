using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class Battlefield3DScene : SubViewportContainer
{
    private const float HeaderHeight = 92;
    private readonly SubViewport _viewport = new();
    private readonly Node3D _world = new();
    private readonly Node3D _terrainRoot = new();
    private readonly Node3D _unitRoot = new();
    private readonly Camera3D _camera = new();
    private readonly Dictionary<string, Node3D> _groupNodes = [];
    private readonly Dictionary<string, Node3D> _officerNodes = [];
    private PendingBattleData? _battle;
    private Vector3 _focus = Vector3.Zero;
    private float _cameraSize = 17;
    private double _time;

    public Battlefield3DScene()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ShowBehindParent = true;
        Stretch = true;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetTop = HeaderHeight;

        _viewport.TransparentBg = false;
        _viewport.OwnWorld3D = true;
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        AddChild(_viewport);
        _viewport.AddChild(_world);
        _world.AddChild(_terrainRoot);
        _world.AddChild(_unitRoot);
        BuildLighting();
        _world.AddChild(_camera);
        _camera.Projection = Camera3D.ProjectionType.Orthogonal;
        _camera.Current = true;
        _camera.Near = .1f;
        _camera.Far = 120;
        UpdateCamera();
    }

    public void SetBattle(PendingBattleData? battle)
    {
        if (ReferenceEquals(_battle, battle))
        {
            UpdateUnits();
            return;
        }
        _battle = battle;
        _time = 0;
        ClearChildren(_terrainRoot);
        ClearChildren(_unitRoot);
        _groupNodes.Clear();
        _officerNodes.Clear();
        if (_battle is null) return;
        BuildTerrain();
        BuildUnits();
        ResetCamera();
    }

    public void SetPlaybackTime(double value)
    {
        _time = Math.Max(0, value);
        UpdateUnits();
    }

    public void ResetCamera()
    {
        _focus = Vector3.Zero;
        _cameraSize = 17;
        UpdateCamera();
    }

    public void Zoom(float factor)
    {
        _cameraSize = Math.Clamp(_cameraSize / factor, 10, 28);
        UpdateCamera();
    }

    public void Pan(Vector2 screenDelta)
    {
        var unitsPerPixel = _cameraSize / Math.Max(240, Size.Y);
        _focus.X = Math.Clamp(_focus.X - screenDelta.X * unitsPerPixel, -6.5f, 6.5f);
        _focus.Z = Math.Clamp(_focus.Z - screenDelta.Y * unitsPerPixel * 1.25f, -5.5f, 5.5f);
        UpdateCamera();
    }

    public void CenterOnLogical(float logicalX, float logicalY)
    {
        var point = LogicalToWorld(logicalX, logicalY, false);
        _focus.X = Math.Clamp(point.X, -6.5f, 6.5f);
        _focus.Z = Math.Clamp(point.Z, -5.5f, 5.5f);
        UpdateCamera();
    }

    public Vector2 ProjectLogical(float logicalX, float logicalY, float lift = 0)
    {
        var world = LogicalToWorld(logicalX, logicalY);
        world.Y += lift;
        return _camera.UnprojectPosition(world) + new Vector2(0, HeaderHeight);
    }

    public Vector2 ScreenToLogical(Vector2 canvasPoint)
    {
        var viewportPoint = canvasPoint - new Vector2(0, HeaderHeight);
        var origin = _camera.ProjectRayOrigin(viewportPoint);
        var direction = _camera.ProjectRayNormal(viewportPoint);
        var hit = new Plane(Vector3.Up, 0).IntersectsRay(origin, direction);
        return hit is Vector3 point ? WorldToLogical(point) : new Vector2(500, 500);
    }

    public Rect2 ApproximateVisibleLogicalRect()
    {
        var topLeft = ScreenToLogical(new Vector2(0, HeaderHeight));
        var bottomRight = ScreenToLogical(new Vector2(Size.X, Size.Y + HeaderHeight));
        var min = new Vector2(Math.Min(topLeft.X, bottomRight.X), Math.Min(topLeft.Y, bottomRight.Y));
        var max = new Vector2(Math.Max(topLeft.X, bottomRight.X), Math.Max(topLeft.Y, bottomRight.Y));
        return new Rect2(min, max - min);
    }

    private void BuildLighting()
    {
        var environment = new global::Godot.Environment
        {
            BackgroundMode = global::Godot.Environment.BGMode.Color,
            BackgroundColor = Color.FromHtml("#a9bca3"),
            AmbientLightSource = global::Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Color.FromHtml("#d8d1b8"),
            AmbientLightEnergy = .72f,
        };
        _world.AddChild(new WorldEnvironment { Environment = environment });
        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-52, -34, 0),
            LightColor = Color.FromHtml("#ffe5b5"),
            LightEnergy = 1.05f,
            ShadowEnabled = true,
        };
        _world.AddChild(sun);
    }

    private void BuildTerrain()
    {
        var groundColor = _battle!.Terrain switch
        {
            "mountain" => Color.FromHtml("#6f7858"),
            "hill" => Color.FromHtml("#71835a"),
            "river" => Color.FromHtml("#758a65"),
            _ => Color.FromHtml("#778c61"),
        };
        AddMesh(_terrainRoot, new PlaneMesh { Size = new Vector2(42, 22), Orientation = PlaneMesh.OrientationEnum.Y }, groundColor, Vector3.Zero);
        AddMesh(_terrainRoot, new PlaneMesh { Size = new Vector2(2.0f, 20), Orientation = PlaneMesh.OrientationEnum.Y }, Color.FromHtml("#a99068"), new Vector3(0, .025f, 0));

        foreach (var zone in _battle.TerrainZones)
        {
            var center = LogicalToWorld(zone.X, zone.Y, false);
            if (zone.Type == "hill") AddHill(center, zone);
            else if (zone.Type == "forest") AddForest(center, zone);
            else if (zone.Type == "shallow") AddShallow(center, zone);
        }
        if (_battle.BattleType == "siege") AddFortification();
    }

    private void AddHill(Vector3 center, BattleTerrainZoneData zone)
    {
        var hill = AddMesh(_terrainRoot, new SphereMesh { Radius = 1, Height = 2 }, Color.FromHtml("#687c50"), new Vector3(center.X, -.30f, center.Z));
        hill.Scale = new Vector3(zone.RadiusY / 29f, .55f + zone.Height * .12f, zone.RadiusX / 45f);
        for (var i = 0; i < 7; i++)
        {
            var angle = i * Mathf.Tau / 7f;
            AddTree(new Vector3(center.X + Mathf.Cos(angle) * zone.RadiusY / 43f, TerrainHeight(zone.X, zone.Y), center.Z + Mathf.Sin(angle) * zone.RadiusX / 80f), .82f);
        }
    }

    private void AddForest(Vector3 center, BattleTerrainZoneData zone)
    {
        for (var i = 0; i < 18; i++)
        {
            var angle = i * 2.39996f;
            var radius = .35f + (i % 6) / 6f;
            var x = center.X + Mathf.Cos(angle) * radius * zone.RadiusY / 30f;
            var z = center.Z + Mathf.Sin(angle) * radius * zone.RadiusX / 50f;
            AddTree(new Vector3(x, .04f, z), .72f + i % 4 * .08f);
        }
    }

    private void AddShallow(Vector3 center, BattleTerrainZoneData zone)
    {
        var water = AddMesh(_terrainRoot,
            new PlaneMesh { Size = new Vector2(zone.RadiusY / 14f, Math.Max(1.0f, zone.RadiusX / 40f)), Orientation = PlaneMesh.OrientationEnum.Y },
            new Color(.28f, .55f, .58f, .88f), new Vector3(center.X, .055f, center.Z));
        water.RotationDegrees = new Vector3(0, -4, 0);
        AddMesh(_terrainRoot, new PlaneMesh { Size = new Vector2(2.25f, Math.Max(1.1f, zone.RadiusX / 38f)), Orientation = PlaneMesh.OrientationEnum.Y }, Color.FromHtml("#b6a67c"), new Vector3(center.X, .065f, center.Z));
    }

    private void AddTree(Vector3 position, float scale)
    {
        var tree = new Node3D { Position = position, Scale = Vector3.One * scale };
        _terrainRoot.AddChild(tree);
        AddMesh(tree, new CylinderMesh { TopRadius = .09f, BottomRadius = .13f, Height = .75f }, Color.FromHtml("#5c402a"), new Vector3(0, .38f, 0));
        AddMesh(tree, new CylinderMesh { TopRadius = 0, BottomRadius = .48f, Height = 1.25f }, Color.FromHtml("#315e3d"), new Vector3(0, 1.05f, 0));
        AddMesh(tree, new CylinderMesh { TopRadius = 0, BottomRadius = .34f, Height = .92f }, Color.FromHtml("#47764a"), new Vector3(0, 1.55f, 0));
    }

    private void AddFortification()
    {
        var sign = _battle!.PlayerSide == "attacker" ? 1 : -1;
        var wallZ = (360 - 500) / 500f * 9.5f * sign;
        for (var i = -12; i <= 12; i++)
        {
            if (Math.Abs(i) <= 1) continue;
            AddMesh(_terrainRoot, new BoxMesh { Size = new Vector3(1.55f, 1.25f, .55f) }, Color.FromHtml("#77684f"), new Vector3(i * 1.55f, .62f, wallZ));
        }
        foreach (var x in new[] { -12.4f, 12.4f })
            AddMesh(_terrainRoot, new BoxMesh { Size = new Vector3(1.2f, 2.25f, 1.2f) }, Color.FromHtml("#6a5b45"), new Vector3(x, 1.12f, wallZ));
        AddMesh(_terrainRoot, new BoxMesh { Size = new Vector3(2.7f, 1.05f, .68f) }, Color.FromHtml("#4b3022"), new Vector3(0, .52f, wallZ));
    }

    private void BuildUnits()
    {
        foreach (var group in _battle!.Groups)
        {
            var root = new Node3D { Name = group.Id };
            _unitRoot.AddChild(root);
            _groupNodes[group.Id] = root;
            var texture = GD.Load<Texture2D>(AssetPaths.TroopSprite(group.TroopType));
            for (var i = 0; i < 5; i++)
            {
                var sprite = new Sprite3D
                {
                    Texture = texture,
                    Hframes = 4,
                    Vframes = 2,
                    PixelSize = .0105f,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    Shaded = false,
                    DoubleSided = true,
                    Position = new Vector3((i % 3 - 1) * .32f, .78f + (i / 3) * .03f, (i / 3) * .28f - .15f),
                    Modulate = group.Side == _battle.PlayerSide ? new Color(.88f, 1, .90f) : new Color(1, .86f, .82f),
                };
                root.AddChild(sprite);
            }
        }
        foreach (var officer in _battle.OfficerUnits)
        {
            var root = new Node3D { Name = officer.OfficerId };
            _unitRoot.AddChild(root);
            _officerNodes[officer.OfficerId] = root;
            root.AddChild(new Sprite3D
            {
                Texture = GD.Load<Texture2D>(AssetPaths.MountedOfficerSprite(officer.SpriteId)),
                Hframes = 4,
                Vframes = 2,
                PixelSize = .014f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Shaded = false,
                DoubleSided = true,
                Position = new Vector3(0, 1.0f, 0),
            });
        }
        UpdateUnits();
    }

    private void UpdateUnits()
    {
        if (_battle is null) return;
        var alpha = _battle.Status == "running" ? (float)Math.Clamp(_battle.SimulationAccumulator / .1, 0, 1) : 1;
        alpha = alpha * alpha * (3 - 2 * alpha);
        foreach (var group in _battle.Groups)
        {
            if (!_groupNodes.TryGetValue(group.Id, out var root)) continue;
            var x = Mathf.Lerp(group.PreviousX, group.X, alpha);
            var y = Mathf.Lerp(group.PreviousY, group.Y, alpha);
            root.Position = LogicalToWorld(x, y);
            root.Visible = (_battle.Status == "planning" ? group.InitialSoldiers : group.FinalSoldiers) > 0;
            var moving = group.State is "move" or "advance";
            var column = moving ? (int)(_time * 7 + group.Id.Length) % 4 : group.State.Contains("attack") || group.State == "siege" ? 2 + (int)(_time * 6) % 2 : 0;
            var row = group.Side == "attacker" ? 0 : 1;
            foreach (var sprite in root.GetChildren().OfType<Sprite3D>()) sprite.Frame = row * 4 + column;
        }
        foreach (var officer in _battle.OfficerUnits)
        {
            if (!_officerNodes.TryGetValue(officer.OfficerId, out var root)) continue;
            var x = Mathf.Lerp(officer.PreviousX, officer.X, alpha);
            var y = Mathf.Lerp(officer.PreviousY, officer.Y, alpha);
            root.Position = LogicalToWorld(x, y) + new Vector3(0, .05f, 0);
            foreach (var sprite in root.GetChildren().OfType<Sprite3D>()) sprite.Frame = (officer.Side == "attacker" ? 0 : 4) + (int)(_time * 6 + officer.OfficerId.Length) % 4;
        }
    }

    private Vector3 LogicalToWorld(float logicalX, float logicalY, bool includeHeight = true)
    {
        var sign = _battle?.PlayerSide == "attacker" ? 1 : -1;
        return new Vector3(
            (logicalY - 500) / 500f * 18f,
            includeHeight ? TerrainHeight(logicalX, logicalY) : 0,
            (logicalX - 500) / 500f * 9.5f * sign);
    }

    private Vector2 WorldToLogical(Vector3 point)
    {
        var sign = _battle?.PlayerSide == "attacker" ? 1 : -1;
        return new Vector2(
            Math.Clamp(500 + point.Z / (9.5f * sign) * 500, 0, 1000),
            Math.Clamp(500 + point.X / 18f * 500, 0, 1000));
    }

    private float TerrainHeight(float logicalX, float logicalY)
    {
        if (_battle is null) return 0;
        var height = 0f;
        foreach (var zone in _battle.TerrainZones.Where(item => item.Type == "hill"))
        {
            var distance = MathF.Pow((logicalX - zone.X) / Math.Max(1, zone.RadiusX), 2) + MathF.Pow((logicalY - zone.Y) / Math.Max(1, zone.RadiusY), 2);
            if (distance < 1) height = Math.Max(height, (1 - distance) * (.45f + zone.Height * .16f));
        }
        return height;
    }

    private void UpdateCamera()
    {
        _camera.Size = _cameraSize;
        var position = _focus + new Vector3(0, 17.5f, 16.5f);
        _camera.LookAtFromPosition(position, _focus, Vector3.Up);
    }

    private static MeshInstance3D AddMesh(Node parent, Mesh mesh, Color color, Vector3 position)
    {
        var material = new StandardMaterial3D { AlbedoColor = color, Roughness = 1 };
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        parent.AddChild(instance);
        return instance;
    }

    private static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren()) child.QueueFree();
    }
}
