namespace ThreeKingdomsSimulator.Godot;

public static class BattleCatalog
{
    public static readonly string[] TroopTypes = ["infantry", "spears", "archers", "cavalry", "siege"];

    public static string TroopName(string value) => value switch
    {
        "infantry" => "步兵",
        "spears" => "枪兵",
        "archers" => "弓兵",
        "cavalry" => "骑兵",
        "siege" => "攻城器械",
        _ => value,
    };

    public static string FormationName(string value) => value switch
    {
        "goose" => "雁行阵",
        "wedge" => "锋矢阵",
        "crane" => "鹤翼阵",
        "shield" => "盾阵",
        "siege-array" => "攻城阵",
        _ => value,
    };

    public static string OrderName(string value) => value switch
    {
        "shield-line" => "盾墙横阵",
        "loose-line" => "疏散横阵",
        "assault-column" => "突击纵队",
        "spear-wall" => "密集枪阵",
        "support-line" => "支援横阵",
        "spear-column" => "推进纵阵",
        "rear-double" => "后排双列",
        "wing-fire" => "翼侧射列",
        "skirmish" => "前出散射",
        "wing-column" => "两翼纵队",
        "cavalry-wedge" => "锋矢冲锋",
        "reserve" => "中央预备",
        "protected-siege" => "后军保护列",
        "gate-column" => "城门攻击列",
        "wall-pressure" => "城墙压制列",
        _ => value,
    };

    public static string TacticName(string value) => value switch
    {
        "steady-advance" => "稳步推进",
        "shield-wall" => "盾墙固守",
        "feigned-retreat" => "佯退诱敌",
        "night-raid" => "夜袭",
        "fire-attack" => "火攻",
        "encirclement" => "合围",
        "arrow-volley" => "箭雨",
        "cavalry-charge" => "骑兵突击",
        "fortify-camp" => "坚营",
        "cut-supply" => "断粮道",
        "siege-ladders" => "云梯攻城",
        "undermine-walls" => "掘地坏墙",
        _ => value,
    };

    public static TroopOrderData DefaultOrder(string troop) => troop switch
    {
        "infantry" => new() { TroopType = troop, OrderId = "shield-line", Depth = 0, TargetPriority = "front" },
        "spears" => new() { TroopType = troop, OrderId = "spear-wall", Depth = 0, TargetPriority = "cavalry" },
        "archers" => new() { TroopType = troop, OrderId = "rear-double", Depth = 2, TargetPriority = "front" },
        "cavalry" => new() { TroopType = troop, OrderId = "wing-column", Depth = 1, TargetPriority = "ranged" },
        _ => new() { TroopType = troop, OrderId = "protected-siege", Depth = 2, TargetPriority = "structure" },
    };
}

public sealed class FormationPlanData
{
    public string FormationId { get; set; } = "goose";
    public Dictionary<string, TroopOrderData> TroopOrders { get; set; } = [];
}

