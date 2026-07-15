using System.Text.Json;

namespace ThreeKingdomsSimulator.Godot;

public sealed partial class GameRuntime
{
    public const int TradeIncomePerMonth = 300;
    private static readonly HashSet<string> SupportedDiplomacyTypes = ["trade", "truce", "captive-exchange"];
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Random _random = new(194);

    public GameSession State { get; private set; }
    public event Action? Changed;
    public event Action<string>? Notice;

    public GameRuntime(ScenarioData scenario, NewGameOptions? options = null)
    {
        State = GameSession.Create(scenario, options);
        State.EnsureDomesticDefaults();
        InitializeStrategicAiState();
    }

    public void Replace(GameSession state)
    {
        State = state;
        foreach (var city in State.Cities)
        {
            if (city.WallDurability <= 0 && city.GateDurability <= 0 && city.InnerControl <= 0) { city.WallDurability = 100; city.GateDurability = 100; city.InnerControl = 100; }
        }
        State.EnsureDomesticDefaults();
        InitializeStrategicAiState();
        Changed?.Invoke();
        Notify("存档已载入。天下局势已恢复。", false);
    }

    public CityData? City(string id) => State.Cities.FirstOrDefault(city => city.Id == id);
    public FactionData? Faction(string? id) => State.Factions.FirstOrDefault(faction => faction.Id == id);
    public ScenarioOfficerData? Officer(string id) => State.Officers.FirstOrDefault(officer => officer.Profile.Id == id);
    public IEnumerable<ScenarioOfficerData> PlayerOfficers() => State.Officers.Where(officer => officer.InitialState.FactionId == State.PlayerFactionId && officer.InitialState.Alive);

    public bool DevelopCity(string cityId, string officerId, string focus)
    {
        return ExecutePlayerCityCommand(cityId, officerId, focus);
    }

    public bool BuildFacility(string cityId, string officerId, string facilityId)
    {
        return ExecutePlayerFacilityBuild(cityId, officerId, facilityId);
    }

    public bool MaintainFacility(string cityId, string instanceId, bool upgrade)
    {
        return ExecutePlayerFacilityMaintenance(cityId, instanceId, upgrade);
    }

    public bool TransferTreasury(string cityId, bool toCity, int gold, int food)
    {
        return ExecutePlayerTreasuryTransfer(cityId, toCity, gold, food);
    }

    public bool AppointOfficer(string officerId, string appointment)
    {
        var officer = Officer(officerId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || officer.InitialState.Appointment == "ruler") return Fail("该武将不能改任。");
        officer.InitialState.Appointment = appointment;
        return Success($"{officer.Profile.Name}已改任{AppointmentLabel(appointment)}。", "city");
    }

    public bool TransferOfficer(string officerId, string targetCityId)
    {
        var officer = Officer(officerId);
        var target = City(targetCityId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || target?.OwnerFactionId != State.PlayerFactionId) return Fail("只能在己方城池间调动己方武将。");
        if (officer.InitialState.Appointment == "ruler") return Fail("君主不可调动。");
        if (officer.InitialState.CityId == target.Id) return Fail("目标城不能与当前所在城相同。");
        officer.InitialState.CityId = target.Id;
        return Success($"{officer.Profile.Name}已调往{target.Name}。", "city");
    }

    public bool Scout(string sourceCityId, string targetCityId, string officerId)
    {
        var source = City(sourceCityId); var target = City(targetCityId); var officer = Officer(officerId);
        if (!CanAct(source, officer, out var error) || target is null) return Fail(target is null ? "目标城池不存在。" : error);
        target.IntelligenceAge = 0; source!.ActionSlots--; source.MonthlyOfficerActionIds.Add(officer!.Profile.Id); officer.InitialState.Fatigue = Clamp100(officer.InitialState.Fatigue + 8);
        AwardOfficerExperience(officer, 40, "完成侦察", "intelligence");
        return Success($"{officer.Profile.Name}完成对{target.Name}的侦察。", "city");
    }

    public bool RecruitOfficer(string candidateId, string actorId, string appointment, string method)
    {
        var candidate = Officer(candidateId); var actor = Officer(actorId);
        if (candidate is null || actor?.InitialState.FactionId != State.PlayerFactionId) return Fail("候选人或执行者无效。");
        var valid = candidate.InitialState.Status == "captive" || candidate.InitialState.Status == "free" && State.DiscoveredOfficerIds.Contains(candidateId) || candidate.InitialState.FactionId != State.PlayerFactionId && candidate.InitialState.Loyalty <= 79;
        if (!valid) return Fail("该人才尚不能招募。");
        var chance = RecruitmentChance(candidateId, actorId, method);
        if (method == "subversion")
        {
            if (State.Resources.Gold < 1000) return Fail("策反需要1000金。");
            State.Resources.Gold -= 1000;
        }
        var roll = _random.Next(1, 101);
        var success = roll <= chance;
        if (success)
        {
            candidate.InitialState.FactionId = State.PlayerFactionId;
            candidate.InitialState.CityId = actor.InitialState.CityId;
            candidate.InitialState.Status = "serving";
            candidate.InitialState.Appointment = appointment;
            candidate.InitialState.Loyalty = Math.Clamp(55 + chance / 3, 50, 90);
        }
        AwardOfficerExperience(actor, success ? 50 : 30, success ? "成功招募人才" : "完成招募交涉", "recruitment");
        return Success($"{actor.Profile.Name}{(success ? "成功招募" : "未能说服")}{candidate.Profile.Name}（成功率{chance}%，判定{roll}）。", "city");
    }

    public int RecruitmentChance(string candidateId, string actorId, string method)
    {
        var candidate = Officer(candidateId); var actor = Officer(actorId);
        if (candidate is null || actor is null) return 0;
        var ruler = State.Officers.FirstOrDefault(item => item.InitialState.FactionId == State.PlayerFactionId && item.InitialState.Appointment == "ruler") ?? actor;
        var actorCharisma = EffectiveAbility(actor, "charisma", "civil");
        var rulerCharisma = EffectiveAbility(ruler, "charisma", "civil");
        var intelligence = EffectiveAbility(actor, "intelligence", "civil");
        var politics = EffectiveAbility(actor, "politics", "civil");
        var idealBonus = RecruitmentIdealBonus(actor, candidate);
        var chance = 10
            + WeightedPercent(actorCharisma, 45)
            + WeightedPercent(rulerCharisma, 15)
            + WeightedPercent(intelligence, 10)
            + WeightedPercent(politics, 5)
            - WeightedPercent(candidate.InitialState.Loyalty, 45)
            + RecruitmentMethodBonus(method)
            + idealBonus;
        return Math.Clamp(chance, 5, 95);
    }

    private static int RecruitmentMethodBonus(string method) => method switch
    {
        "recommendation" => 12,
        "captive" => 8,
        "subversion" => 10,
        _ => 0,
    };

    private static int RecruitmentIdealBonus(ScenarioOfficerData actor, ScenarioOfficerData candidate) =>
        Math.Min(6, actor.Profile.Ideals.Intersect(candidate.Profile.Ideals).Distinct().Count() * 3);

    private static int WeightedPercent(int value, int percent) =>
        (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero);

