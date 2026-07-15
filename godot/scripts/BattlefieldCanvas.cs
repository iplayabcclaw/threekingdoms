using Godot;

namespace ThreeKingdomsSimulator.Godot;

public partial class BattlefieldCanvas : Control
{
    private const float HeaderHeight = 74;
    private const float LogicalExtent = 1000;
    private const float WorldWidth = 2400;
    private const float WorldHeight = 1350;
    private const float BattleGroundTop = .30f;
    private const float BattleGroundHeight = .64f;
    private const float MinimumZoom = .42f;
    private const float MaximumZoom = 1.35f;
    private readonly Dictionary<string, Texture2D> _troopTextures = [];
    private readonly Dictionary<string, Texture2D> _officerTextures = [];
    private readonly Dictionary<string, string> _activeActions = [];
    private readonly Dictionary<string, string> _activeOfficerActions = [];
    private readonly List<BattleTimelineEventData> _activeVolleys = [];
    private readonly List<BattleTimelineEventData> _activeDamage = [];
    private PendingBattleData? _battle;
    private BattleTimelineEventData? _latestActiveEvent;
    private Texture2D? _background;
    private double _time;
    private readonly HashSet<string> _selectedGroupIds = [];
    private string _selectedEnemyId = string.Empty;
    private Vector2 _dragStart;
    private Vector2 _dragCurrent;
    private bool _dragSelecting;
    private Vector2 _cameraCenter = new(WorldWidth / 2, WorldHeight * .62f);
    private float _cameraZoom = .58f;
    private Vector2 _worldOrigin;
    private bool _cameraDragging;
    private bool _reducedMotion;
    private Vector2 _cameraDragStart;
    private Vector2 _cameraCenterAtDragStart;

    public event Action<IReadOnlyCollection<string>>? FriendlySelectionChanged;
    public event Action<string>? EnemySelectionChanged;
    public event Action<IReadOnlyCollection<string>, string>? AttackCommandIssued;
    public event Action<IReadOnlyCollection<string>, Vector2>? MoveCommandIssued;
    public IReadOnlyCollection<string> SelectedGroupIds => _selectedGroupIds;
    public string SelectedEnemyId => _selectedEnemyId;

    public BattlefieldCanvas()
    {
        CustomMinimumSize = new Vector2(0, 610);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Cross;
        ClipContents = true;
    }

    public void SetBattle(PendingBattleData? battle)
    {
        if (ReferenceEquals(_battle, battle))
        {
            EnsureBattleTextures();
            QueueRedraw();
            return;
        }
        _battle = battle;
        _time = 0;
        _selectedGroupIds.Clear();
        _selectedEnemyId = string.Empty;
        ResetCameraView();
        _background = battle is null ? null : GD.Load<Texture2D>(battle.BattleType == "siege" ? AssetPaths.SiegeBackground(battle.Region) : AssetPaths.BattleBackground(battle.Terrain));
        _troopTextures.Clear();
        _officerTextures.Clear();
        EnsureBattleTextures();
        QueueRedraw();
    }

    private void EnsureBattleTextures()
    {
        if (_battle is null) return;
        foreach (var troop in _battle.Groups.Select(item => item.TroopType).Distinct())
            if (!_troopTextures.ContainsKey(troop)) _troopTextures[troop] = GD.Load<Texture2D>(AssetPaths.TroopSprite(troop));
        foreach (var spriteId in _battle.OfficerUnits.Select(item => item.SpriteId).Distinct())
            if (!_officerTextures.ContainsKey(spriteId)) _officerTextures[spriteId] = GD.Load<Texture2D>(AssetPaths.MountedOfficerSprite(spriteId));
    }

    public void SetPlaybackTime(double value)
    {
        _time = Math.Max(0, value);
        QueueRedraw();
    }

