namespace ThreeKingdomsSimulator.Godot;

public sealed partial class GameRuntime
{
    private const double MilitaryActionThreshold = 58;
    private const double DiplomacyActionThreshold = 60;

    public IReadOnlyList<StrategicMilitaryCandidate> EvaluateStrategicMilitaryCandidates(string factionId)
    {
        var cities = State.Cities.Where(city => city.OwnerFactionId == factionId).ToList();
        var activeArmyCount = State.Armies.Count(army => army.FactionId == factionId && army.Status is "marching" or "besieging" or "awaiting-battle");
        var activeArmyLimit = Math.Clamp((cities.Count + 2) / 3, 1, 3);
        var treasury = Treasury(factionId);
        var cityGoldUpkeep = cities.Sum(city => CityMonthlyForecast(city).GoldUpkeep);
        var goldCoverage = treasury.Gold / (double)Math.Max(1, cityGoldUpkeep);
        var results = new List<StrategicMilitaryCandidate>();

        foreach (var source in cities)
        {
            var sourceRoads = State.Roads.Where(road => road.FromCityId == source.Id || road.ToCityId == source.Id);
            foreach (var road in sourceRoads)
            {
                var targetId = road.FromCityId == source.Id ? road.ToCityId : road.FromCityId;
                var target = City(targetId);
                if (target is null || target.OwnerFactionId == factionId) continue;

                var commander = State.Officers
                    .Where(officer => officer.InitialState.FactionId == factionId && officer.InitialState.CityId == source.Id && officer.InitialState.Status == "serving" && officer.InitialState.Alive)
                    .OrderByDescending(officer => EffectiveAbility(officer, "leadership", "military") + OfficerProgressionRules.AllTraits(officer).Count * 2 - officer.InitialState.Fatigue / 4)
                    .FirstOrDefault();
                var enemyNeighbors = State.Roads
                    .Where(candidateRoad => candidateRoad.FromCityId == source.Id || candidateRoad.ToCityId == source.Id)
                    .Select(candidateRoad => City(candidateRoad.FromCityId == source.Id ? candidateRoad.ToCityId : candidateRoad.FromCityId))
                    .Where(city => city is not null && city.OwnerFactionId != factionId)
                    .ToList();
                var incomingPressure = State.Armies
                    .Where(army => army.TargetCityId == source.Id && army.FactionId != factionId && army.Status is "marching" or "besieging" or "awaiting-battle")
                    .Sum(army => army.Soldiers);
                var borderPressure = enemyNeighbors.Select(city => city!.Garrison).DefaultIfEmpty(0).Max();
                var reserve = Math.Max(4000, (int)Math.Ceiling((borderPressure + incomingPressure) * .4)) + Math.Max(0, enemyNeighbors.Count - 1) * 600;
                var soldiers = Math.Min(7000, Math.Max(0, source.Garrison - reserve));
                var factionFoodUpkeep = cities.Sum(item => CityMonthlyForecast(item).FoodUpkeep);
                var foodReserve = Math.Max(4_000, factionFoodUpkeep * 4);
                var food = Math.Max(1200, soldiers * (road.TravelDays + 30) / 120);
                var foodCoverage = (treasury.Food - food) / (double)Math.Max(1, factionFoodUpkeep);
                var forceRatio = soldiers / (double)Math.Max(1, target.Garrison + target.Defense * 22);
                var targetRoads = State.Roads.Count(candidateRoad => candidateRoad.FromCityId == target.Id || candidateRoad.ToCityId == target.Id);
                var targetValue = Math.Min(15, target.Population / 9000d) + (target.Agriculture + target.Commerce) / 11d + Math.Min(8, targetRoads * 2);
                var terrainPenalty = road.Terrain switch { "mountain" => 12, "river" => 9, "hill" => 6, _ => 2 };
                var relation = FactionDiplomacyBetween(factionId, target.OwnerFactionId);
                var relationModifier = relation is null ? 0 : Math.Clamp(-relation.Relation / 8d, -12, 12);
                var retentionMargin = source.Garrison - soldiers - reserve;
                var score = 52
                    + Math.Clamp((forceRatio - .65) * 42, -28, 24)
                    + Math.Clamp(targetValue, 8, 30)
                    + Math.Clamp(retentionMargin / 500d, -10, 8)
                    + Math.Clamp((goldCoverage - 3) * 2, -12, 8)
                    + Math.Clamp((foodCoverage - 4) * 1.5, -12, 8)
                    + relationModifier
                    - road.TravelDays / 5d
                    - terrainPenalty
                    - Math.Clamp(incomingPressure / 700d, 0, 20)
                    - (target.Status == "integrating" ? -6 : 0);

                var reason = string.Empty;
                if (AreUnderTruce(factionId, target.OwnerFactionId)) reason = "双方仍在停战期";
                else if (activeArmyCount >= activeArmyLimit) reason = $"已有{activeArmyCount}支在外军团，达到当前上限{activeArmyLimit}";
                else if (State.Armies.Any(army => army.FactionId == factionId && army.TargetCityId == target.Id && army.Status is "marching" or "besieging" or "awaiting-battle")) reason = "已有军团进攻该目标";
                else if (commander is null) reason = "出发城没有可用主将";
                else if (soldiers < 2500) reason = $"留足{reserve:N0}守军后可用兵力不足";
                else if (treasury.Food - food < foodReserve) reason = $"携粮后势力粮草将低于{foodReserve:N0}安全储备";
                else if (goldCoverage < 1.5) reason = "势力财政不足以支撑新战线";

                results.Add(new StrategicMilitaryCandidate
                {
                    FactionId = factionId,
                    SourceCityId = source.Id,
                    TargetCityId = target.Id,
                    TargetFactionId = target.OwnerFactionId,
                    RoadId = road.Id,
                    CommanderId = commander?.Profile.Id ?? string.Empty,
                    Soldiers = soldiers,
                    Food = food,
                    ReserveGarrison = reserve,
                    TravelDays = road.TravelDays,
                    Terrain = road.Terrain,
                    Score = Math.Round(score, 1),
                    Eligible = string.IsNullOrEmpty(reason),
                    BlockReason = reason,
                    Factors = $"兵力比{forceRatio:0.00}、留守{reserve:N0}、粮期{foodCoverage:0.0}月、财期{goldCoverage:0.0}月、目标值{targetValue:0.0}、{road.TravelDays}日/{road.Terrain}",
                });
            }
        }

        return results.OrderByDescending(candidate => candidate.Eligible).ThenByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.SourceCityId).ThenBy(candidate => candidate.TargetCityId).ToList();
    }

    public IReadOnlyList<StrategicDiplomacyCandidate> EvaluateStrategicDiplomacyCandidates(string factionId)
    {
        var results = new List<StrategicDiplomacyCandidate>();
        foreach (var target in State.Factions.Where(faction => faction.Id != factionId))
        {
            var relation = FactionDiplomacyBetween(factionId, target.Id);
            if (relation is null) continue;
            var border = HasSharedBorder(factionId, target.Id);
            var warPressure = WarPressureBetween(factionId, target.Id);
            var commonThreat = CommonThreatScore(factionId, target.Id);
            var resourceNeed = ResourceNeedScore(factionId) + ResourceNeedScore(target.Id);

            var tradeScore = 28 + relation.Relation * .28 + relation.Trust * .32 + (border ? 8 : 0) + commonThreat * 3 + resourceNeed * 1.8 - warPressure * 4;
            var tradeReason = relation.Treaties.GetValueOrDefault("trade") > 0 ? "已有通商协定" : State.Turn - relation.LastProposalTurn < 3 ? "距离上次交涉不足3个月" : warPressure >= 5 ? "双方战事压力过高" : string.Empty;
            results.Add(NewDiplomaticCandidate("trade", tradeScore, tradeReason));

            var truceScore = 24 - relation.Relation * .18 + relation.Trust * .10 + warPressure * 7 + commonThreat * 2 + resourceNeed * 2.2;
            var truceReason = relation.Treaties.GetValueOrDefault("truce") > 0 ? "已有停战协定" : State.Turn - relation.LastProposalTurn < 3 ? "距离上次交涉不足3个月" : warPressure <= 0 ? "双方当前没有战争压力" : HasPendingBattleBetween(factionId, target.Id) ? "战斗已进入结算" : string.Empty;
            results.Add(NewDiplomaticCandidate("truce", truceScore, truceReason));

            StrategicDiplomacyCandidate NewDiplomaticCandidate(string type, double score, string reason) => new()
            {
                FromFactionId = factionId,
                TargetFactionId = target.Id,
                Type = type,
                Score = Math.Round(score, 1),
                Eligible = string.IsNullOrEmpty(reason),
                BlockReason = reason,
                Factors = $"关系{relation.Relation:+#;-#;0}、信任{relation.Trust}、战争压力{warPressure:0.0}、共同威胁{commonThreat:0.0}、资源需求{resourceNeed:0.0}",
            };
        }
        return results.OrderByDescending(candidate => candidate.Eligible).ThenByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.TargetFactionId).ThenBy(candidate => candidate.Type).ToList();
    }

    public FactionDiplomacyData? FactionDiplomacyBetween(string firstFactionId, string secondFactionId)
    {
        var key = PairKey(firstFactionId, secondFactionId);
        return State.FactionDiplomacy.FirstOrDefault(relation => PairKey(relation.FirstFactionId, relation.SecondFactionId) == key);
    }

    private void InitializeStrategicAiState()
    {
        State.FactionDiplomacy ??= [];
        State.Diplomacy ??= [];
        for (var firstIndex = 0; firstIndex < State.Factions.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < State.Factions.Count; secondIndex++)
            {
                var first = State.Factions[firstIndex].Id;
                var second = State.Factions[secondIndex].Id;
                if (FactionDiplomacyBetween(first, second) is not null) continue;
                var other = first == State.PlayerFactionId ? second : second == State.PlayerFactionId ? first : null;
                var legacy = other is null ? null : State.Diplomacy.FirstOrDefault(relation => relation.FactionId == other);
                var seed = Math.Abs(PairKey(first, second).Aggregate(17, (value, character) => unchecked(value * 31 + character)));
                State.FactionDiplomacy.Add(new FactionDiplomacyData
                {
                    FirstFactionId = first,
                    SecondFactionId = second,
                    Relation = legacy?.Relation ?? seed % 31 - 15,
                    Trust = legacy?.Trust ?? 30 + seed % 21,
                    LastProposalTurn = legacy?.LastProposalTurn ?? -1,
                    Treaties = legacy?.Treaties is null ? [] : new Dictionary<string, int>(legacy.Treaties),
                });
            }
        }
        SynchronizeAllPlayerRelations();
    }

    private void ResolveStrategicAi()
    {
        InitializeStrategicAiState();
        ResolveStrategicMilitaryAi();
        ResolveStrategicDiplomacyAi();
        ResolvePlayerAutomationStrategically();
    }

    private void ResolveStrategicMilitaryAi()
    {
        foreach (var faction in State.Factions.Where(faction => faction.Id != State.PlayerFactionId))
        {
            var candidates = EvaluateStrategicMilitaryCandidates(faction.Id);
            var selected = candidates.FirstOrDefault(candidate => candidate.Eligible && candidate.Score >= MilitaryActionThreshold);
            var selectedKey = selected is null ? string.Empty : MilitaryCandidateKey(selected);
            if (selected is not null) ExecuteStrategicExpedition(selected);
            WriteMilitaryAudit(faction.Id, candidates, selectedKey);
        }
    }

    private void ResolveStrategicDiplomacyAi()
    {
        foreach (var faction in State.Factions.Where(faction => faction.Id != State.PlayerFactionId))
        {
            var candidates = EvaluateStrategicDiplomacyCandidates(faction.Id);
            var selected = candidates.FirstOrDefault(candidate => candidate.Eligible && candidate.Score >= DiplomacyActionThreshold);
            var selectedKey = string.Empty;
            if (selected is not null)
            {
                var pair = FactionDiplomacyBetween(selected.FromFactionId, selected.TargetFactionId)!;
                if (selected.TargetFactionId == State.PlayerFactionId)
                {
                    if (!State.AiDiplomaticProposals.Any(proposal => proposal.Status == "pending"))
                    {
                        pair.LastProposalTurn = State.Turn;
                        State.AiDiplomaticProposals.Add(new AiDiplomaticProposalData { Id = $"ai-proposal-{State.Turn}-{selected.FromFactionId}", FromFactionId = selected.FromFactionId, Type = selected.Type, DurationMonths = selected.Type == "truce" ? 6 : 12, Status = "pending" });
                        SynchronizePlayerRelation(selected.FromFactionId, false);
                        selectedKey = DiplomacyCandidateKey(selected);
                    }
                }
                else
                {
                    pair.LastProposalTurn = State.Turn;
                    pair.Relation = Math.Clamp(pair.Relation + 5, -100, 100);
                    pair.Trust = Math.Clamp(pair.Trust + 3, 0, 100);
                    ApplyTreatyBetween(selected.FromFactionId, selected.TargetFactionId, selected.Type, selected.Type == "truce" ? 6 : 12);
                    State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "diplomacy", Message = $"{Faction(selected.FromFactionId)?.Name}与{Faction(selected.TargetFactionId)?.Name}经战略评估达成{DiplomacyLabel(selected.Type)}。" });
                    selectedKey = DiplomacyCandidateKey(selected);
                }
            }
            WriteDiplomacyAudit(faction.Id, candidates, selectedKey);
        }
    }

    private void ResolvePlayerAutomationStrategically()
    {
        if (!State.Automation.Enabled) return;
        if (State.Automation.Diplomacy && !State.AiDiplomaticProposals.Any(proposal => proposal.Status == "pending"))
        {
            var candidate = EvaluateStrategicDiplomacyCandidates(State.PlayerFactionId)
                .FirstOrDefault(item => item.Eligible && item.Score >= DiplomacyActionThreshold && item.Type == "trade");
            if (candidate is not null && State.Resources.Gold >= State.Automation.MinGoldReserve + 250) ProposeDiplomacy(candidate.TargetFactionId, candidate.Type, 250);
        }
        if (State.Automation.Military)
        {
            var riskModifier = State.Automation.RiskTolerance switch { "high" => -8, "low" => 10, _ => 0 };
            var candidate = EvaluateStrategicMilitaryCandidates(State.PlayerFactionId).FirstOrDefault(item => item.Eligible && item.Score >= MilitaryActionThreshold + riskModifier);
            if (candidate is not null) ExecuteStrategicExpedition(candidate);
        }
    }

    private bool ExecuteStrategicExpedition(StrategicMilitaryCandidate candidate)
    {
        var source = City(candidate.SourceCityId);
        var target = City(candidate.TargetCityId);
        var commander = Officer(candidate.CommanderId);
        var road = State.Roads.FirstOrDefault(item => item.Id == candidate.RoadId);
        if (source?.OwnerFactionId != candidate.FactionId || target?.OwnerFactionId != candidate.TargetFactionId || commander?.InitialState.Status != "serving" || commander.InitialState.CityId != source.Id || road is null) return false;
        var treasury = Treasury(candidate.FactionId);
        if (AreUnderTruce(candidate.FactionId, candidate.TargetFactionId) || source.Garrison - candidate.Soldiers < candidate.ReserveGarrison || treasury.Food < candidate.Food) return false;

        source.Garrison -= candidate.Soldiers;
        treasury.Food -= candidate.Food;
        commander.InitialState.Status = "deployed";
        var armyId = $"army-ai-{State.Turn}-{State.Armies.Count + 1}";
        commander.InitialState.ArmyId = armyId;
        var cavalry = EffectiveAbility(commander, "might", "military") >= 78 ? candidate.Soldiers / 5 : 0;
        var archers = EffectiveAbility(commander, "intelligence", "military") >= 72 ? candidate.Soldiers / 5 : 0;
        var infantry = candidate.Soldiers - cavalry - archers;
        var composition = new Dictionary<string, int> { ["infantry"] = infantry };
        if (cavalry > 0) composition["cavalry"] = cavalry;
        if (archers > 0) composition["archers"] = archers;
        var specialTroops = new Dictionary<string, int>();
        var special = OfficerProgressionRules.SpecialTroops.Values.FirstOrDefault(item => item.FactionIds.Contains(candidate.FactionId) && OfficerProgressionRules.AllTraits(commander).Contains(item.CompatibleTrait));
        if (special is not null)
        {
            var count = Math.Min(composition.GetValueOrDefault(special.BaseTroopType), candidate.Soldiers / 4) / 500 * 500;
            var equipmentCost = count / 500 * special.EquipmentPerFiveHundred;
            if (count >= 500 && treasury.Equipment >= equipmentCost) { specialTroops[special.Id] = count; treasury.Equipment -= equipmentCost; }
        }
        State.Armies.Add(new ArmyData
        {
            Id = armyId, FactionId = candidate.FactionId, SourceCityId = source.Id, TargetCityId = target.Id, CommanderId = commander.Profile.Id,
            OfficerRoles = new Dictionary<string, string> { [commander.Profile.Id] = "commander" }, Composition = composition, SpecialTroops = specialTroops,
            Soldiers = candidate.Soldiers, Food = candidate.Food, Training = source.Training, Morale = 68,
            Stance = commander.Profile.Traits.Contains("豪勇") ? "aggressive" : "standard",
            Tactic = EffectiveAbility(commander, "intelligence", "military") >= 75 ? "feigned-retreat" : "steady-advance",
            RouteRoadIds = [road.Id], RemainingDays = road.TravelDays, TotalDays = road.TravelDays,
        });
        State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "ai", Message = $"{Faction(candidate.FactionId)?.Name}命{commander.Profile.Name}从{source.Name}进军{target.Name}，实拨兵{candidate.Soldiers:N0}、粮{candidate.Food:N0}。" });
        return true;
    }

    private void ResolveStrategicTreaties()
    {
        InitializeStrategicAiState();
        foreach (var relation in State.FactionDiplomacy)
        {
            if (relation.Treaties.GetValueOrDefault("trade") > 0)
            {
                Treasury(relation.FirstFactionId).Gold += TradeIncomePerMonth;
                Treasury(relation.SecondFactionId).Gold += TradeIncomePerMonth;
                if (relation.FirstFactionId == State.PlayerFactionId || relation.SecondFactionId == State.PlayerFactionId)
                {
                    var other = relation.FirstFactionId == State.PlayerFactionId ? relation.SecondFactionId : relation.FirstFactionId;
                    State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "diplomacy", Message = $"与{Faction(other)?.Name}通商，本月双方各获{TradeIncomePerMonth}金。" });
                }
            }
            foreach (var type in relation.Treaties.Keys.ToList())
                if (--relation.Treaties[type] <= 0) relation.Treaties.Remove(type);
        }
        SynchronizeAllPlayerRelations();
    }

    private int TreatyMonthsBetween(string firstFactionId, string secondFactionId, string type) => FactionDiplomacyBetween(firstFactionId, secondFactionId)?.Treaties.GetValueOrDefault(type) ?? 0;

    private void ApplyTreatyBetween(string firstFactionId, string secondFactionId, string type, int durationMonths)
    {
        var relation = FactionDiplomacyBetween(firstFactionId, secondFactionId);
        if (relation is null) return;
        relation.Treaties[type] = durationMonths;
        if (firstFactionId == State.PlayerFactionId) SynchronizePlayerRelation(secondFactionId, false);
        else if (secondFactionId == State.PlayerFactionId) SynchronizePlayerRelation(firstFactionId, false);
        if (type != "truce") return;
        foreach (var army in State.Armies.Where(army => army.Status is "marching" or "besieging" && ((army.FactionId == firstFactionId && City(army.TargetCityId)?.OwnerFactionId == secondFactionId) || (army.FactionId == secondFactionId && City(army.TargetCityId)?.OwnerFactionId == firstFactionId))).ToList())
            RecallArmy(army, $"{Faction(firstFactionId)?.Name}与{Faction(secondFactionId)?.Name}达成停战，{Officer(army.CommanderId)?.Profile.Name}军撤回驻地。");
    }

    private void SynchronizeLegacyRelationToPair(string otherFactionId)
    {
        var legacy = State.Diplomacy.FirstOrDefault(relation => relation.FactionId == otherFactionId);
        var pair = FactionDiplomacyBetween(State.PlayerFactionId, otherFactionId);
        if (legacy is null || pair is null) return;
        pair.Relation = legacy.Relation;
        pair.Trust = legacy.Trust;
        pair.LastProposalTurn = legacy.LastProposalTurn;
        pair.Treaties = new Dictionary<string, int>(legacy.Treaties);
        legacy.Treaties = pair.Treaties;
    }

    private void SynchronizePlayerRelation(string otherFactionId, bool legacyWins)
    {
        var pair = FactionDiplomacyBetween(State.PlayerFactionId, otherFactionId);
        if (pair is null) return;
        var legacy = State.Diplomacy.FirstOrDefault(relation => relation.FactionId == otherFactionId);
        if (legacy is null)
        {
            legacy = new DiplomacyRelationData { FactionId = otherFactionId };
            State.Diplomacy.Add(legacy);
        }
        if (legacyWins)
        {
            pair.Relation = legacy.Relation;
            pair.Trust = legacy.Trust;
            pair.LastProposalTurn = legacy.LastProposalTurn;
            pair.Treaties = new Dictionary<string, int>(legacy.Treaties);
        }
        legacy.Relation = pair.Relation;
        legacy.Trust = pair.Trust;
        legacy.LastProposalTurn = pair.LastProposalTurn;
        legacy.Treaties = pair.Treaties;
    }

    private void SynchronizeAllPlayerRelations()
    {
        foreach (var faction in State.Factions.Where(faction => faction.Id != State.PlayerFactionId)) SynchronizePlayerRelation(faction.Id, false);
    }

    private void WriteMilitaryAudit(string factionId, IReadOnlyList<StrategicMilitaryCandidate> candidates, string selectedKey)
    {
        var entries = candidates.Take(3).Select((candidate, index) =>
        {
            var key = MilitaryCandidateKey(candidate);
            var reason = key == selectedKey ? "已执行" : !candidate.Eligible ? candidate.BlockReason : candidate.Score < MilitaryActionThreshold ? $"低于行动阈值{MilitaryActionThreshold:0}" : "优先级低于首选";
            return $"{index + 1}.{City(candidate.SourceCityId)?.Name}→{City(candidate.TargetCityId)?.Name} {candidate.Score:0.0}分（{reason}；{candidate.Factors}）";
        });
        State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "ai-strategy", Message = $"{Faction(factionId)?.ShortName}军事候选：{(candidates.Count == 0 ? "无接壤敌城" : string.Join("；", entries))}" });
    }

    private void WriteDiplomacyAudit(string factionId, IReadOnlyList<StrategicDiplomacyCandidate> candidates, string selectedKey)
    {
        var entries = candidates.Take(3).Select((candidate, index) =>
        {
            var key = DiplomacyCandidateKey(candidate);
            var reason = key == selectedKey ? "已提案/达成" : !candidate.Eligible ? candidate.BlockReason : candidate.Score < DiplomacyActionThreshold ? $"低于行动阈值{DiplomacyActionThreshold:0}" : "优先级低于首选";
            return $"{index + 1}.{Faction(candidate.TargetFactionId)?.ShortName}{DiplomacyLabel(candidate.Type)} {candidate.Score:0.0}分（{reason}；{candidate.Factors}）";
        });
        State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "ai-strategy", Message = $"{Faction(factionId)?.ShortName}外交候选：{string.Join("；", entries)}" });
    }

    private bool HasSharedBorder(string firstFactionId, string secondFactionId) => State.Roads.Any(road =>
    {
        var firstOwner = City(road.FromCityId)?.OwnerFactionId;
        var secondOwner = City(road.ToCityId)?.OwnerFactionId;
        return firstOwner == firstFactionId && secondOwner == secondFactionId || firstOwner == secondFactionId && secondOwner == firstFactionId;
    });

    private double WarPressureBetween(string firstFactionId, string secondFactionId)
    {
        var armies = State.Armies.Where(army => army.Status is "marching" or "besieging" or "awaiting-battle" && ((army.FactionId == firstFactionId && City(army.TargetCityId)?.OwnerFactionId == secondFactionId) || (army.FactionId == secondFactionId && City(army.TargetCityId)?.OwnerFactionId == firstFactionId))).Sum(army => army.Soldiers / 2500d);
        var battles = State.BattleReports.Count(report => State.Turn - report.Turn <= 6 && (report.AttackerFactionId == firstFactionId && report.DefenderFactionId == secondFactionId || report.AttackerFactionId == secondFactionId && report.DefenderFactionId == firstFactionId));
        return armies + battles * 2 + (HasSharedBorder(firstFactionId, secondFactionId) && FactionDiplomacyBetween(firstFactionId, secondFactionId)?.Relation < -35 ? 1 : 0);
    }

    private double CommonThreatScore(string firstFactionId, string secondFactionId)
    {
        return State.Factions.Where(faction => faction.Id != firstFactionId && faction.Id != secondFactionId)
            .Count(third => HasSharedBorder(firstFactionId, third.Id) && HasSharedBorder(secondFactionId, third.Id) && State.Cities.Count(city => city.OwnerFactionId == third.Id) >= Math.Min(State.Cities.Count(city => city.OwnerFactionId == firstFactionId), State.Cities.Count(city => city.OwnerFactionId == secondFactionId)));
    }

    private double ResourceNeedScore(string factionId)
    {
        var cities = State.Cities.Where(city => city.OwnerFactionId == factionId).ToList();
        var treasury = Treasury(factionId);
        var goldUpkeep = cities.Sum(city => CityMonthlyForecast(city).GoldUpkeep);
        var foodUpkeep = cities.Sum(city => CityMonthlyForecast(city).FoodUpkeep);
        var goldCoverage = treasury.Gold / (double)Math.Max(1, goldUpkeep);
        var foodCoverage = treasury.Food / (double)Math.Max(1, foodUpkeep);
        return Math.Clamp((4 - goldCoverage) / 2, 0, 4) + Math.Clamp((5 - foodCoverage) / 2, 0, 4);
    }

    private static string PairKey(string firstFactionId, string secondFactionId) => string.CompareOrdinal(firstFactionId, secondFactionId) <= 0 ? $"{firstFactionId}|{secondFactionId}" : $"{secondFactionId}|{firstFactionId}";
    private static string MilitaryCandidateKey(StrategicMilitaryCandidate candidate) => $"{candidate.SourceCityId}|{candidate.TargetCityId}|{candidate.CommanderId}";
    private static string DiplomacyCandidateKey(StrategicDiplomacyCandidate candidate) => $"{candidate.FromFactionId}|{candidate.TargetFactionId}|{candidate.Type}";
}