    public bool CreateExpedition(string sourceCityId, string targetCityId, string commanderId, int soldiers, int food, string stance, string tactic, List<string>? deputyIds = null, Dictionary<string, int>? composition = null, string backupTactic = "fortify-camp", Dictionary<string, int>? specialTroops = null, string targetArmyId = "")
    {
        var source = City(sourceCityId); var target = City(targetCityId); var commander = Officer(commanderId);
        var targetArmy = string.IsNullOrEmpty(targetArmyId) ? null : State.Armies.FirstOrDefault(item => item.Id == targetArmyId && item.Status is "marching" or "besieging");
        var targetFactionId = targetArmy?.FactionId ?? target?.OwnerFactionId;
        if (source?.OwnerFactionId != State.PlayerFactionId || target is null || targetFactionId is null || targetFactionId == State.PlayerFactionId) return Fail("出征起点或目标无效。");
        if (!string.IsNullOrEmpty(targetArmyId) && targetArmy is null) return Fail("目标军团已经离开战场，无法拦截。");
        if (AreUnderTruce(State.PlayerFactionId, targetFactionId)) return Fail($"与{Faction(targetFactionId)?.Name}的停战协定尚有{TreatyMonths(targetFactionId, "truce")}个月，不能出征。");
        var route = FindRoute(sourceCityId, targetCityId);
        if (route.Count == 0 && sourceCityId != targetCityId) return Fail("未找到行军路线。");
        if (commander?.InitialState.CityId != sourceCityId || commander.InitialState.FactionId != State.PlayerFactionId || commander.InitialState.Status != "serving") return Fail("主将不在出发城或当前不可用。");
        deputyIds ??= [];
        var commandCapacity = Math.Clamp(3000 + EffectiveAbility(commander, "leadership", "military") * 120 + deputyIds.Select(Officer).Where(item => item is not null).Sum(item => EffectiveAbility(item!, "leadership", "military") * 30), 5000, 25000);
        if (soldiers < 1000 || soldiers > source.Garrison) return Fail($"兵力不符合要求：实际阵型须至少1,000人，且不能超过{source.Name}现有驻军{source.Garrison:N0}人。");
        var treasury = Treasury(State.PlayerFactionId);
        if (food < 1 || treasury.Food < food) return Fail($"势力府库军粮不足：需要{food:N0}，现有{treasury.Food:N0}。");
        if (deputyIds.Count > 2 || deputyIds.Any(id => { var deputy = Officer(id); return deputy?.InitialState.FactionId != State.PlayerFactionId || deputy.InitialState.CityId != sourceCityId || deputy.InitialState.Status != "serving" || id == commanderId; })) return Fail("副将必须是出发城内至多两名可用武将。");
        composition ??= new Dictionary<string, int> { ["infantry"] = soldiers };
        if (composition.Values.Any(value => value < 0) || composition.Values.Sum() != soldiers) return Fail("兵种编制合计必须等于出征兵力。");
        var activeTroops = composition.Where(item => item.Value > 0).ToList();
        if (activeTroops.Count > 3 || activeTroops.Any(item => item.Value < 500)) return Fail("军团最多混编三种兵种，每种至少500人。");
        if (activeTroops.Count == 1 && activeTroops[0].Key == "siege") return Fail("攻城器械不能成为唯一兵种。");
        specialTroops ??= [];
        var specialEquipment = 0;
        foreach (var entry in specialTroops.Where(item => item.Value > 0))
        {
            if (!OfficerProgressionRules.SpecialTroops.TryGetValue(entry.Key, out var definition)) return Fail("特殊部队类型无效。");
            if (!definition.FactionIds.Contains(State.PlayerFactionId)) return Fail($"当前势力不能编成{definition.Name}。");
            if (entry.Value < 500 || entry.Value % 500 != 0 || entry.Value > composition.GetValueOrDefault(definition.BaseTroopType)) return Fail($"{definition.Name}必须以500人为单位，且不能超过对应基础兵种人数。");
            specialEquipment += entry.Value / 500 * definition.EquipmentPerFiveHundred;
        }
        if (specialTroops.Count(item => item.Value > 0) > 1) return Fail("首版每支军团最多编成一种特殊部队。");
        if (State.Resources.Equipment < specialEquipment) return Fail($"编成特殊部队需要军备{specialEquipment:N0}，势力军备不足。");
        source.Garrison -= soldiers; treasury.Food -= food;
        State.Resources.Equipment -= specialEquipment;
        commander.InitialState.Status = "deployed";
        foreach (var deputyId in deputyIds) Officer(deputyId)!.InitialState.Status = "deployed";
        var travelDays = Math.Max(1, route.Sum(id => State.Roads.First(road => road.Id == id).TravelDays));
        var armyId = $"army-{State.Turn}-{State.Armies.Count + 1}";
        commander.InitialState.ArmyId = armyId;
        foreach (var deputyId in deputyIds) Officer(deputyId)!.InitialState.ArmyId = armyId;
        var roles = new Dictionary<string, string> { [commanderId] = "commander" };
        if (deputyIds.Count > 0) roles[deputyIds[0]] = "vanguard";
        if (deputyIds.Count > 1) roles[deputyIds[1]] = "strategist";
        var commandPenalty = Math.Max(0, soldiers - commandCapacity) / Math.Max(1, commandCapacity / 10);
        var army = new ArmyData { Id = armyId, FactionId = State.PlayerFactionId, SourceCityId = sourceCityId, TargetCityId = targetCityId, TargetArmyId = targetArmyId, CommanderId = commanderId, DeputyIds = deputyIds, OfficerRoles = roles, Composition = composition.Where(item => item.Value > 0).ToDictionary(item => item.Key, item => item.Value), SpecialTroops = specialTroops.Where(item => item.Value > 0).ToDictionary(item => item.Key, item => item.Value), Soldiers = soldiers, Food = food, Training = source.Training, Morale = Math.Max(50, 70 - commandPenalty), Stance = stance, Tactic = tactic, BackupTactic = backupTactic, RouteRoadIds = route, RemainingDays = travelDays, TotalDays = travelDays };
        State.Armies.Add(army);
        AdvanceArmy(army);
        var objective = targetArmy is null ? target.Name : $"{Officer(targetArmy.CommanderId)?.Profile.Name ?? "敌军"}军";
        return Success($"{commander.Profile.Name}率{soldiers:N0}兵从{source.Name}出击{objective}，本回合已立即行军{Math.Min(30, travelDays)}日{(army.Status == "marching" ? $"，尚余{army.RemainingDays}日" : "并已接战")}。", "battle");
    }

    public bool MarchArmy(string armyId)
    {
        var army = State.Armies.FirstOrDefault(item => item.Id == armyId);
        if (army is null || army.FactionId != State.PlayerFactionId) return Fail("只能指挥己方军团行军。");
        if (army.Status is not "marching" and not "besieging") return Fail("该军团当前不能行军或继续围城。");
        if (army.LastMarchTurn >= State.Turn) return Fail("该军团本回合已经行军。");
        var before = army.RemainingDays;
        AdvanceArmy(army);
        var commanderName = Officer(army.CommanderId)?.Profile.Name ?? "军团";
        return Success($"{commanderName}军本回合行军{Math.Min(30, before)}日{(army.Status == "marching" ? $"，距离目标尚余{army.RemainingDays}日" : "并已抵达目标")}。", "battle");
    }

    public bool WithdrawArmy(string armyId, string destinationCityId)
    {
        var army = State.Armies.FirstOrDefault(item => item.Id == armyId);
        if (army is null || army.FactionId != State.PlayerFactionId) return Fail("只能撤回己方军团。");
        if (army.Status is not "marching" and not "besieging") return Fail("该军团当前不能撤兵。");
        if (army.LastMarchTurn >= State.Turn) return Fail("该军团本回合已经行动，须到下一回合才能撤兵。");
        var destination = City(destinationCityId);
        if (destination?.OwnerFactionId != army.FactionId) return Fail("撤兵目标必须是己方城市。");
        destination.Garrison += army.Soldiers;
        Treasury(army.FactionId).Food += army.Food;
        ReleaseArmyOfficers(army, destination.Id);
        army.LastMarchTurn = State.Turn;
        army.Status = "withdrawn";
        var commanderName = Officer(army.CommanderId)?.Profile.Name ?? "军团";
        return Success($"{commanderName}军停止进攻，撤回{destination.Name}；兵力归城、剩余军粮归入势力府库。", "battle");
    }

    public int DiplomacyChance(string factionId, string type, int gift)
    {
        var relation = State.Diplomacy.FirstOrDefault(item => item.FactionId == factionId);
        if (relation is null || !SupportedDiplomacyTypes.Contains(type)) return 0;
        var typeModifier = type switch { "trade" => 8, "captive-exchange" => 4, _ => 0 };
        return Math.Clamp(45 + relation.Relation / 2 + relation.Trust / 3 + Math.Max(0, gift) / 250 + typeModifier, 5, 95);
    }

    public bool HasTreaty(string factionId, string type) => TreatyMonths(factionId, type) > 0;

    public bool CanExchangeCaptives(string factionId) => CaptiveHeldBy(State.PlayerFactionId, factionId) is not null && CaptiveHeldBy(factionId, State.PlayerFactionId) is not null;

    public bool ProposeDiplomacy(string factionId, string type, int gift)
    {
        var faction = Faction(factionId);
        if (faction is null || faction.Id == State.PlayerFactionId) return Fail("外交目标无效。");
        if (!SupportedDiplomacyTypes.Contains(type)) return Fail("该外交行动当前不可用。");
        if (gift < 0 || State.Resources.Gold < gift) return Fail("赠礼金额无效或金钱不足。");
        var relation = State.Diplomacy.First(item => item.FactionId == factionId);
        if (relation.LastProposalTurn == State.Turn) return Fail("本月已向该势力交涉。");
        if (type is ("trade" or "truce") && HasTreaty(factionId, type)) return Fail($"与{faction.Name}的{DiplomacyLabel(type)}尚有{TreatyMonths(factionId, type)}个月，无需重复提案。");
        if (type == "captive-exchange" && !CanExchangeCaptives(factionId)) return Fail("双方必须各自扣押至少一名对方武将，才能交换俘虏。");
        if (type == "truce" && HasPendingBattleBetween(State.PlayerFactionId, factionId)) return Fail("双方战斗已进入结算，停战须在本场战斗结束后交涉。");
        var chance = DiplomacyChance(factionId, type, gift);
        var accepted = _random.Next(100) < chance;
        State.Resources.Gold -= gift;
        relation.LastProposalTurn = State.Turn;
        relation.Relation = Math.Clamp(relation.Relation + (accepted ? 6 : -2), -100, 100);
        relation.Trust = Math.Clamp(relation.Trust + (accepted ? 3 : -1), 0, 100);
        SynchronizeLegacyRelationToPair(factionId);
        var result = string.Empty;
        if (accepted && type is ("trade" or "truce")) ApplyTreaty(factionId, type, type == "truce" ? 6 : 12);
        if (accepted && type == "captive-exchange") result = $"，{ExchangeCaptives(factionId)}";
        return Success($"{faction.Name}{(accepted ? "接受" : "拒绝")}了「{DiplomacyLabel(type)}」提案（成功率{chance}%）{result}。", "diplomacy");
    }