    public void SetReducedMotion(bool value)
    {
        _reducedMotion = value;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), GameTheme.Backdrop);
        if (_battle is null)
        {
            DrawString(GetThemeDefaultFont(), new Vector2(40, 70), "尚无待演算战斗", HorizontalAlignment.Left, -1, 24, GameTheme.Paper);
            return;
        }

        ClampCamera();
        ApplyWorldTransform();
        if (_background is not null)
        {
            if (_battle?.PlayerSide == "defender")
            {
                DrawSetTransform(_worldOrigin + new Vector2(WorldWidth * _cameraZoom, 0), 0, new Vector2(-_cameraZoom, _cameraZoom));
                DrawTextureRect(_background, new Rect2(0, 0, WorldWidth, WorldHeight), false, new Color(.98f, .98f, .95f, 1));
                ApplyWorldTransform();
            }
            else DrawTextureRect(_background, new Rect2(0, 0, WorldWidth, WorldHeight), false, new Color(.98f, .98f, .95f, 1));
        }
        DrawRect(new Rect2(0, 0, WorldWidth, WorldHeight), new Color(.035f, .04f, .025f, .055f));

        BuildActiveEventCache();
        DrawRangeBands();
        DrawFortification();
        foreach (var group in _battle!.Groups.OrderBy(item => item.Y).ThenByDescending(item => item.Depth)) DrawGroup(group);
        foreach (var officer in _battle.OfficerUnits.OrderBy(item => item.Y)) DrawMountedOfficer(officer);
        DrawCommandOverlay();
        DrawProjectiles();
        DrawDamageNumbers();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawBattleHeader();
        DrawMiniMap();
        DrawCameraHelp();
        if (_dragSelecting) DrawSelectionBox();
    }

    public void ResetCameraView()
    {
        _cameraZoom = .58f;
        _cameraCenter = new Vector2(WorldWidth / 2, WorldHeight * .62f);
        ClampCamera();
        QueueRedraw();
    }

    public void SelectAllPlayerGroups()
    {
        _selectedGroupIds.Clear();
        if (_battle is not null)
        {
            foreach (var group in _battle.Groups.Where(item => item.Side == _battle.PlayerSide && CurrentSoldiers(item) > 0 && !item.IsRouted && item.Morale >= 25)) _selectedGroupIds.Add(group.Id);
        }
        FriendlySelectionChanged?.Invoke(_selectedGroupIds);
        QueueRedraw();
    }

    public void IssueAttackOnSelectedEnemy()
    {
        if (_selectedGroupIds.Count == 0 || string.IsNullOrEmpty(_selectedEnemyId)) return;
        AttackCommandIssued?.Invoke(_selectedGroupIds, _selectedEnemyId);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (_battle is null) return;
        switch (inputEvent)
        {
            case InputEventMouseButton button when (button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) && button.Pressed:
                ZoomCamera(button.Position, button.ButtonIndex == MouseButton.WheelUp ? 1.12f : 1 / 1.12f);
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Middle && button.Pressed:
                _cameraDragging = true;
                _cameraDragStart = button.Position;
                _cameraCenterAtDragStart = _cameraCenter;
                MouseDefaultCursorShape = CursorShape.Drag;
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _cameraDragging:
                _cameraCenter = _cameraCenterAtDragStart - (motion.Position - _cameraDragStart) / _cameraZoom;
                ClampCamera();
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Middle && !button.Pressed:
                _cameraDragging = false;
                MouseDefaultCursorShape = CursorShape.Cross;
                AcceptEvent();
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Left && button.Pressed && MiniMapRect().HasPoint(button.Position):
                CenterCameraFromMiniMap(button.Position);
                AcceptEvent();
                break;
            case InputEventMouseButton when _battle.Status != "running":
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Left && button.Pressed:
                _dragStart = button.Position;
                _dragCurrent = button.Position;
                _dragSelecting = true;
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragSelecting:
                _dragCurrent = motion.Position;
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Left && !button.Pressed:
                _dragCurrent = button.Position;
                CompleteSelection(button.ShiftPressed);
                _dragSelecting = false;
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Right && button.Pressed:
                if (_selectedGroupIds.Count > 0)
                {
                    var clicked = HitTestGroup(button.Position);
                    if (clicked is not null && clicked.Side != _battle.PlayerSide)
                    {
                        _selectedEnemyId = clicked.Id;
                        EnemySelectionChanged?.Invoke(clicked.Id);
                        AttackCommandIssued?.Invoke(_selectedGroupIds, clicked.Id);
                    }
                    else MoveCommandIssued?.Invoke(_selectedGroupIds, ScreenToLogical(button.Position));
                    QueueRedraw();
                }
                AcceptEvent();
                break;
        }
    }

    private void CompleteSelection(bool append)
    {
        if (_battle is null) return;
        var selectionRect = RectFromPoints(_dragStart, _dragCurrent);
        if (selectionRect.Size.Length() < 8)
        {
            var clicked = HitTestGroup(_dragCurrent);
            if (clicked is null)
            {
                if (!append) _selectedGroupIds.Clear();
            }
            else if (clicked.Side == _battle.PlayerSide)
            {
                if (!append) _selectedGroupIds.Clear();
                if (append && _selectedGroupIds.Contains(clicked.Id)) _selectedGroupIds.Remove(clicked.Id);
                else _selectedGroupIds.Add(clicked.Id);
            }
            else
            {
                _selectedEnemyId = clicked.Id;
                EnemySelectionChanged?.Invoke(clicked.Id);
            }
        }
        else
        {
            if (!append) _selectedGroupIds.Clear();
            foreach (var group in _battle.Groups.Where(item => item.Side == _battle.PlayerSide && CurrentSoldiers(item) > 0 && !item.IsRouted && item.Morale >= 25))
            {
                if (selectionRect.HasPoint(WorldToScreen(BattlePosition(group, true)))) _selectedGroupIds.Add(group.Id);
            }
        }
        FriendlySelectionChanged?.Invoke(_selectedGroupIds);
    }

    private BattleUnitGroupData? HitTestGroup(Vector2 pointer)
    {
        if (_battle is null) return null;
        return _battle.Groups.Where(item => CurrentSoldiers(item) > 0 && (item.Side != _battle.PlayerSide || (!item.IsRouted && item.Morale >= 25)))
            .Select(item => (Group: item, Distance: WorldToScreen(BattlePosition(item, true)).DistanceTo(pointer)))
            .Where(item => item.Distance <= Math.Clamp(48 * _cameraZoom, 24, 62))
            .OrderBy(item => item.Distance)
            .Select(item => item.Group)
            .FirstOrDefault();
    }

    private Vector2 ScreenToLogical(Vector2 point)
    {
        var world = ScreenToWorld(point);
        var x = Math.Clamp(world.X / WorldWidth * LogicalExtent, 0, LogicalExtent);
        if (_battle?.PlayerSide == "defender") x = LogicalExtent - x;
        var y = Math.Clamp((world.Y / WorldHeight - BattleGroundTop) / BattleGroundHeight * LogicalExtent, 0, LogicalExtent);
        return new Vector2(x, y);
    }

    private static Rect2 RectFromPoints(Vector2 first, Vector2 second)
    {
        var position = new Vector2(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y));
        return new Rect2(position, new Vector2(Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y)));
    }

    private void DrawCommandOverlay()
    {
        if (_battle is null) return;
        foreach (var groupId in _selectedGroupIds.ToList())
        {
            var group = _battle.Groups.FirstOrDefault(item => item.Id == groupId && CurrentSoldiers(item) > 0);
            if (group is null) { _selectedGroupIds.Remove(groupId); continue; }
            var position = BattlePosition(group, true);
            DrawArc(position + new Vector2(0, 8), 40, 0, Mathf.Tau, 32, Color.FromHtml("#f3d676"), 2.5f, true);
            if (group.CommandMode == "move")
            {
                var target = LogicalPosition(group.CommandDestinationX, group.CommandDestinationY);
                DrawDashedLine(position, target, new Color(GameTheme.Gold, .66f), 2, 8);
                DrawArc(target, 10, 0, Mathf.Tau, 20, GameTheme.Gold, 2, true);
            }
        }
        if (!string.IsNullOrEmpty(_selectedEnemyId))
        {
            var enemy = _battle.Groups.FirstOrDefault(item => item.Id == _selectedEnemyId && CurrentSoldiers(item) > 0);
            if (enemy is not null)
            {
                var position = BattlePosition(enemy, true);
                DrawArc(position + new Vector2(0, 8), 43, 0, Mathf.Tau, 32, Color.FromHtml("#ef785f"), 2.5f, true);
                foreach (var selected in _battle.Groups.Where(item => _selectedGroupIds.Contains(item.Id) && item.CommandTargetGroupId == enemy.Id))
                    DrawDashedLine(BattlePosition(selected, true), position, new Color(.95f, .35f, .25f, .58f), 2, 9);
            }
        }
    }

    private void BuildActiveEventCache()
    {
        _activeActions.Clear();
        _activeOfficerActions.Clear();
        _activeVolleys.Clear();
        _activeDamage.Clear();
        _latestActiveEvent = null;
        if (_battle is null) return;
        for (var index = _battle.Timeline.Count - 1; index >= 0; index--)
        {
            var item = _battle.Timeline[index];
            if (item.Start < _time - 1.5) break;
            if (item.Start > _time || item.Start + Math.Max(.2, item.Duration) < _time) continue;
            if (!string.IsNullOrEmpty(item.GroupId)) _activeActions[item.GroupId] = item.Action;
            if (!string.IsNullOrEmpty(item.OfficerId) && item.Action is "officer-charge" or "officer-command") _activeOfficerActions[item.OfficerId] = item.Action;
            if (item.Action == "volley") _activeVolleys.Add(item);
            else if (item.Action == "damage") _activeDamage.Add(item);
            if (_latestActiveEvent is null || item.Start >= _latestActiveEvent.Start) _latestActiveEvent = item;
        }
    }

    private void DrawBattleHeader()
    {
        var stage = _battle!.Status == "planning" ? "战前布阵" : _battle.Status == "resolved" ? "战斗结束" : _latestActiveEvent?.Stage ?? "实时交战";
        var hud = new Color(.025f, .035f, .025f, .78f);
        DrawRect(new Rect2(10, 6, 336, 62), hud);
        DrawRect(new Rect2(Size.X - 346, 6, 336, 62), hud);
        DrawRect(new Rect2(Size.X / 2 - 96, 6, 192, 62), new Color(.025f, .035f, .025f, .84f));
        var enemySide = _battle.PlayerSide == "attacker" ? "defender" : "attacker";
        var font = GetThemeDefaultFont();
        DrawForceBar(enemySide, 18, false, Color.FromHtml("#d79077"), "敌军（左侧）");
        DrawForceBar(_battle.PlayerSide, Size.X - 338, true, Color.FromHtml("#88c9a3"), "我军（右侧）");
        var remaining = _battle.Status == "planning" ? _battle.Duration : Math.Max(0, _battle.Duration - _battle.Elapsed);
        DrawString(font, new Vector2(Size.X / 2 - 90, 22), stage, HorizontalAlignment.Center, 180, 15, GameTheme.OnAccent);
        DrawString(font, new Vector2(Size.X / 2 - 78, 51), $"{Math.Ceiling(remaining):00} 秒", HorizontalAlignment.Center, 156, 24, Color.FromHtml("#f0d58a"));
        if (_battle.BattleType == "siege")
        {
            var structureX = _battle.PlayerSide == "defender" ? Size.X - 250 : 18;
            DrawStructureBar("外墙", _battle.WallBefore, _battle.WallAfter, structureX, 51);
            DrawStructureBar("城门", _battle.GateBefore, _battle.GateAfter, structureX, 62);
        }
    }

    private void DrawForceBar(string side, float x, bool alignRight, Color color, string label = "")
    {
        var before = side == "attacker" ? _battle!.AttackerBefore : _battle!.DefenderBefore;
        var current = _battle.Groups.Where(item => item.Side == side).Sum(item => CurrentSoldiers(item));
        var groups = _battle.Groups.Where(item => item.Side == side && CurrentSoldiers(item) > 0).ToList();
        var morale = groups.Count == 0 ? 0 : groups.Sum(item => item.Morale * CurrentSoldiers(item)) / Math.Max(1, current);
        var routed = groups.Count(item => item.IsRouted);
        var name = string.IsNullOrEmpty(label) ? (_battle.BattleType == "field" ? side == "attacker" ? "出击军" : "敌军" : side == "attacker" ? "攻方" : "守方") : label;
        var alignment = alignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        DrawString(GetThemeDefaultFont(), new Vector2(x, 23), $"{name} {current:N0}/{before:N0} · 士气{morale:F0}{(routed > 0 ? $" · 溃{routed}" : "")}", alignment, 320, 15, color);
        DrawRect(new Rect2(x, 31, 320, 10), new Color(.04f, .05f, .04f, .92f));
        var width = 320 * Math.Clamp(current / (float)Math.Max(1, before), 0, 1);
        var barX = alignRight ? x + 320 - width : x;
        DrawRect(new Rect2(barX, 31, width, 10), color);
    }

    private void DrawStructureBar(string label, int before, int current, float x, float y)
    {
        const float width = 140;
        DrawString(GetThemeDefaultFont(), new Vector2(x, y + 8), label, HorizontalAlignment.Left, 38, 10, GameTheme.OnAccent);
        DrawRect(new Rect2(x + 40, y, width, 7), new Color(.05f, .06f, .05f, .9f));
        DrawRect(new Rect2(x + 40, y, width * Math.Clamp(current / (float)Math.Max(1, before), 0, 1), 7), current < before ? Color.FromHtml("#c27d4b") : GameTheme.Bronze);
    }

    private void DrawRangeBands()
    {
        if (_battle is null || _battle.Status != "planning") return;
        var guide = new Color(.84f, .72f, .42f, .09f);
        var groundTop = WorldHeight * BattleGroundTop;
        var groundBottom = WorldHeight * (BattleGroundTop + BattleGroundHeight);
        for (var lane = 0; lane < 5; lane++)
        {
            var y = (BattleGroundTop + (120 + lane * 170) / LogicalExtent * BattleGroundHeight) * WorldHeight;
            DrawLine(new Vector2(0, y), new Vector2(WorldWidth, y), guide, 1);
        }
        foreach (var x in new[] { .08f, .15f, .22f, .78f, .85f, .92f })
            DrawLine(new Vector2(WorldWidth * x, groundTop), new Vector2(WorldWidth * x, groundBottom), guide, 1);
        foreach (var group in _battle.Groups.Where(item => item.TroopType == "archers" && item.Side == _battle.PlayerSide))
        {
            var position = BattlePosition(group, false);
            var direction = group.Side == "attacker" ? -1 : 1;
            if (_battle.PlayerSide == "defender") direction *= -1;
            var width = WorldWidth * group.MaximumRange / LogicalExtent;
            var rect = direction < 0 ? new Rect2(position.X - width, position.Y - 28, width, 56) : new Rect2(position.X, position.Y - 28, width, 56);
            DrawRect(rect, new Color(.76f, .61f, .25f, .055f));
        }
    }

    private void DrawFortification()
    {
        if (_battle?.BattleType != "siege") return;
        var defenderOnRight = _battle.PlayerSide == "defender";
        var wallX = defenderOnRight ? WorldWidth * .64f : WorldWidth * .36f;
        var fieldTop = WorldHeight * BattleGroundTop;
        var fieldBottom = WorldHeight * (BattleGroundTop + BattleGroundHeight);
        var gateY = (fieldTop + fieldBottom) * .5f;
        var zone = defenderOnRight
            ? new Rect2(wallX, fieldTop, WorldWidth - wallX, fieldBottom - fieldTop)
            : new Rect2(0, fieldTop, wallX, fieldBottom - fieldTop);
        DrawRect(zone, new Color(.12f, .10f, .065f, .11f));

        var wallRatio = Math.Clamp(_battle.WallAfter / (float)Math.Max(1, _battle.WallBefore), 0, 1);
        var gateRatio = Math.Clamp(_battle.GateAfter / (float)Math.Max(1, _battle.GateBefore), 0, 1);
        const float gateHalfHeight = 90;
        const float wallWidth = 96;
        const int segmentCount = 8;
        var upperLength = gateY - gateHalfHeight - fieldTop;
        var lowerStart = gateY + gateHalfHeight;
        var lowerLength = fieldBottom - lowerStart;
        for (var index = 0; index < segmentCount; index++)
        {
            var upper = index < segmentCount / 2;
            var localIndex = upper ? index : index - segmentCount / 2;
            var localCount = segmentCount / 2;
            var length = upper ? upperLength : lowerLength;
            var start = upper ? fieldTop : lowerStart;
            var segmentLength = length / localCount;
            var y = start + localIndex * segmentLength;
            var damageRank = (index * 5 + 3) % segmentCount;
            var intact = wallRatio > damageRank / (float)segmentCount;
            if (intact) DrawWallSegment(wallX, y + 2, segmentLength - 4, wallWidth, defenderOnRight, wallRatio);
            else DrawWallRubble(wallX, y + segmentLength * .52f, wallWidth, index);
        }
        DrawGatehouse(wallX, gateY, wallWidth, gateHalfHeight, defenderOnRight, gateRatio);

        var labelX = defenderOnRight ? wallX + 48 : wallX - 156;
        DrawRect(new Rect2(labelX, fieldTop + 18, 108, 24), new Color(.025f, .035f, .025f, .76f));
        DrawString(GetThemeDefaultFont(), new Vector2(labelX + 4, fieldTop + 35), "城内守备区", HorizontalAlignment.Center, 100, 11, Color.FromHtml("#f0dfb8"));
    }

    private void DrawWallSegment(float x, float y, float height, float width, bool defenderOnRight, float integrity)
    {
        var stone = Color.FromHtml("#71634d").Lerp(Color.FromHtml("#514438"), 1 - integrity);
        var light = stone.Lightened(.11f);
        var shadow = stone.Darkened(.28f);
        var left = x - width / 2;
        DrawRect(new Rect2(left, y, width, height), shadow);
        var frontX = defenderOnRight ? left : x + width * .05f;
        DrawRect(new Rect2(frontX, y + 4, width * .45f, height - 8), stone);
        DrawColoredPolygon(
        [
            new Vector2(left, y), new Vector2(x + width / 2, y),
            new Vector2(x + width / 2 - 8, y + 7), new Vector2(left - 8, y + 7),
        ], light);
        for (var blockY = y + 18; blockY < y + height - 6; blockY += 22)
        {
            DrawLine(new Vector2(left + 2, blockY), new Vector2(x + width / 2 - 2, blockY), new Color(.22f, .18f, .13f, .58f), 1.4f);
            var seamOffset = ((int)((blockY - y) / 22) % 2) * width * .25f;
            DrawLine(new Vector2(left + width * .5f + seamOffset - width * .25f, blockY - 20), new Vector2(left + width * .5f + seamOffset - width * .25f, blockY), new Color(.22f, .18f, .13f, .48f), 1.2f);
        }
        var battlementX = defenderOnRight ? left - 8 : x + width / 2 - 2;
        for (var merlonY = y + 5; merlonY < y + height - 10; merlonY += 28)
            DrawRect(new Rect2(battlementX, merlonY, 10, 17), light);
        if (integrity < .72f)
        {
            DrawLine(new Vector2(x - 6, y + height * .25f), new Vector2(x + 12, y + height * .43f), new Color(.18f, .12f, .08f, .86f), 3);
            DrawLine(new Vector2(x + 12, y + height * .43f), new Vector2(x - 3, y + height * .62f), new Color(.18f, .12f, .08f, .76f), 2);
        }
    }

    private void DrawGatehouse(float x, float y, float wallWidth, float gateHalfHeight, bool defenderOnRight, float integrity)
    {
        var towerColor = Color.FromHtml("#655642").Lerp(Color.FromHtml("#493a2f"), 1 - integrity);
        var towerSize = new Vector2(wallWidth + 42, 66);
        foreach (var towerY in new[] { y - gateHalfHeight - towerSize.Y * .30f, y + gateHalfHeight - towerSize.Y * .70f })
        {
            DrawRect(new Rect2(x - towerSize.X / 2, towerY - towerSize.Y / 2, towerSize.X, towerSize.Y), towerColor.Darkened(.18f));
            DrawColoredPolygon(
            [
                new Vector2(x - towerSize.X / 2 - 12, towerY - towerSize.Y / 2 + 10),
                new Vector2(x - towerSize.X / 2, towerY - towerSize.Y / 2),
                new Vector2(x + towerSize.X / 2, towerY - towerSize.Y / 2),
                new Vector2(x + towerSize.X / 2 - 12, towerY - towerSize.Y / 2 + 10),
            ], towerColor.Lightened(.20f));
        }
        if (integrity > .04f)
        {
            var gateColor = Color.FromHtml("#4a2d20").Lerp(Color.FromHtml("#2b1d18"), 1 - integrity);
            DrawRect(new Rect2(x - wallWidth * .34f, y - gateHalfHeight + 22, wallWidth * .68f, gateHalfHeight * 2 - 44), gateColor);
            for (var plankY = y - gateHalfHeight + 30; plankY < y + gateHalfHeight - 20; plankY += 18)
                DrawLine(new Vector2(x - wallWidth * .31f, plankY), new Vector2(x + wallWidth * .31f, plankY), new Color(.72f, .52f, .30f, .34f), 2);
            DrawLine(new Vector2(x, y - gateHalfHeight + 24), new Vector2(x, y + gateHalfHeight - 24), new Color(.12f, .08f, .06f, .86f), 3);
            if (integrity < .65f)
                DrawLine(new Vector2(x - 18, y - 35), new Vector2(x + 18, y + 28), new Color(.08f, .05f, .04f, .9f), 5);
        }
        else
        {
            DrawWallRubble(x, y - 18, wallWidth + 24, 21);
            DrawWallRubble(x, y + 24, wallWidth + 24, 22);
        }
        var textX = defenderOnRight ? x + 58 : x - 118;
        DrawRect(new Rect2(textX, y - 12, 60, 22), new Color(.025f, .035f, .025f, .82f));
        DrawString(GetThemeDefaultFont(), new Vector2(textX + 2, y + 4), integrity > .04f ? "城门" : "缺口", HorizontalAlignment.Center, 56, 11, integrity > .04f ? Color.FromHtml("#f0dfb8") : Color.FromHtml("#e38a68"));
    }

    private void DrawWallRubble(float x, float y, float width, int seed)
    {
        var rubble = Color.FromHtml("#5b4b3b");
        for (var index = 0; index < 7; index++)
        {
            var offsetX = (seed * 17 + index * 23) % (int)width - width / 2;
            var offsetY = (seed * 11 + index * 13) % 34 - 17;
            var size = 8 + (seed + index * 5) % 12;
            DrawRect(new Rect2(x + offsetX, y + offsetY, size, size * .55f), rubble.Lightened(index % 3 * .06f));
        }
    }

    private void DrawGroup(BattleUnitGroupData group)
    {
        if (!_troopTextures.TryGetValue(group.TroopType, out var texture) || texture is null) return;
        var position = BattlePosition(group, true);
        var current = CurrentSoldiers(group);
        if (current <= 0) return;
        var active = ActiveAction(group.Id);
        var moving = active == "move";
        var attacking = active is "volley" or "charge" or "melee" or "brace" or "siege";
        var animationRate = moving ? 9.5 : attacking ? 8.5 : 3.5;
        var phase = group.Id.Sum(character => character) * .17;
        if (!_reducedMotion && (moving || attacking)) position.Y += MathF.Sin((float)(_time * animationRate + phase)) * (moving ? 1.8f : 1.2f);
        var depthScale = DepthScale(group.Y);
        var frame = (int)(_time * animationRate + phase) % 4 + (attacking ? 4 : 0);
        var textureSize = texture.GetSize();
        var frameSize = new Vector2(textureSize.X / 4f, textureSize.Y / 2f);
        var source = new Rect2(new Vector2(frame % 4 * frameSize.X, frame / 4 * frameSize.Y), frameSize);
        var people = Math.Clamp(1 + (int)Math.Ceiling(group.InitialSoldiers / 150d), 1, 6);
        var color = group.Side == "attacker" ? Color.FromHtml("#99cdb0") : Color.FromHtml("#d58c72");
        if (_battle!.PlayerSide == "defender") color = group.Side == "defender" ? Color.FromHtml("#99cdb0") : Color.FromHtml("#d58c72");
        if (group.IsRouted || group.State == "retreat") color = color.Lerp(Colors.Gray, .58f);
        DrawWorldEllipse(position + new Vector2(2, 22 * depthScale), new Vector2(54, 13) * depthScale, new Color(.015f, .02f, .012f, .30f));
        if (moving && !_reducedMotion) DrawMovementDust(position, depthScale, group.Side == _battle.PlayerSide);
        for (var index = 0; index < people; index++)
        {
            var row = index / 3; var column = index % 3;
            var offset = new Vector2((column - 1) * 27 + row * 5, row * 18) * depthScale;
            var baseSize = group.TroopType == "cavalry" ? new Vector2(82, 70) : group.TroopType == "siege" ? new Vector2(92, 68) : new Vector2(62, 68);
            var size = baseSize * depthScale;
            var destination = new Rect2(position - size / 2 + offset, size);
            var alpha = index >= Math.Ceiling(people * current / (double)Math.Max(1, group.InitialSoldiers)) ? .2f : group.IsRouted ? .42f : 1;
            var colorModulate = new Color(1, 1, 1, alpha);
            DrawTroopFrame(texture, destination, source, colorModulate, group.Side == _battle.PlayerSide);
        }
        var barWidth = 78 * depthScale;
        var barY = position.Y + 52 * depthScale;
        DrawRect(new Rect2(position.X - barWidth / 2, barY, barWidth, 6), new Color(.025f, .035f, .025f, .92f));
        DrawRect(new Rect2(position.X - barWidth / 2, barY, barWidth * current / Math.Max(1f, group.InitialSoldiers), 6), color);
        var moraleColor = group.Morale < 10 ? GameTheme.Danger : group.Morale < 25 ? GameTheme.Bronze : Color.FromHtml("#d9c56f");
        DrawRect(new Rect2(position.X - barWidth / 2, barY + 8, barWidth, 3), new Color(.025f, .035f, .025f, .86f));
        DrawRect(new Rect2(position.X - barWidth / 2, barY + 8, barWidth * (float)Math.Clamp(group.Morale / 100, 0, 1), 3), moraleColor);
        var flagTop = position.Y - 51 * depthScale;
        DrawLine(new Vector2(position.X - 43 * depthScale, flagTop), new Vector2(position.X - 43 * depthScale, flagTop + 29 * depthScale), new Color(.20f, .14f, .08f, .9f), 2);
        DrawRect(new Rect2(position.X - 42 * depthScale, flagTop, 20 * depthScale, 16 * depthScale), color);
        DrawString(GetThemeDefaultFont(), new Vector2(position.X - 39 * depthScale, flagTop + 13 * depthScale), ShortName(group.TroopType), HorizontalAlignment.Center, 14 * depthScale, Math.Max(9, (int)(11 * depthScale)), GameTheme.OnAccent);
        if (group.IsRouted || group.State == "retreat")
            DrawString(GetThemeDefaultFont(), new Vector2(position.X - 42, flagTop - 8), group.IsRouted ? "溃散" : "后撤", HorizontalAlignment.Center, 84, 11, moraleColor);
    }

    private void DrawMountedOfficer(BattleOfficerUnitData officer)
    {
        if (!_officerTextures.TryGetValue(officer.SpriteId, out var texture) || texture is null) return;
        var position = OfficerPosition(officer);
        var active = _activeOfficerActions.GetValueOrDefault(officer.OfficerId) ?? officer.State;
        var attacking = active is "officer-charge" or "officer-command";
        var moving = active == "mounted-move";
        var rate = attacking ? 8.5 : moving ? 7.5 : 3.2;
        var phase = officer.OfficerId.Sum(character => character) * .13;
        if (!_reducedMotion) position.Y += MathF.Sin((float)(_time * rate + phase)) * (moving ? 1.6f : .8f);
        var depthScale = DepthScale(officer.Y);
        var frame = (int)(_time * rate + phase) % 4 + (attacking ? 4 : 0);
        var textureSize = texture.GetSize();
        var frameSize = new Vector2(textureSize.X / 4f, textureSize.Y / 2f);
        var source = new Rect2(new Vector2(frame % 4 * frameSize.X, frame / 4 * frameSize.Y), frameSize);
        var size = (officer.SpriteId == "lu-bu" ? new Vector2(136, 104) : new Vector2(126, 96)) * depthScale;
        var destination = new Rect2(position - size / 2, size);
        DrawWorldEllipse(position + new Vector2(3, size.Y * .34f), new Vector2(size.X * .40f, size.Y * .12f), new Color(.015f, .02f, .012f, .34f));
        if (moving && !_reducedMotion) DrawMovementDust(position, depthScale * 1.12f, officer.Side == _battle!.PlayerSide);
        DrawTroopFrame(texture, destination, source, Colors.White, officer.Side == _battle!.PlayerSide);
        var ownColor = officer.Side == _battle.PlayerSide ? Color.FromHtml("#f2d36f") : Color.FromHtml("#ee8a70");
        DrawArc(position + new Vector2(0, 8), size.X * .43f, 0, Mathf.Tau, 28, new Color(ownColor, .78f), 2, true);
        var plaqueWidth = 96 * depthScale;
        var plaqueY = position.Y - size.Y * .60f;
        DrawRect(new Rect2(position.X - plaqueWidth / 2, plaqueY, plaqueWidth, 19), new Color(.025f, .035f, .025f, .88f));
        DrawRect(new Rect2(position.X - plaqueWidth / 2, plaqueY, plaqueWidth, 19), new Color(ownColor, .48f), false, 1);
        DrawString(GetThemeDefaultFont(), new Vector2(position.X - plaqueWidth / 2 + 2, plaqueY + 14), $"将·{officer.Name} {officer.CombatPower:F1}", HorizontalAlignment.Center, plaqueWidth - 4, 10, ownColor);
    }

    private static float DepthScale(float logicalY) => .78f + Math.Clamp(logicalY / LogicalExtent, 0, 1) * .32f;

    private void DrawMovementDust(Vector2 position, float scale, bool movingLeft)
    {
        var trail = movingLeft ? 1 : -1;
        var pulse = .72f + MathF.Sin((float)_time * 8) * .12f;
        for (var index = 0; index < 3; index++)
        {
            var center = position + new Vector2(trail * (26 + index * 16) * scale, (22 + index * 3) * scale);
            var radii = new Vector2((17 - index * 3) * scale, (6 - index) * scale) * pulse;
            DrawWorldEllipse(center, radii, new Color(.72f, .63f, .45f, .11f - index * .022f));
        }
    }

    private void DrawWorldEllipse(Vector2 center, Vector2 radii, Color color)
    {
        const int segments = 36;
        var points = new Vector2[segments];
        for (var index = 0; index < segments; index++)
        {
            var angle = Mathf.Tau * index / segments;
            points[index] = center + new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y);
        }
        DrawColoredPolygon(points, color);
    }

    private void DrawWorldEllipseOutline(Vector2 center, Vector2 radii, Color color, float width)
    {
        const int segments = 48;
        var points = new Vector2[segments + 1];
        for (var index = 0; index <= segments; index++)
        {
            var angle = Mathf.Tau * index / segments;
            points[index] = center + new Vector2(MathF.Cos(angle) * radii.X, MathF.Sin(angle) * radii.Y);
        }
        DrawPolyline(points, color, width, true);
    }

    private void DrawTroopFrame(Texture2D texture, Rect2 destination, Rect2 source, Color color, bool faceLeft)
    {
        if (!faceLeft)
        {
            DrawTextureRectRegion(texture, destination, source, color);
            return;
        }
        var centerX = destination.Position.X + destination.Size.X / 2;
        DrawSetTransform(_worldOrigin + new Vector2(centerX * 2 * _cameraZoom, 0), 0, new Vector2(-_cameraZoom, _cameraZoom));
        DrawTextureRectRegion(texture, destination, source, color);
        ApplyWorldTransform();
    }

    private void DrawProjectiles()
    {
        if (_battle is null || _reducedMotion) return;
        foreach (var item in _activeVolleys)
        {
            var source = _battle.Groups.FirstOrDefault(group => group.Id == item.GroupId); var target = _battle.Groups.FirstOrDefault(group => group.Id == item.TargetGroupId);
            if (source is null || target is null) continue;
            var from = LogicalPosition(item.StartX, item.StartY);
            var to = LogicalPosition(item.EndX, item.EndY);
            var volleyProgress = (float)Math.Clamp((_time - item.Start) / Math.Max(.1, item.Duration), 0, 1);
            var arrowCount = Math.Clamp(4 + source.InitialSoldiers / 160, 5, 11);
            var seed = item.GroupId.Sum(character => character) + item.TargetGroupId.Sum(character => character);
            var color = item.Side == _battle.PlayerSide ? Color.FromHtml("#f4d58a") : Color.FromHtml("#d9b39a");
            for (var arrow = 0; arrow < arrowCount; arrow++)
            {
                var delay = arrow % 4 * .055f + arrow / 4 * .025f;
                var progress = Math.Clamp((volleyProgress - delay) / Math.Max(.25f, 1 - delay), 0, 1);
                if (progress <= 0 || progress >= 1) continue;
                var laneOffset = ((seed + arrow * 37) % 23 - 11) * 1.15f;
                var heightOffset = ((seed + arrow * 19) % 9 - 4) * 1.4f;
                var start = from + new Vector2(0, laneOffset * .35f);
                var end = to + new Vector2(0, laneOffset);
                var arcHeight = Math.Clamp(start.DistanceTo(end) * .15f, 30, 82) + heightOffset;
                var control = (start + end) * .5f + new Vector2(0, -arcHeight);
                var point = QuadraticBezier(start, control, end, progress);
                var tangent = QuadraticBezierTangent(start, control, end, progress).Normalized();
                var normal = new Vector2(-tangent.Y, tangent.X);
                var fade = Math.Clamp(MathF.Sin(progress * MathF.PI) * 1.35f, .25f, 1);
                var shaftColor = new Color(color, fade);
                DrawLine(point - tangent * 11 + new Vector2(2, 3), point + new Vector2(2, 3), new Color(0, 0, 0, .32f * fade), 2.4f, true);
                DrawLine(point - tangent * 11, point, shaftColor, 2.0f, true);
                DrawLine(point, point - tangent * 4 + normal * 3.2f, shaftColor, 1.5f, true);
                DrawLine(point, point - tangent * 4 - normal * 3.2f, shaftColor, 1.5f, true);
                DrawLine(point - tangent * 12, point - tangent * 20, new Color(color, .16f * fade), 1.3f, true);
                if (progress > .86f) DrawArrowImpact(end, arrow, (progress - .86f) / .14f, color);
            }
        }
    }

    private void DrawArrowImpact(Vector2 position, int index, float progress, Color color)
    {
        var fade = Math.Clamp(1 - progress, 0, 1);
        for (var spark = 0; spark < 3; spark++)
        {
            var angle = (index * 1.7f + spark * 2.1f) % (MathF.PI * 2);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            DrawLine(position + direction * progress * 12, position + direction * (progress * 12 + 4), new Color(color, fade * .75f), 1.4f, true);
        }
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
    {
        var inverse = 1 - progress;
        return inverse * inverse * start + 2 * inverse * progress * control + progress * progress * end;
    }

    private static Vector2 QuadraticBezierTangent(Vector2 start, Vector2 control, Vector2 end, float progress) =>
        2 * (1 - progress) * (control - start) + 2 * progress * (end - control);

    private void DrawDamageNumbers()
    {
        if (_battle is null) return;
        foreach (var item in _activeDamage)
        {
            var group = _battle.Groups.FirstOrDefault(value => value.Id == item.GroupId); if (group is null) continue;
            var progress = (float)((_time - item.Start) / Math.Max(.1, item.Duration)); var position = BattlePosition(group, true) + new Vector2(-18, -45 - progress * 20);
            DrawString(GetThemeDefaultFont(), position, $"−{item.Losses}", HorizontalAlignment.Center, 52, 15, Color.FromHtml("#ff9b76"));
        }
    }

    private Vector2 BattlePosition(BattleUnitGroupData group, bool animate)
    {
        var x = group.X; var y = group.Y;
        if (animate && _battle?.Status == "running")
        {
            var alpha = (float)Math.Clamp(_battle.SimulationAccumulator / .1, 0, 1);
            alpha = alpha * alpha * (3 - 2 * alpha);
            x = Mathf.Lerp(group.PreviousX, group.X, alpha);
            y = Mathf.Lerp(group.PreviousY, group.Y, alpha);
        }
        return LogicalPosition(x, y);
    }

    private Vector2 OfficerPosition(BattleOfficerUnitData officer)
    {
        var x = officer.X; var y = officer.Y;
        if (_battle?.Status == "running")
        {
            var alpha = (float)Math.Clamp(_battle.SimulationAccumulator / .1, 0, 1);
            alpha = alpha * alpha * (3 - 2 * alpha);
            x = Mathf.Lerp(officer.PreviousX, officer.X, alpha);
            y = Mathf.Lerp(officer.PreviousY, officer.Y, alpha);
        }
        return LogicalPosition(x, y);
    }

    private Vector2 LogicalPosition(float x, float y)
    {
        if (_battle?.PlayerSide == "defender") x = LogicalExtent - x;
        return new Vector2(
            x / LogicalExtent * WorldWidth,
            (BattleGroundTop + y / LogicalExtent * BattleGroundHeight) * WorldHeight);
    }

    private Rect2 BattlefieldViewportRect() => new(0, HeaderHeight, Math.Max(1, Size.X), Math.Max(1, Size.Y - HeaderHeight));

    private void ApplyWorldTransform()
    {
        var viewport = BattlefieldViewportRect();
        _worldOrigin = viewport.GetCenter() - _cameraCenter * _cameraZoom;
        DrawSetTransform(_worldOrigin, 0, Vector2.One * _cameraZoom);
    }

    private Vector2 WorldToScreen(Vector2 point) => _worldOrigin + point * _cameraZoom;

    private Vector2 ScreenToWorld(Vector2 point) => (point - _worldOrigin) / Math.Max(.01f, _cameraZoom);

    private void ClampCamera()
    {
        var viewport = BattlefieldViewportRect();
        var half = viewport.Size / (2 * Math.Max(.01f, _cameraZoom));
        _cameraCenter.X = half.X * 2 >= WorldWidth ? WorldWidth / 2 : Math.Clamp(_cameraCenter.X, half.X, WorldWidth - half.X);
        _cameraCenter.Y = half.Y * 2 >= WorldHeight ? WorldHeight / 2 : Math.Clamp(_cameraCenter.Y, half.Y, WorldHeight - half.Y);
        _worldOrigin = viewport.GetCenter() - _cameraCenter * _cameraZoom;
    }

    private void ZoomCamera(Vector2 pointer, float factor)
    {
        var worldAtPointer = ScreenToWorld(pointer);
        _cameraZoom = Math.Clamp(_cameraZoom * factor, MinimumZoom, MaximumZoom);
        var viewport = BattlefieldViewportRect();
        _cameraCenter = worldAtPointer - (pointer - viewport.GetCenter()) / _cameraZoom;
        ClampCamera();
        QueueRedraw();
    }

    private Rect2 MiniMapRect()
    {
        const float width = 226;
        const float height = 127;
        return new Rect2(Math.Max(8, Size.X - width - 16), Math.Max(HeaderHeight + 8, Size.Y - height - 16), width, height);
    }

    private void CenterCameraFromMiniMap(Vector2 pointer)
    {
        var map = MiniMapRect();
        var local = pointer - map.Position;
        _cameraCenter = new Vector2(
            Math.Clamp(local.X / map.Size.X * WorldWidth, 0, WorldWidth),
            Math.Clamp(local.Y / map.Size.Y * WorldHeight, 0, WorldHeight));
        ClampCamera();
        QueueRedraw();
    }

    private void DrawMiniMap()
    {
        if (_battle is null) return;
        var map = MiniMapRect();
        DrawRect(map, new Color(.025f, .035f, .025f, .9f));
        DrawRect(map, new Color(GameTheme.Bronze, .68f), false, 1.5f);
        foreach (var group in _battle.Groups.Where(item => CurrentSoldiers(item) > 0))
        {
            var world = BattlePosition(group, true);
            var point = map.Position + new Vector2(world.X / WorldWidth * map.Size.X, world.Y / WorldHeight * map.Size.Y);
            var color = group.Side == _battle.PlayerSide ? Color.FromHtml("#8ed2a9") : Color.FromHtml("#df8d77");
            DrawCircle(point, _selectedGroupIds.Contains(group.Id) ? 3.8f : 2.4f, color);
        }
        var viewport = BattlefieldViewportRect();
        var visibleTopLeft = ScreenToWorld(viewport.Position);
        var visibleBottomRight = ScreenToWorld(viewport.End);
        var cameraRect = new Rect2(
            map.Position + new Vector2(visibleTopLeft.X / WorldWidth * map.Size.X, visibleTopLeft.Y / WorldHeight * map.Size.Y),
            new Vector2((visibleBottomRight.X - visibleTopLeft.X) / WorldWidth * map.Size.X, (visibleBottomRight.Y - visibleTopLeft.Y) / WorldHeight * map.Size.Y));
        DrawRect(cameraRect.Intersection(map), Color.FromHtml("#f0d58a"), false, 1.5f);
        DrawString(GetThemeDefaultFont(), map.Position + new Vector2(8, 17), "全域战场", HorizontalAlignment.Left, 80, 11, GameTheme.OnAccent);
    }

    private void DrawCameraHelp()
    {
        var text = $"高质量2.5D大战场　敌军在左 · 我军在右　视野 {_cameraZoom * 100:0}% · 滚轮缩放 · 中键拖动";
        var width = Math.Min(430, Math.Max(240, Size.X - 270));
        var rect = new Rect2(14, Size.Y - 43, width, 29);
        DrawRect(rect, new Color(.025f, .035f, .025f, .82f));
        DrawString(GetThemeDefaultFont(), rect.Position + new Vector2(10, 19), text, HorizontalAlignment.Left, rect.Size.X - 20, 12, GameTheme.OnAccent);
    }

    private void DrawSelectionBox()
    {
        var rect = RectFromPoints(_dragStart, _dragCurrent);
        DrawRect(rect, new Color(.82f, .72f, .35f, .12f));
        DrawRect(rect, new Color(.92f, .79f, .35f, .85f), false, 1.5f);
    }

    private int CurrentSoldiers(BattleUnitGroupData group)
    {
        return _battle?.Status == "planning" ? group.InitialSoldiers : group.FinalSoldiers;
    }

    private string ActiveAction(string groupId)
    {
        return _activeActions.GetValueOrDefault(groupId) ?? _battle?.Groups.FirstOrDefault(item => item.Id == groupId)?.State ?? "hold";
    }

    private static string ShortName(string troop) => troop switch { "infantry" => "步", "spears" => "枪", "archers" => "弓", "cavalry" => "骑", "siege" => "械", _ => "军" };
}