public sealed class StrategicMilitaryCandidate
{
    public string FactionId { get; set; } = string.Empty;
    public string SourceCityId { get; set; } = string.Empty;
    public string TargetCityId { get; set; } = string.Empty;
    public string TargetFactionId { get; set; } = string.Empty;
    public string RoadId { get; set; } = string.Empty;
    public string CommanderId { get; set; } = string.Empty;
    public int Soldiers { get; set; }
    public int Food { get; set; }
    public int ReserveGarrison { get; set; }
    public int TravelDays { get; set; }
    public string Terrain { get; set; } = "plain";
    public double Score { get; set; }
    public bool Eligible { get; set; }
    public string BlockReason { get; set; } = string.Empty;
    public string Factors { get; set; } = string.Empty;
}

public sealed class StrategicDiplomacyCandidate
{
    public string FromFactionId { get; set; } = string.Empty;
    public string TargetFactionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool Eligible { get; set; }
    public string BlockReason { get; set; } = string.Empty;
    public string Factors { get; set; } = string.Empty;
}

public sealed class FactionDiplomacyData
{
    public string FirstFactionId { get; set; } = string.Empty;
    public string SecondFactionId { get; set; } = string.Empty;
    public int Relation { get; set; }
    public int Trust { get; set; } = 35;
    public int LastProposalTurn { get; set; } = -1;
    public Dictionary<string, int> Treaties { get; set; } = [];
}