    public bool RespondToAiProposal(bool accepted)
    {
        var proposal = State.AiDiplomaticProposals.FirstOrDefault(item => item.Status == "pending");
        if (proposal is null) return Fail("当前没有待回应的外交提案。");
        if (proposal.Type is not ("trade" or "truce")) { proposal.Status = "expired"; return Fail("该提案类型已不再可用。"); }
        if (accepted && proposal.Type == "truce" && HasPendingBattleBetween(State.PlayerFactionId, proposal.FromFactionId)) return Fail("双方战斗已进入结算，停战须在本场战斗结束后回应。");
        proposal.Status = accepted ? "accepted" : "rejected";
        var relation = State.Diplomacy.First(item => item.FactionId == proposal.FromFactionId);
        relation.Relation = Math.Clamp(relation.Relation + (accepted ? 6 : -1), -100, 100);
        relation.Trust = Math.Clamp(relation.Trust + (accepted ? 4 : 0), 0, 100);
        SynchronizeLegacyRelationToPair(proposal.FromFactionId);
        if (accepted) ApplyTreaty(proposal.FromFactionId, proposal.Type, proposal.DurationMonths);
        return Success($"已{(accepted ? "接受" : "拒绝")}{Faction(proposal.FromFactionId)?.Name}提出的{DiplomacyLabel(proposal.Type)}。", "diplomacy");
    }

    public void ConfigureAutomation(bool enabled, bool domestic, bool talent, bool diplomacy, bool military, string risk, int goldReserve, int foodReserve, int garrison)
    {
        State.Automation = new AutomationSettings { Enabled = enabled, Domestic = domestic, Talent = talent, Diplomacy = diplomacy, Military = military, RiskTolerance = risk, MinGoldReserve = goldReserve, MinFoodReserve = foodReserve, MinCityGarrison = garrison };
        Success($"AI势力代理已{(enabled ? "启用" : "停用")}。", "ai");
    }

    public void ConfigureAutoEvolution(bool enabled, string speed, int maxTurns)
    {
        State.AutoEvolution.Enabled = enabled;
        State.AutoEvolution.Status = enabled ? "running" : "paused";
        State.AutoEvolution.Speed = speed;
        State.AutoEvolution.MaxTurns = maxTurns;
        if (enabled && State.AutoEvolution.StartedTurn < 0) State.AutoEvolution.StartedTurn = State.Turn;
        Notify(enabled ? $"自动演进已启动，上限{maxTurns}回合。" : "自动演进已暂停。", true);
    }

    public bool ChooseEvent(string choiceId)
    {
        var pending = State.PendingEvent;
        if (pending is null) return Fail("当前没有待处理事件。");
        var definition = State.Events.FirstOrDefault(item => item.Id == pending.DefinitionId);
        var choice = definition?.Choices.FirstOrDefault(item => item.Id == choiceId);
        if (choice is null) return Fail("事件选项不存在。");
        var city = City(pending.CityId) ?? State.Cities.First(city => city.OwnerFactionId == State.PlayerFactionId);
        foreach (var effect in choice.Effects)
        {
            if (effect.Type == "resource" && effect.Resource is not null) ApplyResource(city, effect.Resource, effect.Amount);
            if (effect.Type == "city" && effect.Metric is not null) ApplyCityMetric(city, effect.Metric, effect.Amount);
        }
        State.EventHistory.Add($"第{State.Turn}回合 · {definition!.Title} · {choice.Label}");
        State.EventCooldowns[definition.Id] = State.Turn + definition.CooldownTurns;
        State.PendingEvent = null;
        return Success($"事件「{definition.Title}」：{choice.Label}。", "event");
    }

    public bool EndTurn()
    {
        if (State.Outcome != "ongoing") return Fail("本局已经结束，请开始新游戏或载入其他存档。");
        if (State.PendingEvent is not null) return Fail("请先处理当前事件。");
        if (State.PendingBattle is not null) return Fail("请先完成当前战斗。");
        if (State.TurnResolutionPending) return Fail("本月军事结算尚未完成。");
        ResolveConstruction();
        ResolveArmies();
        if (State.PendingBattle is not null)
        {
            State.TurnResolutionPending = true;
            return Success($"{City(State.PendingBattle.CityId)?.Name}战事爆发，已进入战前军议。", "battle");
        }
        FinishTurnResolution();
        return Success($"进入{State.Year}年{State.Month}月。月度经济、AI行军与外交均已结算；己方军团可在回合内行军。", "turn");
    }