public sealed class TroopOrderData
{
    public string TroopType { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public int Depth { get; set; }
    public string TargetPriority { get; set; } = "front";
}

public sealed class BattleUnitGroupData
{
    public string Id { get; set; } = string.Empty;
    public string Side { get; set; } = "attacker";
    public string TroopType { get; set; } = "infantry";
    public string FormationId { get; set; } = string.Empty;
    public string AssignedOfficerId { get; set; } = string.Empty;
    public int InitialSoldiers { get; set; }
    public int FinalSoldiers { get; set; }
    public double InitialMorale { get; set; } = 70;
    public double Morale { get; set; } = 70;
    public int Lane { get; set; }
    public int Depth { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float PreviousX { get; set; }
    public float PreviousY { get; set; }
    public float DestinationX { get; set; }
    public float DestinationY { get; set; }
    public float MinimumRange { get; set; }
    public float PreferredRange { get; set; }
    public float MaximumRange { get; set; }
    public double AttackCooldown { get; set; }
    public string TargetGroupId { get; set; } = string.Empty;
    public string State { get; set; } = "hold";
    public string CommandMode { get; set; } = "auto";
    public string CommandTargetGroupId { get; set; } = string.Empty;
    public float CommandDestinationX { get; set; }
    public float CommandDestinationY { get; set; }
    public bool IsSortie { get; set; }
    public bool IsRouted { get; set; }
    public bool RallyAttempted { get; set; }
}

public sealed class BattleTimelineEventData
{
    public double Start { get; set; }
    public double Duration { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string TargetGroupId { get; set; } = string.Empty;
    public string OfficerId { get; set; } = string.Empty;
    public string TargetOfficerId { get; set; } = string.Empty;
    public float StartX { get; set; }
    public float StartY { get; set; }
    public float EndX { get; set; }
    public float EndY { get; set; }
    public int Losses { get; set; }
    public string StructureTarget { get; set; } = string.Empty;
    public int StructureDamage { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class BattleOfficerUnitData
{
    public string OfficerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string SpriteId { get; set; } = "generic-commander";
    public string AssignedGroupId { get; set; } = string.Empty;
    public string TargetGroupId { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float PreviousX { get; set; }
    public float PreviousY { get; set; }
    public double AttackCooldown { get; set; }
    public double CombatPower { get; set; }
    public int MaxHitPoints { get; set; }
    public int HitPoints { get; set; }
    public double InitialMorale { get; set; }
    public double Morale { get; set; }
    public double Defense { get; set; }
    public int Leadership { get; set; }
    public int Might { get; set; }
    public int Intelligence { get; set; }
    public int Charisma { get; set; }
    public int TotalDamage { get; set; }
    public int OfficerDamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public bool IsRouted { get; set; }
    public string State { get; set; } = "mounted-idle";
}

public sealed class BattlePhaseResultData
{
    public string Stage { get; set; } = string.Empty;
    public string Tactic { get; set; } = string.Empty;
    public int AttackerBefore { get; set; }
    public int AttackerAfter { get; set; }
    public int DefenderBefore { get; set; }
    public int DefenderAfter { get; set; }
    public int AttackerLosses { get; set; }
    public int DefenderLosses { get; set; }
    public int WallDamage { get; set; }
    public int GateDamage { get; set; }
    public int InnerDamage { get; set; }
    public double PowerRatio { get; set; }
    public double AttackerMorale { get; set; }
    public double DefenderMorale { get; set; }
    public int AttackerRouted { get; set; }
    public int DefenderRouted { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public Dictionary<string, int> AttackerGroupLosses { get; set; } = [];
    public Dictionary<string, int> DefenderGroupLosses { get; set; } = [];
}

public sealed class PendingBattleData
{
    public string Id { get; set; } = string.Empty;
    public string ArmyId { get; set; } = string.Empty;
    public string DefenderArmyId { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
    public string BattleType { get; set; } = "siege";
    public string Terrain { get; set; } = "plain";
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = "planning";
    public string AttackerFactionId { get; set; } = string.Empty;
    public string DefenderFactionId { get; set; } = string.Empty;
    public string PlayerSide { get; set; } = "attacker";
    public string AttackerCommanderId { get; set; } = string.Empty;
    public string DefenderCommanderId { get; set; } = string.Empty;
    public List<string> AttackerOfficerIds { get; set; } = [];
    public List<string> DefenderOfficerIds { get; set; } = [];
    public Dictionary<string, string> OfficerRoles { get; set; } = [];
    public Dictionary<string, string> OfficerContributions { get; set; } = [];
    public int AttackerBefore { get; set; }
    public int DefenderBefore { get; set; }
    public int AttackerAfter { get; set; }
    public int DefenderAfter { get; set; }
    public int AttackerLosses { get; set; }
    public int DefenderLosses { get; set; }
    public int WallBefore { get; set; } = 100;
    public int GateBefore { get; set; } = 100;
    public int InnerBefore { get; set; } = 100;
    public int WallAfter { get; set; } = 100;
    public int GateAfter { get; set; } = 100;
    public int InnerAfter { get; set; } = 100;
    public string Result { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Stance { get; set; } = "standard";
    public string PrimaryTactic { get; set; } = "steady-advance";
    public string DefenderStance { get; set; } = "standard";
    public string DefenderPrimaryTactic { get; set; } = "fortify-camp";
    public string DecisionSummary { get; set; } = string.Empty;
    public double Duration { get; set; } = 60;
    public double Elapsed { get; set; }
    public double SimulationAccumulator { get; set; }
    public double WallIntegrity { get; set; } = 100;
    public double GateIntegrity { get; set; } = 100;
    public bool DefenderSortie { get; set; }
    public bool DefenderSortieUsed { get; set; }
    public string DefenderSortieSummary { get; set; } = string.Empty;
    public Dictionary<string, double> AttackerStageMultipliers { get; set; } = [];
    public Dictionary<string, double> DefenderStageMultipliers { get; set; } = [];
    public FormationPlanData AttackerFormation { get; set; } = new();
    public FormationPlanData DefenderFormation { get; set; } = new();
    public List<BattleUnitGroupData> Groups { get; set; } = [];
    public List<BattleOfficerUnitData> OfficerUnits { get; set; } = [];
    public List<BattleTimelineEventData> Timeline { get; set; } = [];
    public List<BattlePhaseResultData> PhaseResults { get; set; } = [];
}

public static class BattleCalculator
{
    private const double DefenderSortieRatio = 1.6;
    private const double DefenderRetreatRatio = 1.1;
    private static readonly Dictionary<string, double> TroopPower = new()
    {
        ["infantry"] = 1.00, ["spears"] = .98, ["archers"] = .94, ["cavalry"] = 1.06, ["siege"] = .64,
    };

    public static PendingBattleData Create(GameSession state, ArmyData army, CityData city)
    {
        var defenders = state.Officers
            .Where(item => item.InitialState.FactionId == city.OwnerFactionId && item.InitialState.CityId == city.Id && item.InitialState.Alive && item.InitialState.Status == "serving")
            .OrderByDescending(item => OfficerProgressionRules.EffectiveAbility(item, "leadership", "military") * 2
                + OfficerProgressionRules.EffectiveAbility(item, "might", "military")
                + OfficerProgressionRules.EffectiveAbility(item, "intelligence", "military")
                - item.InitialState.Fatigue)
            .Take(3)
            .ToList();
        var defenderCommander = defenders.FirstOrDefault();
        var defenderComposition = DefenderComposition(city.Garrison, city.Defense);
        var defenderDoctrine = SelectCityDefenseDoctrine(army.Composition, defenderComposition, army.Soldiers, city.Garrison, defenderCommander);
        var attackerFormation = DefaultFormation(army.Composition, true);
        if (army.FactionId != state.PlayerFactionId && IsFormationId(army.FormationId)) attackerFormation.FormationId = army.FormationId;

        var pending = new PendingBattleData
        {
            Id = $"battle-{state.Turn}-{state.BattleReports.Count + 1}",
            ArmyId = army.Id,
            CityId = city.Id,
            Terrain = "plain",
            Region = city.Region,
            AttackerFactionId = army.FactionId,
            DefenderFactionId = city.OwnerFactionId,
            PlayerSide = army.FactionId == state.PlayerFactionId ? "attacker" : "defender",
            AttackerCommanderId = army.CommanderId,
            DefenderCommanderId = defenderCommander?.Profile.Id ?? string.Empty,
            AttackerOfficerIds = [army.CommanderId, .. army.DeputyIds],
            DefenderOfficerIds = defenders.Select(item => item.Profile.Id).ToList(),
            AttackerBefore = army.Soldiers,
            DefenderBefore = city.Garrison,
            WallBefore = city.WallDurability,
            GateBefore = city.GateDurability,
            InnerBefore = city.InnerControl,
            Stance = army.Stance,
            PrimaryTactic = army.Tactic,
            DefenderStance = defenderDoctrine.Stance,
            DefenderPrimaryTactic = defenderDoctrine.Primary,
            AttackerFormation = attackerFormation,
            DefenderFormation = DefaultFormation(defenderComposition, false),
        };
        AssignRoles(pending);
        pending.Groups = ExpandArmy(army.Composition, army.Soldiers, "attacker", pending.AttackerFormation, pending.AttackerOfficerIds);
        pending.Groups.AddRange(ExpandArmy(defenderComposition, city.Garrison, "defender", pending.DefenderFormation, pending.DefenderOfficerIds));
        return pending;
    }

    public static PendingBattleData CreateFieldBattle(GameSession state, ArmyData attacker, ArmyData defender, string encounterRoadId = "")
    {
        var road = state.Roads.FirstOrDefault(item => item.Id == encounterRoadId)
            ?? defender.RouteRoadIds.Select(id => state.Roads.FirstOrDefault(item => item.Id == id)).LastOrDefault(item => item is not null)
            ?? attacker.RouteRoadIds.Select(id => state.Roads.FirstOrDefault(item => item.Id == id)).LastOrDefault(item => item is not null);
        var battlefieldCity = state.Cities.FirstOrDefault(item => item.Id == road?.ToCityId)
            ?? state.Cities.FirstOrDefault(item => item.Id == defender.TargetCityId)
            ?? state.Cities.First(item => item.Id == attacker.TargetCityId);
        List<string> attackerOfficers = [attacker.CommanderId, .. attacker.DeputyIds];
        List<string> defenderOfficers = [defender.CommanderId, .. defender.DeputyIds];
        var pending = new PendingBattleData
        {
            Id = $"battle-{state.Turn}-{state.BattleReports.Count + 1}", ArmyId = attacker.Id, DefenderArmyId = defender.Id,
            CityId = battlefieldCity.Id, BattleType = "field", Terrain = "plain", Region = battlefieldCity.Region,
            AttackerFactionId = attacker.FactionId, DefenderFactionId = defender.FactionId,
            PlayerSide = attacker.FactionId == state.PlayerFactionId ? "attacker" : "defender",
            AttackerCommanderId = attacker.CommanderId, DefenderCommanderId = defender.CommanderId,
            AttackerOfficerIds = attackerOfficers, DefenderOfficerIds = defenderOfficers,
            AttackerBefore = attacker.Soldiers, DefenderBefore = defender.Soldiers,
            WallBefore = 0, GateBefore = 0, InnerBefore = 0, WallAfter = 0, GateAfter = 0, InnerAfter = 0,
            Stance = attacker.Stance, PrimaryTactic = attacker.Tactic,
            DefenderStance = defender.Stance, DefenderPrimaryTactic = defender.Tactic,
            AttackerFormation = ArmyFormation(state, attacker, true), DefenderFormation = ArmyFormation(state, defender, false),
        };
        AssignRoles(pending);
        pending.Groups = ExpandArmy(attacker.Composition, attacker.Soldiers, "attacker", pending.AttackerFormation, attackerOfficers, pending.BattleType);
        pending.Groups.AddRange(ExpandArmy(defender.Composition, defender.Soldiers, "defender", pending.DefenderFormation, defenderOfficers, pending.BattleType));
        return pending;
    }

    public static void Configure(PendingBattleData pending, string formationId, Dictionary<string, string> orders,
        string stance = "", string primaryTactic = "")
    {
        var plan = pending.PlayerSide == "attacker" ? pending.AttackerFormation : pending.DefenderFormation;
        plan.FormationId = formationId;
        foreach (var troop in BattleCatalog.TroopTypes)
        {
            if (!plan.TroopOrders.TryGetValue(troop, out var order)) continue;
            if (orders.TryGetValue(troop, out var value) && !string.IsNullOrWhiteSpace(value)) order.OrderId = value;
        }
        foreach (var troopGroup in pending.Groups.Where(item => item.Side == pending.PlayerSide).GroupBy(item => item.TroopType))
        {
            var index = 0;
            var count = troopGroup.Count();
            foreach (var group in troopGroup)
            {
                group.FormationId = plan.TroopOrders.GetValueOrDefault(group.TroopType)?.OrderId ?? group.FormationId;
                group.Lane = LaneFor(group.TroopType, index);
                group.Depth = DepthFor(group.TroopType, index, count);
                ApplyOrderLayout(group, index++, plan.FormationId);
                ApplyDeployment(group, group.Side, pending.BattleType);
            }
        }
        SpreadGroups(pending.Groups.Where(item => item.Side == pending.PlayerSide).ToList(), pending.PlayerSide);
        if (pending.PlayerSide == "attacker")
        {
            if (!string.IsNullOrEmpty(stance)) pending.Stance = stance;
            if (!string.IsNullOrEmpty(primaryTactic)) pending.PrimaryTactic = primaryTactic;
        }
        else
        {
            if (!string.IsNullOrEmpty(stance)) pending.DefenderStance = stance;
            if (!string.IsNullOrEmpty(primaryTactic)) pending.DefenderPrimaryTactic = primaryTactic;
        }
    }

    public static int IssueRealtimeCommand(PendingBattleData pending, IEnumerable<string> groupIds, string command,
        string targetGroupId = "", float destinationX = 0, float destinationY = 0)
    {
        if (pending.Status != "running") return 0;
        var selected = pending.Groups
            .Where(item => item.Side == pending.PlayerSide && item.FinalSoldiers > 0 && !item.IsRouted && item.Morale >= 25 && groupIds.Contains(item.Id))
            .ToList();
        if (selected.Count == 0) return 0;
        var defensiveCommand = command is "defend-gate" or "inner-city" or "sortie" or "reserve-line";
        if (defensiveCommand && (pending.PlayerSide != "defender" || pending.BattleType != "siege")) return 0;

        BattleUnitGroupData? target = null;
        if (command == "attack")
        {
            target = pending.Groups.FirstOrDefault(item => item.Id == targetGroupId && item.Side != pending.PlayerSide && item.FinalSoldiers > 0);
            if (target is null) return 0;
        }

        destinationX = Math.Clamp(destinationX, 28, 972);
        destinationY = Math.Clamp(destinationY, 35, 965);
        var formationCenterX = selected.Average(item => item.X);
        var formationCenterY = selected.Average(item => item.Y);
        foreach (var group in selected)
        {
            switch (command)
            {
                case "attack":
                    group.CommandMode = "attack";
                    group.CommandTargetGroupId = target!.Id;
                    group.TargetGroupId = target.Id;
                    break;
                case "move":
                    group.CommandMode = "move";
                    group.CommandTargetGroupId = string.Empty;
                    group.CommandDestinationX = Math.Clamp(destinationX + group.X - (float)formationCenterX, 28, 972);
                    group.CommandDestinationY = Math.Clamp(destinationY + group.Y - (float)formationCenterY, 35, 965);
                    group.Lane = Math.Clamp((int)Math.Round((group.CommandDestinationY - 120) / 170f), 0, 4);
                    group.TargetGroupId = string.Empty;
                    break;
                case "hold":
                    group.CommandMode = "hold";
                    group.CommandTargetGroupId = string.Empty;
                    group.CommandDestinationX = group.X;
                    group.CommandDestinationY = group.Y;
                    group.TargetGroupId = string.Empty;
                    group.State = "hold";
                    break;
                case "auto":
                    group.CommandMode = "auto";
                    group.CommandTargetGroupId = string.Empty;
                    group.TargetGroupId = string.Empty;
                    break;
                case "defend-gate":
                    group.CommandMode = "move";
                    group.CommandTargetGroupId = string.Empty;
                    group.CommandDestinationX = 305;
                    group.CommandDestinationY = Math.Clamp(460 + group.Y - (float)formationCenterY, 120, 800);
                    group.Lane = 2;
                    group.IsSortie = false;
                    group.TargetGroupId = string.Empty;
                    break;
                case "inner-city":
                    group.CommandMode = "move";
                    group.CommandTargetGroupId = string.Empty;
                    group.CommandDestinationX = 165 + group.Depth * 18;
                    group.CommandDestinationY = group.Y;
                    group.IsSortie = false;
                    group.TargetGroupId = string.Empty;
                    break;
                case "sortie":
                    DeploySortieGroup(group);
                    group.CommandMode = "move";
                    group.CommandDestinationX = 560;
                    group.CommandDestinationY = group.Y;
                    break;
                case "reserve-line":
                    group.CommandMode = "move";
                    group.CommandTargetGroupId = string.Empty;
                    group.CommandDestinationX = 215 + group.Depth * 18;
                    group.CommandDestinationY = group.Y;
                    group.IsSortie = false;
                    group.TargetGroupId = string.Empty;
                    break;
                default:
                    return 0;
            }
        }

        var commandText = command switch
        {
            "attack" => $"集火{BattleCatalog.TroopName(target!.TroopType)}军团",
            "move" => "向指定位置移动",
            "hold" => "原地固守",
            "defend-gate" => "增援城门",
            "inner-city" => "退守内城",
            "sortie" => "出城突袭",
            "reserve-line" => "转入后军预备",
            _ => "恢复自由接敌",
        };
        AddRealtimeEvent(pending, "实时军令", "command", pending.PlayerSide, selected[0], target,
            $"{selected.Count}支军团奉命{commandText}", 0, 0);
        return selected.Count;
    }

    public static void Generate(GameSession state, PendingBattleData pending)
    {
        if (pending.Status != "planning") return;
        var army = state.Armies.First(item => item.Id == pending.ArmyId);
        var city = state.Cities.First(item => item.Id == pending.CityId);
        var defenderArmy = string.IsNullOrEmpty(pending.DefenderArmyId) ? null : state.Armies.FirstOrDefault(item => item.Id == pending.DefenderArmyId);
        pending.DefenderSortie = pending.BattleType == "siege" && ShouldDefenderSortie(pending.AttackerBefore, pending.DefenderBefore);
        pending.DefenderSortieUsed = pending.DefenderSortie;
        pending.DefenderSortieSummary = pending.DefenderSortie
            ? $"守军兵力达到攻军的{pending.DefenderBefore / (double)Math.Max(1, pending.AttackerBefore):F1}倍，开城主动迎战"
            : string.Empty;
        foreach (var group in pending.Groups)
        {
            group.FinalSoldiers = group.InitialSoldiers;
            var sideMorale = group.Side == "attacker"
                ? army.Morale
                : defenderArmy?.Morale ?? Math.Clamp(58 + city.Defense / 4, 45, 88);
            var factionId = group.Side == "attacker" ? pending.AttackerFactionId : pending.DefenderFactionId;
            sideMorale = Math.Clamp((int)Math.Round(sideMorale + OfficerProgressionRules.CourtMoraleBonus(state, factionId)), 0, 100);
            if (group.Side == "defender" && pending.DefenderOfficerIds.Count == 0) sideMorale = Math.Min(sideMorale, 55);
            group.InitialMorale = sideMorale;
            group.Morale = sideMorale;
            group.AttackCooldown = group.Id.Sum(character => character) % 9 / 10d;
            group.TargetGroupId = string.Empty;
            group.State = group.Side == "defender" ? "guard" : "advance";
            group.CommandMode = "auto";
            group.CommandTargetGroupId = string.Empty;
            group.CommandDestinationX = group.X;
            group.CommandDestinationY = group.Y;
            group.IsSortie = false;
            group.IsRouted = false;
            group.RallyAttempted = false;
            ApplyDeployment(group, group.Side, pending.BattleType);
            if (pending.DefenderSortie && group.Side == "defender" && group.TroopType is "infantry" or "spears" or "cavalry") DeploySortieGroup(group);
        }
        pending.OfficerUnits = BuildMountedOfficerUnits(state, pending);
        pending.PhaseResults.Clear();
        pending.Timeline.Clear();
        pending.Stance = army.Stance;
        pending.DecisionSummary = $"攻方执行“{BattleCatalog.TacticName(pending.PrimaryTactic)}”；守方执行“{BattleCatalog.TacticName(pending.DefenderPrimaryTactic)}”；双方朝堂任命已计入全军战力、战术、士气与武将体力";
        pending.Result = string.Empty;
        pending.Summary = string.Empty;
        pending.Elapsed = 0;
        pending.SimulationAccumulator = 0;
        pending.Duration = 50;
        pending.WallIntegrity = pending.WallBefore;
        pending.GateIntegrity = pending.GateBefore;
        pending.WallAfter = pending.WallBefore;
        pending.GateAfter = pending.GateBefore;
        pending.InnerAfter = pending.InnerBefore;
        var attackerOfficer = OfficerMultiplier(state, pending.AttackerOfficerIds, pending.OfficerRoles, pending.OfficerContributions, true);
        var defenderOfficer = pending.DefenderOfficerIds.Count == 0
            ? .82
            : OfficerMultiplier(state, pending.DefenderOfficerIds, pending.OfficerRoles, pending.OfficerContributions, false);
        if (pending.DefenderOfficerIds.Count == 0) pending.DecisionSummary += "；守方无将指挥，战力与士气受限";
        var defenderComposition = defenderArmy?.Composition ?? DefenderComposition(city.Garrison, city.Defense);

        double AttackerTrait(string stage) => OfficerProgressionRules.BattleTraitMultiplier(state, pending.AttackerOfficerIds, pending.OfficerRoles, army.Composition, army.SpecialTroops, stage, pending.OfficerContributions);
        double DefenderTrait(string stage) => OfficerProgressionRules.BattleTraitMultiplier(state, pending.DefenderOfficerIds, pending.OfficerRoles, defenderComposition, [], stage, pending.OfficerContributions);

        var attackerState = attackerOfficer * StateMultiplier(army.Training, army.Morale, army.Fatigue, army.Food > 0);
        var defenderState = defenderOfficer * (defenderArmy is null
            ? StateMultiplier(city.Training, 72, city.Fatigue, true)
            : StateMultiplier(defenderArmy.Training, defenderArmy.Morale, defenderArmy.Fatigue, defenderArmy.Food > 0));
        foreach (var stage in new[] { "远程压制", "正面接战", "决胜", "攻城与内城" })
        {
            pending.AttackerStageMultipliers[stage] = attackerState * AttackerTrait(stage) * OfficerProgressionRules.CourtBattleMultiplier(state, pending.AttackerFactionId, stage);
            pending.DefenderStageMultipliers[stage] = defenderState * DefenderTrait(stage) * OfficerProgressionRules.CourtBattleMultiplier(state, pending.DefenderFactionId, stage);
        }
        pending.Timeline.Add(new BattleTimelineEventData
        {
            Start = 0, Duration = 1.2, Stage = "列阵", Action = "message", Side = "attacker",
            Text = pending.BattleType == "siege"
                ? $"攻方在城外展开{BattleCatalog.FormationName(pending.AttackerFormation.FormationId)}，实时执行{BattleCatalog.TacticName(pending.PrimaryTactic)}；守军据城固守"
                : $"双方军团在野外展开，攻方以{BattleCatalog.FormationName(pending.AttackerFormation.FormationId)}执行{BattleCatalog.TacticName(pending.PrimaryTactic)}"
        });
        if (pending.DefenderSortie)
        {
            AddRealtimeEvent(pending, "守军出击", "command", "defender",
                pending.Groups.FirstOrDefault(item => item.IsSortie), null, pending.DefenderSortieSummary, 0, 0);
        }
        pending.Status = "running";
    }

    public static bool ShouldDefenderSortie(int attackerSoldiers, int defenderSoldiers) =>
        attackerSoldiers > 0 && defenderSoldiers / (double)attackerSoldiers >= DefenderSortieRatio;

    public static void Advance(GameSession state, PendingBattleData pending, double delta)
    {
        if (pending.Status != "running" || delta <= 0) return;
        pending.SimulationAccumulator += delta;
        const double fixedStep = .1;
        while (pending.SimulationAccumulator >= fixedStep && pending.Status == "running")
        {
            pending.SimulationAccumulator -= fixedStep;
            pending.Elapsed = Math.Min(pending.Duration, pending.Elapsed + fixedStep);
            StepRealtime(state, pending, fixedStep);
            EvaluateRealtimeResult(state, pending);
        }
    }

    public static void RunToCompletion(GameSession state, PendingBattleData pending)
    {
        var guard = 0;
        while (pending.Status == "running" && guard++ < 1000) Advance(state, pending, .5);
        if (pending.Status == "running") FinishRealtime(state, pending, "defeat", pending.BattleType == "field" ? "演算达到安全上限，出击军团撤离战场" : "演算达到安全上限，守军守住城池");
    }

    private static void StepRealtime(GameSession state, PendingBattleData pending, double delta)
    {
        var city = state.Cities.First(item => item.Id == pending.CityId);
        var siege = pending.BattleType == "siege";
        foreach (var group in pending.Groups)
        {
            group.PreviousX = group.X;
            group.PreviousY = group.Y;
        }
        foreach (var officer in pending.OfficerUnits)
        {
            officer.PreviousX = officer.X;
            officer.PreviousY = officer.Y;
        }
        var attackers = pending.Groups.Where(item => item.Side == "attacker" && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
        var defenders = pending.Groups.Where(item => item.Side == "defender" && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
        var breached = !siege || pending.WallAfter <= 0 || pending.GateAfter <= 0;
        EvaluateDefenderSortie(pending, attackers, defenders, breached);

        var defenderFirst = (int)Math.Floor(pending.Elapsed * 10) % 2 == 0;
        var actingGroups = pending.Groups
            .Where(item => item.FinalSoldiers > 0)
            .OrderBy(item => item.Side == (defenderFirst ? "defender" : "attacker") ? 0 : 1)
            .ThenBy(item => item.Id)
            .ToList();

        foreach (var source in actingGroups)
        {
            if (source.FinalSoldiers <= 0) continue;
            source.AttackCooldown = Math.Max(0, source.AttackCooldown - delta);
            breached = !siege || pending.WallAfter <= 0 || pending.GateAfter <= 0;

            if (source.IsRouted || source.State == "retreat")
            {
                TryRallyGroup(state, pending, source);
                if (source.Morale < 25 || source.IsRouted)
                {
                    var pursuers = source.Side == "attacker" ? defenders : attackers;
                    ApplyPursuitLosses(pending, source, pursuers);
                    var withdrawalX = source.Side == "attacker" ? 972f : 28f;
                    MoveGroup(pending, source, withdrawalX, source.Y, delta);
                    source.State = source.IsRouted ? "routed" : "retreat";
                    continue;
                }
            }

            if (source.CommandMode == "move")
            {
                var commandX = source.CommandDestinationX;
                if (siege && !breached)
                {
                    if (source.Side == "attacker") commandX = Math.Max(398, commandX);
                    else if (!source.IsSortie) commandX = Math.Min(340, commandX);
                }
                if (Math.Abs(source.X - commandX) > 3 || Math.Abs(source.Y - source.CommandDestinationY) > 3)
                {
                    MoveGroup(pending, source, commandX, source.CommandDestinationY, delta);
                    continue;
                }
                source.CommandMode = "hold";
                source.State = "hold";
            }

            var sortieDefendersActive = defenders.Any(item => item.IsSortie && item.FinalSoldiers > 0);
            if (siege && source.Side == "attacker" && !breached && !sortieDefendersActive && source.TroopType != "archers")
            {
                var standX = source.TroopType == "siege" ? 445f : 402f;
                if (source.X > standX + 2)
                {
                    MoveGroup(pending, source, standX, source.Y, delta);
                    continue;
                }
                source.State = "siege";
                if (source.AttackCooldown <= 0) AttackStructure(pending, source);
                continue;
            }

            var enemies = source.Side == "attacker" ? defenders : attackers;
            var allies = source.Side == "attacker" ? attackers : defenders;
            var target = source.CommandMode == "attack"
                ? enemies.FirstOrDefault(item => item.Id == source.CommandTargetGroupId && item.FinalSoldiers > 0)
                : SelectRealtimeTarget(source, enemies, allies);
            if (source.CommandMode == "attack" && target is null)
            {
                source.CommandMode = "auto";
                source.CommandTargetGroupId = string.Empty;
                target = SelectRealtimeTarget(source, enemies, allies);
            }
            if (source.CommandMode == "hold" && target is not null && BattleDistance(source, target) > source.MaximumRange)
            {
                target = enemies.Where(item => item.FinalSoldiers > 0 && BattleDistance(source, item) <= source.MaximumRange)
                    .OrderBy(item => BattleDistance(source, item)).FirstOrDefault();
            }
            if (target is null)
            {
                source.TargetGroupId = string.Empty;
                source.State = "hold";
                continue;
            }
            source.TargetGroupId = target.Id;
            var distance = BattleDistance(source, target);
            var attackRange = source.MaximumRange;
            if (source.Side == "defender" && source.TroopType != "archers" && !breached) attackRange = Math.Max(attackRange, 96);

            if (source.MinimumRange > 0 && distance < source.MinimumRange)
            {
                source.State = "threatened";
                if (source.CommandMode != "hold" && (!siege || source.Side == "attacker" || source.IsSortie))
                {
                    var retreatX = source.Side == "attacker" ? target.X + source.MinimumRange * 1.18f : target.X - source.MinimumRange * 1.18f;
                    MoveGroup(pending, source, Math.Clamp(retreatX, 28, 972), source.Y, delta);
                }
                continue;
            }

            if (source.CommandMode != "hold" && (!siege || source.Side == "attacker" || source.IsSortie) && distance > source.PreferredRange)
            {
                var spacing = Math.Max(source.PreferredRange, source.MaximumRange * .82f);
                var targetX = source.Side == "attacker" ? target.X + spacing : target.X - spacing;
                if (siege && !breached) targetX = Math.Max(targetX, 398f);
                MoveGroup(pending, source, targetX, target.Y, delta);
                distance = BattleDistance(source, target);
            }
            else if (siege && source.Side == "defender")
            {
                source.State = distance <= attackRange ? "guard-attack" : "guard";
            }

            if (distance <= attackRange && source.AttackCooldown <= 0)
            {
                if (!HasEngagementSlot(source, target, allies))
                {
                    source.State = "reserve";
                    continue;
                }
                AttackTroop(state, pending, city, source, target, breached);
            }
        }
        StepMountedOfficers(pending, delta);
    }

    private static void TryRallyGroup(GameSession state, PendingBattleData pending, BattleUnitGroupData group)
    {
        if (group.RallyAttempted || string.IsNullOrEmpty(group.AssignedOfficerId)) return;
        group.RallyAttempted = true;
        var officer = state.Officers.FirstOrDefault(item => item.Profile.Id == group.AssignedOfficerId);
        if (officer is null) return;
        var leadership = OfficerProgressionRules.EffectiveAbility(officer, "leadership", "military");
        var charisma = OfficerProgressionRules.EffectiveAbility(officer, "charisma", "military");
        if (leadership + charisma < 145) return;
        var restored = Math.Clamp(8 + (leadership + charisma - 145) * .22, 8, 18);
        group.Morale = Math.Min(100, group.Morale + restored);
        if (group.Morale >= 10) group.IsRouted = false;
        if (group.Morale >= 25)
        {
            group.CommandMode = "auto";
            group.State = "rallied";
        }
        AddRealtimeEvent(pending, "收拢溃兵", "rally", group.Side, group, null,
            $"{officer.Profile.Name}收拢所属战斗队，士气恢复至{group.Morale:F0}", 0, 0);
    }

    private static void ApplyPursuitLosses(PendingBattleData pending, BattleUnitGroupData routed, List<BattleUnitGroupData> enemies)
    {
        if (routed.FinalSoldiers <= 0 || routed.AttackCooldown > 0) return;
        var nearby = enemies
            .Where(item => item.FinalSoldiers > 0 && !item.IsRouted)
            .Where(item => BattleDistance(item, routed) <= (item.TroopType == "cavalry" ? 340 : item.TroopType == "archers" ? 250 : 190))
            .OrderBy(item => BattleDistance(item, routed))
            .ToList();
        if (nearby.Count == 0) return;
        var pressure = nearby.Sum(item => item.FinalSoldiers * (item.TroopType == "cavalry" ? 1.45 : item.TroopType == "archers" ? .45 : .85));
        var pressureRatio = Math.Clamp(pressure / Math.Max(1d, routed.FinalSoldiers), .65, 2.4);
        var lossRate = routed.IsRouted ? .025 : .013;
        var losses = Math.Clamp((int)Math.Ceiling(routed.FinalSoldiers * lossRate * pressureRatio), 1, Math.Max(1, (int)Math.Ceiling(routed.FinalSoldiers * .10)));
        losses = Math.Min(losses, routed.FinalSoldiers);
        routed.FinalSoldiers -= losses;
        routed.AttackCooldown = .9;
        var lead = nearby[0];
        AddRealtimeEvent(pending, routed.IsRouted ? "溃军追击" : "撤退追击", "damage", routed.Side, routed, lead,
            $"{BattleCatalog.TroopName(lead.TroopType)}追击，−{losses}", losses, 0);
    }

    private static List<BattleOfficerUnitData> BuildMountedOfficerUnits(GameSession state, PendingBattleData pending)
    {
        var result = new List<BattleOfficerUnitData>();
        AddSide(pending.AttackerOfficerIds, "attacker");
        AddSide(pending.DefenderOfficerIds, "defender");
        return result;

        void AddSide(IEnumerable<string> officerIds, string side)
        {
            foreach (var officerId in officerIds.Where(item => !string.IsNullOrEmpty(item)).Distinct())
            {
                var officer = state.Officers.FirstOrDefault(item => item.Profile.Id == officerId);
                if (officer is null) continue;
                var leadership = OfficerProgressionRules.EffectiveAbility(officer, "leadership", "military");
                var might = OfficerProgressionRules.EffectiveAbility(officer, "might", "military");
                var intelligence = OfficerProgressionRules.EffectiveAbility(officer, "intelligence", "military");
                var charisma = OfficerProgressionRules.EffectiveAbility(officer, "charisma", "military");
                var spriteId = MountedOfficerSpriteId(officerId, leadership, might, intelligence);
                var anchor = pending.Groups.Where(item => item.Side == side && item.AssignedOfficerId == officerId)
                    .OrderBy(item => item.Depth).ThenByDescending(item => item.InitialSoldiers).FirstOrDefault()
                    ?? pending.Groups.Where(item => item.Side == side).OrderByDescending(item => item.InitialSoldiers).FirstOrDefault();
                if (anchor is null) continue;
                var rearOffset = side == "attacker" ? 30 : -30;
                var courtHealth = OfficerProgressionRules.CourtOfficerHealthMultiplier(state, officer.InitialState.FactionId ?? string.Empty);
                var maxHitPoints = (int)Math.Round(MountedOfficerMaxHitPoints(leadership, might) * courtHealth);
                var morale = Math.Min(100, MountedOfficerInitialMorale(leadership, charisma, officer.InitialState.Fatigue)
                    + OfficerProgressionRules.CourtMoraleBonus(state, officer.InitialState.FactionId ?? string.Empty));
                result.Add(new BattleOfficerUnitData
                {
                    OfficerId = officerId,
                    Name = officer.Profile.Name,
                    Side = side,
                    SpriteId = spriteId,
                    AssignedGroupId = anchor.Id,
                    X = anchor.X + rearOffset,
                    Y = anchor.Y - 28,
                    PreviousX = anchor.X + rearOffset,
                    PreviousY = anchor.Y - 28,
                    AttackCooldown = (officerId.Sum(character => character) % 8) / 5d,
                    CombatPower = MountedOfficerCombatPower(spriteId, leadership, might, intelligence),
                    MaxHitPoints = maxHitPoints,
                    HitPoints = maxHitPoints,
                    InitialMorale = morale,
                    Morale = morale,
                    Defense = MountedOfficerDefense(leadership, might),
                    Leadership = leadership,
                    Might = might,
                    Intelligence = intelligence,
                    Charisma = charisma,
                });
            }
        }
    }

    private static string MountedOfficerSpriteId(string officerId, int leadership, int might, int intelligence) => officerId switch
    {
        "officer-liu-bei" => "liu-bei",
        "officer-guan-yu" => "guan-yu",
        "officer-zhang-fei" => "zhang-fei",
        "officer-lu-bu" => "lu-bu",
        _ when intelligence >= might + 8 && intelligence >= leadership => "generic-strategist",
        _ when might >= 86 => "generic-vanguard",
        _ => "generic-commander",
    };

    public static double MountedOfficerCombatPower(string spriteId, int leadership, int might, int intelligence)
    {
        var basePower = spriteId == "generic-strategist"
            ? 1.5 + intelligence * .050 + leadership * .015 + might * .005
            : 1.5 + might * .045 + leadership * .015 + intelligence * .008;
        var historical = spriteId switch { "lu-bu" => 1.18, "guan-yu" => 1.12, "zhang-fei" => 1.11, "liu-bei" => 1.06, _ => 1 };
        return Math.Round(Math.Clamp(basePower * historical, 3.5, 10.5), 2);
    }

    public static int MountedOfficerMaxHitPoints(int leadership, int might) =>
        Math.Clamp(900 + might * 7 + leadership * 3, 1100, 2100);

    public static double MountedOfficerDefense(int leadership, int might) =>
        Math.Round(Math.Clamp(20 + might * .35 + leadership * .25, 30, 90), 1);

    public static double MountedOfficerInitialMorale(int leadership, int charisma, int fatigue) =>
        Math.Round(Math.Clamp(42 + leadership * .28 + charisma * .28 - fatigue * .12, 55, 100), 1);

    private static void StepMountedOfficers(PendingBattleData pending, double delta)
    {
        foreach (var officer in pending.OfficerUnits)
        {
            officer.AttackCooldown = Math.Max(0, officer.AttackCooldown - delta);
            if (officer.HitPoints <= 0 || officer.Morale < 20 || officer.IsRouted)
            {
                officer.IsRouted = true;
                officer.State = officer.HitPoints <= 0 ? "mounted-defeated" : "mounted-retreat";
                var withdrawalX = officer.Side == "attacker" ? 990f : 10f;
                officer.X = MoveTowards(officer.X, withdrawalX, 115f * (float)delta);
                continue;
            }
            var allies = pending.Groups.Where(item => item.Side == officer.Side && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
            var enemies = pending.Groups.Where(item => item.Side != officer.Side && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
            if (allies.Count == 0 || enemies.Count == 0) { officer.State = "mounted-retreat"; continue; }
            var anchor = allies.FirstOrDefault(item => item.Id == officer.AssignedGroupId)
                ?? allies.Where(item => item.AssignedOfficerId == officer.OfficerId).OrderByDescending(item => item.FinalSoldiers).FirstOrDefault()
                ?? allies.OrderBy(item => OfficerDistance(officer, item)).First();
            officer.AssignedGroupId = anchor.Id;
            var rearOffset = officer.Side == "attacker" ? 28 : -28;
            var desiredX = anchor.X + rearOffset;
            var desiredY = anchor.Y - 28;
            var moved = Math.Abs(officer.X - desiredX) > 2 || Math.Abs(officer.Y - desiredY) > 2;
            officer.X = MoveTowards(officer.X, desiredX, 96f * (float)delta);
            officer.Y = MoveTowards(officer.Y, desiredY, 48f * (float)delta);
            officer.State = moved ? "mounted-move" : "mounted-idle";

            var target = enemies.FirstOrDefault(item => item.Id == anchor.TargetGroupId)
                ?? enemies.OrderBy(item => OfficerDistance(officer, item)).First();
            officer.TargetGroupId = target.Id;
            var strategist = officer.SpriteId == "generic-strategist";
            var range = strategist ? 285 : 105;
            var breached = pending.BattleType != "siege" || pending.WallAfter <= 0 || pending.GateAfter <= 0;
            if (!breached && !anchor.IsSortie && !target.IsSortie) continue;
            var enemyOfficer = pending.OfficerUnits
                .Where(item => item.Side != officer.Side && item.HitPoints > 0 && !item.IsRouted && item.Morale >= 20)
                .OrderBy(item => OfficerDistance(officer, item))
                .FirstOrDefault();
            var officerRange = strategist ? 175 : 125;
            if (enemyOfficer is not null && OfficerDistance(officer, enemyOfficer) <= officerRange && officer.AttackCooldown <= 0)
            {
                AttackMountedOfficer(pending, officer, enemyOfficer, strategist);
                continue;
            }
            if (OfficerDistance(officer, target) > range || officer.AttackCooldown > 0) continue;
            var losses = Math.Clamp((int)Math.Round(officer.CombatPower), 2, Math.Max(2, (int)Math.Ceiling(target.FinalSoldiers * .035)));
            losses = Math.Min(losses, target.FinalSoldiers);
            target.FinalSoldiers -= losses;
            officer.TotalDamage += losses;
            officer.AttackCooldown = strategist ? 2.1 : officer.SpriteId == "lu-bu" ? 1.25 : 1.55;
            officer.State = strategist ? "mounted-command" : "mounted-charge";
            AddMountedOfficerEvent(pending, officer, target, losses, strategist);
            ApplyMountedOfficerCounterfire(pending, officer, target, strategist);
        }
    }

    private static double OfficerDistance(BattleOfficerUnitData officer, BattleUnitGroupData target)
    {
        var dx = officer.X - target.X;
        var dy = (officer.Y - target.Y) * .55f;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double OfficerDistance(BattleOfficerUnitData first, BattleOfficerUnitData second)
    {
        var dx = first.X - second.X;
        var dy = (first.Y - second.Y) * .55f;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static void AttackMountedOfficer(PendingBattleData pending, BattleOfficerUnitData source, BattleOfficerUnitData target, bool strategist)
    {
        var rawDamage = 30 + source.Might * .58 + source.CombatPower * 4 + (strategist ? source.Intelligence * .18 : 0);
        var damage = DamageMountedOfficer(pending, source, null, target, rawDamage,
            strategist ? "谋将交锋" : "武将交锋", strategist ? $"{source.Name}临阵施策" : $"{source.Name}纵马交锋");
        source.OfficerDamageDealt += damage;
        source.AttackCooldown = strategist ? 2.0 : source.SpriteId == "lu-bu" ? 1.2 : 1.45;
        source.State = strategist ? "mounted-command" : "mounted-duel";
        pending.Timeline.Add(new BattleTimelineEventData
        {
            Start = pending.Elapsed,
            Duration = .9,
            Stage = strategist ? "谋将交锋" : "武将交锋",
            Action = "officer-duel",
            Side = source.Side,
            OfficerId = source.OfficerId,
            TargetOfficerId = target.OfficerId,
            StartX = source.X,
            StartY = source.Y,
            EndX = target.X,
            EndY = target.Y,
            Losses = damage,
            Text = $"{source.Name}迎战{target.Name}",
        });
    }

    private static void ApplyMountedOfficerCounterfire(PendingBattleData pending, BattleOfficerUnitData officer, BattleUnitGroupData target, bool strategist)
    {
        var troopPressure = target.TroopType switch { "archers" => 1.18, "spears" => 1.12, "cavalry" => 1.08, "siege" => .72, _ => 1 };
        var rawDamage = ((strategist ? 13 : 22) + Math.Sqrt(Math.Max(1, target.FinalSoldiers)) * .55) * troopPressure;
        DamageMountedOfficer(pending, null, target, officer, rawDamage, "阵前承伤", $"{officer.Name}遭{BattleCatalog.TroopName(target.TroopType)}反击");
    }

    private static int DamageMountedOfficer(PendingBattleData pending, BattleOfficerUnitData? sourceOfficer,
        BattleUnitGroupData? sourceGroup, BattleOfficerUnitData target, double rawDamage, string stage, string text)
    {
        var resistance = 100d / (100 + target.Defense * 1.8);
        var damage = Math.Clamp((int)Math.Round(rawDamage * resistance), 1, Math.Max(1, target.HitPoints));
        target.HitPoints = Math.Max(0, target.HitPoints - damage);
        target.DamageTaken += damage;
        var pressure = sourceOfficer is null ? 0 : Math.Max(0, sourceOfficer.Might - target.Leadership) * .02;
        var moraleLoss = .8 + damage / (double)Math.Max(1, target.MaxHitPoints) * 55 + pressure;
        target.Morale = Math.Max(0, target.Morale - moraleLoss);
        if (target.HitPoints <= 0)
        {
            target.IsRouted = true;
            target.State = "mounted-defeated";
            text += "，已无力再战";
        }
        else if (target.Morale < 20)
        {
            target.IsRouted = true;
            target.State = "mounted-retreat";
            text += "，士气崩溃撤离战线";
        }
        pending.Timeline.Add(new BattleTimelineEventData
        {
            Start = pending.Elapsed + .16,
            Duration = .8,
            Stage = stage,
            Action = "officer-damage",
            Side = target.Side,
            GroupId = sourceGroup?.Id ?? string.Empty,
            OfficerId = sourceOfficer?.OfficerId ?? string.Empty,
            TargetOfficerId = target.OfficerId,
            StartX = sourceOfficer?.X ?? sourceGroup?.X ?? target.X,
            StartY = sourceOfficer?.Y ?? sourceGroup?.Y ?? target.Y,
            EndX = target.X,
            EndY = target.Y,
            Losses = damage,
            Text = $"{text}，体力 −{damage}，士气 {target.Morale:F0}",
        });
        return damage;
    }

    private static void AddMountedOfficerEvent(PendingBattleData pending, BattleOfficerUnitData officer, BattleUnitGroupData target, int losses, bool strategist)
    {
        pending.Timeline.Add(new BattleTimelineEventData
        {
            Start = pending.Elapsed,
            Duration = .9,
            Stage = strategist ? "武将指挥" : "骑将突击",
            Action = strategist ? "officer-command" : "officer-charge",
            Side = officer.Side,
            OfficerId = officer.OfficerId,
            TargetGroupId = target.Id,
            StartX = officer.X,
            StartY = officer.Y,
            EndX = target.X,
            EndY = target.Y,
            Losses = losses,
            Text = $"{officer.Name}{(strategist ? "马上挥令" : "纵马突击")}，造成 {losses} 人损失",
        });
        AddRealtimeEvent(pending, strategist ? "武将指挥" : "骑将突击", "damage", target.Side, target, null,
            $"−{losses}", losses, 0, "", .18);
    }

    private static void MoveGroup(PendingBattleData pending, BattleUnitGroupData group, float targetX, float targetY, double delta)
    {
        var speed = group.TroopType switch { "cavalry" => 72f, "infantry" => 42f, "spears" => 38f, "archers" => 34f, _ => 24f };
        speed *= (float)LocalMoveMultiplier(pending, group.TroopType, group.X, group.Y);
        group.X = MoveTowards(group.X, targetX, speed * (float)delta);
        group.Y = MoveTowards(group.Y, targetY, speed * .35f * (float)delta);
        group.State = "move";
    }

    private static float MoveTowards(float current, float target, float maximumDelta)
    {
        if (Math.Abs(target - current) <= maximumDelta) return target;
        return current + Math.Sign(target - current) * maximumDelta;
    }

    private static BattleUnitGroupData? SelectRealtimeTarget(BattleUnitGroupData source, List<BattleUnitGroupData> enemies, List<BattleUnitGroupData> allies)
    {
        var alive = enemies.Where(item => item.FinalSoldiers > 0).ToList();
        if (alive.Count == 0) return null;
        double Priority(BattleUnitGroupData target)
        {
            var preference = source.TroopType switch
            {
                "cavalry" when target.TroopType is "archers" or "siege" => -180,
                "spears" when target.TroopType == "cavalry" => -160,
                "archers" when target.TroopType is "infantry" or "spears" => -90,
                _ => 0,
            };
            return BattleDistance(source, target) + Math.Abs(source.Lane - target.Lane) * 55 + preference;
        }
        return alive.OrderBy(target => HasEngagementSlot(source, target, allies) ? 0 : 1).ThenBy(Priority).ThenBy(item => item.Id).First();
    }

    private static bool HasEngagementSlot(BattleUnitGroupData source, BattleUnitGroupData target, List<BattleUnitGroupData> allies)
    {
        var capacity = EngagementCapacity(source.TroopType);
        var engaged = allies
            .Where(item => item.FinalSoldiers > 0 && !item.IsRouted && item.TargetGroupId == target.Id)
            .OrderBy(item => BattleDistance(item, target))
            .ThenBy(item => item.Id)
            .Take(capacity)
            .Select(item => item.Id)
            .ToHashSet();
        return engaged.Count < capacity || engaged.Contains(source.Id);
    }

    private static double BattleDistance(BattleUnitGroupData first, BattleUnitGroupData second)
    {
        var dx = first.X - second.X;
        var dy = (first.Y - second.Y) * .55f;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static void AttackTroop(GameSession state, PendingBattleData pending, CityData city, BattleUnitGroupData source, BattleUnitGroupData target, bool breached)
    {
        var stage = source.TroopType == "archers" ? "远程压制" : breached ? "决胜" : "正面接战";
        var ownGroups = pending.Groups.Where(item => item.Side == source.Side && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
        var stateMultiplier = source.Side == "attacker"
            ? pending.AttackerStageMultipliers.GetValueOrDefault(stage, 1)
            : pending.DefenderStageMultipliers.GetValueOrDefault(stage, 1);
        var formation = source.Side == "attacker" ? pending.AttackerFormation.FormationId : pending.DefenderFormation.FormationId;
        var rate = source.TroopType switch { "archers" => .0080, "cavalry" => .0095, "spears" => .0080, "infantry" => .0075, _ => .0030 };
        var power = source.FinalSoldiers * rate
            * TroopPower.GetValueOrDefault(source.TroopType, 1)
            * StageTroopPower(source.TroopType, stage)
            * Matchup(source.TroopType, target.TroopType)
            * Terrain(source.TroopType, pending.Terrain)
            * LocalAttackMultiplier(pending, source)
            * OrderStagePower(source.FormationId, stage)
            * FormationStageModifier(formation, stage)
            * MoralePower(source.Morale)
            * stateMultiplier;
        power *= StanceOffense(SideStance(pending, source.Side), stage)
            * TacticOffense(SideTactic(pending, source.Side), stage, ownGroups);
        var surrounding = ownGroups.Count(item => item.TargetGroupId == target.Id && Math.Abs(item.Lane - target.Lane) <= 1);
        var encirclement = 1 + Math.Clamp(surrounding - 1, 0, 4) * .12;
        var defense = pending.BattleType == "siege" && target.Side == "defender" && !target.IsSortie
            ? 1 + city.Defense / (breached ? 450d : 150d)
            : 1d;
        var targetFormation = target.Side == "attacker" ? pending.AttackerFormation.FormationId : pending.DefenderFormation.FormationId;
        defense *= LocalDefenseMultiplier(pending, target)
            * FormationDefenseModifier(targetFormation, stage)
            * OrderDefenseModifier(target.FormationId, stage);
        var incoming = StanceLoss(SideStance(pending, target.Side)) * TacticLoss(SideTactic(pending, target.Side), stage);
        var losses = Math.Clamp((int)Math.Round(power * encirclement * incoming / defense), 1, Math.Max(1, (int)Math.Ceiling(target.FinalSoldiers * .16)));
        losses = Math.Min(losses, target.FinalSoldiers);
        target.FinalSoldiers -= losses;
        ApplyMoraleImpact(pending, source, target, losses, encirclement);
        source.State = source.TroopType == "archers" ? "volley" : source.TroopType == "cavalry" ? "charge" : "melee";
        source.AttackCooldown = AttackInterval(source.TroopType);
        var action = source.TroopType == "archers" ? "volley" : source.TroopType == "cavalry" ? "charge" : "melee";
        AddRealtimeEvent(pending, stage, action, source.Side, source, target, $"{BattleCatalog.TroopName(source.TroopType)}接敌", 0, 0);
        AddRealtimeEvent(pending, stage, "damage", target.Side, target, source, $"−{losses}", losses, 0, "", source.TroopType == "archers" ? .62 : 0);

        if (pending.BattleType == "siege" && breached && target.Side == "defender")
        {
            var defendersLeft = pending.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
            pending.InnerAfter = Math.Min(pending.InnerAfter, (int)Math.Round(pending.InnerBefore * defendersLeft / (double)Math.Max(1, pending.DefenderBefore)));
        }
    }

    private static void ApplyMoraleImpact(PendingBattleData pending, BattleUnitGroupData source, BattleUnitGroupData target, int losses, double encirclement)
    {
        var lossShare = losses / (double)Math.Max(1, target.InitialSoldiers);
        var shock = 1.2 + lossShare * 48 + Math.Max(0, encirclement - 1) * 7;
        if (source.TroopType == "cavalry") shock += 1.8;
        if (source.TroopType == "archers") shock *= .72;
        target.Morale = Math.Max(0, target.Morale - shock);
        var assignedOfficer = pending.OfficerUnits.FirstOrDefault(item => item.OfficerId == target.AssignedOfficerId && !item.IsRouted);
        if (assignedOfficer is not null)
        {
            var officerShock = .15 + lossShare * 6 + (target.Morale < 25 ? 1.5 : 0);
            assignedOfficer.Morale = Math.Max(0, assignedOfficer.Morale - officerShock);
            if (assignedOfficer.Morale < 20)
            {
                assignedOfficer.IsRouted = true;
                assignedOfficer.State = "mounted-retreat";
            }
        }
        if (target.Morale >= 25) return;

        target.CommandTargetGroupId = string.Empty;
        target.TargetGroupId = string.Empty;
        if (target.Morale < 10)
        {
            if (!target.IsRouted)
            {
                target.IsRouted = true;
                target.CommandMode = "routed";
                target.State = "routed";
                foreach (var ally in pending.Groups.Where(item => item.Side == target.Side && item.Id != target.Id && item.FinalSoldiers > 0 && !item.IsRouted && Math.Abs(item.Lane - target.Lane) <= 1))
                    ally.Morale = Math.Max(0, ally.Morale - 3);
                AddRealtimeEvent(pending, "军心崩溃", "rout", target.Side, target, source,
                    $"{BattleCatalog.TroopName(target.TroopType)}战斗队士气崩溃，退出战线", 0, 0);
            }
        }
        else
        {
            target.CommandMode = "retreat";
            target.State = "retreat";
            AddRealtimeEvent(pending, "战线后撤", "retreat", target.Side, target, source,
                $"{BattleCatalog.TroopName(target.TroopType)}战斗队士气降至{target.Morale:F0}，向后军撤退", 0, 0);
        }
    }

    private static void AttackStructure(PendingBattleData pending, BattleUnitGroupData source)
    {
        var target = source.FormationId == "wall-pressure" ? "wall" : "gate";
        if (target == "wall" && pending.WallAfter <= 0) target = "gate";
        if (target == "gate" && pending.GateAfter <= 0) target = "wall";
        var stateMultiplier = pending.AttackerStageMultipliers.GetValueOrDefault("攻城与内城", 1);
        var tacticMultiplier = pending.PrimaryTactic switch
        {
            "undermine-walls" => 1.55,
            "siege-ladders" => 1.38,
            "fire-attack" => 1.28,
            "fortify-camp" => .68,
            _ => 1,
        };
        var rate = source.TroopType == "siege" ? .009 : .001;
        var damage = source.FinalSoldiers * rate * stateMultiplier
            * FormationStageModifier(pending.AttackerFormation.FormationId, "攻城与内城")
            * OrderStagePower(source.FormationId, "攻城与内城")
            * StanceStructure(pending.Stance) * tacticMultiplier;
        var before = target == "wall" ? pending.WallAfter : pending.GateAfter;
        if (target == "wall")
        {
            pending.WallIntegrity = Math.Max(0, pending.WallIntegrity - damage);
            pending.WallAfter = Math.Max(0, (int)Math.Ceiling(pending.WallIntegrity));
        }
        else
        {
            pending.GateIntegrity = Math.Max(0, pending.GateIntegrity - damage);
            pending.GateAfter = Math.Max(0, (int)Math.Ceiling(pending.GateIntegrity));
        }
        var after = target == "wall" ? pending.WallAfter : pending.GateAfter;
        var applied = Math.Max(0, before - after);
        source.State = "siege";
        source.AttackCooldown = AttackInterval(source.TroopType);
        AddRealtimeEvent(pending, "城墙攻防", "structure", "attacker", source, null,
            $"{BattleCatalog.TroopName(source.TroopType)}攻击{(target == "wall" ? "外墙" : "城门")} −{applied}", 0, applied, target);
        if (before > 0 && after == 0)
        {
            AddRealtimeEvent(pending, "破城", "breach", "attacker", source, null,
                $"{(target == "wall" ? "外墙" : "城门")}被攻破，攻军开始进入城内", 0, 0, target);
        }
    }

    private static double AttackInterval(string troop) => troop switch
    {
        "archers" => 1.4,
        "cavalry" => 1.0,
        "spears" => 1.15,
        "infantry" => 1.1,
        _ => 2.2,
    };

    private static void AddRealtimeEvent(PendingBattleData pending, string stage, string action, string side,
        BattleUnitGroupData? source, BattleUnitGroupData? target, string text, int losses, int structureDamage, string structureTarget = "", double startDelay = 0)
    {
        pending.Timeline.Add(new BattleTimelineEventData
        {
            Start = pending.Elapsed + startDelay,
            Duration = action == "damage" ? .7 : action == "volley" ? .8 : 1.0,
            Stage = stage,
            Action = action,
            Side = side,
            GroupId = source?.Id ?? string.Empty,
            TargetGroupId = target?.Id ?? string.Empty,
            OfficerId = source?.AssignedOfficerId ?? string.Empty,
            StartX = source?.X ?? 0,
            StartY = source?.Y ?? 0,
            EndX = target?.X ?? source?.X ?? 0,
            EndY = target?.Y ?? source?.Y ?? 0,
            Losses = losses,
            StructureTarget = structureTarget,
            StructureDamage = structureDamage,
            Text = text,
        });
    }

    private static void EvaluateRealtimeResult(GameSession state, PendingBattleData pending)
    {
        var attackerLeft = pending.Groups.Where(item => item.Side == "attacker").Sum(item => item.FinalSoldiers);
        var defenderLeft = pending.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
        var attackerEffective = pending.Groups.Where(item => item.Side == "attacker" && !item.IsRouted && item.Morale >= 25).Sum(item => item.FinalSoldiers);
        var defenderEffective = pending.Groups.Where(item => item.Side == "defender" && !item.IsRouted && item.Morale >= 25).Sum(item => item.FinalSoldiers);
        var fieldBattle = pending.BattleType == "field";
        var breached = fieldBattle || pending.WallAfter <= 0 || pending.GateAfter <= 0;
        if (attackerLeft <= Math.Max(1, pending.AttackerBefore * .03) || attackerEffective <= 0)
        {
            FinishRealtime(state, pending, "defeat", attackerEffective <= 0 ? "攻军士气崩溃，战斗队全部退出战线" : fieldBattle ? "出击军团总兵力条耗尽，被迫撤离战场" : "攻军总兵力条耗尽，被迫撤退");
            return;
        }
        if (breached && (defenderLeft <= Math.Max(0, pending.DefenderBefore * .03) || defenderEffective <= 0))
        {
            FinishRealtime(state, pending, "victory", defenderEffective <= 0 ? "守军士气崩溃，防线失去有效抵抗" : fieldBattle ? "敌方军团总兵力条耗尽，出击军团赢得野战" : "守军总兵力条耗尽，攻方控制城池");
            return;
        }
        if (pending.Elapsed + .0001 < pending.Duration) return;
        var attackerRatio = attackerLeft / (double)Math.Max(1, pending.AttackerBefore);
        var defenderRatio = defenderLeft / (double)Math.Max(1, pending.DefenderBefore);
        var victory = breached && attackerRatio > defenderRatio;
        FinishRealtime(state, pending, victory ? "victory" : "defeat", victory
            ? fieldBattle ? "倒计时结束，出击军团剩余兵力比例占优" : "倒计时结束，攻方已破城且剩余兵力比例占优"
            : fieldBattle ? "倒计时结束，敌方军团剩余兵力比例占优" : breached ? "倒计时结束，守军剩余兵力比例占优" : "倒计时结束，城防未破");
    }

    private static void FinishRealtime(GameSession state, PendingBattleData pending, string result, string reason)
    {
        if (pending.Status != "running") return;
        var city = state.Cities.First(item => item.Id == pending.CityId);
        pending.AttackerAfter = pending.Groups.Where(item => item.Side == "attacker").Sum(item => item.FinalSoldiers);
        pending.DefenderAfter = pending.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
        pending.AttackerLosses = pending.AttackerBefore - pending.AttackerAfter;
        pending.DefenderLosses = pending.DefenderBefore - pending.DefenderAfter;
        pending.Result = result;
        foreach (var officer in pending.OfficerUnits)
        {
            var current = pending.OfficerContributions.GetValueOrDefault(officer.OfficerId);
            var mounted = $"骑将战力 {officer.CombatPower:F1} · 体力 {officer.HitPoints:N0}/{officer.MaxHitPoints:N0} · 士气 {officer.Morale:F0} · 杀兵 {officer.TotalDamage:N0} · 对将伤害 {officer.OfficerDamageDealt:N0}";
            pending.OfficerContributions[officer.OfficerId] = string.IsNullOrEmpty(current) ? mounted : $"{current} · {mounted}";
        }
        if (result == "victory" && pending.BattleType == "siege") pending.InnerAfter = Math.Min(25, pending.InnerAfter);
        var attackerPower = RealtimePowerSnapshot(pending, "attacker");
        var defenderPower = RealtimePowerSnapshot(pending, "defender");
        var powerRatio = attackerPower / Math.Max(1, defenderPower);
        pending.PhaseResults =
        [
            new BattlePhaseResultData
            {
                Stage = "实时交战",
                Tactic = $"{pending.PrimaryTactic}/{pending.DefenderPrimaryTactic}",
                AttackerBefore = pending.AttackerBefore,
                AttackerAfter = pending.AttackerAfter,
                DefenderBefore = pending.DefenderBefore,
                DefenderAfter = pending.DefenderAfter,
                AttackerLosses = pending.AttackerLosses,
                DefenderLosses = pending.DefenderLosses,
                WallDamage = pending.WallBefore - pending.WallAfter,
                GateDamage = pending.GateBefore - pending.GateAfter,
                InnerDamage = pending.InnerBefore - pending.InnerAfter,
                PowerRatio = Math.Round(powerRatio, 3),
                AttackerMorale = Math.Round(AverageMorale(pending, "attacker"), 1),
                DefenderMorale = Math.Round(AverageMorale(pending, "defender"), 1),
                AttackerRouted = pending.Groups.Count(item => item.Side == "attacker" && item.IsRouted),
                DefenderRouted = pending.Groups.Count(item => item.Side == "defender" && item.IsRouted),
                Explanation = $"攻方以{StanceName(pending.Stance)}姿态执行{BattleCatalog.TacticName(pending.PrimaryTactic)}，守方以{StanceName(pending.DefenderStance)}姿态执行{BattleCatalog.TacticName(pending.DefenderPrimaryTactic)}；战斗队按距离、兵种克制、最小/最大射程、接战宽度、士气与防御减伤实时结算；{(string.IsNullOrEmpty(pending.DefenderSortieSummary) ? "" : pending.DefenderSortieSummary + "；")}{reason}。倒计时剩余 {Math.Max(0, pending.Duration - pending.Elapsed):F1} 秒",
                AttackerGroupLosses = pending.Groups.Where(item => item.Side == "attacker" && item.InitialSoldiers > item.FinalSoldiers).ToDictionary(item => item.Id, item => item.InitialSoldiers - item.FinalSoldiers),
                DefenderGroupLosses = pending.Groups.Where(item => item.Side == "defender" && item.InitialSoldiers > item.FinalSoldiers).ToDictionary(item => item.Id, item => item.InitialSoldiers - item.FinalSoldiers),
            }
        ];
        pending.Summary = pending.BattleType == "field"
            ? $"{city.Name}近郊军团战结束：{reason}。双方剩余 {pending.AttackerAfter:N0}/{pending.DefenderAfter:N0}。"
            : result == "victory"
                ? $"{city.Name}实时攻防结束：{reason}，攻方夺取城池。攻守剩余 {pending.AttackerAfter:N0}/{pending.DefenderAfter:N0}。"
                : $"{city.Name}实时攻防结束：{reason}，守方守住城池。攻守剩余 {pending.AttackerAfter:N0}/{pending.DefenderAfter:N0}。";
        AddRealtimeEvent(pending, "战果", "result", result == "victory" ? "attacker" : "defender", null, null, pending.Summary, 0, 0);
        pending.Status = "resolved";
    }

    private static double AverageMorale(PendingBattleData pending, string side)
    {
        var groups = pending.Groups.Where(item => item.Side == side && item.FinalSoldiers > 0).ToList();
        var soldiers = groups.Sum(item => item.FinalSoldiers);
        return soldiers <= 0 ? 0 : groups.Sum(item => item.Morale * item.FinalSoldiers) / soldiers;
    }

    private static double RealtimePowerSnapshot(PendingBattleData pending, string side)
    {
        var own = pending.Groups.Where(item => item.Side == side && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
        var enemies = pending.Groups.Where(item => item.Side != side && item.FinalSoldiers > 0 && !item.IsRouted).ToList();
        if (own.Count == 0) return 0;
        var enemyTotal = Math.Max(1, enemies.Sum(item => item.FinalSoldiers));
        var stage = pending.BattleType == "siege" && pending.WallAfter > 0 && pending.GateAfter > 0 ? "攻城与内城" : "决胜";
        var formation = side == "attacker" ? pending.AttackerFormation.FormationId : pending.DefenderFormation.FormationId;
        var stateMultiplier = side == "attacker"
            ? pending.AttackerStageMultipliers.GetValueOrDefault(stage, 1)
            : pending.DefenderStageMultipliers.GetValueOrDefault(stage, 1);
        var raw = own.Sum(group =>
        {
            var matchup = enemies.Count == 0 ? 1 : enemies.Sum(target => target.FinalSoldiers / (double)enemyTotal * Matchup(group.TroopType, target.TroopType));
            return group.FinalSoldiers * TroopPower.GetValueOrDefault(group.TroopType, 1) * StageTroopPower(group.TroopType, stage)
                * matchup * Terrain(group.TroopType, pending.Terrain) * LocalAttackMultiplier(pending, group)
                * OrderStagePower(group.FormationId, stage) * MoralePower(group.Morale);
        });
        return raw * FormationStageModifier(formation, stage) * stateMultiplier
            * StanceOffense(SideStance(pending, side), stage) * TacticOffense(SideTactic(pending, side), stage, own);
    }

    private static FormationPlanData DefaultFormation(Dictionary<string, int> composition, bool attacker)
    {
        var total = Math.Max(1, composition.Values.Sum());
        var formation = composition.GetValueOrDefault("siege") > 0 && attacker
            ? "siege-array"
            : composition.GetValueOrDefault("cavalry") >= total * .25
                ? "wedge"
                : composition.GetValueOrDefault("archers") >= total * .25
                    ? "goose"
                    : attacker && composition.Count(item => item.Value > 0) >= 3 ? "crane" : "shield";
        var plan = new FormationPlanData { FormationId = formation };
        foreach (var troop in BattleCatalog.TroopTypes) plan.TroopOrders[troop] = BattleCatalog.DefaultOrder(troop);
        return plan;
    }

    private static FormationPlanData ArmyFormation(GameSession state, ArmyData army, bool attacker)
    {
        var plan = DefaultFormation(army.Composition, attacker);
        if (army.FactionId != state.PlayerFactionId && IsFormationId(army.FormationId)) plan.FormationId = army.FormationId;
        return plan;
    }

    private static bool IsFormationId(string value) => value is "goose" or "wedge" or "crane" or "shield" or "siege-array";

    private static (string Stance, string Primary) SelectCityDefenseDoctrine(
        Dictionary<string, int> attackerComposition,
        Dictionary<string, int> defenderComposition,
        int attackerSoldiers,
        int defenderSoldiers,
        ScenarioOfficerData? commander)
    {
        var attackerTotal = Math.Max(1, attackerComposition.Values.Sum());
        var defenderTotal = Math.Max(1, defenderComposition.Values.Sum());
        var intelligence = commander is null ? 0 : OfficerProgressionRules.EffectiveAbility(commander, "intelligence", "military");
        var sortieAdvantage = defenderSoldiers / (double)Math.Max(1, attackerSoldiers) >= DefenderSortieRatio;
        if (sortieAdvantage && defenderComposition.GetValueOrDefault("cavalry") + defenderComposition.GetValueOrDefault("infantry") >= defenderTotal * .45)
            return ("aggressive", "encirclement");
        if (attackerComposition.GetValueOrDefault("archers") >= attackerTotal * .25 || attackerComposition.GetValueOrDefault("siege") >= attackerTotal * .08)
            return ("cautious", "shield-wall");
        if (intelligence >= 75 && defenderComposition.GetValueOrDefault("archers") >= defenderTotal * .12)
            return ("cautious", "arrow-volley");
        return ("cautious", "fortify-camp");
    }

    private static Dictionary<string, int> DefenderComposition(int soldiers, int defense)
    {
        var infantry = (int)Math.Round(soldiers * (defense >= 65 ? .40 : .47));
        var spears = (int)Math.Round(soldiers * (defense >= 65 ? .30 : .24));
        var archers = (int)Math.Round(soldiers * .22);
        var cavalry = Math.Max(0, soldiers - infantry - spears - archers);
        return new Dictionary<string, int> { ["infantry"] = infantry, ["spears"] = spears, ["archers"] = archers, ["cavalry"] = cavalry };
    }

    private static List<BattleUnitGroupData> ExpandArmy(Dictionary<string, int> composition, int total, string side, FormationPlanData plan, List<string> officers, string battleType = "siege")
    {
        if (total <= 0) return [];
        var valid = composition.Where(item => item.Value > 0).OrderByDescending(item => item.Value).ToList();
        if (valid.Count == 0) valid = [new KeyValuePair<string, int>("infantry", total)];
        var compositionTotal = valid.Sum(item => item.Value);
        if (compositionTotal != total)
        {
            var scaled = valid
                .Select(item => new
                {
                    item.Key,
                    Exact = item.Value * total / (double)Math.Max(1, compositionTotal),
                })
                .Select(item => new { item.Key, item.Exact, Count = (int)Math.Floor(item.Exact) })
                .ToList();
            var remainder = total - scaled.Sum(item => item.Count);
            var extras = scaled.OrderByDescending(item => item.Exact - item.Count).ThenBy(item => item.Key).Take(remainder).Select(item => item.Key).ToHashSet();
            valid = scaled
                .Select(item => new KeyValuePair<string, int>(item.Key, item.Count + (extras.Contains(item.Key) ? 1 : 0)))
                .Where(item => item.Value > 0)
                .OrderByDescending(item => item.Value)
                .ToList();
        }
        var groupCount = Math.Max(ExpectedGroupCount(total), valid.Count);
        var counts = valid.ToDictionary(item => item.Key, _ => 1);
        for (var index = valid.Count; index < groupCount; index++)
        {
            var selected = valid.OrderByDescending(item => item.Value / (double)(counts[item.Key] + 1)).First();
            counts[selected.Key]++;
        }

        var result = new List<BattleUnitGroupData>();
        foreach (var entry in valid)
        {
            var count = counts[entry.Key];
            if (count <= 0) continue;
            for (var index = 0; index < count; index++)
            {
                var soldiers = entry.Value / count + (index < entry.Value % count ? 1 : 0);
                var lane = LaneFor(entry.Key, index);
                var depth = DepthFor(entry.Key, index, count);
                var order = plan.TroopOrders.GetValueOrDefault(entry.Key) ?? BattleCatalog.DefaultOrder(entry.Key);
                var officer = OfficerFor(entry.Key, depth, officers);
                var group = new BattleUnitGroupData
                {
                    Id = $"{side}-{entry.Key}-{index + 1}", Side = side, TroopType = entry.Key, FormationId = order.OrderId,
                    AssignedOfficerId = officer, InitialSoldiers = soldiers, FinalSoldiers = soldiers, Lane = lane, Depth = depth,
                    MinimumRange = entry.Key == "archers" ? 80 : 0, PreferredRange = entry.Key == "archers" ? 240 : entry.Key == "spears" ? 55 : 40,
                    MaximumRange = entry.Key == "archers" ? 300 : entry.Key == "spears" ? 55 : entry.Key == "cavalry" ? 45 : entry.Key == "siege" ? 35 : 40,
                };
                ApplyDeployment(group, side, battleType);
                result.Add(group);
            }
        }
        SpreadGroups(result, side);
        return result;
    }

    private static void ApplyDeployment(BattleUnitGroupData group, string side, string battleType = "siege")
    {
        var depthX = side == "attacker"
            ? group.Depth switch { 0 => 760f, 1 => 850f, _ => 925f }
            : battleType == "field"
                ? group.Depth switch { 0 => 240f, 1 => 165f, _ => 95f }
                : group.Depth switch { 0 => 330f, 1 => 270f, _ => 210f };
        group.X = depthX + (group.Id.Sum(character => character) % 17 - 8);
        group.Y = 120 + group.Lane * 170 + (group.Id.Sum(character => character) % 23 - 11);
        group.PreviousX = group.X;
        group.PreviousY = group.Y;
        group.DestinationX = battleType == "field"
            ? side == "attacker" ? 560f : 440f
            : side == "defender" ? group.X : group.TroopType switch { "archers" => 520f, "siege" => 445f, _ => 402f };
        group.DestinationY = group.Y;
    }

    private static void DeploySortieGroup(BattleUnitGroupData group)
    {
        group.IsSortie = true;
        group.DestinationX = 560f;
        group.CommandMode = "auto";
        group.CommandTargetGroupId = string.Empty;
        group.CommandDestinationX = group.X;
        group.CommandDestinationY = group.Y;
        group.State = "advance";
    }

    private static void EvaluateDefenderSortie(PendingBattleData pending, List<BattleUnitGroupData> attackers,
        List<BattleUnitGroupData> defenders, bool breached)
    {
        if (pending.BattleType != "siege" || breached) return;
        var attackerLeft = attackers.Sum(item => item.FinalSoldiers);
        var defenderLeft = defenders.Sum(item => item.FinalSoldiers);
        if (pending.DefenderSortie && defenders.All(item => !item.IsSortie)) pending.DefenderSortie = false;
        if (!pending.DefenderSortieUsed && ShouldDefenderSortie(attackerLeft, defenderLeft))
        {
            pending.DefenderSortie = true;
            pending.DefenderSortieUsed = true;
            pending.DefenderSortieSummary = $"交战中守军形成{defenderLeft / (double)Math.Max(1, attackerLeft):F1}倍兵力优势，开城主动迎战";
            foreach (var group in defenders.Where(item => item.TroopType is "infantry" or "spears" or "cavalry")) DeploySortieGroup(group);
            AddRealtimeEvent(pending, "守军出击", "command", "defender", defenders.FirstOrDefault(item => item.IsSortie), null,
                pending.DefenderSortieSummary, 0, 0);
            return;
        }
        if (!pending.DefenderSortie || defenderLeft / (double)Math.Max(1, attackerLeft) > DefenderRetreatRatio) return;
        pending.DefenderSortie = false;
        pending.DefenderSortieSummary += $"；兵力优势缩小至{defenderLeft / (double)Math.Max(1, attackerLeft):F1}倍后退守城内";
        foreach (var group in defenders.Where(item => item.IsSortie))
        {
            group.IsSortie = false;
            group.CommandMode = "move";
            group.CommandTargetGroupId = string.Empty;
            group.TargetGroupId = string.Empty;
            group.CommandDestinationX = Math.Min(330, 300 + group.Depth * 15);
            group.CommandDestinationY = group.Y;
        }
        AddRealtimeEvent(pending, "退守城内", "command", "defender", defenders.FirstOrDefault(), null,
            "出城部队失去压倒性优势，依令退回城内据墙防守", 0, 0);
    }

    private static void ApplyOrderLayout(BattleUnitGroupData group, int index, string armyFormation)
    {
        switch (group.FormationId)
        {
            case "shield-line": group.Lane = new[] { 1, 3, 2, 0, 4 }[index % 5]; group.Depth = 0; break;
            case "loose-line": group.Lane = new[] { 0, 2, 4, 1, 3 }[index % 5]; group.Depth = index % 2; break;
            case "assault-column": group.Lane = 2 + (index % 3 - 1); group.Depth = index % 3 == 0 ? 0 : 1; break;
            case "spear-wall": group.Lane = new[] { 0, 4, 1, 3, 2 }[index % 5]; group.Depth = 0; break;
            case "support-line": group.Lane = new[] { 1, 3, 2, 0, 4 }[index % 5]; group.Depth = 1; break;
            case "spear-column": group.Lane = new[] { 2, 1, 3 }[index % 3]; group.Depth = index % 2; break;
            case "skirmish": group.Lane = new[] { 1, 3, 2, 0, 4 }[index % 5]; group.Depth = 1; break;
            case "wing-fire": group.Lane = index % 2 == 0 ? 0 : 4; group.Depth = 2; break;
            case "rear-double": group.Lane = new[] { 1, 3, 2, 0, 4 }[index % 5]; group.Depth = 2; break;
            case "reserve": group.Lane = 2; group.Depth = 1; break;
            case "cavalry-wedge": group.Lane = new[] { 2, 1, 3 }[index % 3]; group.Depth = 0; break;
            case "wing-column": group.Lane = index % 2 == 0 ? 0 : 4; group.Depth = 1; break;
            case "gate-column": group.Lane = 2; group.Depth = 2; break;
            case "wall-pressure": group.Lane = new[] { 1, 3, 0, 4, 2 }[index % 5]; group.Depth = 2; break;
            case "protected-siege": group.Lane = new[] { 2, 1, 3 }[index % 3]; group.Depth = 2; break;
        }
        ApplyArmyFormationLayout(group, index, armyFormation);
    }

    private static void ApplyArmyFormationLayout(BattleUnitGroupData group, int index, string formation)
    {
        switch (formation)
        {
            case "goose":
                group.Depth = Math.Clamp(group.Depth + (Math.Abs(group.Lane - 2) >= 2 ? 1 : 0), 0, 2);
                break;
            case "wedge":
                group.Lane = new[] { 2, 1, 3, 0, 4 }[index % 5];
                group.Depth = Math.Clamp((Math.Abs(group.Lane - 2) + 1) / 2 + (group.TroopType is "archers" or "siege" ? 1 : 0), 0, 2);
                break;
            case "crane":
                if (group.TroopType is "infantry" or "cavalry") group.Lane = index % 2 == 0 ? 0 : 4;
                else group.Lane = new[] { 2, 1, 3 }[index % 3];
                group.Depth = group.TroopType is "archers" or "siege" ? 2 : Math.Min(group.Depth, 1);
                break;
            case "shield":
                group.Lane = new[] { 1, 3, 2, 0, 4 }[index % 5];
                group.Depth = group.TroopType is "infantry" or "spears" ? 0 : group.TroopType is "archers" or "siege" ? 2 : 1;
                break;
            case "siege-array":
                group.Lane = group.TroopType == "siege" ? new[] { 2, 1, 3 }[index % 3] : group.Lane;
                group.Depth = group.TroopType is "infantry" or "spears" ? 0 : group.TroopType == "cavalry" ? 1 : 2;
                break;
        }
    }

    private static void SpreadGroups(List<BattleUnitGroupData> groups, string side)
    {
        foreach (var bucket in groups.GroupBy(item => (item.Lane, item.Depth)))
        {
            var ordered = bucket.OrderBy(item => item.TroopType).ThenBy(item => item.Id).ToList();
            for (var index = 0; index < ordered.Count; index++)
            {
                var group = ordered[index];
                var yOffset = (index - (ordered.Count - 1) / 2f) * 38f;
                var rankOffset = index % 2 * 62f;
                var xOffset = side == "attacker" ? rankOffset : -rankOffset;
                group.X += xOffset;
                group.Y += yOffset;
                group.DestinationX += xOffset * .7f;
                group.DestinationY += yOffset;
            }
        }
    }

    private static int LaneFor(string troop, int index) => troop switch
    {
        "cavalry" => index % 2 == 0 ? 0 : 4,
        "archers" => new[] { 1, 3, 2, 0, 4 }[index % 5],
        "siege" => new[] { 2, 1, 3 }[index % 3],
        "spears" => new[] { 0, 4, 1, 3, 2 }[index % 5],
        _ => new[] { 2, 1, 3, 0, 4 }[index % 5],
    };

    private static int DepthFor(string troop, int index, int count) => troop switch
    {
        "archers" or "siege" => 2,
        "cavalry" => 1,
        "spears" => 0,
        _ => index < Math.Max(1, (int)Math.Ceiling(count * .65)) ? 0 : 1,
    };

    private static string OfficerFor(string troop, int depth, List<string> officers)
    {
        if (officers.Count == 0) return string.Empty;
        if (officers.Count > 2 && (troop is "archers" or "siege")) return officers[2];
        if (officers.Count > 1 && (depth == 0 || troop == "cavalry")) return officers[1];
        return officers[0];
    }

    private static void AssignRoles(PendingBattleData pending)
    {
        if (pending.AttackerOfficerIds.Count > 0) pending.OfficerRoles[pending.AttackerOfficerIds[0]] = "主将·统筹全军";
        if (pending.AttackerOfficerIds.Count > 1) pending.OfficerRoles[pending.AttackerOfficerIds[1]] = "前锋·率前军突破";
        if (pending.AttackerOfficerIds.Count > 2) pending.OfficerRoles[pending.AttackerOfficerIds[2]] = "军师·远程与计策";
        if (pending.DefenderOfficerIds.Count > 0) pending.OfficerRoles[pending.DefenderOfficerIds[0]] = pending.BattleType == "field" ? "敌军主将·统筹全军" : "守城主将·稳定全军";
        if (pending.DefenderOfficerIds.Count > 1) pending.OfficerRoles[pending.DefenderOfficerIds[1]] = pending.BattleType == "field" ? "敌军前锋·正面拒止" : "城门督·前线拒止";
        if (pending.DefenderOfficerIds.Count > 2) pending.OfficerRoles[pending.DefenderOfficerIds[2]] = pending.BattleType == "field" ? "敌军军师·远程策应" : "守城军师·墙头压制";
    }

    private static double OfficerMultiplier(GameSession state, List<string> ids, Dictionary<string, string> roles, Dictionary<string, string> descriptions, bool attacker)
    {
        double total = 1;
        for (var index = 0; index < ids.Count; index++)
        {
            var officer = state.Officers.FirstOrDefault(item => item.Profile.Id == ids[index]);
            if (officer is null) continue;
            var contribution = OfficerRoleContribution(officer, index);
            total += contribution;
            var role = roles.GetValueOrDefault(officer.Profile.Id, attacker ? "随军武将" : "守城武将");
            descriptions[officer.Profile.Id] = $"{role} · 战力贡献 +{contribution:P1}";
        }
        return Math.Clamp(total, .85, 1.25);
    }

    public static double OfficerRoleContribution(ScenarioOfficerData officer, int roleIndex)
    {
        var leadership = OfficerProgressionRules.EffectiveAbility(officer, "leadership", "military");
        var might = OfficerProgressionRules.EffectiveAbility(officer, "might", "military");
        var intelligence = OfficerProgressionRules.EffectiveAbility(officer, "intelligence", "military");
        var charisma = OfficerProgressionRules.EffectiveAbility(officer, "charisma", "military");
        var contribution = roleIndex == 0
            ? Math.Clamp((leadership - 45) * .0018 + charisma * .00025, .015, .13)
            : roleIndex == 1
                ? Math.Clamp((might + leadership) / 2800d, .025, .075)
                : Math.Clamp((intelligence + leadership) / 3000d, .025, .07);
        return contribution * Math.Clamp(officer.InitialState.Health / 100d, .45, 1) * Math.Clamp(1 - officer.InitialState.Fatigue / 180d, .45, 1);
    }

    private static double StateMultiplier(int training, int morale, int fatigue, bool supplied) =>
        (.80 + Math.Clamp(training, 0, 100) / 500d)
        * (.75 + Math.Clamp(morale, 0, 100) / 400d)
        * (1 - Math.Clamp(fatigue, 0, 100) * .003)
        * (supplied ? 1 : .72);

    private static BattlePhaseResultData ResolveTroopPhase(
        string stage,
        string tactic,
        List<BattleUnitGroupData> attackers,
        List<BattleUnitGroupData> defenders,
        PendingBattleData pending,
        double attackerState,
        double defenderState,
        double attackerBaseLoss,
        double defenderBaseLoss)
    {
        var result = new BattlePhaseResultData
        {
            Stage = stage,
            Tactic = tactic,
            AttackerBefore = attackers.Sum(item => item.FinalSoldiers),
            DefenderBefore = defenders.Sum(item => item.FinalSoldiers),
        };
        var attackerPower = StagePower(attackers, defenders, pending.Terrain, stage, pending.AttackerFormation.FormationId)
            * attackerState * StanceOffense(pending.Stance, stage) * TacticOffense(tactic, stage, attackers);
        var defenderPower = StagePower(defenders, attackers, pending.Terrain, stage, pending.DefenderFormation.FormationId) * defenderState;
        var ratio = Math.Clamp(attackerPower / Math.Max(1, defenderPower), .28, 3.6);
        var attackerLossRate = attackerBaseLoss * Math.Clamp(Math.Pow(1 / ratio, .48), .62, 1.72)
            * StanceLoss(pending.Stance) * TacticLoss(tactic, stage);
        var defenderLossRate = defenderBaseLoss * Math.Clamp(Math.Pow(ratio, .48), .62, 1.72);
        var attackerLosses = result.AttackerBefore > 0 ? Math.Clamp((int)Math.Round(result.AttackerBefore * attackerLossRate), 1, result.AttackerBefore) : 0;
        var defenderLosses = result.DefenderBefore > 0 ? Math.Clamp((int)Math.Round(result.DefenderBefore * defenderLossRate), 1, result.DefenderBefore) : 0;
        result.AttackerGroupLosses = AllocatePhaseLosses(attackers, attackerLosses, stage);
        result.DefenderGroupLosses = AllocatePhaseLosses(defenders, defenderLosses, stage);
        result.AttackerAfter = attackers.Sum(item => item.FinalSoldiers);
        result.DefenderAfter = defenders.Sum(item => item.FinalSoldiers);
        result.AttackerLosses = result.AttackerBefore - result.AttackerAfter;
        result.DefenderLosses = result.DefenderBefore - result.DefenderAfter;
        result.PowerRatio = Math.Round(ratio, 3);
        result.Explanation = $"{BattleCatalog.TacticName(tactic)}在{stage}生效（{TacticEffectSummary(tactic)}）；{StanceName(pending.Stance)}姿态，攻守战力比 {ratio:F2}，兵力承接 {result.AttackerBefore:N0}→{result.AttackerAfter:N0} / {result.DefenderBefore:N0}→{result.DefenderAfter:N0}";
        return result;
    }

    private static BattlePhaseResultData ResolveSiegePhase(
        List<BattleUnitGroupData> attackers,
        List<BattleUnitGroupData> defenders,
        PendingBattleData pending,
        CityData city,
        double attackerState,
        double defenderState)
    {
        const string stage = "攻城与内城";
        var tactic = pending.PrimaryTactic;
        var result = new BattlePhaseResultData
        {
            Stage = stage,
            Tactic = tactic,
            AttackerBefore = attackers.Sum(item => item.FinalSoldiers),
            DefenderBefore = defenders.Sum(item => item.FinalSoldiers),
        };
        var attackerPower = StagePower(attackers, defenders, pending.Terrain, stage, pending.AttackerFormation.FormationId)
            * attackerState * StanceOffense(pending.Stance, stage) * TacticOffense(tactic, stage, attackers);
        var defenderPower = StagePower(defenders, attackers, pending.Terrain, stage, pending.DefenderFormation.FormationId)
            * defenderState * (1 + city.Defense / 300d);
        var ratio = Math.Clamp(attackerPower / Math.Max(1, defenderPower), .28, 3.6);
        var siegeShare = attackers.Where(item => item.TroopType == "siege").Sum(item => item.FinalSoldiers) / (double)Math.Max(1, result.AttackerBefore);
        var structureFactor = StanceStructure(pending.Stance) * FormationStageModifier(pending.AttackerFormation.FormationId, stage);
        var baseDamage = (5 + siegeShare * 145 + Math.Clamp(ratio, .45, 1.9) * 7 + TacticStructure(tactic, siegeShare > 0)) * structureFactor - city.Defense * .115;
        result.WallDamage = Math.Clamp((int)Math.Round(baseDamage + (tactic == "undermine-walls" ? 9 : tactic == "siege-ladders" ? 6 : 0)), 2, 72);
        result.GateDamage = Math.Clamp((int)Math.Round(baseDamage + (tactic == "fire-attack" ? 12 : tactic == "siege-ladders" ? 8 : 2)), 2, 78);
        if (siegeShare <= 0 && tactic is not ("siege-ladders" or "undermine-walls"))
        {
            result.WallDamage = Math.Min(8, result.WallDamage);
            result.GateDamage = Math.Min(8, result.GateDamage);
        }
        var breached = pending.WallBefore - result.WallDamage <= 0 || pending.GateBefore - result.GateDamage <= 0;
        result.InnerDamage = breached
            ? Math.Clamp((int)Math.Round(ratio * 42 + TacticInner(tactic)), 16, 82)
            : 0;
        var attackerLossRate = (breached ? .049 : .036) * Math.Clamp(Math.Pow(1 / ratio, .45), .65, 1.65)
            * StanceLoss(pending.Stance) * TacticLoss(tactic, stage);
        var defenderLossRate = (breached ? .082 : .030) * Math.Clamp(Math.Pow(ratio, .45), .65, 1.65);
        var attackerLosses = result.AttackerBefore > 0 ? Math.Clamp((int)Math.Round(result.AttackerBefore * attackerLossRate), 1, result.AttackerBefore) : 0;
        var defenderLosses = result.DefenderBefore > 0 ? Math.Clamp((int)Math.Round(result.DefenderBefore * defenderLossRate), 1, result.DefenderBefore) : 0;
        result.AttackerGroupLosses = AllocatePhaseLosses(attackers, attackerLosses, stage);
        result.DefenderGroupLosses = AllocatePhaseLosses(defenders, defenderLosses, stage);
        result.AttackerAfter = attackers.Sum(item => item.FinalSoldiers);
        result.DefenderAfter = defenders.Sum(item => item.FinalSoldiers);
        result.AttackerLosses = result.AttackerBefore - result.AttackerAfter;
        result.DefenderLosses = result.DefenderBefore - result.DefenderAfter;
        result.PowerRatio = Math.Round(ratio, 3);
        result.Explanation = $"{BattleCatalog.TacticName(tactic)}作用于城防（{TacticEffectSummary(tactic)}）；攻守战力比 {ratio:F2}，墙/门/内城损伤 {result.WallDamage}/{result.GateDamage}/{result.InnerDamage}，本阶段损失 {result.AttackerLosses:N0}/{result.DefenderLosses:N0}";
        return result;
    }

    private static double StagePower(List<BattleUnitGroupData> own, List<BattleUnitGroupData> enemy, string terrain, string stage, string formation)
    {
        var enemyTotal = Math.Max(1, enemy.Sum(item => item.FinalSoldiers));
        var power = own.Sum(group =>
        {
            var matchup = enemy.Sum(target => target.FinalSoldiers / (double)enemyTotal * Matchup(group.TroopType, target.TroopType));
            return group.FinalSoldiers
                * TroopPower.GetValueOrDefault(group.TroopType, 1)
                * StageTroopPower(group.TroopType, stage)
                * matchup
                * Terrain(group.TroopType, terrain)
                * OrderStagePower(group.FormationId, stage);
        });
        return power * FormationStageModifier(formation, stage);
    }

    private static double StageTroopPower(string troop, string stage) => stage switch
    {
        "远程压制" => troop switch { "archers" => 1.42, "siege" => .48, "cavalry" => .16, _ => .22 },
        "正面接战" => troop switch { "infantry" => 1.05, "spears" => 1.08, "cavalry" => 1.02, "archers" => .48, _ => .22 },
        "决胜" => troop switch { "cavalry" => 1.20, "infantry" => 1.02, "spears" => .94, "archers" => .70, _ => .34 },
        _ => troop switch { "siege" => 1.65, "infantry" => .68, "archers" => .38, "spears" => .42, _ => .45 },
    };

    private static double FormationStageModifier(string formation, string stage) => (formation, stage) switch
    {
        ("goose", "远程压制") => 1.12,
        ("wedge", "正面接战") or ("wedge", "决胜") => 1.12,
        ("crane", "远程压制") => 1.05,
        ("crane", "决胜") => 1.10,
        ("shield", "远程压制") => 1.04,
        ("shield", "正面接战") => .96,
        ("siege-array", "攻城与内城") => 1.20,
        ("siege-array", "远程压制") => .92,
        _ => 1,
    };

    private static double StanceOffense(string stance, string stage) => stance switch
    {
        "cautious" => stage == "远程压制" ? .98 : stage == "攻城与内城" ? .88 : .92,
        "aggressive" => stage == "远程压制" ? 1.06 : stage == "攻城与内城" ? 1.12 : 1.13,
        _ => 1,
    };

    private static double StanceLoss(string stance) => stance switch { "cautious" => .82, "aggressive" => 1.18, _ => 1 };
    private static double StanceStructure(string stance) => stance switch { "cautious" => .88, "aggressive" => 1.12, _ => 1 };
    private static string StanceName(string stance) => stance switch { "cautious" => "稳健", "aggressive" => "激进", _ => "标准" };

    private static double TacticOffense(string tactic, string stage, List<BattleUnitGroupData> groups) => tactic switch
    {
        "steady-advance" => stage switch { "正面接战" => 1.05, "决胜" => 1.04, _ => 1.02 },
        "shield-wall" => stage switch { "远程压制" => .98, "正面接战" => .94, "决胜" => .96, _ => .90 },
        "feigned-retreat" => stage switch { "正面接战" => .90, "决胜" => 1.22, _ => .98 },
        "night-raid" => stage switch { "远程压制" => 1.13, "决胜" => 1.13, "攻城与内城" => .92, _ => 1.04 },
        "fire-attack" => stage switch { "远程压制" => 1.14, "攻城与内城" => 1.16, _ => 1.02 },
        "encirclement" => stage == "决胜" ? 1.18 + TroopShare(groups, "cavalry") * .16 : stage == "正面接战" ? 1.03 : .97,
        "arrow-volley" => stage == "远程压制" ? 1.18 + TroopShare(groups, "archers") * .28 : stage == "正面接战" ? .94 : 1,
        "cavalry-charge" => stage is "正面接战" or "决胜" ? 1.10 + TroopShare(groups, "cavalry") * .38 : .94,
        "fortify-camp" => stage switch { "远程压制" => .98, "正面接战" => .91, "决胜" => .94, _ => .84 },
        "cut-supply" => stage switch { "决胜" => 1.16, "攻城与内城" => 1.08, _ => .97 },
        "siege-ladders" => stage == "攻城与内城" ? 1.20 : stage == "正面接战" ? .96 : .91,
        "undermine-walls" => stage == "攻城与内城" ? 1.25 : stage == "远程压制" ? .88 : .94,
        _ => 1,
    };

    private static double TacticLoss(string tactic, string stage) => tactic switch
    {
        "steady-advance" => .95,
        "shield-wall" => stage is "远程压制" or "正面接战" ? .76 : .88,
        "feigned-retreat" => stage == "正面接战" ? 1.13 : stage == "决胜" ? .92 : 1,
        "night-raid" => stage == "正面接战" ? 1.12 : .96,
        "fire-attack" => stage is "正面接战" or "攻城与内城" ? 1.09 : .98,
        "encirclement" => stage == "决胜" ? 1.08 : 1,
        "arrow-volley" => stage == "远程压制" ? .91 : 1.04,
        "cavalry-charge" => stage is "正面接战" or "决胜" ? 1.16 : 1,
        "fortify-camp" => .72,
        "cut-supply" => stage == "决胜" ? .90 : 1,
        "siege-ladders" => stage == "攻城与内城" ? 1.18 : 1.04,
        "undermine-walls" => stage == "攻城与内城" ? 1.10 : .98,
        _ => 1,
    };

    private static string SideStance(PendingBattleData pending, string side) => side == "attacker" ? pending.Stance : pending.DefenderStance;

    private static string SideTactic(PendingBattleData pending, string side) => side == "attacker" ? pending.PrimaryTactic : pending.DefenderPrimaryTactic;

    private static double MoralePower(double morale) => .75 + Math.Clamp(morale, 0, 100) / 400d;

    private static double FormationDefenseModifier(string formation, string stage) => (formation, stage) switch
    {
        ("shield", "远程压制") => 1.14,
        ("shield", "正面接战") => 1.10,
        ("goose", "远程压制") => 1.04,
        ("crane", "决胜") => 1.06,
        ("siege-array", "攻城与内城") => 1.08,
        ("wedge", "远程压制") => .94,
        _ => 1,
    };

    private static double OrderDefenseModifier(string order, string stage) => (order, stage) switch
    {
        ("shield-line", "远程压制") => 1.14,
        ("shield-line", "正面接战") => 1.10,
        ("loose-line", "远程压制") => 1.12,
        ("spear-wall", "正面接战") => 1.12,
        ("support-line", "决胜") => 1.07,
        ("rear-double", "正面接战") => .92,
        ("wing-fire", "正面接战") => .90,
        ("protected-siege", "攻城与内城") => 1.14,
        ("reserve", "决胜") => 1.08,
        ("assault-column", "远程压制") => .92,
        ("cavalry-wedge", "远程压制") => .90,
        _ => 1,
    };

    private static double TacticStructure(string tactic, bool hasSiege) => tactic switch
    {
        "fire-attack" => 10,
        "cut-supply" => 4,
        "siege-ladders" => hasSiege ? 16 : 7,
        "undermine-walls" => hasSiege ? 20 : 9,
        "steady-advance" => 2,
        "night-raid" => -2,
        "fortify-camp" => -5,
        _ => 0,
    };

    private static double TacticInner(string tactic) => tactic switch
    {
        "feigned-retreat" => 7,
        "night-raid" => 8,
        "fire-attack" => 6,
        "encirclement" => 12,
        "cavalry-charge" => 8,
        "cut-supply" => 10,
        "siege-ladders" => 5,
        _ => 0,
    };

    public static string TacticEffectSummary(string tactic) => tactic switch
    {
        "steady-advance" => "各阶段小幅增益并降低损失，爆发较弱",
        "shield-wall" => "显著降低远程与接战损失，推进和攻城变慢",
        "feigned-retreat" => "接战示弱换取决胜反击，接战损失风险上升",
        "night-raid" => "远程与决胜先手增强，接战失序风险上升且攻城较弱",
        "fire-attack" => "强化远程、城门和城防破坏，近战自损风险上升",
        "encirclement" => "决胜阶段按骑兵占比增强，接战与决胜损失略升",
        "arrow-volley" => "按弓兵占比强化远程并降低该阶段损失，接战较弱",
        "cavalry-charge" => "按骑兵占比强化接战与决胜，冲锋损失风险上升",
        "fortify-camp" => "各阶段大幅减损，但主动推进与攻城能力明显下降",
        "cut-supply" => "决胜、内城控制增强且决胜减损，前期正面能力较弱",
        "siege-ladders" => "强化城墙、城门和内城突入，攻城伤亡风险上升",
        "undermine-walls" => "最高城防破坏，前期掩护不足且攻城伤亡略升",
        _ => "无额外战术修正",
    };

    public static string StanceEffectSummary(string stance) => stance switch
    {
        "cautious" => "正面攻势0.92，承受损失0.82，攻城效率0.88",
        "aggressive" => "正面攻势1.13，承受损失1.18，攻城效率1.12",
        _ => "正面攻势1.00，承受损失1.00，攻城效率1.00",
    };

    public static string TerrainEffectSummary(string terrain) => "平地，无树林、坡顶或浅滩额外修正";

    private static double TroopShare(List<BattleUnitGroupData> groups, string troop) =>
        groups.Where(item => item.TroopType == troop).Sum(item => item.FinalSoldiers) / (double)Math.Max(1, groups.Sum(item => item.FinalSoldiers));

    public static string TacticRequirement(string primary, IReadOnlyDictionary<string, int> composition)
    {
        var total = Math.Max(1, composition.Values.Sum());
        return primary switch
        {
            "arrow-volley" when composition.GetValueOrDefault("archers") < total * .12 => "主战术“箭雨”条件不足：弓兵需达到总兵力的12%。",
            "cavalry-charge" when composition.GetValueOrDefault("cavalry") < total * .12 => "主战术“骑兵突击”条件不足：骑兵需达到总兵力的12%。",
            "encirclement" when composition.GetValueOrDefault("cavalry") + composition.GetValueOrDefault("infantry") < total * .45 => "主战术“包围”条件不足：步兵与骑兵合计需达到总兵力的45%。",
            "siege-ladders" when composition.GetValueOrDefault("siege") < total * .06 => "主战术“云梯强攻”条件不足：攻城器械需达到总兵力的6%。",
            "undermine-walls" when composition.GetValueOrDefault("siege") < total * .06 => "主战术“掘城墙”条件不足：攻城器械需达到总兵力的6%。",
            _ => string.Empty,
        };
    }

    private static double Matchup(string attacker, string defender) => (attacker, defender) switch
    {
        ("spears", "cavalry") => 1.18,
        ("cavalry", "archers") => 1.18,
        ("cavalry", "infantry") => 1.12,
        ("archers", "infantry") => 1.12,
        ("archers", "spears") => 1.10,
        ("cavalry", "spears") => .82,
        ("infantry", "archers") => .94,
        ("siege", _) => .72,
        _ => 1,
    };

    private static double Terrain(string troop, string terrain) => 1;

    public static double LocalMoveMultiplier(PendingBattleData pending, string troop, float x, float y) => 1;

    private static double LocalAttackMultiplier(PendingBattleData pending, BattleUnitGroupData source) => 1;

    private static double LocalDefenseMultiplier(PendingBattleData pending, BattleUnitGroupData target) => 1;

    private static double OrderStagePower(string order, string stage) => (order, stage) switch
    {
        ("shield-line", "远程压制") => 1.05,
        ("loose-line", "远程压制") => 1.10,
        ("assault-column", "正面接战") or ("assault-column", "决胜") => 1.13,
        ("spear-wall", "正面接战") => 1.12,
        ("support-line", "决胜") => 1.08,
        ("spear-column", "决胜") => 1.10,
        ("rear-double", "远程压制") => 1.13,
        ("wing-fire", "远程压制") => 1.15,
        ("skirmish", "远程压制") => 1.09,
        ("wing-column", "决胜") => 1.12,
        ("cavalry-wedge", "正面接战") or ("cavalry-wedge", "决胜") => 1.16,
        ("reserve", "决胜") => 1.12,
        ("protected-siege", "攻城与内城") => 1.10,
        ("gate-column", "攻城与内城") => 1.17,
        ("wall-pressure", "攻城与内城") => 1.14,
        _ => 1,
    };

    private static Dictionary<string, int> AllocatePhaseLosses(List<BattleUnitGroupData> groups, int totalLosses, string stage)
    {
        var result = new Dictionary<string, int>();
        var weights = groups.ToDictionary(group => group.Id, group => group.FinalSoldiers * PhaseExposure(group, stage));
        var weightTotal = Math.Max(1, weights.Values.Sum());
        var assigned = 0;
        foreach (var group in groups.OrderBy(item => item.Id))
        {
            var loss = Math.Min(group.FinalSoldiers, (int)Math.Floor(totalLosses * weights[group.Id] / weightTotal));
            group.FinalSoldiers -= loss;
            if (loss > 0) result[group.Id] = loss;
            assigned += loss;
        }
        var remaining = totalLosses - assigned;
        foreach (var group in groups.OrderByDescending(item => PhaseExposure(item, stage)).ThenByDescending(item => item.FinalSoldiers).ThenBy(item => item.Id))
        {
            if (remaining <= 0) break;
            var loss = Math.Min(group.FinalSoldiers, remaining);
            group.FinalSoldiers -= loss;
            if (loss > 0) result[group.Id] = result.GetValueOrDefault(group.Id) + loss;
            remaining -= loss;
        }
        return result;
    }

    private static double PhaseExposure(BattleUnitGroupData group, string stage)
    {
        var exposure = group.Depth switch { 0 => 1.2, 1 => .92, _ => .68 };
        if (stage == "远程压制" && group.Depth == 0) exposure += .22;
        if (stage == "正面接战" && group.TroopType is "infantry" or "spears") exposure += .15;
        if (stage == "决胜" && group.TroopType == "cavalry") exposure += .20;
        if (stage == "攻城与内城" && group.TroopType == "siege") exposure += .28;
        if (group.FormationId is "shield-line" or "spear-wall" or "protected-siege") exposure -= .12;
        if (group.FormationId is "assault-column" or "wing-fire" or "cavalry-wedge") exposure += .10;
        return Math.Max(.45, exposure);
    }

    private static void BuildTimeline(PendingBattleData pending, List<BattleUnitGroupData> attackers, List<BattleUnitGroupData> defenders, int wallDamage, int gateDamage, bool breached)
    {
        pending.Timeline.Clear();
        pending.Timeline.Add(new BattleTimelineEventData { Start = 0, Duration = .8, Stage = "列阵", Action = "message", Text = $"{BattleCatalog.FormationName(pending.AttackerFormation.FormationId)}展开，以{StanceName(pending.Stance)}姿态执行{BattleCatalog.TacticName(pending.PrimaryTactic)}" });
        foreach (var group in pending.Groups)
        {
            pending.Timeline.Add(new BattleTimelineEventData { Start = .6, Duration = 3.0, Stage = "推进", Action = "move", Side = group.Side, GroupId = group.Id, OfficerId = group.AssignedOfficerId, StartX = group.X, StartY = group.Y, EndX = group.DestinationX, EndY = group.DestinationY, Text = group.TroopType == "archers" ? "弓兵推进至有效射程" : $"{BattleCatalog.TroopName(group.TroopType)}按阵位推进" });
        }
        AddAttacks(pending, attackers, defenders, "archers", 3.8, "volley", "远程压制");
        AddAttacks(pending, defenders, attackers, "archers", 4.25, "volley", "守军齐射");
        AddPhaseDamageEvents(pending, pending.PhaseResults.First(item => item.Stage == "远程压制"), 4.55);
        AddAttacks(pending, attackers, defenders, "cavalry", 5.3, "charge", "侧翼冲击");
        AddAttacks(pending, defenders, attackers, "cavalry", 5.7, "charge", "守军反冲锋");
        AddAttacks(pending, attackers, defenders, "infantry", 6.2, "melee", "正面接战");
        AddAttacks(pending, attackers, defenders, "spears", 6.45, "brace", "枪阵接敌");
        AddAttacks(pending, defenders, attackers, "infantry", 6.7, "melee", "守军接战");
        AddAttacks(pending, defenders, attackers, "spears", 6.9, "brace", "守军架枪");
        AddPhaseDamageEvents(pending, pending.PhaseResults.First(item => item.Stage == "正面接战"), 7.25);
        pending.Timeline.Add(new BattleTimelineEventData { Start = 8.05, Duration = 1.0, Stage = "战术执行", Action = "message", Side = "attacker", Text = pending.DecisionSummary });
        AddAttacks(pending, attackers, defenders, "cavalry", 8.65, "charge", "决胜");
        AddAttacks(pending, attackers, defenders, "infantry", 8.9, "melee", "决胜");
        AddPhaseDamageEvents(pending, pending.PhaseResults.First(item => item.Stage == "决胜"), 9.45);
        AddAttacks(pending, attackers, defenders, "siege", 10.1, "siege", "攻城与内城");
        pending.Timeline.Add(new BattleTimelineEventData { Start = 10.75, Duration = 1, Stage = "外城墙", Action = "structure", Side = "attacker", StructureTarget = "wall", StructureDamage = wallDamage, Text = $"{BattleCatalog.TacticName(pending.PrimaryTactic)}：外城墙耐久 −{wallDamage}" });
        pending.Timeline.Add(new BattleTimelineEventData { Start = 11.65, Duration = 1, Stage = "城门", Action = "structure", Side = "attacker", StructureTarget = "gate", StructureDamage = gateDamage, Text = $"城门耐久 −{gateDamage}" });
        AddPhaseDamageEvents(pending, pending.PhaseResults.First(item => item.Stage == "攻城与内城"), 12.35);
        if (breached) pending.Timeline.Add(new BattleTimelineEventData { Start = 13.05, Duration = 1.5, Stage = "缺口争夺", Action = "breach", Side = "attacker", Text = $"城防出现缺口，内城控制 −{pending.PhaseResults.Last().InnerDamage}" });
        else pending.Timeline.Add(new BattleTimelineEventData { Start = 13.05, Duration = 1.2, Stage = "收兵", Action = "retreat", Side = "attacker", Text = "入口尚未突破，各队有序撤离城下" });
        pending.Timeline.Add(new BattleTimelineEventData { Start = 14.65, Duration = 1.2, Stage = "战果", Action = "result", Text = pending.Summary });
        pending.Duration = 16.1;
    }

    private static void AddAttacks(PendingBattleData pending, List<BattleUnitGroupData> sources, List<BattleUnitGroupData> targets, string troop, double start, string action, string stage)
    {
        var matching = sources.Where(item => item.TroopType == troop && item.InitialSoldiers > 0).ToList();
        if (targets.Count == 0) return;
        for (var index = 0; index < matching.Count; index++)
        {
            var source = matching[index];
            var target = SelectTarget(source, targets);
            pending.Timeline.Add(new BattleTimelineEventData { Start = start + index % 4 * .09, Duration = action == "volley" ? 1.0 : 1.35, Stage = stage, Action = action, Side = source.Side, GroupId = source.Id, TargetGroupId = target.Id, OfficerId = source.AssignedOfficerId, StartX = source.DestinationX, StartY = source.DestinationY, EndX = target.DestinationX, EndY = target.DestinationY, Text = $"{BattleCatalog.TroopName(troop)}{(action == "volley" ? "进入射程并齐射" : "发起" + stage)}" });
        }
    }

    private static BattleUnitGroupData SelectTarget(BattleUnitGroupData source, List<BattleUnitGroupData> targets)
    {
        if (source.TroopType == "cavalry") return targets.Where(item => item.TroopType is "archers" or "siege").OrderBy(item => Math.Abs(item.Lane - source.Lane)).FirstOrDefault() ?? targets.OrderBy(item => Math.Abs(item.Lane - source.Lane)).First();
        if (source.TroopType == "spears") return targets.Where(item => item.TroopType == "cavalry").OrderBy(item => Math.Abs(item.Lane - source.Lane)).FirstOrDefault() ?? targets.OrderBy(item => Math.Abs(item.Lane - source.Lane)).First();
        return targets.OrderBy(item => item.Depth).ThenBy(item => Math.Abs(item.Lane - source.Lane)).First();
    }

    private static void AddPhaseDamageEvents(PendingBattleData pending, BattlePhaseResultData phase, double start)
    {
        var losses = phase.AttackerGroupLosses.Select(item => (Side: "attacker", GroupId: item.Key, Loss: item.Value))
            .Concat(phase.DefenderGroupLosses.Select(item => (Side: "defender", GroupId: item.Key, Loss: item.Value)))
            .OrderBy(item => item.Side)
            .ThenBy(item => item.GroupId)
            .ToList();
        for (var index = 0; index < losses.Count; index++)
        {
            var item = losses[index];
            pending.Timeline.Add(new BattleTimelineEventData { Start = start + index % 8 * .025, Duration = .7, Stage = phase.Stage, Action = "damage", Side = item.Side, GroupId = item.GroupId, Losses = item.Loss, Text = $"−{item.Loss}" });
        }
    }

    public static int ExpectedGroupCount(int soldiers) => soldiers <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(soldiers / 600d), 1, 40);

    public static double MatchupModifier(string attacker, string defender) => Matchup(attacker, defender);

    public static int EngagementCapacity(string troop) => troop == "archers" ? 5 : troop == "siege" ? 2 : 3;

    public static bool IsWithinEffectiveRange(BattleUnitGroupData source, BattleUnitGroupData target)
    {
        var distance = BattleDistance(source, target);
        return distance >= source.MinimumRange && distance <= source.MaximumRange;
    }

    public static double DefensiveDamageMultiplier(string stance, string tactic, string formation, string order, string stage) =>
        StanceLoss(stance) * TacticLoss(tactic, stage) / Math.Max(.01, FormationDefenseModifier(formation, stage) * OrderDefenseModifier(order, stage));
}
