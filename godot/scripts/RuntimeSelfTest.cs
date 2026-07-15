using Godot;

namespace ThreeKingdomsSimulator.Godot;

public static class RuntimeSelfTest
{
    public static void Run(ScenarioData scenario)
    {
        var runtime = new GameRuntime(scenario);
        Require(runtime.State.Cities.Count == 33, "剧本城市数量");
        Require(runtime.State.Cities.Sum(city => city.Population) == 2_502_000 && runtime.State.Cities.All(city => city.Population is >= 43_000 and <= 128_000), "战乱时期城池人口规模");
        Require(runtime.State.Cities.Sum(city => city.Garrison) == 358_000 && runtime.State.Cities.All(city => city.Garrison is >= 6_900 and <= 14_700), "初始守军规模");
        Require(runtime.State.Officers.Count >= 100, "武将数据载入");
        Require(runtime.State.Events.Count == 40, "事件定义载入");
        Require(runtime.State.Officers.All(officer => ResourceLoader.Exists(AssetPaths.OfficerPortrait(officer.Profile.Id))), "全部武将肖像绑定");
        Require(runtime.State.Cities.All(city => ResourceLoader.Exists(AssetPaths.CityMarker(city.Region, city.Id))), "全部城池图标绑定");
        Require(new[] { "plain", "hill", "river", "mountain" }.All(terrain => ResourceLoader.Exists(AssetPaths.BattleBackground(terrain))), "四类野战背景绑定");
        Require(BattleCatalog.TroopTypes.All(troop => ResourceLoader.Exists(AssetPaths.TroopSprite(troop))), "五兵种战斗图集绑定");
        Require(new[] { "generic-vanguard", "generic-commander", "generic-strategist", "liu-bei", "guan-yu", "zhang-fei", "lu-bu" }
            .All(sprite => ResourceLoader.Exists(AssetPaths.MountedOfficerSprite(sprite))), "七套骑乘武将战斗图集绑定");
        Require(AssetPaths.AmbientMusic.All(path => ResourceLoader.Exists(path)) && ResourceLoader.Exists(AssetPaths.BattleMusic), "全局与战斗音乐资产绑定");
        Require(BattleCalculator.ExpectedGroupCount(6000) == 10 && BattleCalculator.ExpectedGroupCount(12000) == 20 && BattleCalculator.ExpectedGroupCount(24000) == 40, "军团人数映射10/20/40战斗队");
        Require(BattleCalculator.MatchupModifier("infantry", "cavalry") == 1
            && BattleCalculator.MatchupModifier("spears", "cavalry") > 1
            && BattleCalculator.MatchupModifier("cavalry", "infantry") > 1
            && BattleCalculator.MatchupModifier("cavalry", "archers") > 1
            && BattleCalculator.MatchupModifier("cavalry", "spears") < 1,
            "枪兵克骑兵且骑兵克步兵弓兵");
        Require(global::Godot.FileAccess.FileExists(GameTheme.EmbeddedFontPath) && GD.Load<Font>(GameTheme.EmbeddedFontPath) is not null, "内置简体中文字体可加载");

        Require(scenario.Factions.Count == 16 && scenario.Factions.All(faction => scenario.Cities.Any(city => city.OwnerFactionId == faction.Id)), "新游戏提供16个可选有城势力");
        var alternateFaction = scenario.Factions.First(item => item.Id != scenario.PlayerFactionId);
        var alternate = new GameRuntime(scenario, new NewGameOptions { PlayerFactionId = alternateFaction.Id, Difficulty = "hard", AutoSaveFrequency = "quarterly" });
        var alternateExpected = GameSession.PreviewInitialResources(scenario, alternateFaction.Id, "hard");
        Require(alternate.State.PlayerFactionId == alternateFaction.Id && alternate.State.Difficulty == "hard" && alternate.State.AutoSaveFrequency == "quarterly", "新游戏势力、难度与自动存档设置写入会话");
        Require(alternate.State.Resources.Gold == alternateExpected.Gold && alternate.State.Resources.Food == alternateExpected.Food && !alternate.State.FactionTreasuries.ContainsKey(alternateFaction.Id) && alternate.State.FactionTreasuries.ContainsKey(scenario.PlayerFactionId), "改选势力后中央府库归属正确");
        Require(alternate.State.Diplomacy.Count == scenario.Factions.Count - 1 && alternate.State.Diplomacy.All(item => item.FactionId != alternateFaction.Id), "改选势力后外交关系重新初始化");
        Require(!SaveService.ShouldWriteAuto(new GameSession { Turn = 2, AutoSaveFrequency = "quarterly" }) && SaveService.ShouldWriteAuto(new GameSession { Turn = 4, AutoSaveFrequency = "quarterly" }) && !SaveService.ShouldWriteAuto(new GameSession { Turn = 13, AutoSaveFrequency = "off" }), "自动存档频率规则");
        var legacyDefaults = GameSession.Create(scenario);
        legacyDefaults.SchemaVersion = 3; legacyDefaults.Difficulty = ""; legacyDefaults.AutoSaveFrequency = ""; legacyDefaults.NineCityControlMonths = -1; legacyDefaults.EnsureDomesticDefaults();
        Require(legacyDefaults.SchemaVersion == GameSession.CurrentSchemaVersion && legacyDefaults.Difficulty == "standard" && legacyDefaults.AutoSaveFrequency == "monthly" && legacyDefaults.NineCityControlMonths == 0, "旧档补齐新游戏与胜利进度默认值");
        Require(GameSession.CurrentSchemaVersion == 6 && legacyDefaults.Officers.All(item => item.InitialState.Level is >= 1 and <= 20 && item.Profile.GrowthPlan.Count == 23 && item.Profile.AbilityPotential.Leadership >= item.Profile.Abilities.Leadership && item.InitialState.CourtOfficeId is not null), "schema v6 补齐等级、成长方案、潜力与朝堂职位");

        var resourceForecast = new GameRuntime(scenario);
        var forecastBefore = new ResourceData
        {
            Gold = resourceForecast.State.Resources.Gold,
            Food = resourceForecast.State.Resources.Food,
            Prestige = resourceForecast.State.Resources.Prestige,
            Equipment = resourceForecast.State.Resources.Equipment,
        };
        var forecastDelta = resourceForecast.PreviewEndTurnResourceDelta();
        Require(resourceForecast.EndTurn() &&
            resourceForecast.State.Resources.Gold - forecastBefore.Gold == forecastDelta.Gold &&
            resourceForecast.State.Resources.Food - forecastBefore.Food == forecastDelta.Food &&
            resourceForecast.State.Resources.Prestige - forecastBefore.Prestige == forecastDelta.Prestige &&
            resourceForecast.State.Resources.Equipment - forecastBefore.Equipment == forecastDelta.Equipment,
            "右上角资源预估与实际月末结算一致");

        var recruitment = new GameRuntime(scenario);
        var recruiter = recruitment.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving");
        var recruitmentTarget = recruitment.State.Officers.First(item => item.InitialState.FactionId != recruitment.State.PlayerFactionId && item.InitialState.Loyalty <= 79);
        recruiter.InitialState.OfficeRank = 0; recruiter.InitialState.GrowthBonuses = new OfficerAbilitiesData(); recruiter.Profile.Abilities.Charisma = 30; recruiter.Profile.Abilities.Intelligence = 70; recruiter.Profile.Abilities.Politics = 70;
        var lowCharismaChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id, "direct");
        recruiter.Profile.Abilities.Charisma = 90;
        var highCharismaChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id, "direct");
        var recommendationChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id, "recommendation");
        Require(highCharismaChance > lowCharismaChance && recommendationChance > highCharismaChance && recommendationChance <= 95, "招募成功率由魅力主导并受方式修正且不保证必成");

        var transfer = new GameRuntime(scenario);
        var transferOfficer = transfer.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving");
        var transferSourceId = transferOfficer.InitialState.CityId;
        var transferTarget = transfer.State.Cities.First(item => item.OwnerFactionId == transfer.State.PlayerFactionId && item.Id != transferSourceId);
        Require(!transfer.TransferOfficer(transferOfficer.Profile.Id, transferSourceId) && transfer.TransferOfficer(transferOfficer.Profile.Id, transferTarget.Id) && transferOfficer.InitialState.CityId == transferTarget.Id, "武将调动拒绝原地操作并写入起讫城市");

        var court = new GameRuntime(scenario);
        var chancellor = court.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && item.InitialState.OfficeTrack == "civil");
        chancellor.InitialState.OfficeRank = 3; chancellor.InitialState.CourtOfficeId = string.Empty;
        var courtSalaryBefore = OfficerProgressionRules.Salary(chancellor);
        var courtPoliticsBefore = court.EffectiveAbility(chancellor, "politics", "civil");
        Require(court.AppointCourtOffice(chancellor.Profile.Id, "chancellor") && OfficerProgressionRules.Salary(chancellor) == courtSalaryBefore + 100 && court.EffectiveAbility(chancellor, "politics", "civil") == courtPoliticsBefore + 6, "朝堂职位提供唯一任命、专属效果与俸禄津贴");
        var successor = court.PlayerOfficers().First(item => item.Profile.Id != chancellor.Profile.Id && item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && item.InitialState.OfficeTrack == "civil");
        successor.InitialState.OfficeRank = 3; successor.InitialState.CourtOfficeId = string.Empty;
        Require(court.AppointCourtOffice(successor.Profile.Id, "chancellor") && string.IsNullOrEmpty(chancellor.InitialState.CourtOfficeId) && successor.InitialState.CourtOfficeId == "chancellor", "同一朝堂席位改任时自动卸任前任");

        var progression = new GameRuntime(scenario);
        var growingOfficer = progression.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.Profile.Abilities.Leadership < 100);
        growingOfficer.InitialState.Level = 1; growingOfficer.InitialState.CareerExperience = 199; growingOfficer.InitialState.ExperienceEarnedThisTurn = 0; growingOfficer.InitialState.GrowthBonuses = new OfficerAbilitiesData();
        var permanentBefore = new[] { "leadership", "might", "intelligence", "politics", "charisma" }.Sum(ability => progression.PermanentAbility(growingOfficer, ability));
        progression.AwardOfficerExperience(growingOfficer, 1, "自测履历", "self-test");
        var permanentAfter = new[] { "leadership", "might", "intelligence", "politics", "charisma" }.Sum(ability => progression.PermanentAbility(growingOfficer, ability));
        Require(growingOfficer.InitialState.Level == 2 && permanentAfter == permanentBefore + 1 && growingOfficer.InitialState.CareerRecords.GetValueOrDefault("self-test") == 1, "阅历升级按确定性方案增加永久五维");

        foreach (var item in progression.PlayerOfficers()) item.InitialState.OfficeRank = 0;
        growingOfficer.InitialState.Level = 4; growingOfficer.InitialState.Merit = 600; growingOfficer.InitialState.OfficeTrack = "civil"; growingOfficer.InitialState.LastPromotionTurn = -99;
        var politicsBeforeOffice = progression.EffectiveAbility(growingOfficer, "politics", "civil");
        Require(progression.PromoteOfficer(growingOfficer.Profile.Id, "civil"), "文职晋升命令");
        Require(growingOfficer.InitialState.OfficeRank == 1 && progression.EffectiveAbility(growingOfficer, "politics", "civil") == politicsBeforeOffice + 1, "文职晋升提高匹配任务有效属性");
        progression.State.Roads.Clear(); progression.State.Events.Clear();
        Require(progression.EndTurn() && progression.State.PayrollLedgerEntries.Any(item => item.OfficerId == growingOfficer.Profile.Id && item.Due == 10) && progression.State.MonthlySalaryDue >= 10, "月结算按唯一支付方发放逐人俸禄");

        var arrears = new GameRuntime(scenario);
        foreach (var item in arrears.PlayerOfficers()) item.InitialState.OfficeRank = 0;
        var unpaidOfficer = arrears.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving");
        unpaidOfficer.InitialState.OfficeTrack = "civil"; unpaidOfficer.InitialState.OfficeRank = 7;
        var unpaidCity = arrears.City(unpaidOfficer.InitialState.CityId)!;
        arrears.State.Resources.Gold = 0;
        foreach (var cityState in arrears.State.Cities.Where(item => item.OwnerFactionId == arrears.State.PlayerFactionId)) { cityState.Population = 0; cityState.Commerce = 0; cityState.Facilities.Clear(); }
        arrears.State.Roads.Clear(); arrears.State.Events.Clear();
        Require(arrears.EndTurn() && unpaidOfficer.InitialState.SalaryArrears == 220 && arrears.State.Resources.Gold >= 0, "府库不足产生欠俸但不产生负金");

        var gaoShun = runtime.State.Officers.First(item => item.Profile.Id == "officer-gao-shun");
        var traitDescriptions = new Dictionary<string, string>();
        var noEliteTrait = OfficerProgressionRules.BattleTraitMultiplier(runtime.State, [gaoShun.Profile.Id], new Dictionary<string, string> { [gaoShun.Profile.Id] = "主将" }, new Dictionary<string, int> { ["infantry"] = 8000 }, [], "正面接战", traitDescriptions);
        var eliteTrait = OfficerProgressionRules.BattleTraitMultiplier(runtime.State, [gaoShun.Profile.Id], new Dictionary<string, string> { [gaoShun.Profile.Id] = "主将" }, new Dictionary<string, int> { ["infantry"] = 8000 }, new Dictionary<string, int> { ["trap-camp"] = 2000 }, "正面接战", traitDescriptions);
        Require(eliteTrait > noEliteTrait && traitDescriptions.GetValueOrDefault(gaoShun.Profile.Id).Contains("陷阵之志"), "名将特性只在特殊部队人数与占比达标时生效");
        var civilOfficer = runtime.PlayerOfficers().First(item => item.Profile.Traits.Contains("善政"));
        Require(runtime.CityCommandPreview(civilOfficer.InitialState.CityId, civilOfficer.Profile.Id, "agriculture").Contains("善政+4"), "文臣特性进入内政预估与正式公式");

        var strategicVictory = new GameRuntime(scenario);
        var remainingFactionId = strategicVictory.State.Factions.First(item => item.Id != strategicVictory.State.PlayerFactionId).Id;
        foreach (var cityState in strategicVictory.State.Cities) cityState.OwnerFactionId = remainingFactionId;
        foreach (var cityState in strategicVictory.State.Cities.Take(GameSession.StrategicVictoryCityCount)) cityState.OwnerFactionId = strategicVictory.State.PlayerFactionId;
        strategicVictory.State.Roads.Clear(); strategicVictory.State.Events.Clear();
        Require(strategicVictory.EndTurn() && strategicVictory.State.NineCityControlMonths == 1 && strategicVictory.State.Outcome == "ongoing", "控制九城首月记录");
        Require(strategicVictory.EndTurn() && strategicVictory.State.NineCityControlMonths == 2 && strategicVictory.State.Outcome == "ongoing", "控制九城连续第二月记录");
        Require(strategicVictory.EndTurn() && strategicVictory.State.NineCityControlMonths == 3 && strategicVictory.State.Outcome == "victory" && strategicVictory.State.OutcomeMessage.Contains("九城归心"), "控制九城连续三个月胜利");

        var interruptedVictory = new GameRuntime(scenario);
        remainingFactionId = interruptedVictory.State.Factions.First(item => item.Id != interruptedVictory.State.PlayerFactionId).Id;
        foreach (var cityState in interruptedVictory.State.Cities) cityState.OwnerFactionId = remainingFactionId;
        foreach (var cityState in interruptedVictory.State.Cities.Take(GameSession.StrategicVictoryCityCount)) cityState.OwnerFactionId = interruptedVictory.State.PlayerFactionId;
        interruptedVictory.State.Roads.Clear(); interruptedVictory.State.Events.Clear();
        Require(interruptedVictory.EndTurn() && interruptedVictory.State.NineCityControlMonths == 1, "九城维持进度开始累计");
        interruptedVictory.State.Cities.First(city => city.OwnerFactionId == interruptedVictory.State.PlayerFactionId).OwnerFactionId = remainingFactionId;
        Require(interruptedVictory.EndTurn() && interruptedVictory.State.NineCityControlMonths == 0 && interruptedVictory.State.Outcome == "ongoing", "不足九城时连续进度清零");

        var domestic = new GameRuntime(scenario);
        var playerCities = domestic.State.Cities.Where(item => item.OwnerFactionId == domestic.State.PlayerFactionId).Take(2).ToList();
        Require(playerCities.Count == 2, "玩家至少拥有两座独立城池");
        var firstCity = playerCities[0];
        var secondCity = playerCities[1];
        var domesticOfficer = domestic.PlayerOfficers().First(item => item.InitialState.CityId == firstCity.Id && item.InitialState.Status == "serving");
        var secondSlots = secondCity.ActionSlots;
        var factionGold = domestic.State.Resources.Gold;
        Require(domestic.DevelopCity(firstCity.Id, domesticOfficer.Profile.Id, "agriculture"), "本地城务命令");
        Require(domestic.State.Resources.Gold < factionGold, "普通城务扣除势力共用府库");
        Require(secondCity.ActionSlots == secondSlots, "一城行动不消耗另一城城务额度");
        Require(!domestic.TransferTreasury(firstCity.Id, true, 100, 200), "共用府库不再需要城市调拨");
        Require(domestic.ConfigureCityGovernance(secondCity.Id, "delegated", "agriculture", "granary", true), "逐城设置太守委任方针");
        Require(domestic.EndTurn(), "智能内政月度结算");
        Require(domestic.State.Cities.All(item => item.ActionCapacity is >= 1 and <= 4 && item.ActionSlots == item.ActionCapacity), "逐城城务额度独立重置");
        Require(domestic.State.Cities.Any(item => item.OwnerFactionId != domestic.State.PlayerFactionId && item.LedgerEntries.Any(entry => entry.Category == "command")), "AI 使用同规则执行城务并写入台账");
        Require(domestic.State.Log.Any(item => item.Category == "ai" && item.Message.Contains("评估")), "AI 多因素评估记录");

        var strategic = new GameRuntime(scenario);
        var expectedDiplomacyPairs = strategic.State.Factions.Count * (strategic.State.Factions.Count - 1) / 2;
        Require(strategic.State.FactionDiplomacy.Count == expectedDiplomacyPairs, "全势力两两外交状态初始化");
        var strategicFaction = strategic.State.Factions
            .Where(item => item.Id != strategic.State.PlayerFactionId)
            .Select(item => new { item.Id, Candidates = strategic.EvaluateStrategicMilitaryCandidates(item.Id) })
            .OrderByDescending(item => item.Candidates.Count)
            .First();
        Require(strategicFaction.Candidates.Count >= 3, "战略 AI 每月生成多个军事候选");
        Require(strategicFaction.Candidates.SequenceEqual(strategicFaction.Candidates.OrderByDescending(item => item.Eligible).ThenByDescending(item => item.Score).ThenBy(item => item.SourceCityId).ThenBy(item => item.TargetCityId)), "军事候选按可执行性与得分排序");
        var resourceCandidate = strategicFaction.Candidates.First(item => item.Eligible);
        var resourceCity = strategic.City(resourceCandidate.SourceCityId)!;
        var garrisonBeforeEvaluation = resourceCity.Garrison;
        var factionTreasury = strategic.State.FactionTreasuries[strategicFaction.Id];
        var foodBeforeEvaluation = factionTreasury.Food;
        strategic.EvaluateStrategicMilitaryCandidates(strategicFaction.Id);
        Require(resourceCity.Garrison == garrisonBeforeEvaluation && factionTreasury.Food == foodBeforeEvaluation, "战略评估不会凭空增减兵粮");
        factionTreasury.Food = 0;
        var foodBlocked = strategic.EvaluateStrategicMilitaryCandidates(strategicFaction.Id).First(item => item.RoadId == resourceCandidate.RoadId && item.SourceCityId == resourceCandidate.SourceCityId && item.TargetCityId == resourceCandidate.TargetCityId);
        Require(!foodBlocked.Eligible && foodBlocked.BlockReason.Contains("安全储备"), "低粮城市不会被 AI 强制出征");
        factionTreasury.Food = foodBeforeEvaluation;
        var aiTargetCandidate = strategicFaction.Candidates.First(item => item.TargetFactionId != strategic.State.PlayerFactionId);
        var aiPair = strategic.FactionDiplomacyBetween(strategicFaction.Id, aiTargetCandidate.TargetFactionId)!;
        aiPair.Treaties["truce"] = 2;
        var truceBlocked = strategic.EvaluateStrategicMilitaryCandidates(strategicFaction.Id).First(item => item.RoadId == aiTargetCandidate.RoadId && item.SourceCityId == aiTargetCandidate.SourceCityId && item.TargetCityId == aiTargetCandidate.TargetCityId);
        Require(!truceBlocked.Eligible && truceBlocked.BlockReason.Contains("停战"), "AI-AI 停战状态阻止军事候选");
        aiPair.Treaties.Remove("truce");
        var diplomacyCandidates = strategic.EvaluateStrategicDiplomacyCandidates(strategicFaction.Id);
        Require(diplomacyCandidates.Count >= 3 && diplomacyCandidates.SequenceEqual(diplomacyCandidates.OrderByDescending(item => item.Eligible).ThenByDescending(item => item.Score).ThenBy(item => item.TargetFactionId).ThenBy(item => item.Type)), "外交候选按关系信任与战争资源压力排序");
        Require(strategic.EndTurn() && strategic.State.Log.Any(item => item.Category == "ai-strategy" && item.Message.Contains("军事候选：") && item.Message.Contains("1.")), "战略 AI 记录候选得分与未选原因");

        var simulation = new GameRuntime(scenario);
        simulation.ConfigureAutomation(true, true, true, false, true, "medium", 3000, 10000, 5000);
        foreach (var delegatedCity in simulation.State.Cities.Where(item => item.OwnerFactionId == simulation.State.PlayerFactionId)) delegatedCity.GovernanceMode = "delegated";
        var targetTurn = simulation.State.Turn + 60;
        var guard = 0;
        while (simulation.State.Turn < targetTurn && guard++ < 500)
        {
            if (simulation.State.PendingEvent is not null)
            {
                var eventDefinition = simulation.State.Events.First(item => item.Id == simulation.State.PendingEvent.DefinitionId);
                simulation.ChooseEvent(eventDefinition.Choices[0].Id);
                continue;
            }
            if (simulation.State.PendingBattle is not null)
            {
                if (simulation.State.PendingBattle.Status == "planning") simulation.StartPendingBattle();
                if (simulation.State.PendingBattle?.Status == "running") simulation.CompletePendingBattle();
                continue;
            }
            simulation.EndTurn();
        }
        Require(simulation.State.Turn == targetTurn, "60回合智能内政压力演进");
        Require(simulation.State.Resources.Gold >= 0 && simulation.State.Resources.Food >= 0 && simulation.State.Cities.All(item => item.ActionCapacity is >= 1 and <= 4), "长期势力资源与城市额度不变量");
        Require(simulation.State.Cities.All(item => item.LedgerEntries.Count <= 80), "城市台账保留上限");

        var city = runtime.State.Cities.First(item => item.OwnerFactionId == runtime.State.PlayerFactionId);
        var officer = runtime.PlayerOfficers().First(item => item.InitialState.CityId == city.Id && item.InitialState.Status == "serving");
        var agriculture = city.Agriculture;
        Require(runtime.DevelopCity(city.Id, officer.Profile.Id, "agriculture"), "城市开发命令");
        Require(city.Agriculture > agriculture, "开发数值写回");

        var targetFaction = runtime.State.Factions.First(item => item.Id != runtime.State.PlayerFactionId);
        var targetRelation = runtime.State.Diplomacy.First(item => item.FactionId == targetFaction.Id);
        targetRelation.Relation = 100; targetRelation.Trust = 100;
        Require(runtime.DiplomacyChance(targetFaction.Id, "trade", 0) == 95, "外交成功率预览");
        Require(!runtime.ProposeDiplomacy(targetFaction.Id, "alliance", 0), "已移除同盟提案");
        Require(runtime.ProposeDiplomacy(targetFaction.Id, "trade", 0), "外交命令");
        Require(runtime.HasTreaty(targetFaction.Id, "trade"), "通商条约建立");

        runtime.State.Roads.Clear(); // 隔离事件与通商测试，避免战略 AI 在第三月插入战前结算。
        for (var index = 0; index < 3; index++) Require(runtime.EndTurn(), $"回合结算{index + 1}");
        Require(runtime.State.Log.Any(item => item.Category == "diplomacy" && item.Message.Contains($"双方各获{GameRuntime.TradeIncomePerMonth}金")), "通商月度收益结算");
        Require(runtime.State.PendingEvent is not null, "四回合事件排队");
        var definition = runtime.State.Events.First(item => item.Id == runtime.State.PendingEvent!.DefinitionId);
        Require(runtime.ChooseEvent(definition.Choices[0].Id), "事件选项结算");
        Require(runtime.State.EventHistory.Count == 1, "事件历史记录");

        SaveService.WriteManual(runtime.State, 10);
        var loaded = SaveService.Load("manual", 10);
        Require(loaded is not null && loaded.Turn == runtime.State.Turn && loaded.Cities.Count == runtime.State.Cities.Count && loaded.Difficulty == "standard" && loaded.AutoSaveFrequency == "monthly", "完整存读档含新游戏设置");

        var fullSortie = new GameRuntime(scenario);
        var fullSource = fullSortie.State.Cities.First(item => item.OwnerFactionId == fullSortie.State.PlayerFactionId && item.Garrison >= 1000 && fullSortie.State.Roads.Any(road => (road.FromCityId == item.Id && fullSortie.City(road.ToCityId)?.OwnerFactionId != fullSortie.State.PlayerFactionId) || (road.ToCityId == item.Id && fullSortie.City(road.FromCityId)?.OwnerFactionId != fullSortie.State.PlayerFactionId)));
        var fullRoad = fullSortie.State.Roads.First(road => (road.FromCityId == fullSource.Id && fullSortie.City(road.ToCityId)?.OwnerFactionId != fullSortie.State.PlayerFactionId) || (road.ToCityId == fullSource.Id && fullSortie.City(road.FromCityId)?.OwnerFactionId != fullSortie.State.PlayerFactionId));
        var fullTargetId = fullRoad.FromCityId == fullSource.Id ? fullRoad.ToCityId : fullRoad.FromCityId;
        var fullCommander = fullSortie.PlayerOfficers().First(item => item.InitialState.CityId == fullSource.Id && item.InitialState.Status == "serving");
        var fullGarrison = fullSource.Garrison;
        var equipmentBeforeSpecialTroop = fullSortie.State.Resources.Equipment;
        Require(fullSortie.CreateExpedition(fullSource.Id, fullTargetId, fullCommander.Profile.Id, fullGarrison, Math.Min(1000, fullSortie.State.Resources.Food), "standard", "steady-advance", [], new Dictionary<string, int> { ["infantry"] = fullGarrison - 500, ["archers"] = 500 }, "fortify-camp", new Dictionary<string, int> { ["danyang-veterans"] = 500 }), "允许玩家按实际阵型全城出击并编成特殊部队");
        Require(fullSource.Garrison == 0 && fullSortie.State.Armies.Last().Soldiers == fullGarrison, "全城出击真实扣空驻军且军团兵力一致");
        Require(fullSortie.State.Armies.Last().SpecialTroops.GetValueOrDefault("danyang-veterans") == 500 && fullSortie.State.Resources.Equipment == equipmentBeforeSpecialTroop - 40, "特殊部队写入军团并扣除军备");

        var withdrawal = new GameRuntime(scenario);
        var withdrawalSource = withdrawal.State.Cities.First(item => item.OwnerFactionId == withdrawal.State.PlayerFactionId && withdrawal.State.Roads.Any(road => road.TravelDays > 30 && ((road.FromCityId == item.Id && withdrawal.City(road.ToCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId) || (road.ToCityId == item.Id && withdrawal.City(road.FromCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId))));
        var withdrawalRoad = withdrawal.State.Roads.First(road => road.TravelDays > 30 && ((road.FromCityId == withdrawalSource.Id && withdrawal.City(road.ToCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId) || (road.ToCityId == withdrawalSource.Id && withdrawal.City(road.FromCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId)));
        var withdrawalTargetId = withdrawalRoad.FromCityId == withdrawalSource.Id ? withdrawalRoad.ToCityId : withdrawalRoad.FromCityId;
        var withdrawalCommander = withdrawal.PlayerOfficers().First(item => item.InitialState.CityId == withdrawalSource.Id && item.InitialState.Status == "serving");
        Require(withdrawal.CreateExpedition(withdrawalSource.Id, withdrawalTargetId, withdrawalCommander.Profile.Id, 3000, 5000, "standard", "steady-advance", [], new Dictionary<string, int> { ["infantry"] = 2500, ["archers"] = 500 }, "fortify-camp"), "撤兵自测军团完成首回合出征");
        var withdrawingArmy = withdrawal.State.Armies.Last();
        Require(!withdrawal.WithdrawArmy(withdrawingArmy.Id, withdrawalSource.Id), "出征当回合不能重复下达撤兵命令");
        Require(withdrawal.EndTurn(), "撤兵前推进到下一回合");
        var returnGarrisonBefore = withdrawalSource.Garrison; var returnFoodBefore = withdrawal.State.Resources.Food; var returningSoldiers = withdrawingArmy.Soldiers; var returningFood = withdrawingArmy.Food;
        Require(withdrawal.WithdrawArmy(withdrawingArmy.Id, withdrawalSource.Id), "下一回合可选择己方城市撤兵");
        Require(withdrawingArmy.Status == "withdrawn" && withdrawalSource.Garrison == returnGarrisonBefore + returningSoldiers && withdrawal.State.Resources.Food == returnFoodBefore + returningFood && withdrawalCommander.InitialState.CityId == withdrawalSource.Id && withdrawalCommander.InitialState.Status == "serving", "撤兵后兵力归城、军粮归入势力府库");

        var campaign = new GameRuntime(scenario);
        var source = campaign.State.Cities.First(item => item.OwnerFactionId == campaign.State.PlayerFactionId && campaign.State.Roads.Any(road => road.TravelDays > 30 && ((road.FromCityId == item.Id && campaign.City(road.ToCityId)?.OwnerFactionId != campaign.State.PlayerFactionId) || (road.ToCityId == item.Id && campaign.City(road.FromCityId)?.OwnerFactionId != campaign.State.PlayerFactionId))));
        var road = campaign.State.Roads.First(item => item.TravelDays > 30 && ((item.FromCityId == source.Id && campaign.City(item.ToCityId)?.OwnerFactionId != campaign.State.PlayerFactionId) || (item.ToCityId == source.Id && campaign.City(item.FromCityId)?.OwnerFactionId != campaign.State.PlayerFactionId)));
        var targetId = road.FromCityId == source.Id ? road.ToCityId : road.FromCityId;
        var campaignTargetFactionId = campaign.City(targetId)!.OwnerFactionId!;
        var commander = campaign.PlayerOfficers().First(item => item.InitialState.CityId == source.Id && item.InitialState.Status == "serving");
        campaign.State.Diplomacy.First(item => item.FactionId == campaignTargetFactionId).Treaties["truce"] = 2;
        Require(!campaign.CreateExpedition(source.Id, targetId, commander.Profile.Id, 3000, 5000, "standard", "steady-advance", [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, "fortify-camp"), "停战阻止玩家出征");
        campaign.State.Diplomacy.First(item => item.FactionId == campaignTargetFactionId).Treaties.Remove("truce");
        Require(campaign.CreateExpedition(source.Id, targetId, commander.Profile.Id, 3000, 5000, "standard", "steady-advance", [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, "fortify-camp"), "军团编成与出征");
        var army = campaign.State.Armies.Last();
        Require(army.LastMarchTurn == campaign.State.Turn && army.RemainingDays == army.TotalDays - 30, "出征后在回合内立即行军");
        Require(!campaign.MarchArmy(army.Id), "同一军团每回合只能行军一次");
        var remainingDays = army.RemainingDays;
        Require(campaign.EndTurn() && army.RemainingDays == remainingDays, "月末不重复推进己方军团");
        Require(campaign.MarchArmy(army.Id) && campaign.State.PendingBattle is not null, "新回合继续行军并抵达战场");
        var pending = campaign.State.PendingBattle!;
        var forestZone = pending.TerrainZones.FirstOrDefault(item => item.Type == "forest");
        Require(forestZone is not null && pending.TerrainZones.Any(item => item.Type == "hill")
            && BattleCalculator.LocalMoveMultiplier(pending, "cavalry", forestZone.X, forestZone.Y)
                < BattleCalculator.LocalMoveMultiplier(pending, "infantry", forestZone.X, forestZone.Y), "大战场生成局部树林与坡地，骑兵在树林中减速更明显");
        Require(pending.Groups.Count(item => item.Side == "attacker") == BattleCalculator.ExpectedGroupCount(pending.AttackerBefore), "兵力按人数展开战斗队");
        Require(BattleCalculator.ExpectedGroupCount(400) == 1 && BattleCalculator.ExpectedGroupCount(3700) == 7, "小股守军与大军显示数量明显区分");
        Require(pending.Groups.Where(item => item.Side == "attacker" && item.TroopType == "archers").All(item => item.Depth == 2 && item.MaximumRange == 300), "弓兵后排与射程规则");
        var targetCity = campaign.City(targetId)!;
        Require(!BattleCalculator.ShouldDefenderSortie(1000, 1599) && BattleCalculator.ShouldDefenderSortie(1000, 1600), "守军达到1.6倍兵力优势才主动出城");
        var sortieBattle = BattleCalculator.Create(campaign.State, army, targetCity);
        sortieBattle.AttackerBefore = Math.Max(1, sortieBattle.DefenderBefore / 2);
        BattleCalculator.Generate(campaign.State, sortieBattle);
        Require(sortieBattle.DefenderSortie && sortieBattle.DefenderSortieUsed
            && sortieBattle.Groups.Where(item => item.Side == "defender" && item.TroopType is "infantry" or "spears" or "cavalry").All(item => item.IsSortie)
            && sortieBattle.Groups.Where(item => item.Side == "defender" && item.TroopType == "archers").All(item => !item.IsSortie), "优势守军步枪骑出城且弓兵留城守墙");
        Require(sortieBattle.Timeline.Any(item => item.Stage == "守军出击") && sortieBattle.DefenderSortieSummary.Contains("开城主动迎战"), "守军出城决策进入实时事件与战报摘要");
        var steadyBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "fortify-camp", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        var repeatBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "fortify-camp", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(steadyBattle.PhaseResults.Count == 1 && steadyBattle.PhaseResults[0].Stage == "实时交战", "战斗按实时接敌生成最终汇总结算");
        Require(BattleSignature(steadyBattle) == BattleSignature(repeatBattle), "同输入战斗演算完全确定");
        Require(steadyBattle.PhaseResults.All(phase => phase.Explanation.Contains("实时结算")) && steadyBattle.Timeline.Where(item => item.Action == "damage").Sum(item => item.Losses) == steadyBattle.AttackerLosses + steadyBattle.DefenderLosses, "实时事件与最终损耗一致");
        var aggressiveBattle = CreateBattleVariant(campaign.State, army, targetCity, "aggressive", "steady-advance", "fortify-camp", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(aggressiveBattle.Stance == "aggressive" && aggressiveBattle.PhaseResults[0].Explanation.Contains("激进姿态") && BattleCalculator.StanceEffectSummary("aggressive") != BattleCalculator.StanceEffectSummary("standard"), "军团姿态进入实时攻防公式");
        var wedgeBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "fortify-camp", "wedge", new Dictionary<string, string> { ["infantry"] = "assault-column", ["archers"] = "rear-double" });
        Require(wedgeBattle.PhaseResults[0].PowerRatio != steadyBattle.PhaseResults[0].PowerRatio, "全军阵型与兵种军令改变实时战果");
        var volleyBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "arrow-volley", "fortify-camp", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(volleyBattle.PrimaryTactic == "arrow-volley" && BattleCalculator.TacticEffectSummary("arrow-volley") != BattleCalculator.TacticEffectSummary("steady-advance"), "主战术进入实时演算");
        var fallbackBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "cavalry-charge", "shield-wall", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(fallbackBattle.BackupTriggered && fallbackBattle.EffectiveTactic == "shield-wall" && fallbackBattle.PhaseResults[0].Tactic == "shield-wall" && fallbackBattle.DecisionSummary.Contains("兵种条件不足"), "主战术条件不足时备用战术实时接管");
        var tacticIds = new[] { "steady-advance", "shield-wall", "feigned-retreat", "night-raid", "fire-attack", "encirclement", "arrow-volley", "cavalry-charge", "fortify-camp", "cut-supply", "siege-ladders", "undermine-walls" };
        Require(tacticIds.All(id => BattleCalculator.TacticEffectSummary(id) != "无额外战术修正"), "12种战术均提供明确收益与风险说明");
        Require(campaign.ConfigurePendingBattle("goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" }), "战前布阵配置");
        Require(campaign.StartPendingBattle(), "战斗开始演算");
        Require(campaign.State.PendingBattle!.OfficerUnits.Count == campaign.State.PendingBattle.AttackerOfficerIds.Distinct().Count() + campaign.State.PendingBattle.DefenderOfficerIds.Distinct().Count()
            && campaign.State.PendingBattle.OfficerUnits.All(item => item.SpriteId.Contains("generic") || item.SpriteId is "liu-bei" or "guan-yu" or "zhang-fei" or "lu-bu"), "双方参战武将全部展开为独立骑将单位");
        Require(BattleCalculator.MountedOfficerCombatPower("lu-bu", 92, 100, 42) > BattleCalculator.MountedOfficerCombatPower("generic-commander", 70, 70, 70)
            && campaign.State.PendingBattle.OfficerUnits.All(item => item.CombatPower >= 3.5), "骑将战力由属性计算且历史名将拥有专属强度");
        var commandedGroup = campaign.State.PendingBattle!.Groups.First(item => item.Side == campaign.State.PendingBattle.PlayerSide && item.FinalSoldiers > 0);
        var commandedTarget = campaign.State.PendingBattle.Groups.First(item => item.Side != campaign.State.PendingBattle.PlayerSide && item.FinalSoldiers > 0);
        Require(campaign.IssueBattleCommand([commandedGroup.Id], "attack", commandedTarget.Id)
            && commandedGroup.CommandMode == "attack" && commandedGroup.CommandTargetGroupId == commandedTarget.Id, "RTS指定敌军集火命令进入实时模拟");
        var commandStartX = commandedGroup.X;
        Require(campaign.IssueBattleCommand([commandedGroup.Id], "move", destinationX: commandedGroup.X - 60, destinationY: commandedGroup.Y + 30), "RTS地面移动命令");
        campaign.AdvancePendingBattle(.5);
        Require(commandedGroup.X < commandStartX && commandedGroup.CommandMode == "move", "RTS军团按移动目标持续推进");
        Require(campaign.IssueBattleCommand([commandedGroup.Id], "hold") && commandedGroup.CommandMode == "hold"
            && campaign.State.PendingBattle.Timeline.Any(item => item.Action == "command"), "RTS原地固守与军令事件");
        BattleCalculator.RunToCompletion(campaign.State, campaign.State.PendingBattle!);
        Require(campaign.State.PendingBattle!.PhaseResults.Count == 1 && campaign.State.PendingBattle.Status == "resolved", "实时战斗倒计时结束并生成正式结果");
        Require(campaign.State.PendingBattle!.Timeline.Any(item => item.Action == "volley")
            && (campaign.State.PendingBattle.DefenderSortieUsed
                ? campaign.State.PendingBattle.Timeline.Any(item => item.Stage == "守军出击")
                : campaign.State.PendingBattle.Timeline.Any(item => item.Action == "structure")), "远程与守军出城或攻城阶段事件");
        Require(campaign.State.PendingBattle.Timeline.Any(item => item.Action is "officer-charge" or "officer-command")
            && campaign.State.PendingBattle.OfficerUnits.Any(item => item.TotalDamage > 0), "骑乘武将动画事件与属性杀伤进入实时战斗");
        Require(campaign.CompletePendingBattle(), "战斗时间线结算");
        Require(campaign.State.BattleReports.Count > 0, "行军与战斗结算");
        var completedReport = campaign.State.BattleReports.Last();
        Require(completedReport.PhaseResults.Count == 1 && completedReport.PrimaryTactic == "steady-advance" && completedReport.PhaseResults.Sum(phase => phase.AttackerLosses) == completedReport.AttackerLosses, "战报持久保存实时结果与战术决策");

        var interception = new GameRuntime(scenario);
        var interceptSource = interception.State.Cities.First(item => item.OwnerFactionId == interception.State.PlayerFactionId && item.Garrison >= 3000 && interception.PlayerOfficers().Any(officer => officer.InitialState.CityId == item.Id && officer.InitialState.Status == "serving"));
        var enemySource = interception.State.Cities.First(item => item.OwnerFactionId != interception.State.PlayerFactionId && interception.State.Officers.Any(officer => officer.InitialState.FactionId == item.OwnerFactionId && officer.InitialState.CityId == item.Id && officer.InitialState.Status == "serving"));
        var enemyCommander = interception.State.Officers.First(item => item.InitialState.FactionId == enemySource.OwnerFactionId && item.InitialState.CityId == enemySource.Id && item.InitialState.Status == "serving");
        var enemyArmy = new ArmyData
        {
            Id = "self-test-enemy-army", FactionId = enemySource.OwnerFactionId!, SourceCityId = enemySource.Id, TargetCityId = interceptSource.Id,
            CommanderId = enemyCommander.Profile.Id, Soldiers = 3700, Food = 5000, Training = enemySource.Training, Morale = 70,
            Composition = new Dictionary<string, int> { ["infantry"] = 2200, ["archers"] = 1000, ["cavalry"] = 500 },
            RemainingDays = 15, TotalDays = 60, Status = "marching",
        };
        enemyCommander.InitialState.Status = "deployed"; enemyCommander.InitialState.ArmyId = enemyArmy.Id;
        interception.State.Armies.Add(enemyArmy);
        var interceptCommander = interception.PlayerOfficers().First(item => item.InitialState.CityId == interceptSource.Id && item.InitialState.Status == "serving");
        var cityOwnerBeforeInterception = interceptSource.OwnerFactionId;
        Require(interception.CreateExpedition(interceptSource.Id, interceptSource.Id, interceptCommander.Profile.Id, 3000, 3000, "standard", "encirclement", [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, "shield-wall", [], enemyArmy.Id), "可选中敌方军团并发起专门拦截");
        var fieldPending = interception.State.PendingBattle ?? throw new InvalidOperationException("[GodotSelfTest] FAIL · 未创建军团野战");
        Require(fieldPending.BattleType == "field" && fieldPending.DefenderArmyId == enemyArmy.Id && fieldPending.DefenderBefore == 3700, "拦截目标绑定敌军团并按真实兵力展开野战");
        var shallowZone = fieldPending.TerrainZones.First(item => item.Type == "shallow");
        Require(BattleCalculator.LocalMoveMultiplier(fieldPending, "infantry", shallowZone.X, shallowZone.Y) < 1
            && BattleCalculator.LocalMoveMultiplier(fieldPending, "cavalry", shallowZone.X, shallowZone.Y)
                < BattleCalculator.LocalMoveMultiplier(fieldPending, "infantry", shallowZone.X, shallowZone.Y), "野战浅滩降低全军机动且骑兵受影响最大");
        Require(interception.StartPendingBattle(), "军团野战开始实时演算");
        BattleCalculator.RunToCompletion(interception.State, fieldPending);
        Require(fieldPending.Status == "resolved" && fieldPending.Timeline.All(item => item.Action != "structure") && fieldPending.WallBefore == 0 && fieldPending.GateBefore == 0, "军团野战不读取城墙城门且无攻城事件");
        Require(interception.CompletePendingBattle(), "军团野战完成结算");
        var fieldReport = interception.State.BattleReports.Last();
        Require(fieldReport.BattleType == "field" && !fieldReport.CityCaptured && interceptSource.OwnerFactionId == cityOwnerBeforeInterception && interception.State.Armies.Where(item => item.Id is "self-test-enemy-army" || item.TargetArmyId == "self-test-enemy-army").All(item => item.Status is "field-victory" or "field-defeat"), "野战只结算两军并回营，不改变城池归属");

        var exchange = new GameRuntime(scenario);
        var exchangeFaction = exchange.State.Factions.First(item => item.Id != exchange.State.PlayerFactionId);
        var playerCaptive = exchange.PlayerOfficers().First(item => item.InitialState.Status == "serving");
        var targetCaptive = exchange.State.Officers.First(item => item.InitialState.FactionId == exchangeFaction.Id && item.InitialState.Status == "serving");
        playerCaptive.InitialState.Status = "captive"; playerCaptive.InitialState.CityId = exchange.State.Cities.First(item => item.OwnerFactionId == exchangeFaction.Id).Id;
        targetCaptive.InitialState.Status = "captive"; targetCaptive.InitialState.CityId = exchange.State.Cities.First(item => item.OwnerFactionId == exchange.State.PlayerFactionId).Id;
        var exchangeRelation = exchange.State.Diplomacy.First(item => item.FactionId == exchangeFaction.Id); exchangeRelation.Relation = 100; exchangeRelation.Trust = 100;
        Require(exchange.CanExchangeCaptives(exchangeFaction.Id), "双方俘虏交换条件");
        Require(exchange.ProposeDiplomacy(exchangeFaction.Id, "captive-exchange", 0), "交换俘虏提案");
        Require(playerCaptive.InitialState.Status == "serving" && targetCaptive.InitialState.Status == "serving" && !exchange.HasTreaty(exchangeFaction.Id, "captive-exchange"), "交换俘虏即时结算且不生成条约");
        GD.Print($"[GodotSelfTest] PASS · {runtime.State.Cities.Count}城 / {runtime.State.Officers.Count}将 / {runtime.State.Events.Count}事件 / 第{runtime.State.Turn}回合");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException($"[GodotSelfTest] FAIL · {label}");
        GD.Print($"[GodotSelfTest] OK · {label}");
    }

    private static PendingBattleData CreateBattleVariant(GameSession state, ArmyData army, CityData city, string stance, string tactic, string backup, string formation, Dictionary<string, string> orders)
    {
        var originalStance = army.Stance;
        var originalTactic = army.Tactic;
        var originalBackup = army.BackupTactic;
        try
        {
            army.Stance = stance;
            army.Tactic = tactic;
            army.BackupTactic = backup;
            var battle = BattleCalculator.Create(state, army, city);
            BattleCalculator.Configure(battle, formation, orders);
            BattleCalculator.Generate(state, battle);
            BattleCalculator.RunToCompletion(state, battle);
            return battle;
        }
        finally
        {
            army.Stance = originalStance;
            army.Tactic = originalTactic;
            army.BackupTactic = originalBackup;
        }
    }

    private static string BattleSignature(PendingBattleData battle) => string.Join('|', battle.PhaseResults.Select(phase => $"{phase.Stage}:{phase.Tactic}:{phase.AttackerAfter}:{phase.DefenderAfter}:{phase.PowerRatio:F3}:{phase.WallDamage}:{phase.GateDamage}:{phase.InnerDamage}"));
}