    public ResourceData PreviewEndTurnResourceDelta()
    {
        var snapshot = JsonSerializer.Deserialize<GameSession>(JsonSerializer.Serialize(State, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException("无法生成月末资源预估。");
        snapshot.AutoSaveFrequency = "off";
        var previewScenario = new ScenarioData
        {
            PlayerFactionId = snapshot.PlayerFactionId,
            Factions = [new FactionData { Id = snapshot.PlayerFactionId }],
        };
        var preview = new GameRuntime(previewScenario);
        preview.Replace(snapshot);
        var before = new ResourceData
        {
            Gold = preview.State.Resources.Gold,
            Food = preview.State.Resources.Food,
            Prestige = preview.State.Resources.Prestige,
            Equipment = preview.State.Resources.Equipment,
        };

        preview.ResolveConstruction();
        preview.ResolveArmies();
        preview.FinishTurnResolution();
        return new ResourceData
        {
            Gold = preview.State.Resources.Gold - before.Gold,
            Food = preview.State.Resources.Food - before.Food,
            Prestige = preview.State.Resources.Prestige - before.Prestige,
            Equipment = preview.State.Resources.Equipment - before.Equipment,
        };
    }

    private void FinishTurnResolution()
    {
        ResolveEconomy();
        ResolveOfficerSalaries();
        ResolveAi();
        ResolveAiPromotions();
        ResolveCityContributions();
        ResolveTreaties();
        State.Turn++;
        State.Month++;
        if (State.Month > 12) { State.Month = 1; State.Year++; }
        foreach (var city in State.Cities)
        {
            city.IntelligenceAge++;
            city.Fatigue = Math.Max(0, city.Fatigue - 2);
            ResetCityCivicCapacity(city);
        }
        foreach (var officer in State.Officers)
        {
            officer.InitialState.Fatigue = Math.Max(0, officer.InitialState.Fatigue - 8);
            if (officer.InitialState.TrackTransitionMonths > 0) officer.InitialState.TrackTransitionMonths--;
        }
        QueueEvent();
        EvaluateOutcome(true);
        State.TurnResolutionPending = false;
        if (SaveService.ShouldWriteAuto(State)) SaveService.WriteAuto(State);
    }

    private void ResolveConstruction()
    {
        foreach (var city in State.Cities.Where(city => city.ConstructionQueue is not null))
        {
            var queue = city.ConstructionQueue!;
            queue.RemainingMonths--;
            if (queue.RemainingMonths > 0) continue;
            if (queue.Kind == "build") city.Facilities.Add(new FacilityInstanceData { Id = $"{city.Id}-{queue.DefinitionId}-{State.Turn}", DefinitionId = queue.DefinitionId, Level = 1, Condition = 100 });
            else
            {
                var target = city.Facilities.FirstOrDefault(item => item.Id == queue.TargetInstanceId);
                if (target is not null) { if (queue.Kind == "upgrade") target.Level = Math.Min(3, target.Level + 1); target.Condition = 100; }
            }
            var builder = Officer(queue.OfficerId);
            if (builder is not null) AwardOfficerExperience(builder, 30, queue.Kind == "build" ? "完成设施建设" : "完成设施维护", "construction");
            city.ConstructionQueue = null;
        }
    }

    private void ResolveArmies()
    {
        if (State.PendingBattle is not null) return;
        foreach (var army in State.Armies.Where(item => (item.Status is "marching" or "besieging") && item.FactionId != State.PlayerFactionId && item.LastMarchTurn < State.Turn).ToList())
        {
            AdvanceArmy(army);
            if (State.PendingBattle is not null) return;
        }
    }

    private void AdvanceArmy(ArmyData army)
    {
        army.Food = Math.Max(0, army.Food - Math.Max(100, army.Soldiers / 25));
        army.Fatigue = Math.Clamp(army.Fatigue + (army.Status == "besieging" ? 8 : 5), 0, 100);
        army.Morale = Math.Clamp(army.Morale + (army.Food == 0 ? -12 : -1), 0, 100);
        if (army.Food == 0) army.Tactic = army.BackupTactic;
        army.RemainingDays = Math.Max(0, army.RemainingDays - (army.Food == 0 ? 18 : 30));
        army.LastMarchTurn = State.Turn;
        if (army.RemainingDays == 0) PrepareBattle(army);
    }

    private void PrepareBattle(ArmyData army)
    {
        if (!string.IsNullOrEmpty(army.TargetArmyId))
        {
            PrepareArmyBattle(army);
            return;
        }
        var city = City(army.TargetCityId);
        if (city is null || city.OwnerFactionId == army.FactionId) return;
        if (AreUnderTruce(army.FactionId, city.OwnerFactionId))
        {
            RecallArmy(army, $"因与{Faction(city.OwnerFactionId)?.Name}仍在停战期，{Officer(army.CommanderId)?.Profile.Name}军停止进攻并返回驻地。");
            return;
        }
        if (army.Composition.Count == 0) army.Composition["infantry"] = army.Soldiers;
        army.Status = "awaiting-battle";
        State.PendingBattle = BattleCalculator.Create(State, army, city);
        if (army.FactionId != State.PlayerFactionId && city.OwnerFactionId != State.PlayerFactionId)
        {
            BattleCalculator.Generate(State, State.PendingBattle);
            BattleCalculator.RunToCompletion(State, State.PendingBattle);
            ApplyPendingBattle(false);
        }
    }

    private void PrepareArmyBattle(ArmyData army)
    {
        var defender = State.Armies.FirstOrDefault(item => item.Id == army.TargetArmyId && item.Status is "marching" or "besieging");
        if (defender is null || defender.FactionId == army.FactionId)
        {
            RecallArmy(army, $"{Officer(army.CommanderId)?.Profile.Name}军未能追上目标军团，已返回驻地。");
            return;
        }
        if (AreUnderTruce(army.FactionId, defender.FactionId))
        {
            RecallArmy(army, $"因双方处于停战期，{Officer(army.CommanderId)?.Profile.Name}军停止拦截并返回驻地。");
            return;
        }
        if (army.Composition.Count == 0) army.Composition["infantry"] = army.Soldiers;
        if (defender.Composition.Count == 0) defender.Composition["infantry"] = defender.Soldiers;
        army.Status = "awaiting-battle";
        defender.Status = "awaiting-battle";
        State.PendingBattle = BattleCalculator.CreateFieldBattle(State, army, defender);
    }

    public bool ConfigurePendingBattle(string formationId, Dictionary<string, string> troopOrders)
    {
        var pending = State.PendingBattle;
        if (pending is null || pending.Status != "planning") return Fail("当前没有可布阵的战斗。");
        BattleCalculator.Configure(pending, formationId, troopOrders);
        return Success($"已采用{BattleCatalog.FormationName(formationId)}，战斗队完成重新展开。", "battle");
    }

    public bool StartPendingBattle()
    {
        var pending = State.PendingBattle;
        if (pending is null || pending.Status != "planning") return Fail("当前战斗不能开始演算。");
        BattleCalculator.Generate(State, pending);
        return Success("军令已下，双方战斗队开始推进。", "battle");
    }

    public bool AdvancePendingBattle(double delta)
    {
        var pending = State.PendingBattle;
        if (pending is null || pending.Status != "running") return false;
        BattleCalculator.Advance(State, pending, delta);
        return true;
    }

    public bool IssueBattleCommand(IEnumerable<string> groupIds, string command, string targetGroupId = "", float destinationX = 0, float destinationY = 0)
    {
        var pending = State.PendingBattle;
        if (pending is null || pending.Status != "running") return Fail("当前没有可以实时指挥的战斗。");
        var affected = BattleCalculator.IssueRealtimeCommand(pending, groupIds, command, targetGroupId, destinationX, destinationY);
        if (affected <= 0) return Fail(command == "attack" ? "请先选中我方军团和有效的敌方目标。" : "请先选中仍在作战的我方军团。");
        var action = command switch { "attack" => "集火目标", "move" => "移动", "hold" => "原地固守", _ => "自由接敌" };
        return Success($"实时军令已下达：{affected}支军团开始{action}。", "battle");
    }

    public bool CompletePendingBattle()
    {
        if (State.PendingBattle is null || State.PendingBattle.Status is not ("running" or "resolved")) return Fail("当前没有可完成的实时战斗。");
        if (State.PendingBattle.Status == "running") BattleCalculator.RunToCompletion(State, State.PendingBattle);
        var summary = ApplyPendingBattle(true);
        return Success(summary, "battle");
    }

    private string ApplyPendingBattle(bool continueTurn)
    {
        var pending = State.PendingBattle!;
        if (pending.BattleType == "field") return ApplyArmyBattle(pending, continueTurn);
        var army = State.Armies.First(item => item.Id == pending.ArmyId);
        var city = City(pending.CityId)!;
        var formerOwner = city.OwnerFactionId;
        var commander = Officer(army.CommanderId)!;
        var attackerGroups = pending.Groups.Where(item => item.Side == "attacker").ToList();
        var compositionBeforeBattle = army.Composition.ToDictionary(item => item.Key, item => item.Value);

        army.Composition = attackerGroups.GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.FinalSoldiers));
        foreach (var specialId in army.SpecialTroops.Keys.ToList())
        {
            if (!OfficerProgressionRules.SpecialTroops.TryGetValue(specialId, out var definition)) { army.SpecialTroops.Remove(specialId); continue; }
            var before = Math.Max(1, compositionBeforeBattle.GetValueOrDefault(definition.BaseTroopType));
            var after = army.Composition.GetValueOrDefault(definition.BaseTroopType);
            army.SpecialTroops[specialId] = Math.Min(after, (int)Math.Round(army.SpecialTroops[specialId] * after / (double)before));
            if (army.SpecialTroops[specialId] < 1) army.SpecialTroops.Remove(specialId);
        }
        army.Soldiers = pending.AttackerAfter;
        city.Garrison = pending.DefenderAfter;
        city.WallDurability = pending.WallAfter;
        city.GateDurability = pending.GateAfter;
        city.InnerControl = pending.InnerAfter;
        army.WoundedByTroop = SplitLossesByTroop(attackerGroups, .55);
        army.RoutedByTroop = SplitLossesByTroop(attackerGroups, pending.Result == "defeat" ? .28 : .12);

        foreach (var officerId in pending.AttackerOfficerIds)
        {
            var officer = Officer(officerId); if (officer is null) continue;
            officer.InitialState.Merit += pending.Result == "victory" ? 18 : pending.Result == "stalemate" ? 8 : 3;
            officer.InitialState.Health = Math.Max(25, officer.InitialState.Health - (pending.Result == "defeat" ? 6 : 2));
            officer.InitialState.Fatigue = Math.Clamp(officer.InitialState.Fatigue + 12, 0, 100);
            AwardOfficerExperience(officer, 35 + (officerId == pending.AttackerCommanderId ? 25 : 0) + (pending.Result == "victory" ? 40 : 0), pending.Result == "victory" ? "赢得正式战斗" : "完成正式战斗", pending.Result == "victory" ? "battle-victory" : "battle-participation");
        }
        foreach (var officerId in pending.DefenderOfficerIds)
        {
            var officer = Officer(officerId); if (officer is null) continue;
            officer.InitialState.Merit += pending.Result == "victory" ? 2 : 7;
            officer.InitialState.Health = Math.Max(25, officer.InitialState.Health - (pending.Result == "victory" ? 7 : 2));
            AwardOfficerExperience(officer, 35 + (pending.Result != "victory" ? 30 : 0), pending.Result != "victory" ? "完成守城" : "参加守城战", pending.Result != "victory" ? "city-defense" : "battle-participation");
        }

        if (pending.Result == "victory")
        {
            city.OwnerFactionId = army.FactionId;
            city.GovernorId = army.CommanderId;
            city.GovernorName = commander.Profile.Name;
            city.Garrison += army.Soldiers;
            city.WallDurability = Math.Max(20, city.WallDurability);
            city.GateDurability = Math.Max(20, city.GateDurability);
            city.InnerControl = 45;
            city.Status = "integrating";
            city.IntegrationMonthsRemaining = 3;
            city.GovernanceMode = army.FactionId == State.PlayerFactionId ? "manual" : "delegated";
            city.GovernancePolicy = "integration";
            city.MonthlyOfficerActionIds.Clear();
            ReleaseArmyOfficers(army, city.Id);
            army.Status = "victorious";
            var retreatCity = State.Cities.FirstOrDefault(item => item.OwnerFactionId == formerOwner && item.Id != city.Id);
            foreach (var defender in State.Officers.Where(item => item.InitialState.FactionId == formerOwner && item.InitialState.CityId == city.Id && item.InitialState.Status != "deployed"))
            {
                if (retreatCity is not null && _random.Next(100) < 72) defender.InitialState.CityId = retreatCity.Id;
                else { defender.InitialState.Status = "captive"; defender.InitialState.CityId = city.Id; }
            }
            var defeatedTreasury = Treasury(formerOwner);
            var lootGold = Math.Min(defeatedTreasury.Gold / 10, 1200); var lootFood = Math.Min(defeatedTreasury.Food / 10, 4000);
            defeatedTreasury.Gold -= lootGold; defeatedTreasury.Food -= lootFood;
            var victorTreasury = Treasury(army.FactionId); victorTreasury.Gold += lootGold; victorTreasury.Food += lootFood; victorTreasury.Prestige += 5;
        }
        else if (pending.Result == "defeat")
        {
            var source = City(army.SourceCityId); if (source is not null) source.Garrison += army.Soldiers;
            ReleaseArmyOfficers(army, army.SourceCityId);
            army.Status = "defeated";
        }
        else
        {
            army.Status = "besieging";
            army.RemainingDays = 30;
            army.Morale = Math.Max(20, army.Morale - 5);
        }

        State.BattleReports.Add(new BattleReportData
        {
            Id = pending.Id, Turn = State.Turn, CityId = city.Id, CityName = city.Name, AttackerFactionId = army.FactionId, DefenderFactionId = formerOwner,
            PlayerSide = pending.PlayerSide,
            AttackerCommanderId = army.CommanderId, AttackerOfficerIds = pending.AttackerOfficerIds, DefenderOfficerIds = pending.DefenderOfficerIds,
            OfficerContributions = pending.OfficerContributions,
            AttackerComposition = pending.Groups.Where(item => item.Side == "attacker").GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.InitialSoldiers)),
            DefenderComposition = pending.Groups.Where(item => item.Side == "defender").GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.InitialSoldiers)),
            AttackerBefore = pending.AttackerBefore, AttackerAfter = pending.AttackerAfter, DefenderBefore = pending.DefenderBefore, DefenderAfter = pending.DefenderAfter,
            AttackerLosses = pending.AttackerLosses, DefenderLosses = pending.DefenderLosses,
            WallBefore = pending.WallBefore, WallAfter = pending.WallAfter, GateBefore = pending.GateBefore, GateAfter = pending.GateAfter, InnerBefore = pending.InnerBefore, InnerAfter = pending.InnerAfter,
            Result = pending.Result, CityCaptured = pending.Result == "victory", Terrain = pending.Terrain, FormationId = (pending.PlayerSide == "attacker" ? pending.AttackerFormation : pending.DefenderFormation).FormationId,
            Tactic = pending.PrimaryTactic, Stance = pending.Stance, PrimaryTactic = pending.PrimaryTactic, BackupTactic = pending.BackupTactic,
            EffectiveTactic = pending.EffectiveTactic, BackupTriggered = pending.BackupTriggered, DecisionSummary = pending.DecisionSummary,
            Narrative = pending.Summary, Timeline = pending.Timeline, PhaseResults = pending.PhaseResults,
        });
        State.CampaignTimeline.Add($"第{State.Turn}回合 · {city.Name}攻防 · {Faction(army.FactionId)?.ShortName}{(pending.Result == "victory" ? "胜" : pending.Result == "stalemate" ? "围" : "败")}{Faction(formerOwner)?.ShortName}");
        var summary = pending.Summary;
        State.PendingBattle = null;
        EvaluateOutcome(false);

        if (continueTurn && State.TurnResolutionPending)
        {
            ResolveArmies();
            if (State.PendingBattle is null) FinishTurnResolution();
        }
        return summary;
    }

    private string ApplyArmyBattle(PendingBattleData pending, bool continueTurn)
    {
        var attacker = State.Armies.First(item => item.Id == pending.ArmyId);
        var defender = State.Armies.First(item => item.Id == pending.DefenderArmyId);
        var battlefield = City(pending.CityId)!;
        var attackerGroups = pending.Groups.Where(item => item.Side == "attacker").ToList();
        var defenderGroups = pending.Groups.Where(item => item.Side == "defender").ToList();
        UpdateArmyAfterFieldBattle(attacker, attackerGroups, pending.AttackerAfter, pending.Result == "defeat");
        UpdateArmyAfterFieldBattle(defender, defenderGroups, pending.DefenderAfter, pending.Result == "victory");

        foreach (var officerId in pending.AttackerOfficerIds.Concat(pending.DefenderOfficerIds))
        {
            var officer = Officer(officerId); if (officer is null) continue;
            var won = pending.AttackerOfficerIds.Contains(officerId) ? pending.Result == "victory" : pending.Result != "victory";
            officer.InitialState.Merit += won ? 14 : 4;
            officer.InitialState.Health = Math.Max(25, officer.InitialState.Health - (won ? 2 : 6));
            officer.InitialState.Fatigue = Math.Clamp(officer.InitialState.Fatigue + 12, 0, 100);
            AwardOfficerExperience(officer, won ? 70 : 40, won ? "赢得军团野战" : "参加军团野战", won ? "battle-victory" : "battle-participation");
        }

        ReturnArmyAfterFieldBattle(attacker, pending.Result == "victory" ? "field-victory" : "field-defeat");
        ReturnArmyAfterFieldBattle(defender, pending.Result == "victory" ? "field-defeat" : "field-victory");
        State.BattleReports.Add(new BattleReportData
        {
            Id = pending.Id, Turn = State.Turn, CityId = battlefield.Id, CityName = $"{battlefield.Name}近郊", BattleType = "field",
            AttackerFactionId = attacker.FactionId, DefenderFactionId = defender.FactionId, PlayerSide = pending.PlayerSide,
            AttackerCommanderId = attacker.CommanderId, AttackerOfficerIds = pending.AttackerOfficerIds, DefenderOfficerIds = pending.DefenderOfficerIds,
            OfficerContributions = pending.OfficerContributions,
            AttackerComposition = attackerGroups.GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.InitialSoldiers)),
            DefenderComposition = defenderGroups.GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.InitialSoldiers)),
            AttackerBefore = pending.AttackerBefore, AttackerAfter = pending.AttackerAfter, DefenderBefore = pending.DefenderBefore, DefenderAfter = pending.DefenderAfter,
            AttackerLosses = pending.AttackerLosses, DefenderLosses = pending.DefenderLosses,
            Result = pending.Result, CityCaptured = false, Terrain = pending.Terrain,
            FormationId = (pending.PlayerSide == "attacker" ? pending.AttackerFormation : pending.DefenderFormation).FormationId,
            Tactic = pending.PrimaryTactic, Stance = pending.Stance, PrimaryTactic = pending.PrimaryTactic, BackupTactic = pending.BackupTactic,
            EffectiveTactic = pending.EffectiveTactic, BackupTriggered = pending.BackupTriggered, DecisionSummary = pending.DecisionSummary,
            Narrative = pending.Summary, Timeline = pending.Timeline, PhaseResults = pending.PhaseResults,
        });
        State.CampaignTimeline.Add($"第{State.Turn}回合 · {battlefield.Name}近郊军团战 · {Faction(attacker.FactionId)?.ShortName}{(pending.Result == "victory" ? "胜" : "败")}{Faction(defender.FactionId)?.ShortName}");
        var summary = pending.Summary;
        State.PendingBattle = null;
        EvaluateOutcome(false);
        if (continueTurn && State.TurnResolutionPending)
        {
            ResolveArmies();
            if (State.PendingBattle is null) FinishTurnResolution();
        }
        return summary;
    }

    private static void UpdateArmyAfterFieldBattle(ArmyData army, List<BattleUnitGroupData> groups, int soldiers, bool routed)
    {
        var before = army.Composition.ToDictionary(item => item.Key, item => item.Value);
        army.Composition = groups.GroupBy(item => item.TroopType).ToDictionary(group => group.Key, group => group.Sum(item => item.FinalSoldiers));
        foreach (var specialId in army.SpecialTroops.Keys.ToList())
        {
            if (!OfficerProgressionRules.SpecialTroops.TryGetValue(specialId, out var definition)) { army.SpecialTroops.Remove(specialId); continue; }
            var previous = Math.Max(1, before.GetValueOrDefault(definition.BaseTroopType));
            var current = army.Composition.GetValueOrDefault(definition.BaseTroopType);
            army.SpecialTroops[specialId] = Math.Min(current, (int)Math.Round(army.SpecialTroops[specialId] * current / (double)previous));
            if (army.SpecialTroops[specialId] < 1) army.SpecialTroops.Remove(specialId);
        }
        army.Soldiers = soldiers;
        army.WoundedByTroop = SplitLossesByTroop(groups, .55);
        army.RoutedByTroop = SplitLossesByTroop(groups, routed ? .28 : .12);
    }

    private void ReturnArmyAfterFieldBattle(ArmyData army, string status)
    {
        var destination = City(army.SourceCityId)?.OwnerFactionId == army.FactionId
            ? City(army.SourceCityId)
            : State.Cities.FirstOrDefault(item => item.OwnerFactionId == army.FactionId);
        if (destination is not null)
        {
            destination.Garrison += army.Soldiers;
            Treasury(army.FactionId).Food += army.Food;
            ReleaseArmyOfficers(army, destination.Id);
        }
        army.Status = status;
        army.RemainingDays = 0;
    }

    private static Dictionary<string, int> SplitLossesByTroop(List<BattleUnitGroupData> groups, double share) => groups
        .GroupBy(item => item.TroopType)
        .ToDictionary(group => group.Key, group => (int)Math.Round(group.Sum(item => item.InitialSoldiers - item.FinalSoldiers) * share));

    private void ReleaseArmyOfficers(ArmyData army, string cityId)
    {
        foreach (var officerId in new[] { army.CommanderId }.Concat(army.DeputyIds))
        {
            var officer = Officer(officerId); if (officer is null) continue;
            officer.InitialState.CityId = cityId;
            officer.InitialState.Status = "serving";
            officer.InitialState.ArmyId = null;
        }
    }

    private void ResolveEconomy()
    {
        ResolveCityEconomy();
    }

    private void ResolveAi()
    {
        ResolveSmartDomesticAi();
        ResolveStrategicAi();
    }

    private void ResolveTreaties()
    {
        ResolveStrategicTreaties();
    }

    private int TreatyMonths(string factionId, string type) => TreatyMonthsBetween(State.PlayerFactionId, factionId, type);

    private bool AreUnderTruce(string? firstFactionId, string? secondFactionId)
    {
        if (firstFactionId is null || secondFactionId is null || firstFactionId == secondFactionId) return false;
        return TreatyMonthsBetween(firstFactionId, secondFactionId, "truce") > 0;
    }

    private bool HasPendingBattleBetween(string firstFactionId, string secondFactionId)
    {
        var pending = State.PendingBattle;
        if (pending is null) return false;
        return pending.AttackerFactionId == firstFactionId && pending.DefenderFactionId == secondFactionId || pending.AttackerFactionId == secondFactionId && pending.DefenderFactionId == firstFactionId;
    }

    private void ApplyTreaty(string factionId, string type, int durationMonths)
    {
        ApplyTreatyBetween(State.PlayerFactionId, factionId, type, durationMonths);
    }

    private void RecallArmy(ArmyData army, string message)
    {
        var destination = City(army.SourceCityId)?.OwnerFactionId == army.FactionId ? City(army.SourceCityId) : State.Cities.FirstOrDefault(item => item.OwnerFactionId == army.FactionId);
        if (destination is not null)
        {
            destination.Garrison += army.Soldiers;
            Treasury(army.FactionId).Food += army.Food;
            ReleaseArmyOfficers(army, destination.Id);
        }
        army.Status = "recalled";
        State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "diplomacy", Message = message });
    }

    private ScenarioOfficerData? CaptiveHeldBy(string holderFactionId, string originalFactionId) => State.Officers.FirstOrDefault(item => item.InitialState.Status == "captive" && item.InitialState.FactionId == originalFactionId && City(item.InitialState.CityId)?.OwnerFactionId == holderFactionId);

    private string ExchangeCaptives(string factionId)
    {
        var targetOfficer = CaptiveHeldBy(State.PlayerFactionId, factionId)!;
        var playerOfficer = CaptiveHeldBy(factionId, State.PlayerFactionId)!;
        targetOfficer.InitialState.Status = "serving";
        targetOfficer.InitialState.CityId = State.Cities.First(item => item.OwnerFactionId == factionId).Id;
        targetOfficer.InitialState.ArmyId = null;
        playerOfficer.InitialState.Status = "serving";
        playerOfficer.InitialState.CityId = State.Cities.First(item => item.OwnerFactionId == State.PlayerFactionId).Id;
        playerOfficer.InitialState.ArmyId = null;
        return $"{playerOfficer.Profile.Name}与{targetOfficer.Profile.Name}均已获释";
    }

    private void QueueEvent()
    {
        if (State.Turn % 4 != 0 || State.Events.Count == 0) return;
        var playerFoodUpkeep = State.Cities.Where(city => city.OwnerFactionId == State.PlayerFactionId).Sum(city => CityMonthlyForecast(city).FoodUpkeep);
        var eligible = State.Events.Where(item => (State.EventCooldowns.GetValueOrDefault(item.Id) <= State.Turn) && (item.Condition == "always" || item.Condition == "low-food" && State.Resources.Food < Math.Max(1, playerFoodUpkeep * 2) || item.Condition == "high-fatigue" && State.Cities.Any(city => city.Fatigue >= 8) || item.Condition == "after-battle" && State.BattleReports.LastOrDefault()?.Turn == State.Turn)).ToList();
        if (eligible.Count == 0) return;
        var definition = eligible[_random.Next(eligible.Count)];
        var city = State.Cities.Where(item => item.OwnerFactionId == State.PlayerFactionId).OrderByDescending(item => item.Fatigue).First();
        State.PendingEvent = new PendingEventData { Id = $"pending-{State.Turn}", DefinitionId = definition.Id, CityId = city.Id };
    }

    private void EvaluateOutcome(bool advanceControlMonth)
    {
        var owned = State.Cities.Count(city => city.OwnerFactionId == State.PlayerFactionId);
        if (owned == State.Cities.Count)
        {
            State.Outcome = "victory";
            State.OutcomeMessage = "天下一统，汉室山河归于一旗。";
        }
        else if (owned == 0)
        {
            State.NineCityControlMonths = 0;
            State.Outcome = "defeat";
            State.OutcomeMessage = "最后一座城池失守，势力覆亡。";
        }
        else
        {
            if (owned < GameSession.StrategicVictoryCityCount) State.NineCityControlMonths = 0;
            else if (advanceControlMonth) State.NineCityControlMonths++;
            if (State.NineCityControlMonths >= GameSession.StrategicVictoryRequiredMonths)
            {
                State.Outcome = "victory";
                State.OutcomeMessage = $"九城归心：已控制至少{GameSession.StrategicVictoryCityCount}城并连续维持{GameSession.StrategicVictoryRequiredMonths}个月。";
            }
        }
        if (State.Outcome == "ongoing" && State.AutoEvolution.Enabled && State.Turn - State.AutoEvolution.StartedTurn >= State.AutoEvolution.MaxTurns)
        {
            var leader = State.Cities.GroupBy(city => city.OwnerFactionId).OrderByDescending(group => group.Count()).First();
            State.AutoEvolution.Enabled = false; State.AutoEvolution.Status = "completed"; State.AutoEvolution.WinnerFactionId = leader.Key;
            State.Outcome = "victory"; State.OutcomeMessage = $"自动演进结束：{Faction(leader.Key)?.Name}以{leader.Count()}城居首。";
        }
    }

    private List<string> FindRoute(string sourceId, string targetId)
    {
        var queue = new Queue<(string City, List<string> Roads)>();
        var seen = new HashSet<string> { sourceId };
        queue.Enqueue((sourceId, []));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.City == targetId) return current.Roads;
            foreach (var road in State.Roads.Where(road => road.FromCityId == current.City || road.ToCityId == current.City))
            {
                var next = road.FromCityId == current.City ? road.ToCityId : road.FromCityId;
                if (seen.Add(next)) queue.Enqueue((next, [.. current.Roads, road.Id]));
            }
        }
        return [];
    }

    private bool CanAct(CityData? city, ScenarioOfficerData? officer, out string error)
    {
        return CanCityAct(city, officer, State.PlayerFactionId, false, out error);
    }

    private void ApplyResource(CityData city, string resource, int amount)
    {
        var treasury = Treasury(city.OwnerFactionId);
        switch (resource) { case "gold": treasury.Gold = Math.Max(0, treasury.Gold + amount); break; case "food": treasury.Food = Math.Max(0, treasury.Food + amount); break; case "prestige": treasury.Prestige = Math.Max(0, treasury.Prestige + amount); break; case "equipment": treasury.Equipment = Math.Max(0, treasury.Equipment + amount); break; }
        if (resource is "gold" or "food") RecordCityLedger(city, "event", resource == "gold" ? amount : 0, resource == "food" ? amount : 0, $"城市事件影响{resource}：{amount:+#,0;-#,0;0}。");
    }

    private static void ApplyCityMetric(CityData city, string metric, int amount)
    {
        switch (metric) { case "publicSupport": city.PublicSupport = Clamp100(city.PublicSupport + amount); break; case "publicOrder": city.PublicOrder = Clamp100(city.PublicOrder + amount); break; case "fatigue": city.Fatigue = Clamp100(city.Fatigue + amount); break; case "defense": city.Defense = Clamp100(city.Defense + amount); break; }
    }

    private bool Success(string message, string category)
    {
        State.Log.Add(new LogEntryData { Turn = State.Turn, Category = category, Message = message });
        Notify(message, true);
        return true;
    }
    private bool Fail(string message) { Notify(message, false); return false; }
    private void Notify(string message, bool changed) { Notice?.Invoke(message); if (changed) Changed?.Invoke(); }
    private static int Clamp100(int value) => Math.Clamp(value, 0, 100);
    private static string FocusLabel(string value) => new Dictionary<string, string> { ["agriculture"] = "劝课农桑", ["commerce"] = "振兴商业", ["patrol"] = "整顿治安", ["defense"] = "修缮城防", ["recruit"] = "征募士卒", ["train"] = "操练兵马", ["search"] = "寻访人才", ["relief"] = "赈济百姓" }.GetValueOrDefault(value, value);
    public static string AppointmentLabel(string value) => new Dictionary<string, string> { ["ruler"] = "君主", ["governor"] = "太守", ["strategist"] = "军师", ["civil"] = "文官", ["general"] = "武将", ["reserve"] = "待命" }.GetValueOrDefault(value, value);
    public static string DiplomacyLabel(string value) => new Dictionary<string, string> { ["trade"] = "通商", ["truce"] = "停战", ["captive-exchange"] = "交换俘虏" }.GetValueOrDefault(value, value);
    public static string DiplomacyEffect(string value) => value switch
    {
        "trade" => $"缔结12个月通商协定，双方每月各获{TradeIncomePerMonth}金",
        "truce" => "缔结6个月停战协定，双方在途进攻军团撤回并禁止新出征",
        "captive-exchange" => "双方立即各释放一名被扣押武将，不生成长期条约",
        _ => "不可用",
    };
    public static string AttitudeLabel(int relation) => relation switch { >= 60 => "友好", >= 20 => "亲善", > -20 => "中立", > -60 => "警惕", _ => "敌对" };
    public static string FacilityName(string id) => FacilityCatalog.GetValueOrDefault(id)?.Name ?? id;
    public static string FacilityEffect(string id) => id switch
    {
        "irrigation" => "提升农桑城务成效，并增加每月粮食产出。",
        "granary" => "提升农桑与赈济成效，并增加每月粮食产出。",
        "market" => "提升商业城务成效，并增加每月金钱产出。",
        "workshop" => "每月产出军备，供全势力统一使用。",
        "barracks" => "强化征募与驻军整备。",
        "drill-ground" => "提升操练兵马的城务成效。",
        "walls" => "提升修缮城防成效，增强守城韧性。",
        "academy" => "提升寻访人才成效与城市文化。",
        "clinic" => "提升赈济成效，缓解城池疲敝。",
        "administration" => "提升城务额度，增强太守治理能力。",
        _ => "提供城池发展加成。",
    };
    public static readonly Dictionary<string, FacilityDefinition> FacilityCatalog = new()
    {
        ["irrigation"] = new("灌溉渠", 800, 300, 2), ["granary"] = new("粮仓", 1000, 400, 2), ["market"] = new("市集", 900, 150, 2), ["workshop"] = new("工坊", 1200, 250, 3), ["barracks"] = new("兵营", 1300, 500, 3), ["drill-ground"] = new("校场", 1100, 450, 2), ["walls"] = new("城墙", 1600, 600, 3), ["academy"] = new("学宫", 1400, 250, 3), ["clinic"] = new("医馆", 1000, 300, 2), ["administration"] = new("郡府", 1500, 200, 3),
    };
}

