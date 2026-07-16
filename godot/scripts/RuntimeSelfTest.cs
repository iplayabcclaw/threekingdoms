using Godot;

namespace ThreeKingdomsSimulator.Godot;

public static class RuntimeSelfTest
{
    public static void Run(ScenarioData scenario)
    {
        var runtime = new GameRuntime(scenario);
        Require(runtime.State.Cities.Count == 33, "剧本城市数量");
        Require(runtime.State.Cities.Sum(city => city.Population) == 2_502_000 && runtime.State.Cities.All(city => city.Population is >= 43_000 and <= 128_000), "战乱时期城池人口规模");
        Require(runtime.State.Cities.Sum(city => city.Garrison) == 197_100 && runtime.State.Cities.All(city => city.Garrison is >= 3_800 and <= 8_100), "初始守军规模");
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
        var rangeArcher = new BattleUnitGroupData { X = 100, Y = 100, MinimumRange = 80, MaximumRange = 300 };
        Require(!BattleCalculator.IsWithinEffectiveRange(rangeArcher, new BattleUnitGroupData { X = 150, Y = 100 })
            && BattleCalculator.IsWithinEffectiveRange(rangeArcher, new BattleUnitGroupData { X = 260, Y = 100 }), "弓兵最小射程阻止贴身无惩罚射击");
        Require(BattleCalculator.EngagementCapacity("archers") == 5 && BattleCalculator.EngagementCapacity("infantry") == 3 && BattleCalculator.EngagementCapacity("siege") == 2, "接战宽度限制同时围攻数量");
        Require(BattleCalculator.DefensiveDamageMultiplier("cautious", "shield-wall", "shield", "shield-line", "正面接战")
            < BattleCalculator.DefensiveDamageMultiplier("aggressive", "steady-advance", "wedge", "assault-column", "正面接战"), "姿态、战术、阵型与军令实时改变承伤");
        Require(global::Godot.FileAccess.FileExists(GameTheme.EmbeddedFontPath) && GD.Load<Font>(GameTheme.EmbeddedFontPath) is not null, "内置简体中文字体可加载");

        Require(scenario.Factions.Count == 16 && scenario.Factions.All(faction => scenario.Cities.Any(city => city.OwnerFactionId == faction.Id)), "新游戏提供16个可选有城势力");
        var alternateFaction = scenario.Factions.First(item => item.Id != scenario.PlayerFactionId);
        var alternate = new GameRuntime(scenario, new NewGameOptions { PlayerFactionId = alternateFaction.Id, Difficulty = "hard", AutoSaveFrequency = "quarterly" });
        var alternateExpected = GameSession.PreviewInitialResources(scenario, alternateFaction.Id, "hard");
        Require(alternate.State.PlayerFactionId == alternateFaction.Id && alternate.State.Difficulty == "hard" && alternate.State.AutoSaveFrequency == "quarterly", "新游戏势力、难度与自动存档设置写入会话");
        Require(alternate.State.Resources.Gold == alternateExpected.Gold && alternate.State.Resources.Food == alternateExpected.Food && !alternate.State.FactionTreasuries.ContainsKey(alternateFaction.Id) && alternate.State.FactionTreasuries.ContainsKey(scenario.PlayerFactionId), "改选势力后中央府库归属正确");
        var standardResources = GameSession.PreviewInitialResources(scenario, scenario.PlayerFactionId, "standard");
        Require(standardResources.Gold == scenario.Cities.Where(item => item.OwnerFactionId == scenario.PlayerFactionId).Sum(item => item.Gold)
            && standardResources.Food == scenario.Cities.Where(item => item.OwnerFactionId == scenario.PlayerFactionId).Sum(item => item.Food), "初始势力府库按所辖城池库存汇总");
        var foodBalance = new GameRuntime(scenario);
        var foodCities = foodBalance.State.Cities.Where(item => item.OwnerFactionId == foodBalance.State.PlayerFactionId).ToList();
        var baseFoodIncome = foodCities.Sum(item => foodBalance.CityMonthlyForecast(item).FoodIncome);
        var baseFoodUpkeep = foodCities.Sum(item => foodBalance.CityMonthlyForecast(item).FoodUpkeep);
        var facilityTestCity = foodCities.First(item => item.Facilities.Count == 0);
        var facilityFoodBefore = foodBalance.CityMonthlyForecast(facilityTestCity).FoodIncome;
        facilityTestCity.Facilities.Add(new FacilityInstanceData { Id = "self-test-irrigation", DefinitionId = "irrigation", Level = 1, Condition = 100 });
        var facilityFoodGain = foodBalance.CityMonthlyForecast(facilityTestCity).FoodIncome - facilityFoodBefore;
        Require(baseFoodIncome >= baseFoodUpkeep / 2 && baseFoodIncome < baseFoodUpkeep
            && facilityFoodGain == GameRuntime.FoodFacilityIncomePerLevel
            && Math.Abs(baseFoodIncome + facilityFoodGain * foodCities.Count - baseFoodUpkeep) <= baseFoodUpkeep / 10,
            "初始自然产粮承担主要维护且每城一座一级粮食设施后接近平衡");
        Require(alternate.State.Diplomacy.Count == scenario.Factions.Count - 1 && alternate.State.Diplomacy.All(item => item.FactionId != alternateFaction.Id), "改选势力后外交关系重新初始化");
        Require(!SaveService.ShouldWriteAuto(new GameSession { Turn = 2, AutoSaveFrequency = "quarterly" }) && SaveService.ShouldWriteAuto(new GameSession { Turn = 4, AutoSaveFrequency = "quarterly" }) && !SaveService.ShouldWriteAuto(new GameSession { Turn = 13, AutoSaveFrequency = "off" }), "自动存档频率规则");
        var legacyDefaults = GameSession.Create(scenario);
        legacyDefaults.SchemaVersion = 3; legacyDefaults.Difficulty = ""; legacyDefaults.AutoSaveFrequency = ""; legacyDefaults.NineCityControlMonths = -1; legacyDefaults.EnsureDomesticDefaults();
        Require(legacyDefaults.SchemaVersion == GameSession.CurrentSchemaVersion && legacyDefaults.Difficulty == "standard" && legacyDefaults.AutoSaveFrequency == "monthly" && legacyDefaults.NineCityControlMonths == 0, "旧档补齐新游戏与胜利进度默认值");
        Require(GameSession.CurrentSchemaVersion == 9
            && legacyDefaults.Cities.All(item => item.CityLevel is >= GameRuntime.MinCityLevel and <= GameRuntime.MaxCityLevel)
            && legacyDefaults.Officers.All(item => item.InitialState.Level is >= 1 and <= 20 && item.Profile.GrowthPlan.Count == 23 && item.Profile.AbilityPotential.Leadership >= item.Profile.Abilities.Leadership && item.InitialState.CourtOfficeId is not null),
            "schema v9 包含城池升级、武将成长、朝堂职位、武将调动与单城治理状态");
        Require(legacyDefaults.Cities.All(item => item.CityLevel == 1 && item.FacilitySlots == 4)
            && Enumerable.Range(1, 8).Select(GameRuntime.FacilitySlotsForCityLevel).SequenceEqual(new[] { 4, 5, 5, 6, 6, 7, 7, 8 }),
            "城池等级与4/5/5/6/6/7/7/8建造位置规则");
        Require(typeof(GameSession).GetProperty("Automation") is null
            && typeof(GameSession).GetProperty("AutoEvolution") is null
            && typeof(GameRuntime).GetMethod("ConfigureAutomation") is null
            && typeof(GameRuntime).GetMethod("ConfigureAutoEvolution") is null,
            "全局玩家托管与全 AI 演进状态及运行入口已移除");
        Require(typeof(ResourceData).GetProperty("Equipment") is null
            && typeof(GameRuntime).GetMethod("TransferTreasury") is null
            && !GameRuntime.FacilityCatalog.ContainsKey("workshop"),
            "独立军备资源、工坊产出与府库调拨入口已移除");
        Require(legacyDefaults.Officers.All(item => item.InitialState.Health == 100), "武将战略健康固定保持100");

        var resourceForecast = new GameRuntime(scenario);
        var forecastBefore = new ResourceData
        {
            Gold = resourceForecast.State.Resources.Gold,
            Food = resourceForecast.State.Resources.Food,
            Prestige = resourceForecast.State.Resources.Prestige,
        };
        var forecastDelta = resourceForecast.PreviewEndTurnResourceDelta();
        Require(forecastDelta.Gold >= 300, "开局月度金钱收入与建筑成本处于同一量级");
        Require(resourceForecast.EndTurn() &&
            resourceForecast.State.Resources.Gold - forecastBefore.Gold == forecastDelta.Gold &&
            resourceForecast.State.Resources.Food - forecastBefore.Food == forecastDelta.Food &&
            resourceForecast.State.Resources.Prestige - forecastBefore.Prestige == forecastDelta.Prestige,
            "右上角资源预估与实际月末结算一致");

        var recruitment = new GameRuntime(scenario);
        var recruiter = recruitment.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving");
        var recruitmentTarget = recruitment.State.Officers.First(item => item.InitialState.FactionId != recruitment.State.PlayerFactionId && item.InitialState.Status == "serving");
        recruitmentTarget.InitialState.Loyalty = 59;
        recruiter.InitialState.OfficeRank = 0; recruiter.InitialState.GrowthBonuses = new OfficerAbilitiesData(); recruiter.Profile.Abilities.Charisma = 30; recruiter.Profile.Abilities.Intelligence = 70; recruiter.Profile.Abilities.Politics = 70;
        var lowCharismaChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id);
        recruiter.Profile.Abilities.Charisma = 90;
        var subversionChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id);
        Require(recruitment.RecruitmentMethod(recruitmentTarget.Profile.Id) == "subversion" && subversionChance > lowCharismaChance && subversionChance <= 95, "低忠敌将只能通过策反招募，成功率随执行者变化");
        recruitmentTarget.InitialState.Loyalty = 60;
        Require(!recruitment.IsRecruitmentCandidate(recruitmentTarget.Profile.Id) && recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id) == 0, "忠诚60的敌将不能选择策反");
        recruitmentTarget.InitialState.Loyalty = 59;
        recruitmentTarget.InitialState.Status = "free"; recruitmentTarget.InitialState.FactionId = null; recruitment.State.DiscoveredOfficerIds.Add(recruitmentTarget.Profile.Id);
        var directChance = recruitment.RecruitmentChance(recruitmentTarget.Profile.Id, recruiter.Profile.Id);
        Require(directChance == subversionChance, "策反不再提供额外成功率加成");

        var captiveRecruitment = new GameRuntime(scenario);
        var captiveActor = captiveRecruitment.PlayerOfficers().First(item => item.InitialState.Status == "serving");
        var remoteCaptive = captiveRecruitment.State.Officers.First(item => item.InitialState.FactionId != captiveRecruitment.State.PlayerFactionId && item.InitialState.Status == "serving");
        remoteCaptive.InitialState.Status = "captive";
        remoteCaptive.InitialState.CityId = captiveRecruitment.State.Cities.First(item => item.OwnerFactionId != captiveRecruitment.State.PlayerFactionId).Id;
        Require(!captiveRecruitment.IsRecruitmentCandidate(remoteCaptive.Profile.Id)
            && captiveRecruitment.RecruitmentChance(remoteCaptive.Profile.Id, captiveActor.Profile.Id) == 0,
            "其他势力扣押的俘虏不能跨地图招降");
        remoteCaptive.InitialState.CityId = captiveActor.InitialState.CityId;
        var distantActor = captiveRecruitment.PlayerOfficers().First(item => item.InitialState.Status == "serving" && item.InitialState.CityId != captiveActor.InitialState.CityId);
        Require(captiveRecruitment.RecruitmentMethod(remoteCaptive.Profile.Id) == "captive"
            && captiveRecruitment.RecruitmentChance(remoteCaptive.Profile.Id, captiveActor.Profile.Id) > 0
            && captiveRecruitment.RecruitmentChance(remoteCaptive.Profile.Id, distantActor.Profile.Id) == 0
            && !captiveRecruitment.RecruitOfficer(remoteCaptive.Profile.Id, distantActor.Profile.Id, "reserve"),
            "我方扣押的俘虏只能由同城在职武将劝降");

        var governorAppointment = new GameRuntime(scenario);
        var governorCity = governorAppointment.State.Cities.First(item => item.OwnerFactionId == governorAppointment.State.PlayerFactionId
            && !string.IsNullOrEmpty(item.GovernorId)
            && governorAppointment.PlayerOfficers().Count(officer => officer.InitialState.Status == "serving" && officer.InitialState.CityId == item.Id && officer.InitialState.Appointment != "ruler") >= 2);
        var previousGovernor = governorAppointment.Officer(governorCity.GovernorId)!;
        var replacementGovernor = governorAppointment.PlayerOfficers().First(item => item.InitialState.Status == "serving"
            && item.InitialState.CityId == governorCity.Id && item.Profile.Id != previousGovernor.Profile.Id && item.InitialState.Appointment != "ruler");
        Require(governorAppointment.AppointOfficer(replacementGovernor.Profile.Id, "governor")
            && governorCity.GovernorId == replacementGovernor.Profile.Id && governorCity.GovernorName == replacementGovernor.Profile.Name
            && replacementGovernor.InitialState.Appointment == "governor"
            && previousGovernor.InitialState.Appointment != "governor",
            "任命太守同步替换武将所在城的实际治理者");
        Require(governorAppointment.AppointOfficer(replacementGovernor.Profile.Id, "general")
            && string.IsNullOrEmpty(governorCity.GovernorId) && governorCity.GovernorName == "空缺",
            "实际太守改任其他职责时同步解除城市治理职位");

        var transfer = new GameRuntime(scenario);
        var transferOfficer = transfer.State.Cities
            .Where(item => item.OwnerFactionId == transfer.State.PlayerFactionId && !string.IsNullOrEmpty(item.GovernorId))
            .Select(item => transfer.Officer(item.GovernorId))
            .First(item => item is not null && item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving")!;
        var transferSourceId = transferOfficer.InitialState.CityId;
        var transferSource = transfer.City(transferSourceId)!;
        var governorIdBeforeMarch = transferSource.GovernorId;
        var transferTarget = transfer.State.Cities.First(item => item.OwnerFactionId == transfer.State.PlayerFactionId && item.Id != transferSourceId && transfer.OfficerTransferDays(transferOfficer.Profile.Id, item.Id) > 0);
        transferOfficer.InitialState.ArmyId = "self-test-army";
        Require(transfer.OfficerTransferDays(transferOfficer.Profile.Id, transferTarget.Id) == 0 && !transfer.TransferOfficer(transferOfficer.Profile.Id, transferTarget.Id), "军团中的武将不能单独发起城池调动");
        transferOfficer.InitialState.ArmyId = null;
        var transferRoad = transfer.State.Roads.First(item => (item.FromCityId == transferSourceId && item.ToCityId == transferTarget.Id) || (item.ToCityId == transferSourceId && item.FromCityId == transferTarget.Id));
        transferRoad.TravelDays = 64; transfer.State.Roads.RemoveAll(item => item.Id != transferRoad.Id); transfer.State.Events.Clear();
        Require(!transfer.TransferOfficer(transferOfficer.Profile.Id, transferSourceId) && transfer.TransferOfficer(transferOfficer.Profile.Id, transferTarget.Id) && transferOfficer.InitialState.Status == "marching" && transferOfficer.InitialState.CityId == transferSourceId && transferOfficer.InitialState.TravelRemainingDays == 64, "武将调动拒绝原地命令并写入路线与剩余日数");
        Require(transferSource.GovernorId == governorIdBeforeMarch && transferSource.GovernorName == transferOfficer.Profile.Name, "太守跨城调动期间保留原城太守职位");
        Require(transfer.EndTurn() && transferOfficer.InitialState.Status == "marching" && transferOfficer.InitialState.TravelRemainingDays == 34 && transferOfficer.InitialState.CityId == transferSourceId, "武将调动每月推进30日且途中不能在目标城任职");
        Require(transfer.EndTurn() && transfer.EndTurn() && transferOfficer.InitialState.Status == "serving" && transferOfficer.InitialState.CityId == transferTarget.Id && transferOfficer.InitialState.TravelRemainingDays == 0, "武将走完道路后抵达目标城并恢复任职");
        Require(transferSource.GovernorId == governorIdBeforeMarch, "太守抵达其他城池后仍保留原任职位");

        var court = new GameRuntime(scenario);
        var chancellor = court.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && item.InitialState.OfficeTrack == "civil");
        chancellor.InitialState.OfficeRank = 3; chancellor.InitialState.CourtOfficeId = string.Empty;
        chancellor.Profile.Traits = ["善政"];
        var courtSalaryBefore = OfficerProgressionRules.Salary(chancellor);
        var courtPoliticsBefore = court.EffectiveAbility(chancellor, "politics", "civil");
        var courtCity = court.State.Cities.First(item => item.OwnerFactionId == court.State.PlayerFactionId && court.CityMonthlyForecast(item).GoldIncome > 0 && court.CityMonthlyForecast(item).FoodIncome > 0);
        var courtForecastBefore = court.CityMonthlyForecast(courtCity);
        Require(court.AppointCourtOffice(chancellor.Profile.Id, "chancellor") && OfficerProgressionRules.Salary(chancellor) == courtSalaryBefore + 100 && court.EffectiveAbility(chancellor, "politics", "civil") == courtPoliticsBefore, "朝堂职位改为势力光环并保留真实俸禄成本");
        var chancellorInfluence = OfficerProgressionRules.FactionCourtInfluence(court.State, court.State.PlayerFactionId);
        var courtForecastAfter = court.CityMonthlyForecast(courtCity);
        Require(chancellorInfluence.GoldIncomeRate > .03 && chancellorInfluence.FoodIncomeRate > .03 && chancellorInfluence.DomesticActionRate > .03
            && courtForecastAfter.GoldIncome > courtForecastBefore.GoldIncome && courtForecastAfter.FoodIncome > courtForecastBefore.FoodIncome
            && OfficerProgressionRules.CourtInfluenceSummary(chancellor, OfficerProgressionRules.CourtOffice("chancellor")!).Contains("善政"), "丞相能力与善政特性转化为全势力钱粮及城务加成");
        var successor = court.PlayerOfficers().First(item => item.Profile.Id != chancellor.Profile.Id && item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && item.InitialState.OfficeTrack == "civil");
        successor.InitialState.OfficeRank = 3; successor.InitialState.CourtOfficeId = string.Empty; successor.Profile.Traits = ["贪财"];
        Require(OfficerProgressionRules.CourtInfluenceSummary(chancellor, OfficerProgressionRules.CourtOffice("chancellor")!) != OfficerProgressionRules.CourtInfluenceSummary(successor, OfficerProgressionRules.CourtOffice("chancellor")!), "同一职位因任职者能力与特性产生不同势力效果");
        Require(!court.AppointCourtOffice(successor.Profile.Id, "chancellor") && chancellor.InitialState.CourtOfficeId == "chancellor" && string.IsNullOrEmpty(successor.InitialState.CourtOfficeId), "已占用的朝堂职位不能直接替换人选");
        Require(!court.AppointCourtOffice(chancellor.Profile.Id, "strategist-general") && chancellor.InitialState.CourtOfficeId == "chancellor", "已任朝堂职位的武将不能同时选择其他职位");
        Require(court.VacateCourtOffice(chancellor.Profile.Id) && court.AppointCourtOffice(successor.Profile.Id, "chancellor") && string.IsNullOrEmpty(chancellor.InitialState.CourtOfficeId) && successor.InitialState.CourtOfficeId == "chancellor", "先卸任后才能重新任命朝堂职位");

        var captiveCourt = new GameRuntime(scenario);
        var captiveCourtOfficer = captiveCourt.PlayerOfficers().First(item => item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving" && item.InitialState.OfficeTrack == "civil");
        captiveCourtOfficer.InitialState.OfficeRank = 3;
        captiveCourtOfficer.InitialState.CourtOfficeId = string.Empty;
        Require(captiveCourt.AppointCourtOffice(captiveCourtOfficer.Profile.Id, "chancellor"), "俘虏朝堂自测前置任命");
        captiveCourtOfficer.InitialState.Status = "captive";
        var captiveInfluence = OfficerProgressionRules.FactionCourtInfluence(captiveCourt.State, captiveCourt.State.PlayerFactionId);
        OfficerProgressionRules.EnsureDefaults(captiveCourtOfficer, captiveCourt.State.Year);
        Require(captiveInfluence.GoldIncomeRate == 0 && captiveInfluence.FoodIncomeRate == 0 && captiveInfluence.DomesticActionRate == 0
            && string.IsNullOrEmpty(captiveCourtOfficer.InitialState.CourtOfficeId),
            "被俘武将立即失去朝堂加成且朝堂职位自动解除");

        var militaryCourt = new GameRuntime(scenario);
        var zhaoYun = militaryCourt.Officer("officer-zhao-yun")!;
        zhaoYun.InitialState.OfficeTrack = "military"; zhaoYun.InitialState.OfficeRank = 3; zhaoYun.InitialState.Status = "serving"; zhaoYun.InitialState.CourtOfficeId = string.Empty;
        Require(militaryCourt.AppointCourtOffice(zhaoYun.Profile.Id, "grand-general")
            && OfficerProgressionRules.CourtBattleMultiplier(militaryCourt.State, militaryCourt.State.PlayerFactionId, "正面接战") > 1.02
            && OfficerProgressionRules.CourtOfficerHealthMultiplier(militaryCourt.State, militaryCourt.State.PlayerFactionId) >= 1.08
            && OfficerProgressionRules.CourtMoraleBonus(militaryCourt.State, militaryCourt.State.PlayerFactionId) >= 4,
            "名将常胜护军通过大将军职位提升全军战力、武将体力与士气");

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
        OfficerProgressionRules.BattleTraitMultiplier(runtime.State, [gaoShun.Profile.Id], new Dictionary<string, string> { [gaoShun.Profile.Id] = "主将" }, new Dictionary<string, int> { ["infantry"] = 8000 }, new Dictionary<string, int> { ["trap-camp"] = 2000 }, "决胜", traitDescriptions);
        Require(traitDescriptions[gaoShun.Profile.Id].Split("陷阵之志").Length - 1 == 1, "同一特性作用于多个战斗阶段时战报只展示一次");
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
        var construction = new GameRuntime(scenario);
        var constructionCity = construction.State.Cities.First(item => item.OwnerFactionId == construction.State.PlayerFactionId);
        var builder = construction.PlayerOfficers().First(item => item.InitialState.CityId == constructionCity.Id && item.InitialState.Status == "serving");
        construction.State.Resources.Gold = Math.Max(construction.State.Resources.Gold, 100_000);
        construction.State.Resources.Food = Math.Max(construction.State.Resources.Food, 100_000);
        Require(construction.BuildFacility(constructionCity.Id, builder.Profile.Id, "market", 2)
            && constructionCity.ConstructionQueue is { DefinitionId: "market", TargetSlotIndex: 2 }
            && !GameRuntime.FacilitiesBySlot(constructionCity).ContainsKey(2),
            "设施开工后绑定所选空地并以建造中状态占位");

        var cancelledUpgrade = new GameRuntime(scenario);
        var cancelledCity = cancelledUpgrade.State.Cities
            .Where(item => item.OwnerFactionId == cancelledUpgrade.State.PlayerFactionId)
            .First(item => cancelledUpgrade.PlayerOfficers().Count(officer => officer.InitialState.CityId == item.Id && officer.InitialState.Status == "serving") >= 2);
        var cancelledBuilders = cancelledUpgrade.PlayerOfficers().Where(item => item.InitialState.CityId == cancelledCity.Id && item.InitialState.Status == "serving").Take(2).ToList();
        cancelledUpgrade.State.Resources.Gold = 100_000; cancelledUpgrade.State.Resources.Food = 100_000;
        var cancelledGold = cancelledUpgrade.State.Resources.Gold; var cancelledFood = cancelledUpgrade.State.Resources.Food; var cancelledActions = cancelledCity.ActionSlots;
        Require(cancelledUpgrade.UpgradeCity(cancelledCity.Id, cancelledBuilders[0].Profile.Id)
            && cancelledCity.ConstructionQueue is { Kind: "city-upgrade", TargetCityLevel: 2, GoldCost: 2400, FoodCost: 800 }
            && cancelledUpgrade.State.Resources.Gold == cancelledGold - 2400 && cancelledUpgrade.State.Resources.Food == cancelledFood - 800,
            "城池升级开工扣除资源并进入独立工程类型");
        cancelledBuilders[0].InitialState.Status = "marching";
        Require(cancelledUpgrade.CityUpgradePauseReason(cancelledCity).Contains("负责武将")
            && cancelledUpgrade.ReassignCityUpgrade(cancelledCity.Id, cancelledBuilders[1].Profile.Id)
            && string.IsNullOrEmpty(cancelledUpgrade.CityUpgradePauseReason(cancelledCity)),
            "负责武将不可用时城池升级暂停并可改派驻城武将");
        Require(cancelledUpgrade.CancelCityUpgrade(cancelledCity.Id)
            && cancelledUpgrade.State.Resources.Gold == cancelledGold && cancelledUpgrade.State.Resources.Food == cancelledFood
            && cancelledCity.ActionSlots == cancelledActions - 1 && cancelledCity.ConstructionQueue is null,
            "主动取消城池升级全额返还金粮但不返还城务额度");
        var refundedGold = cancelledUpgrade.State.Resources.Gold; var refundedFood = cancelledUpgrade.State.Resources.Food;
        Require(!cancelledUpgrade.CancelCityUpgrade(cancelledCity.Id)
            && cancelledUpgrade.State.Resources.Gold == refundedGold && cancelledUpgrade.State.Resources.Food == refundedFood,
            "重复取消不会重复返还资源");

        var completedUpgrade = new GameRuntime(scenario);
        completedUpgrade.State.Roads.Clear(); completedUpgrade.State.Events.Clear();
        var completedCity = completedUpgrade.State.Cities.First(item => item.OwnerFactionId == completedUpgrade.State.PlayerFactionId);
        var completedBuilder = completedUpgrade.PlayerOfficers().First(item => item.InitialState.CityId == completedCity.Id && item.InitialState.Status == "serving");
        completedUpgrade.State.Resources.Gold = 100_000; completedUpgrade.State.Resources.Food = 100_000;
        var agricultureBeforeUpgrade = completedCity.Agriculture; var commerceBeforeUpgrade = completedCity.Commerce; var defenseBeforeUpgrade = completedCity.Defense; var cultureBeforeUpgrade = completedCity.Culture;
        Require(completedUpgrade.UpgradeCity(completedCity.Id, completedBuilder.Profile.Id)
            && completedUpgrade.EndTurn() && completedCity.ConstructionQueue is { Kind: "city-upgrade", RemainingMonths: 1 }
            && completedUpgrade.EndTurn()
            && completedCity.CityLevel == 2 && completedCity.FacilitySlots == 5
            && completedCity.Agriculture == Math.Min(100, agricultureBeforeUpgrade + 1)
            && completedCity.Commerce == Math.Min(100, commerceBeforeUpgrade + 1)
            && completedCity.Defense == Math.Min(100, defenseBeforeUpgrade + 2)
            && completedCity.Culture == Math.Min(100, cultureBeforeUpgrade + 1),
            "城池升级按工期完成并提升基础属性与解锁第5个位置");

        var maxUpgrade = new GameRuntime(scenario);
        maxUpgrade.State.Roads.Clear(); maxUpgrade.State.Events.Clear();
        var maxCity = maxUpgrade.State.Cities.First(item => item.OwnerFactionId == maxUpgrade.State.PlayerFactionId);
        var maxBuilder = maxUpgrade.PlayerOfficers().First(item => item.InitialState.CityId == maxCity.Id && item.InitialState.Status == "serving");
        maxCity.CityLevel = 7; maxCity.FacilitySlots = GameRuntime.FacilitySlotsForCityLevel(maxCity.CityLevel);
        maxUpgrade.State.Resources.Gold = 100_000; maxUpgrade.State.Resources.Food = 100_000;
        Require(maxUpgrade.UpgradeCity(maxCity.Id, maxBuilder.Profile.Id) && maxCity.ConstructionQueue is { Kind: "city-upgrade" }, "Lv.7城池可开始最高级升级");
        var maxQueue = maxCity.ConstructionQueue!;
        maxQueue.RemainingMonths = 1;
        Require(maxUpgrade.EndTurn() && maxCity.CityLevel == 8 && maxCity.FacilitySlots == 8 && !maxUpgrade.UpgradeCity(maxCity.Id, maxBuilder.Profile.Id),
            "Lv.8解锁第8个位置且不能继续升级");

        var aiUpgrade = new GameRuntime(scenario);
        aiUpgrade.State.Roads.Clear(); aiUpgrade.State.Events.Clear();
        var aiCity = aiUpgrade.State.Cities.First(item => item.OwnerFactionId != aiUpgrade.State.PlayerFactionId
            && aiUpgrade.State.Officers.Any(officer => officer.InitialState.FactionId == item.OwnerFactionId && officer.InitialState.CityId == item.Id && officer.InitialState.Status == "serving"));
        aiCity.Status = "stable"; aiCity.PublicOrder = 80; aiCity.IntegrationMonthsRemaining = 0; aiCity.CityLevel = 1; aiCity.FacilitySlots = 4;
        aiCity.Facilities = GameRuntime.FacilityCatalog.Keys.Take(4).Select((id, index) => new FacilityInstanceData { Id = $"ai-upgrade-{index}", DefinitionId = id, SlotIndex = index }).ToList();
        aiUpgrade.State.FactionTreasuries[aiCity.OwnerFactionId] = new ResourceData { Gold = 100_000, Food = 100_000, Prestige = 20 };
        Require(aiUpgrade.EndTurn() && aiCity.ConstructionQueue is { Kind: "city-upgrade", TargetCityLevel: 2 }, "AI在设施已满且储备充足时使用同规则升级城池");
        Require(domestic.ConfigureCityGovernance(secondCity.Id, "delegated", "agriculture", "granary"), "逐城设置方针委任与城市定位");
        Require(domestic.EndTurn(), "智能内政月度结算");
        Require(domestic.State.Cities.All(item => item.ActionCapacity is >= 1 and <= 4 && item.ActionSlots == item.ActionCapacity), "逐城城务额度独立重置");
        Require(secondCity.LedgerEntries.Any(entry => entry.Category == "command"), "方针委任城在月末自动使用本城额度执行城务");
        Require(domestic.State.Cities.Any(item => item.OwnerFactionId != domestic.State.PlayerFactionId && item.LedgerEntries.Any(entry => entry.Category == "command")), "AI 使用同规则执行城务并写入台账");
        Require(domestic.State.Log.Any(item => item.Category == "ai" && item.Message.Contains("评估")), "AI 多因素评估记录");
        var manualOnly = new GameRuntime(scenario);
        Require(manualOnly.EndTurn() && manualOnly.State.Cities.Where(item => item.OwnerFactionId == manualOnly.State.PlayerFactionId).All(item => item.LedgerEntries.All(entry => entry.Category != "command")), "亲自治理城在月末不会自动执行城务");

        var adaptiveDomestic = new GameRuntime(scenario);
        var adaptiveCity = adaptiveDomestic.State.Cities.First(city => city.OwnerFactionId != adaptiveDomestic.State.PlayerFactionId
            && adaptiveDomestic.State.Roads.Any(road => (road.FromCityId == city.Id && adaptiveDomestic.City(road.ToCityId)?.OwnerFactionId != city.OwnerFactionId)
                || (road.ToCityId == city.Id && adaptiveDomestic.City(road.FromCityId)?.OwnerFactionId != city.OwnerFactionId)));
        adaptiveCity.CityRole = "granary";
        adaptiveCity.RoleTransitionMonths = 0;
        adaptiveDomestic.State.Events.Clear();
        Require(adaptiveDomestic.EndTurn() && adaptiveCity.CityRole == "garrison" && adaptiveCity.RoleTransitionMonths > 0,
            "敌方发展 AI 会在城市转为前线后重新调整城市定位");

        var deployedGovernorRuntime = new GameRuntime(scenario);
        var deployedGovernorCity = deployedGovernorRuntime.State.Cities.First(item => item.OwnerFactionId == deployedGovernorRuntime.State.PlayerFactionId
            && deployedGovernorRuntime.Officer(item.GovernorId) is { } governor
            && deployedGovernorRuntime.EffectiveAbility(governor, "politics", "civil") >= 75);
        var deployedGovernor = deployedGovernorRuntime.Officer(deployedGovernorCity.GovernorId)!;
        var deployedGovernorId = deployedGovernorCity.GovernorId;
        deployedGovernor.InitialState.Status = "deployed";
        deployedGovernor.InitialState.ArmyId = "self-test-governor-army";
        deployedGovernorRuntime.State.Resources.Gold = Math.Max(deployedGovernorRuntime.State.Resources.Gold, 100_000);
        deployedGovernorRuntime.State.Resources.Food = Math.Max(deployedGovernorRuntime.State.Resources.Food, 100_000);
        deployedGovernorRuntime.State.Roads.Clear();
        deployedGovernorRuntime.State.Events.Clear();
        Require(deployedGovernorRuntime.EndTurn()
            && deployedGovernorCity.GovernorId == deployedGovernorId
            && deployedGovernor.InitialState.Status == "deployed"
            && deployedGovernorCity.ActionCapacity >= 3,
            "太守出征后职位与治理额度加成保持不变");

        var strategic = new GameRuntime(scenario);
        var expectedDiplomacyPairs = strategic.State.Factions.Count * (strategic.State.Factions.Count - 1) / 2;
        Require(strategic.State.FactionDiplomacy.Count == expectedDiplomacyPairs, "全势力两两外交状态初始化");
        var strategicEvaluations = strategic.State.Factions
            .Where(item => item.Id != strategic.State.PlayerFactionId)
            .Select(item => new { item.Id, Candidates = strategic.EvaluateStrategicMilitaryCandidates(item.Id) })
            .ToList();
        Require(strategicEvaluations.Max(item => item.Candidates.Count) >= 3, "战略 AI 每月生成多个军事候选");
        var strategicSeed = strategicEvaluations
            .Where(item => strategic.State.Officers.Count(officer => officer.InitialState.FactionId == item.Id) >= 3)
            .OrderByDescending(item => item.Candidates.Count)
            .First();
        var seedCandidate = strategicSeed.Candidates.First();
        var resourceCity = strategic.City(seedCandidate.SourceCityId)!;
        var preparedTarget = strategic.City(seedCandidate.TargetCityId)!;
        foreach (var factionOfficer in strategic.State.Officers.Where(officer => officer.InitialState.FactionId == strategicSeed.Id).Take(3))
        {
            factionOfficer.InitialState.CityId = resourceCity.Id;
            factionOfficer.InitialState.Status = "serving";
            factionOfficer.InitialState.Alive = true;
            factionOfficer.InitialState.ArmyId = null;
        }
        resourceCity.Garrison = 12_000;
        preparedTarget.Garrison = 2_500;
        preparedTarget.Defense = 40;
        strategic.State.FactionTreasuries[strategicSeed.Id].Gold = 100_000;
        strategic.State.FactionTreasuries[strategicSeed.Id].Food = 100_000;
        strategic.FactionDiplomacyBetween(strategicSeed.Id, preparedTarget.OwnerFactionId)!.Treaties.Remove("truce");
        var strategicFaction = new { strategicSeed.Id, Candidates = strategic.EvaluateStrategicMilitaryCandidates(strategicSeed.Id) };
        Require(strategicFaction.Candidates.SequenceEqual(strategicFaction.Candidates.OrderByDescending(item => item.Eligible).ThenByDescending(item => item.Score).ThenBy(item => item.SourceCityId).ThenBy(item => item.TargetCityId)), "军事候选按可执行性与得分排序");
        var resourceCandidate = strategicFaction.Candidates.First(item => item.SourceCityId == seedCandidate.SourceCityId && item.TargetCityId == seedCandidate.TargetCityId);
        var plannedCandidate = resourceCandidate;
        Require(plannedCandidate.Eligible
            && plannedCandidate.Composition.Values.Sum() == plannedCandidate.Soldiers
            && plannedCandidate.Composition.Count is >= 2 and <= 3
            && plannedCandidate.Composition.GetValueOrDefault("infantry") >= 1000
            && plannedCandidate.DeputyIds.Count is >= 1 and <= 2
            && string.IsNullOrEmpty(BattleCalculator.TacticRequirement(plannedCandidate.Tactic, plannedCandidate.Composition)),
            "敌方出征会留守武将并生成副将、混合编制和满足兵种条件的主战术");
        var garrisonBeforeEvaluation = resourceCity.Garrison;
        var factionTreasury = strategic.State.FactionTreasuries[strategicFaction.Id];
        var foodBeforeEvaluation = factionTreasury.Food;
        strategic.EvaluateStrategicMilitaryCandidates(strategicFaction.Id);
        Require(resourceCity.Garrison == garrisonBeforeEvaluation && factionTreasury.Food == foodBeforeEvaluation, "战略评估不会凭空增减兵粮");
        var hardTarget = strategic.City(resourceCandidate.TargetCityId)!;
        var hardTargetGarrison = hardTarget.Garrison;
        hardTarget.Garrison = 50_000;
        var forceBlocked = strategic.EvaluateStrategicMilitaryCandidates(strategicFaction.Id).First(item => item.RoadId == resourceCandidate.RoadId && item.SourceCityId == resourceCandidate.SourceCityId && item.TargetCityId == resourceCandidate.TargetCityId);
        Require(!forceBlocked.Eligible && (forceBlocked.BlockReason.Contains("兵力比") || forceBlocked.BlockReason.Contains("可用兵力不足")), "敌方 AI 不会对兵力明显不足的目标自杀式出征");
        hardTarget.Garrison = hardTargetGarrison;
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

        var staleObjective = new GameRuntime(scenario);
        var staleCity = staleObjective.State.Cities.First(item => item.OwnerFactionId != staleObjective.State.PlayerFactionId
            && staleObjective.State.Officers.Any(officer => officer.InitialState.FactionId == item.OwnerFactionId && officer.InitialState.CityId == item.Id && officer.InitialState.Status == "serving"));
        var staleCommander = staleObjective.State.Officers.First(item => item.InitialState.FactionId == staleCity.OwnerFactionId && item.InitialState.CityId == staleCity.Id && item.InitialState.Status == "serving");
        var staleArmy = new ArmyData
        {
            Id = "self-test-stale-objective", FactionId = staleCity.OwnerFactionId, SourceCityId = staleCity.Id, TargetCityId = staleCity.Id,
            CommanderId = staleCommander.Profile.Id, Soldiers = 2500, Food = 2000, Composition = new Dictionary<string, int> { ["infantry"] = 2500 },
            RemainingDays = 1, TotalDays = 1, LastMarchTurn = 0, Status = "marching",
        };
        staleCommander.InitialState.Status = "deployed";
        staleCommander.InitialState.ArmyId = staleArmy.Id;
        staleObjective.State.Armies.Add(staleArmy);
        staleObjective.State.Roads.Clear();
        staleObjective.State.Events.Clear();
        Require(staleObjective.EndTurn() && staleArmy.Status == "recalled" && staleCommander.InitialState.Status == "serving" && string.IsNullOrEmpty(staleCommander.InitialState.ArmyId),
            "目标已由己方控制时敌方军团会撤回，不会卡在零日行军状态");

        var simulation = new GameRuntime(scenario);
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
        Require(simulation.State.Turn == targetTurn, "60回合长期压力运行");
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
        Require(fullSortie.CreateExpedition(fullSource.Id, fullTargetId, fullCommander.Profile.Id, fullGarrison, Math.Min(1000, fullSortie.State.Resources.Food), [], new Dictionary<string, int> { ["infantry"] = fullGarrison - 500, ["archers"] = 500 }, new Dictionary<string, int> { ["danyang-veterans"] = 500 }), "允许玩家按实际阵型全城出击并编成特殊部队");
        Require(fullSource.Garrison == 0 && fullSortie.State.Armies.Last().Soldiers == fullGarrison, "全城出击真实扣空驻军且军团兵力一致");
        Require(fullSortie.State.Armies.Last().SpecialTroops.GetValueOrDefault("danyang-veterans") == 500,
            "特殊部队按势力、兵种和人数条件正常编成");

        var withdrawal = new GameRuntime(scenario);
        var withdrawalSource = withdrawal.State.Cities.First(item => item.OwnerFactionId == withdrawal.State.PlayerFactionId && withdrawal.State.Roads.Any(road => road.TravelDays > 30 && ((road.FromCityId == item.Id && withdrawal.City(road.ToCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId) || (road.ToCityId == item.Id && withdrawal.City(road.FromCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId))));
        var withdrawalRoad = withdrawal.State.Roads.First(road => road.TravelDays > 30 && ((road.FromCityId == withdrawalSource.Id && withdrawal.City(road.ToCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId) || (road.ToCityId == withdrawalSource.Id && withdrawal.City(road.FromCityId)?.OwnerFactionId != withdrawal.State.PlayerFactionId)));
        var withdrawalTargetId = withdrawalRoad.FromCityId == withdrawalSource.Id ? withdrawalRoad.ToCityId : withdrawalRoad.FromCityId;
        var withdrawalCommander = withdrawal.PlayerOfficers().First(item => item.InitialState.CityId == withdrawalSource.Id && item.InitialState.Status == "serving");
        Require(withdrawal.CreateExpedition(withdrawalSource.Id, withdrawalTargetId, withdrawalCommander.Profile.Id, 3000, 5000, [], new Dictionary<string, int> { ["infantry"] = 2500, ["archers"] = 500 }), "撤兵自测军团完成首回合出征");
        var withdrawingArmy = withdrawal.State.Armies.Last();
        Require(!withdrawal.WithdrawArmy(withdrawingArmy.Id, withdrawalSource.Id), "出征当回合不能重复下达撤兵命令");
        Require(withdrawal.EndTurn(), "撤兵前推进到下一回合");
        var returnGarrisonBefore = withdrawalSource.Garrison; var returnFoodBefore = withdrawal.State.Resources.Food; var returningSoldiers = withdrawingArmy.Soldiers; var returningFood = withdrawingArmy.Food;
        Require(withdrawal.WithdrawArmy(withdrawingArmy.Id, withdrawalSource.Id), "下一回合可选择己方城市撤兵");
        Require(withdrawingArmy.Status == "withdrawn" && withdrawalSource.Garrison == returnGarrisonBefore + returningSoldiers && withdrawal.State.Resources.Food == returnFoodBefore + returningFood && withdrawalCommander.InitialState.CityId == withdrawalSource.Id && withdrawalCommander.InitialState.Status == "serving", "撤兵后兵力归城、军粮归入势力府库");

        var automaticEncounter = new GameRuntime(scenario);
        var automaticArmies = AddHeadOnRoadArmies(automaticEncounter, "automatic");
        Require(automaticEncounter.MarchArmy(automaticArmies.PlayerArmy.Id)
            && automaticEncounter.State.PendingBattle is { BattleType: "field" } automaticField
            && automaticField.DefenderArmyId == automaticArmies.EnemyArmy.Id
            && automaticField.Terrain == "plain",
            "敌对军团在同一道路行军区间相交时自动触发平地野战");
        var automaticSettlement = automaticEncounter.State.PendingBattle!;
        foreach (var group in automaticSettlement.Groups.Where(item => item.Side == "defender"))
            group.FinalSoldiers = Math.Max(1, (int)Math.Round(group.InitialSoldiers * .65));
        automaticSettlement.AttackerAfter = automaticSettlement.Groups.Where(item => item.Side == "attacker").Sum(item => item.FinalSoldiers);
        automaticSettlement.DefenderAfter = automaticSettlement.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
        automaticSettlement.AttackerLosses = automaticSettlement.AttackerBefore - automaticSettlement.AttackerAfter;
        automaticSettlement.DefenderLosses = automaticSettlement.DefenderBefore - automaticSettlement.DefenderAfter;
        automaticSettlement.Result = "victory";
        automaticSettlement.Status = "resolved";
        automaticSettlement.Summary = "自测野战由进攻方获胜。";
        var enemyGarrisonBeforeRetreat = automaticEncounter.State.Cities.Where(item => item.OwnerFactionId == automaticArmies.EnemyArmy.FactionId).Sum(item => item.Garrison);
        var winnerFoodAtSettlement = automaticArmies.PlayerArmy.Food;
        var loserFoodAtSettlement = automaticArmies.EnemyArmy.Food;
        Require(automaticSettlement.EncounterRoadId == automaticArmies.Road.Id && automaticSettlement.EncounterOffsetDays > 0
            && automaticSettlement.EncounterOffsetDays < automaticArmies.Road.TravelDays, "野战结算保存道路上的真实交战点");
        Require(automaticEncounter.CompletePendingBattle(), "道路野战结算胜军与败军去向");
        Require(automaticArmies.PlayerArmy.Status == "marching" && automaticArmies.PlayerArmy.RemainingDays > 0
            && automaticArmies.PlayerArmy.Food == winnerFoodAtSettlement, "野战胜军停在交战点并保留军粮，未瞬移回城");
        Require(automaticArmies.EnemyArmy.Status == "retreating" && automaticArmies.EnemyArmy.RemainingDays > 0
            && automaticArmies.EnemyArmy.RouteRoadIds.Count > 0
            && automaticEncounter.City(automaticArmies.EnemyArmy.TargetCityId)?.OwnerFactionId == automaticArmies.EnemyArmy.FactionId
            && automaticEncounter.State.Cities.Where(item => item.OwnerFactionId == automaticArmies.EnemyArmy.FactionId).Sum(item => item.Garrison) == enemyGarrisonBeforeRetreat
            && automaticArmies.EnemyArmy.Food == loserFoodAtSettlement,
            "野战败军携剩余兵粮沿真实路线撤往距交战点最近的己城，不即时归城");
        var retreatDaysBeforeAdvance = automaticArmies.EnemyArmy.RemainingDays;
        var retreatDestination = automaticEncounter.City(automaticArmies.EnemyArmy.TargetCityId)!;
        automaticEncounter.State.Turn++;
        automaticArmies.PlayerArmy.LastMarchTurn = automaticEncounter.State.Turn;
        Require(automaticEncounter.EndTurn(), "野战败军进入下一月撤退结算");
        Require(automaticArmies.EnemyArmy.Status == "retreating"
                ? automaticArmies.EnemyArmy.RemainingDays < retreatDaysBeforeAdvance
                : automaticArmies.EnemyArmy.Status == "field-defeat" && automaticArmies.EnemyArmy.RemainingDays == 0
                    && automaticArmies.EnemyArmy.Food == 0
                    && automaticEncounter.Officer(automaticArmies.EnemyArmy.CommanderId)?.InitialState is { Status: "serving" } commanderState
                    && commanderState.CityId == retreatDestination.Id,
            "败军每月按道路日数推进，只有实际抵城后才归还兵力与余粮");

        var redirectedEncounter = new GameRuntime(scenario);
        var redirectedArmies = AddHeadOnRoadArmies(redirectedEncounter, "redirected");
        Require(redirectedEncounter.CanOrderArmyIntercept(redirectedArmies.PlayerArmy.Id, redirectedArmies.EnemyArmy.Id, out _)
            && redirectedEncounter.OrderArmyIntercept(redirectedArmies.PlayerArmy.Id, redirectedArmies.EnemyArmy.Id)
            && redirectedArmies.PlayerArmy.TargetArmyId == redirectedArmies.EnemyArmy.Id
            && redirectedEncounter.State.PendingBattle is { BattleType: "field" },
            "已出征己方军团可改令拦截来袭敌军并在共同路段接战");

        var campaign = new GameRuntime(scenario);
        var source = campaign.State.Cities.First(item => item.OwnerFactionId == campaign.State.PlayerFactionId && campaign.State.Roads.Any(road => road.TravelDays > 30 && ((road.FromCityId == item.Id && campaign.City(road.ToCityId)?.OwnerFactionId != campaign.State.PlayerFactionId) || (road.ToCityId == item.Id && campaign.City(road.FromCityId)?.OwnerFactionId != campaign.State.PlayerFactionId))));
        var road = campaign.State.Roads.First(item => item.TravelDays > 30 && ((item.FromCityId == source.Id && campaign.City(item.ToCityId)?.OwnerFactionId != campaign.State.PlayerFactionId) || (item.ToCityId == source.Id && campaign.City(item.FromCityId)?.OwnerFactionId != campaign.State.PlayerFactionId)));
        var targetId = road.FromCityId == source.Id ? road.ToCityId : road.FromCityId;
        var campaignTargetFactionId = campaign.City(targetId)!.OwnerFactionId!;
        var commander = campaign.PlayerOfficers().First(item => item.InitialState.CityId == source.Id && item.InitialState.Status == "serving");
        campaign.State.Diplomacy.First(item => item.FactionId == campaignTargetFactionId).Treaties["truce"] = 2;
        Require(!campaign.CreateExpedition(source.Id, targetId, commander.Profile.Id, 3000, 5000, [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }), "停战阻止玩家出征");
        campaign.State.Diplomacy.First(item => item.FactionId == campaignTargetFactionId).Treaties.Remove("truce");
        var insufficientTacticNotice = string.Empty;
        campaign.Notice += message => insufficientTacticNotice = message;
        Require(campaign.CreateExpedition(source.Id, targetId, commander.Profile.Id, 3000, 5000, [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }), "军团编成与出征不提前选择战术");
        var army = campaign.State.Armies.Last();
        Require(army.LastMarchTurn == campaign.State.Turn && army.RemainingDays == army.TotalDays - 30, "出征后在回合内立即行军");
        Require(!campaign.MarchArmy(army.Id), "同一军团每回合只能行军一次");
        var remainingDays = army.RemainingDays;
        Require(campaign.EndTurn() && army.RemainingDays == remainingDays, "月末不重复推进己方军团");
        Require(campaign.EndTurn() && campaign.State.PendingBattle is not null && army.LastMarchTurn == campaign.State.Turn, "新回合未主动行动的己方军团在月末自动行军并抵达战场");
        var pending = campaign.State.PendingBattle!;
        Require(pending.Terrain == "plain"
            && BattleCalculator.LocalMoveMultiplier(pending, "cavalry", 500, 500) == 1,
            "大战场统一按平地演算且不生成局部地形区");
        Require(pending.Groups.Count(item => item.Side == "attacker") == BattleCalculator.ExpectedGroupCount(pending.AttackerBefore), "兵力按人数展开战斗队");
        Require(BattleCalculator.ExpectedGroupCount(400) == 1 && BattleCalculator.ExpectedGroupCount(3700) == 7, "小股守军与大军显示数量明显区分");
        Require(pending.Groups.Where(item => item.Side == "attacker" && item.TroopType == "archers").All(item => item.Depth == 2 && item.MaximumRange == 300), "弓兵后排与射程规则");
        var targetCity = campaign.City(targetId)!;
        var formationPreview = BattleCalculator.Create(campaign.State, army, targetCity);
        var previewOrders = new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" };
        BattleCalculator.Configure(formationPreview, "goose", previewOrders);
        var goosePreview = string.Join('|', formationPreview.Groups.Where(item => item.Side == formationPreview.PlayerSide).OrderBy(item => item.Id).Select(item => $"{item.Id}:{item.Lane}:{item.Depth}:{item.X:F0}:{item.Y:F0}"));
        BattleCalculator.Configure(formationPreview, "wedge", previewOrders);
        var wedgePreview = string.Join('|', formationPreview.Groups.Where(item => item.Side == formationPreview.PlayerSide).OrderBy(item => item.Id).Select(item => $"{item.Id}:{item.Lane}:{item.Depth}:{item.X:F0}:{item.Y:F0}"));
        BattleCalculator.Configure(formationPreview, "goose", previewOrders);
        var restoredGoosePreview = string.Join('|', formationPreview.Groups.Where(item => item.Side == formationPreview.PlayerSide).OrderBy(item => item.Id).Select(item => $"{item.Id}:{item.Lane}:{item.Depth}:{item.X:F0}:{item.Y:F0}"));
        Require(goosePreview != wedgePreview && goosePreview == restoredGoosePreview, "切换全军阵型会即时重排军团且切回后坐标可重复");
        var localDefenders = campaign.State.Officers.Where(item => item.InitialState.FactionId == targetCity.OwnerFactionId && item.InitialState.CityId == targetCity.Id && item.InitialState.Alive && item.InitialState.Status == "serving").ToList();
        var defenderStatuses = localDefenders.Select(item => item.InitialState.Status).ToList();
        for (var index = 0; index < localDefenders.Count; index++) localDefenders[index].InitialState.Status = "deployed";
        localDefenders[0].InitialState.Status = "marching";
        var leaderlessBattle = BattleCalculator.Create(campaign.State, army, targetCity);
        for (var index = 0; index < localDefenders.Count; index++) localDefenders[index].InitialState.Status = defenderStatuses[index];
        Require(leaderlessBattle.DefenderOfficerIds.Count == 0 && string.IsNullOrEmpty(leaderlessBattle.DefenderCommanderId), "守城只选择本城驻留可战武将，不调用调动中或异地武将");
        var defenderPool = campaign.State.Officers.Where(item => item.InitialState.FactionId == targetCity.OwnerFactionId && item.InitialState.Alive).Take(2).ToList();
        var governorDefender = defenderPool[0];
        targetCity.GovernorId = governorDefender.Profile.Id;
        governorDefender.InitialState.FactionId = targetCity.OwnerFactionId;
        governorDefender.InitialState.CityId = targetCity.Id;
        governorDefender.InitialState.Status = "serving";
        var militaryDefender = defenderPool[1];
        militaryDefender.InitialState.CityId = targetCity.Id;
        militaryDefender.InitialState.Status = "serving";
        var governorAbilities = (governorDefender.Profile.Abilities.Leadership, governorDefender.Profile.Abilities.Might, governorDefender.Profile.Abilities.Intelligence);
        var militaryAbilities = (militaryDefender.Profile.Abilities.Leadership, militaryDefender.Profile.Abilities.Might, militaryDefender.Profile.Abilities.Intelligence);
        governorDefender.Profile.Abilities.Leadership = governorDefender.Profile.Abilities.Might = governorDefender.Profile.Abilities.Intelligence = 1;
        militaryDefender.Profile.Abilities.Leadership = militaryDefender.Profile.Abilities.Might = militaryDefender.Profile.Abilities.Intelligence = 100;
        var meritBattle = BattleCalculator.Create(campaign.State, army, targetCity);
        governorDefender.Profile.Abilities.Leadership = governorAbilities.Leadership;
        governorDefender.Profile.Abilities.Might = governorAbilities.Might;
        governorDefender.Profile.Abilities.Intelligence = governorAbilities.Intelligence;
        militaryDefender.Profile.Abilities.Leadership = militaryAbilities.Leadership;
        militaryDefender.Profile.Abilities.Might = militaryAbilities.Might;
        militaryDefender.Profile.Abilities.Intelligence = militaryAbilities.Intelligence;
        Require(meritBattle.DefenderCommanderId == militaryDefender.Profile.Id, "太守职位不再强制覆盖守城主将能力评估");
        Require(!BattleCalculator.ShouldDefenderSortie(1000, 1599) && BattleCalculator.ShouldDefenderSortie(1000, 1600), "守军达到1.6倍兵力优势才主动出城");
        var sortieBattle = BattleCalculator.Create(campaign.State, army, targetCity);
        sortieBattle.AttackerBefore = Math.Max(1, sortieBattle.DefenderBefore / 2);
        BattleCalculator.Generate(campaign.State, sortieBattle);
        Require(sortieBattle.DefenderSortie && sortieBattle.DefenderSortieUsed
            && sortieBattle.Groups.Where(item => item.Side == "defender" && item.TroopType is "infantry" or "spears" or "cavalry").All(item => item.IsSortie)
            && sortieBattle.Groups.Where(item => item.Side == "defender" && item.TroopType == "archers").All(item => !item.IsSortie), "优势守军步枪骑出城且弓兵留城守墙");
        Require(sortieBattle.Timeline.Any(item => item.Stage == "守军出击") && sortieBattle.DefenderSortieSummary.Contains("开城主动迎战"), "守军出城决策进入实时事件与战报摘要");
        sortieBattle.PlayerSide = "defender";
        var defenseCommandGroup = sortieBattle.Groups.First(item => item.Side == "defender" && item.FinalSoldiers > 0);
        Require(BattleCalculator.IssueRealtimeCommand(sortieBattle, [defenseCommandGroup.Id], "defend-gate") == 1 && defenseCommandGroup.CommandDestinationX == 305
            && BattleCalculator.IssueRealtimeCommand(sortieBattle, [defenseCommandGroup.Id], "inner-city") == 1 && defenseCommandGroup.CommandDestinationX < 230
            && BattleCalculator.IssueRealtimeCommand(sortieBattle, [defenseCommandGroup.Id], "sortie") == 1 && defenseCommandGroup.IsSortie
            && BattleCalculator.IssueRealtimeCommand(sortieBattle, [defenseCommandGroup.Id], "reserve-line") == 1 && !defenseCommandGroup.IsSortie, "玩家守城可增援城门、退守内城、出城突袭与保留预备队");
        var steadyBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        var repeatBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(steadyBattle.PhaseResults.Count == 1 && steadyBattle.PhaseResults[0].Stage == "实时交战", "战斗按实时接敌生成最终汇总结算");
        Require(BattleSignature(steadyBattle) == BattleSignature(repeatBattle), "同输入战斗演算完全确定");
        var timelineTroopLosses = steadyBattle.Timeline.Where(item => item.Action == "damage").Sum(item => item.Losses);
        var settledTroopLosses = steadyBattle.AttackerLosses + steadyBattle.DefenderLosses;
        var groupTroopLosses = steadyBattle.Groups.Sum(item => item.InitialSoldiers - item.FinalSoldiers);
        var timelineLossBreakdown = string.Join('、', steadyBattle.Timeline.Where(item => item.Losses > 0).GroupBy(item => item.Action).Select(group => $"{group.Key}:{group.Sum(item => item.Losses)}"));
        Require(steadyBattle.PhaseResults.All(phase => phase.Explanation.Contains("实时结算")) && timelineTroopLosses == groupTroopLosses && groupTroopLosses == settledTroopLosses, $"实时事件与最终损耗一致（事件{timelineTroopLosses}/分队{groupTroopLosses}/结算{settledTroopLosses}；{timelineLossBreakdown}）");
        var aggressiveBattle = CreateBattleVariant(campaign.State, army, targetCity, "aggressive", "steady-advance", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(aggressiveBattle.Stance == "aggressive" && aggressiveBattle.PhaseResults[0].Explanation.Contains("激进姿态") && BattleCalculator.StanceEffectSummary("aggressive") != BattleCalculator.StanceEffectSummary("standard"), "军团姿态进入实时攻防公式");
        var wedgeBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "steady-advance", "wedge", new Dictionary<string, string> { ["infantry"] = "assault-column", ["archers"] = "rear-double" });
        Require(wedgeBattle.AttackerFormation.FormationId == "wedge"
            && wedgeBattle.Groups.Where(item => item.Side == "attacker" && item.TroopType == "infantry").All(item => item.FormationId == "assault-column")
            && steadyBattle.Groups.Where(item => item.Side == "attacker" && item.TroopType == "infantry").All(item => item.FormationId == "shield-line")
            && wedgeBattle.Timeline.Any(item => item.Stage == "列阵" && item.Text.Contains("锋矢阵")),
            "全军阵型与兵种军令写入实时演算");
        var volleyBattle = CreateBattleVariant(campaign.State, army, targetCity, "standard", "arrow-volley", "goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" });
        Require(volleyBattle.PrimaryTactic == "arrow-volley" && BattleCalculator.TacticEffectSummary("arrow-volley") != BattleCalculator.TacticEffectSummary("steady-advance"), "主战术进入实时演算");
        Require(BattleCalculator.TacticRequirement("cavalry-charge", army.Composition).Contains("骑兵需达到总兵力的12%"), "主战术条件不足时返回明确的兵种要求");
        var tacticIds = new[] { "steady-advance", "shield-wall", "feigned-retreat", "night-raid", "fire-attack", "encirclement", "arrow-volley", "cavalry-charge", "fortify-camp", "cut-supply", "siege-ladders", "undermine-walls" };
        Require(tacticIds.All(id => BattleCalculator.TacticEffectSummary(id) != "无额外战术修正"), "12种战术均提供明确收益与风险说明");
        Require(campaign.ConfigurePendingBattle("goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" }, "standard", "cavalry-charge")
            && !campaign.StartPendingBattle() && insufficientTacticNotice.Contains("骑兵需达到总兵力的12%"), "临战主战术条件不足时阻止开战并显示具体条件");
        Require(campaign.ConfigurePendingBattle("goose", new Dictionary<string, string> { ["infantry"] = "shield-line", ["archers"] = "rear-double" }, "standard", "steady-advance"), "战前布阵配置");
        Require(campaign.StartPendingBattle(), "战斗开始演算");
        Require(campaign.State.PendingBattle!.OfficerUnits.Count == campaign.State.PendingBattle.AttackerOfficerIds.Distinct().Count() + campaign.State.PendingBattle.DefenderOfficerIds.Distinct().Count()
            && campaign.State.PendingBattle.OfficerUnits.All(item => item.SpriteId.Contains("generic") || item.SpriteId is "liu-bei" or "guan-yu" or "zhang-fei" or "lu-bu"), "双方参战武将全部展开为独立骑将单位");
        Require(BattleCalculator.MountedOfficerCombatPower("lu-bu", 92, 100, 42) > BattleCalculator.MountedOfficerCombatPower("generic-commander", 70, 70, 70)
            && campaign.State.PendingBattle.OfficerUnits.All(item => item.CombatPower >= 3.5), "骑将战力由属性计算且历史名将拥有专属强度");
        Require(BattleCalculator.MountedOfficerMaxHitPoints(95, 95) > BattleCalculator.MountedOfficerMaxHitPoints(50, 50)
            && BattleCalculator.MountedOfficerDefense(95, 95) > BattleCalculator.MountedOfficerDefense(50, 50)
            && campaign.State.PendingBattle.OfficerUnits.All(item => item.MaxHitPoints >= 1100 && item.HitPoints == item.MaxHitPoints && item.Morale == item.InitialMorale && item.Defense >= 30),
            "武将独立体力、士气与属性抗打数值完成初始化");
        var commandedGroup = campaign.State.PendingBattle!.Groups.First(item => item.Side == campaign.State.PendingBattle.PlayerSide && item.FinalSoldiers > 0);
        var commandedTarget = campaign.State.PendingBattle.Groups.First(item => item.Side != campaign.State.PendingBattle.PlayerSide && item.FinalSoldiers > 0);
        var commandMorale = commandedGroup.Morale;
        commandedGroup.Morale = 9; commandedGroup.IsRouted = true; commandedGroup.RallyAttempted = true;
        Require(!campaign.IssueBattleCommand([commandedGroup.Id], "attack", commandedTarget.Id), "溃散战斗队不再接受集火军令");
        commandedGroup.X = commandedTarget.X + 10; commandedGroup.Y = commandedTarget.Y; commandedGroup.AttackCooldown = 0;
        var routedSoldiers = commandedGroup.FinalSoldiers;
        campaign.AdvancePendingBattle(.2);
        Require(commandedGroup.FinalSoldiers < routedSoldiers && campaign.State.PendingBattle.Timeline.Any(item => item.Stage is "溃军追击" or "撤退追击"), "溃散战斗队撤离时仍会承受追击损失");
        commandedGroup.Morale = commandMorale; commandedGroup.IsRouted = false; commandedGroup.RallyAttempted = false;
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
        var earlyMoraleResolution = campaign.State.PendingBattle.Timeline.Any(item => item.Action is "rout" or "retreat");
        Require((campaign.State.PendingBattle.Timeline.Any(item => item.Action == "volley") || earlyMoraleResolution)
            && (campaign.State.PendingBattle.DefenderSortieUsed
                ? campaign.State.PendingBattle.Timeline.Any(item => item.Stage == "守军出击")
                : campaign.State.PendingBattle.Timeline.Any(item => item.Action == "structure") || earlyMoraleResolution), "远程、攻城或士气提前决胜均生成正式事件");
        Require((campaign.State.PendingBattle.Timeline.Any(item => item.Action is "officer-charge" or "officer-command")
                && campaign.State.PendingBattle.OfficerUnits.Any(item => item.TotalDamage > 0))
            || earlyMoraleResolution, "骑乘武将参战，或军心先于骑将冲锋崩溃");
        Require((campaign.State.PendingBattle.Timeline.Any(item => item.Action == "officer-damage")
                && campaign.State.PendingBattle.OfficerUnits.Any(item => item.DamageTaken > 0 && item.HitPoints < item.MaxHitPoints))
            || earlyMoraleResolution, "武将体力与士气在实时交锋中独立承伤");
        campaign.State.TurnResolutionPending = false;
        Require(campaign.CompletePendingBattle(), "战斗时间线结算");
        Require(pending.AttackerOfficerIds.Concat(pending.DefenderOfficerIds).All(id => campaign.Officer(id)?.InitialState.Health == 100), "战斗结算不再扣减战略健康");
        Require(campaign.State.BattleReports.Count > 0, "行军与战斗结算");
        var completedReport = campaign.State.BattleReports.Last();
        Require(completedReport.PhaseResults.Count == 1 && completedReport.PrimaryTactic == "steady-advance" && completedReport.PhaseResults.Sum(phase => phase.AttackerLosses) == completedReport.AttackerLosses, "战报持久保存实时结果与战术决策");

        RunSiegeSettlementTest(scenario);
        RunSiegeDefeatFoodReturnTest(scenario);
        RunLargeBattleSmokeTest(scenario);

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
        Require(interception.CreateExpedition(interceptSource.Id, interceptSource.Id, interceptCommander.Profile.Id, 3000, 3000, [], new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, [], enemyArmy.Id), "可选中敌方军团并发起专门拦截");
        var fieldPending = interception.State.PendingBattle ?? throw new InvalidOperationException("[GodotSelfTest] FAIL · 未创建军团野战");
        Require(fieldPending.BattleType == "field" && fieldPending.DefenderArmyId == enemyArmy.Id && fieldPending.DefenderBefore == 3700, "拦截目标绑定敌军团并按真实兵力展开野战");
        Require(fieldPending.Terrain == "plain"
            && BattleCalculator.LocalMoveMultiplier(fieldPending, "infantry", 500, 500) == 1,
            "军团野战统一按平地演算且没有浅滩减速");
        Require(interception.StartPendingBattle(), "军团野战开始实时演算");
        BattleCalculator.RunToCompletion(interception.State, fieldPending);
        Require(fieldPending.Status == "resolved" && fieldPending.Timeline.All(item => item.Action != "structure") && fieldPending.WallBefore == 0 && fieldPending.GateBefore == 0, "军团野战不读取城墙城门且无攻城事件");
        foreach (var officerId in fieldPending.AttackerOfficerIds.Concat(fieldPending.DefenderOfficerIds)) interception.Officer(officerId)!.InitialState.Loyalty = 50;
        var fieldAttackerWon = fieldPending.Result == "victory";
        Require(interception.CompletePendingBattle(), "军团野战完成结算");
        var fieldReport = interception.State.BattleReports.Last();
        var fieldWinner = fieldAttackerWon
            ? interception.State.Armies.First(item => item.Id == fieldPending.ArmyId)
            : interception.State.Armies.First(item => item.Id == fieldPending.DefenderArmyId);
        var fieldLoser = fieldAttackerWon
            ? interception.State.Armies.First(item => item.Id == fieldPending.DefenderArmyId)
            : interception.State.Armies.First(item => item.Id == fieldPending.ArmyId);
        Require(fieldReport.BattleType == "field" && !fieldReport.CityCaptured && interceptSource.OwnerFactionId == cityOwnerBeforeInterception
            && fieldWinner.Status == "marching"
            && (fieldLoser.Soldiers <= 0 ? fieldLoser.Status == "field-destroyed" : fieldLoser.Status is "retreating" or "field-defeat"),
            "野战只结算两军，胜军留场、败军撤退且不改变城池归属");
        Require(fieldPending.AttackerOfficerIds.All(id => interception.Officer(id)?.InitialState.Loyalty == (fieldAttackerWon ? 51 : 49))
            && fieldPending.DefenderOfficerIds.All(id => interception.Officer(id)?.InitialState.Loyalty == (fieldAttackerWon ? 49 : 51)),
            "军团野战胜方参战武将忠诚+1且败方-1");

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

    private static (ArmyData PlayerArmy, ArmyData EnemyArmy, RoadData Road) AddHeadOnRoadArmies(GameRuntime runtime, string prefix)
    {
        var road = runtime.State.Roads.First(item =>
        {
            var fromOwner = runtime.City(item.FromCityId)?.OwnerFactionId;
            var toOwner = runtime.City(item.ToCityId)?.OwnerFactionId;
            return item.TravelDays > 30 && fromOwner != toOwner && (fromOwner == runtime.State.PlayerFactionId || toOwner == runtime.State.PlayerFactionId);
        });
        var from = runtime.City(road.FromCityId)!;
        var to = runtime.City(road.ToCityId)!;
        var playerCity = from.OwnerFactionId == runtime.State.PlayerFactionId ? from : to;
        var enemyCity = playerCity == from ? to : from;
        var playerCommander = runtime.State.Officers.First(item => item.InitialState.FactionId == runtime.State.PlayerFactionId && item.InitialState.Status == "serving");
        var enemyCommander = runtime.State.Officers.First(item => item.InitialState.FactionId == enemyCity.OwnerFactionId && item.InitialState.Status == "serving");
        runtime.State.Diplomacy.FirstOrDefault(item => item.FactionId == enemyCity.OwnerFactionId)?.Treaties.Remove("truce");
        var playerArmy = new ArmyData
        {
            Id = $"{prefix}-player-army", FactionId = runtime.State.PlayerFactionId, SourceCityId = playerCity.Id, TargetCityId = enemyCity.Id,
            CommanderId = playerCommander.Profile.Id, Soldiers = 3000, Food = 5000, Composition = new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 },
            RouteRoadIds = [road.Id], TotalDays = road.TravelDays, RemainingDays = road.TravelDays, Status = "marching",
        };
        var enemyArmy = new ArmyData
        {
            Id = $"{prefix}-enemy-army", FactionId = enemyCity.OwnerFactionId!, SourceCityId = enemyCity.Id, TargetCityId = playerCity.Id,
            CommanderId = enemyCommander.Profile.Id, Soldiers = 3200, Food = 5000, Composition = new Dictionary<string, int> { ["infantry"] = 2200, ["cavalry"] = 1000 },
            RouteRoadIds = [road.Id], TotalDays = road.TravelDays, RemainingDays = Math.Min(15, road.TravelDays), Status = "marching",
        };
        runtime.State.Armies.Add(playerArmy);
        runtime.State.Armies.Add(enemyArmy);
        return (playerArmy, enemyArmy, road);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException($"[GodotSelfTest] FAIL · {label}");
        GD.Print($"[GodotSelfTest] OK · {label}");
    }

    private static PendingBattleData CreateBattleVariant(GameSession state, ArmyData army, CityData city, string stance, string tactic, string formation, Dictionary<string, string> orders)
    {
        var originalStance = army.Stance;
        var originalTactic = army.Tactic;
        try
        {
            army.Stance = stance;
            army.Tactic = tactic;
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
        }
    }

    private static void RunLargeBattleSmokeTest(ScenarioData scenario)
    {
        var runtime = new GameRuntime(scenario);
        var attackerOfficer = runtime.PlayerOfficers().First(item => item.InitialState.Status == "serving");
        var defenderOfficer = runtime.State.Officers.First(item => item.InitialState.FactionId != runtime.State.PlayerFactionId && item.InitialState.Status == "serving");
        var battlefield = runtime.State.Cities.First(item => item.OwnerFactionId == defenderOfficer.InitialState.FactionId);
        var composition = new Dictionary<string, int> { ["infantry"] = 7200, ["spears"] = 4800, ["archers"] = 4800, ["cavalry"] = 4800, ["siege"] = 2400 };
        var attacker = new ArmyData
        {
            Id = "self-test-large-attacker", FactionId = runtime.State.PlayerFactionId, SourceCityId = attackerOfficer.InitialState.CityId, TargetCityId = battlefield.Id,
            CommanderId = attackerOfficer.Profile.Id, Soldiers = 24000, Food = 30000, Training = 70, Morale = 72, Composition = new Dictionary<string, int>(composition), Status = "marching",
        };
        var defender = new ArmyData
        {
            Id = "self-test-large-defender", FactionId = defenderOfficer.InitialState.FactionId!, SourceCityId = battlefield.Id, TargetCityId = attackerOfficer.InitialState.CityId,
            CommanderId = defenderOfficer.Profile.Id, Soldiers = 24000, Food = 30000, Training = 70, Morale = 72, Composition = new Dictionary<string, int>(composition), Status = "marching",
        };
        runtime.State.Armies.Add(attacker); runtime.State.Armies.Add(defender);
        var battle = BattleCalculator.CreateFieldBattle(runtime.State, attacker, defender);
        Require(battle.Groups.Count(item => item.Side == "attacker") == 40 && battle.Groups.Count(item => item.Side == "defender") == 40, "2.4万对2.4万正确展开40对40战斗队");
        BattleCalculator.Generate(runtime.State, battle);
        BattleCalculator.Advance(runtime.State, battle, .5);
        Require(battle.Status is "running" or "resolved" && battle.Timeline.Count < 1200, "40对40实时演算首帧事件量受控");
    }

    private static void RunSiegeSettlementTest(ScenarioData scenario)
    {
        var runtime = new GameRuntime(scenario);
        var source = runtime.State.Cities.First(item => item.OwnerFactionId == runtime.State.PlayerFactionId
            && runtime.Officer(item.GovernorId) is { InitialState.Status: "serving" });
        var target = runtime.State.Cities.First(item => item.OwnerFactionId != runtime.State.PlayerFactionId);
        var commander = runtime.Officer(source.GovernorId)!;
        var sourceGovernorId = source.GovernorId;
        var army = new ArmyData
        {
            Id = "self-test-siege-settlement", FactionId = runtime.State.PlayerFactionId, SourceCityId = source.Id, TargetCityId = target.Id,
            CommanderId = commander.Profile.Id, Soldiers = 3000, Food = 5000, Training = 70, Morale = 72,
            Composition = new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, Status = "arrived",
        };
        commander.InitialState.Status = "deployed";
        commander.InitialState.ArmyId = army.Id;
        runtime.State.Armies.Add(army);
        var pending = BattleCalculator.Create(runtime.State, army, target);
        runtime.State.PendingBattle = pending;
        BattleCalculator.Generate(runtime.State, pending);
        foreach (var group in pending.Groups.Where(item => item.Side == "attacker")) group.FinalSoldiers = Math.Min(group.FinalSoldiers, 180);
        foreach (var group in pending.Groups.Where(item => item.Side == "defender")) group.FinalSoldiers = Math.Min(group.FinalSoldiers, 120);
        pending.AttackerAfter = pending.Groups.Where(item => item.Side == "attacker").Sum(item => item.FinalSoldiers);
        pending.DefenderAfter = pending.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
        pending.AttackerLosses = pending.AttackerBefore - pending.AttackerAfter;
        pending.DefenderLosses = pending.DefenderBefore - pending.DefenderAfter;
        pending.Result = "victory";
        pending.Status = "resolved";
        foreach (var officerId in pending.AttackerOfficerIds.Concat(pending.DefenderOfficerIds)) runtime.Officer(officerId)!.InitialState.Loyalty = 50;
        runtime.State.TurnResolutionPending = false;
        var expectedGarrison = pending.AttackerAfter;
        var treasuryFoodBefore = runtime.State.Resources.Food;
        var carriedFood = army.Food;
        var formerOwnerId = target.OwnerFactionId!;
        var expectedLootFood = Math.Min(runtime.State.FactionTreasuries[formerOwnerId].Food / 10, 4000);
        Require(runtime.CompletePendingBattle() && target.OwnerFactionId == runtime.State.PlayerFactionId && target.Garrison == expectedGarrison,
            "攻陷城池只驻扎攻方幸存兵，不吸收守军残部");
        Require(army.Food == 0 && runtime.State.Resources.Food == treasuryFoodBefore + carriedFood + expectedLootFood,
            "攻城胜利后剩余军粮归还势力府库且不会重复保留在军团");
        Require(pending.AttackerOfficerIds.All(id => runtime.Officer(id)?.InitialState.Loyalty == 51)
            && pending.DefenderOfficerIds.All(id => runtime.Officer(id)?.InitialState.Loyalty == 49)
            && runtime.State.BattleReports.Last().Narrative.Contains("胜方参战武将忠诚+1，败方-1"),
            "攻城战胜方参战武将忠诚+1且败方-1并写入战报");
        Require(source.GovernorId == sourceGovernorId && target.GovernorId != commander.Profile.Id,
            "原任太守攻下新城后保留原职位，不被重复改任新城太守");
    }

    private static void RunSiegeDefeatFoodReturnTest(ScenarioData scenario)
    {
        var runtime = new GameRuntime(scenario);
        var source = runtime.State.Cities.First(item => item.OwnerFactionId == runtime.State.PlayerFactionId
            && runtime.Officer(item.GovernorId) is { InitialState.Status: "serving" });
        var target = runtime.State.Cities.First(item => item.OwnerFactionId != runtime.State.PlayerFactionId);
        var commander = runtime.Officer(source.GovernorId)!;
        var army = new ArmyData
        {
            Id = "self-test-siege-defeat-food", FactionId = runtime.State.PlayerFactionId, SourceCityId = source.Id, TargetCityId = target.Id,
            CommanderId = commander.Profile.Id, Soldiers = 3000, Food = 4200, Training = 70, Morale = 72,
            Composition = new Dictionary<string, int> { ["infantry"] = 2000, ["archers"] = 1000 }, Status = "arrived",
        };
        commander.InitialState.Status = "deployed";
        commander.InitialState.ArmyId = army.Id;
        runtime.State.Armies.Add(army);
        var pending = BattleCalculator.Create(runtime.State, army, target);
        runtime.State.PendingBattle = pending;
        BattleCalculator.Generate(runtime.State, pending);
        foreach (var group in pending.Groups.Where(item => item.Side == "attacker")) group.FinalSoldiers = Math.Min(group.FinalSoldiers, 80);
        pending.AttackerAfter = pending.Groups.Where(item => item.Side == "attacker").Sum(item => item.FinalSoldiers);
        pending.DefenderAfter = pending.Groups.Where(item => item.Side == "defender").Sum(item => item.FinalSoldiers);
        pending.AttackerLosses = pending.AttackerBefore - pending.AttackerAfter;
        pending.DefenderLosses = pending.DefenderBefore - pending.DefenderAfter;
        pending.Result = "defeat";
        pending.Status = "resolved";
        var treasuryFoodBefore = runtime.State.Resources.Food;
        var carriedFood = army.Food;
        Require(runtime.CompletePendingBattle() && army.Status == "defeated"
            && army.Food == 0 && runtime.State.Resources.Food == treasuryFoodBefore + carriedFood,
            "攻城失败后剩余军粮归还势力府库且不会重复保留在军团");
    }

    private static string BattleSignature(PendingBattleData battle) => string.Join('|', battle.PhaseResults.Select(phase => $"{phase.Stage}:{phase.Tactic}:{phase.AttackerAfter}:{phase.DefenderAfter}:{phase.PowerRatio:F3}:{phase.WallDamage}:{phase.GateDamage}:{phase.InnerDamage}"));
}