public sealed record FacilityDefinition(string Name, int Gold, int Food, int Months);

public sealed class GameSession
{
    public const int CurrentSchemaVersion = 6;
    public const int StrategicVictoryCityCount = 9;
    public const int StrategicVictoryRequiredMonths = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ScenarioName { get; set; } = string.Empty;
    public int Turn { get; set; } = 1;
    public int Year { get; set; }
    public int Month { get; set; }
    public string PlayerFactionId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "standard";
    public string AutoSaveFrequency { get; set; } = "monthly";
    public int NineCityControlMonths { get; set; }
    public ResourceData Resources { get; set; } = new();
    public Dictionary<string, ResourceData> FactionTreasuries { get; set; } = [];
    public List<FactionData> Factions { get; set; } = [];
    public List<CityData> Cities { get; set; } = [];
    public List<ScenarioOfficerData> Officers { get; set; } = [];
    public List<RoadData> Roads { get; set; } = [];
    public List<PassData> Passes { get; set; } = [];
    public List<EventDefinitionData> Events { get; set; } = [];
    public List<string> DiscoveredOfficerIds { get; set; } = [];
    public List<ArmyData> Armies { get; set; } = [];
    public List<BattleReportData> BattleReports { get; set; } = [];
    public List<DiplomacyRelationData> Diplomacy { get; set; } = [];
    public List<FactionDiplomacyData> FactionDiplomacy { get; set; } = [];
    public List<AiDiplomaticProposalData> AiDiplomaticProposals { get; set; } = [];
    public AutomationSettings Automation { get; set; } = new();
    public AutoEvolutionData AutoEvolution { get; set; } = new();
    public PendingEventData? PendingEvent { get; set; }
    public PendingBattleData? PendingBattle { get; set; }
    public bool TurnResolutionPending { get; set; }
    public Dictionary<string, int> EventCooldowns { get; set; } = [];
    public List<string> EventHistory { get; set; } = [];
    public List<LogEntryData> Log { get; set; } = [];
    public List<string> CampaignTimeline { get; set; } = [];
    public List<PayrollLedgerEntryData> PayrollLedgerEntries { get; set; } = [];
    public int MonthlySalaryDue { get; set; }
    public int MonthlySalaryPaid { get; set; }
    public string Outcome { get; set; } = "ongoing";
    public string OutcomeMessage { get; set; } = string.Empty;

    public static GameSession Create(ScenarioData scenario, NewGameOptions? options = null)
    {
        var clone = JsonSerializer.Deserialize<ScenarioData>(JsonSerializer.Serialize(scenario))!;
        var playerFactionId = clone.Factions.Any(item => item.Id == options?.PlayerFactionId) ? options!.PlayerFactionId : clone.PlayerFactionId;
        var difficulty = NormalizeDifficulty(options?.Difficulty);
        var autoSaveFrequency = NormalizeAutoSaveFrequency(options?.AutoSaveFrequency);
        var initialTreasuries = clone.Factions.ToDictionary(item => item.Id, item => BaseTreasury(clone, item.Id));
        var session = new GameSession
        {
            ScenarioName = clone.Name, Year = clone.Year, Month = clone.Month, PlayerFactionId = playerFactionId, Difficulty = difficulty, AutoSaveFrequency = autoSaveFrequency,
            Resources = ApplyPlayerDifficulty(initialTreasuries[playerFactionId], difficulty),
            Factions = clone.Factions, Cities = clone.Cities, Officers = clone.Officers, Roads = clone.Roads, Passes = clone.Passes, Events = clone.Events,
            Diplomacy = clone.Factions.Where(item => item.Id != playerFactionId).Select(item => new DiplomacyRelationData { FactionId = item.Id, Relation = item.Id.Contains("lu-bu") ? -45 : 0, Trust = 35 }).ToList(),
        };
        foreach (var faction in clone.Factions.Where(item => item.Id != playerFactionId))
        {
            session.FactionTreasuries[faction.Id] = initialTreasuries[faction.Id];
        }
        session.EnsureDomesticDefaults();
        return session;
    }

    public static ResourceData PreviewInitialResources(ScenarioData scenario, string factionId, string difficulty) =>
        ApplyPlayerDifficulty(BaseTreasury(scenario, scenario.Factions.Any(item => item.Id == factionId) ? factionId : scenario.PlayerFactionId), NormalizeDifficulty(difficulty));

    public static string NormalizeDifficulty(string? value) => value is "relaxed" or "hard" ? value : "standard";

    public static string NormalizeAutoSaveFrequency(string? value) => value is "quarterly" or "yearly" or "off" ? value : "monthly";

    private static ResourceData BaseTreasury(ScenarioData scenario, string factionId)
    {
        if (factionId == scenario.PlayerFactionId)
        {
            return new ResourceData { Gold = scenario.Resources.Gold, Food = scenario.Resources.Food, Equipment = scenario.Resources.Equipment, Prestige = scenario.Resources.Prestige };
        }
        var cities = scenario.Cities.Where(city => city.OwnerFactionId == factionId).ToList();
        return new ResourceData
        {
            Gold = 2500 + cities.Count * 1200,
            Food = 8000 + cities.Sum(city => city.Garrison / 3),
            Equipment = 900 + cities.Count * 450,
            Prestige = 20 + cities.Count * 3,
        };
    }

    private static ResourceData ApplyPlayerDifficulty(ResourceData source, string difficulty)
    {
        var resourceMultiplier = difficulty switch { "relaxed" => 1.2, "hard" => .85, _ => 1.0 };
        return new ResourceData
        {
            Gold = (int)Math.Round(source.Gold * resourceMultiplier),
            Food = (int)Math.Round(source.Food * resourceMultiplier),
            Equipment = (int)Math.Round(source.Equipment * resourceMultiplier),
            Prestige = source.Prestige,
        };
    }

    public void EnsureDomesticDefaults()
    {
        SchemaVersion = Math.Max(SchemaVersion, CurrentSchemaVersion);
        Difficulty = NormalizeDifficulty(Difficulty);
        AutoSaveFrequency = NormalizeAutoSaveFrequency(AutoSaveFrequency);
        NineCityControlMonths = Math.Clamp(NineCityControlMonths, 0, StrategicVictoryRequiredMonths);
        FactionTreasuries ??= [];
        Diplomacy ??= [];
        AiDiplomaticProposals ??= [];
        PayrollLedgerEntries ??= [];
        foreach (var relation in Diplomacy)
        {
            relation.Treaties ??= [];
            foreach (var obsoleteType in relation.Treaties.Keys.Where(key => key is not ("trade" or "truce")).ToList()) relation.Treaties.Remove(obsoleteType);
        }
        foreach (var proposal in AiDiplomaticProposals.Where(item => item.Status == "pending" && item.Type is not ("trade" or "truce"))) proposal.Status = "expired";
        foreach (var faction in Factions.Where(item => item.Id != PlayerFactionId))
        {
            if (!FactionTreasuries.ContainsKey(faction.Id))
            {
                var cityCount = Cities.Count(city => city.OwnerFactionId == faction.Id);
                FactionTreasuries[faction.Id] = new ResourceData { Gold = 2500 + cityCount * 1200, Food = 8000 + cityCount * 3000, Equipment = 900 + cityCount * 450, Prestige = 20 };
            }
        }
        foreach (var city in Cities)
        {
            city.ActionCapacity = city.ActionCapacity <= 0 ? 2 : Math.Clamp(city.ActionCapacity, 1, 4);
            city.ActionSlots = Math.Clamp(city.ActionSlots, 0, city.ActionCapacity);
            city.GovernanceMode = string.IsNullOrWhiteSpace(city.GovernanceMode) ? "manual" : city.GovernanceMode;
            city.GovernancePolicy = string.IsNullOrWhiteSpace(city.GovernancePolicy) ? "balanced" : city.GovernancePolicy;
            city.CityRole = string.IsNullOrWhiteSpace(city.CityRole) ? "unassigned" : city.CityRole;
            city.MonthlyOfficerActionIds ??= [];
            city.LedgerEntries ??= [];
        }
        foreach (var army in Armies) army.SpecialTroops ??= [];
        foreach (var officer in Officers) OfficerProgressionRules.EnsureDefaults(officer, Year);
    }
}

public sealed class PayrollLedgerEntryData
{
    public int Turn { get; set; }
    public string FactionId { get; set; } = string.Empty;
    public string OfficerId { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public int Due { get; set; }
    public int Paid { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class ArmyData
{
    public string Id { get; set; } = string.Empty; public string FactionId { get; set; } = string.Empty; public string SourceCityId { get; set; } = string.Empty; public string TargetCityId { get; set; } = string.Empty; public string TargetArmyId { get; set; } = string.Empty; public string CommanderId { get; set; } = string.Empty; public List<string> DeputyIds { get; set; } = []; public Dictionary<string, int> Composition { get; set; } = []; public Dictionary<string, int> SpecialTroops { get; set; } = []; public Dictionary<string, string> OfficerRoles { get; set; } = []; public Dictionary<string, int> WoundedByTroop { get; set; } = []; public Dictionary<string, int> RoutedByTroop { get; set; } = []; public int Soldiers { get; set; } public int Food { get; set; } public int Training { get; set; } = 65; public int Morale { get; set; } = 70; public int Fatigue { get; set; } public string FormationId { get; set; } = "goose"; public string Stance { get; set; } = "standard"; public string Tactic { get; set; } = "steady-advance"; public string BackupTactic { get; set; } = "fortify-camp"; public List<string> RouteRoadIds { get; set; } = []; public int TotalDays { get; set; } public int RemainingDays { get; set; } public int LastMarchTurn { get; set; } public string Status { get; set; } = "marching";
}
public sealed class BattleReportData
{
    public string Id { get; set; } = string.Empty; public int Turn { get; set; } public string CityId { get; set; } = string.Empty; public string CityName { get; set; } = string.Empty; public string BattleType { get; set; } = "siege"; public string AttackerFactionId { get; set; } = string.Empty; public string DefenderFactionId { get; set; } = string.Empty; public string PlayerSide { get; set; } = "attacker"; public string AttackerCommanderId { get; set; } = string.Empty; public List<string> AttackerOfficerIds { get; set; } = []; public List<string> DefenderOfficerIds { get; set; } = []; public Dictionary<string, string> OfficerContributions { get; set; } = []; public Dictionary<string, int> AttackerComposition { get; set; } = []; public Dictionary<string, int> DefenderComposition { get; set; } = []; public int AttackerBefore { get; set; } public int AttackerAfter { get; set; } public int DefenderBefore { get; set; } public int DefenderAfter { get; set; } public int AttackerLosses { get; set; } public int DefenderLosses { get; set; } public int WallBefore { get; set; } public int WallAfter { get; set; } public int GateBefore { get; set; } public int GateAfter { get; set; } public int InnerBefore { get; set; } public int InnerAfter { get; set; } public string Result { get; set; } = string.Empty; public bool CityCaptured { get; set; } public string Terrain { get; set; } = "plain"; public string FormationId { get; set; } = string.Empty; public string Tactic { get; set; } = string.Empty; public string Stance { get; set; } = "standard"; public string PrimaryTactic { get; set; } = string.Empty; public string BackupTactic { get; set; } = string.Empty; public string EffectiveTactic { get; set; } = string.Empty; public bool BackupTriggered { get; set; } public string DecisionSummary { get; set; } = string.Empty; public string Narrative { get; set; } = string.Empty; public List<BattleTimelineEventData> Timeline { get; set; } = []; public List<BattlePhaseResultData> PhaseResults { get; set; } = [];
}
public sealed class DiplomacyRelationData
{
    public string FactionId { get; set; } = string.Empty; public int Relation { get; set; } public int Trust { get; set; } public int LastProposalTurn { get; set; } = -1; public Dictionary<string, int> Treaties { get; set; } = [];
}
public sealed class AiDiplomaticProposalData { public string Id { get; set; } = string.Empty; public string FromFactionId { get; set; } = string.Empty; public string Type { get; set; } = "trade"; public int DurationMonths { get; set; } = 6; public string Status { get; set; } = "pending"; }
public sealed class AutomationSettings
{
    public bool Enabled { get; set; } public bool Domestic { get; set; } = true; public bool Talent { get; set; } = true; public bool Diplomacy { get; set; } = true; public bool Military { get; set; } = true; public string RiskTolerance { get; set; } = "medium"; public int MinGoldReserve { get; set; } = 3000; public int MinFoodReserve { get; set; } = 10000; public int MinCityGarrison { get; set; } = 3500;
}
public sealed class AutoEvolutionData
{
    public bool Enabled { get; set; } public string Status { get; set; } = "idle"; public string Speed { get; set; } = "normal"; public int MaxTurns { get; set; } = 240; public int StartedTurn { get; set; } = -1; public string? WinnerFactionId { get; set; }
}
public sealed class PendingEventData { public string Id { get; set; } = string.Empty; public string DefinitionId { get; set; } = string.Empty; public string CityId { get; set; } = string.Empty; }
public sealed class LogEntryData { public int Turn { get; set; } public string Category { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; }
