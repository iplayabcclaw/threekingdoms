namespace ThreeKingdomsSimulator.Godot;

public sealed partial class GameRuntime
{
    private static readonly HashSet<string> GovernanceModes = ["manual", "delegated"];
    private static readonly HashSet<string> GovernancePolicies = ["balanced", "recovery", "commerce", "agriculture", "military", "integration"];
    private static readonly HashSet<string> CityRoles = ["unassigned", "granary", "market", "garrison", "academy", "hub"];
    public bool ConfigureCityGovernance(string cityId, string mode, string policy, string role)
    {
        var city = City(cityId);
        if (city?.OwnerFactionId != State.PlayerFactionId) return Fail("只能设置己方城池的治理方针。");
        if (!GovernanceModes.Contains(mode) || !GovernancePolicies.Contains(policy) || !CityRoles.Contains(role)) return Fail("治理方式、方针或城市定位无效。");
        if (city.CityRole != role)
        {
            city.CityRole = role;
            city.RoleTransitionMonths = role == "unassigned" ? 0 : 3;
        }
        city.GovernanceMode = mode;
        city.GovernancePolicy = policy;
        return Success($"{city.Name}已设置为{GovernanceModeLabel(mode)}，方针为{GovernancePolicyLabel(policy)}。", "city");
    }

    public (int GoldIncome, int FoodIncome, int GoldUpkeep, int FoodUpkeep) CityMonthlyForecast(CityData city)
    {
        var orderFactor = city.PublicOrder < 40 ? .72 : city.PublicOrder < 60 ? .88 : 1d;
        var roleReady = city.RoleTransitionMonths == 0;
        var goldRole = roleReady && city.CityRole == "market" ? 1.15 : 1d;
        var foodRole = roleReady && city.CityRole == "granary" ? 1.15 : 1d;
        var gold = (int)((city.Population * city.Commerce / 340_000d + city.Facilities.Where(item => item.DefinitionId == "market").Sum(item => 180 * item.Level)) * orderFactor * goldRole);
        var seasonalFood = State.Month is >= 8 and <= 10 ? 1.25 : State.Month is 1 or 2 ? .88 : 1d;
        var food = (int)((city.Population * city.Agriculture / 135_000d + city.Facilities.Where(item => item.DefinitionId is "irrigation" or "granary").Sum(item => 260 * item.Level)) * seasonalFood * foodRole);
        var goldUpkeep = city.Facilities.Sum(item => 20 * item.Level);
        var foodUpkeep = Math.Max(80, city.Garrison / 14);
        return (Math.Max(0, gold), Math.Max(0, food), goldUpkeep, foodUpkeep);
    }

    public string CityPrioritySummary(CityData city)
    {
        var threats = AdjacentEnemyPressure(city);
        var treasury = Treasury(city.OwnerFactionId);
        var cities = State.Cities.Where(item => item.OwnerFactionId == city.OwnerFactionId).ToList();
        var foodUpkeep = cities.Sum(item => CityMonthlyForecast(item).FoodUpkeep);
        var goldUpkeep = cities.Sum(item => CityMonthlyForecast(item).GoldUpkeep);
        if (treasury.Food < Math.Max(foodUpkeep * 2, city.Garrison / 3)) return "势力粮草紧张";
        if (city.PublicOrder < 40) return "治安动荡";
        if (city.PublicSupport < 40) return "民心低迷";
        if (threats > city.Garrison * .8) return "前线守备吃紧";
        if (treasury.Gold < goldUpkeep * 2) return "势力财政紧张";
        if (city.ConstructionQueue is not null) return $"{FacilityName(city.ConstructionQueue.DefinitionId)}建设中";
        return city.GovernanceMode == "delegated" ? $"太守按{GovernancePolicyLabel(city.GovernancePolicy)}治理" : "等待本月安排";
    }

    public string CityCommandPreview(string cityId, string officerId, string focus)
    {
        var city = City(cityId);
        var officer = Officer(officerId);
        if (city is null || officer is null) return "请先选择当前城池的可用武将。";
        if (!TryGetCommandCost(city, officer, focus, out var gold, out var food, out var equipment, out var population)) return "未知城务命令。";
        var ability = CommandAbility(officer, focus);
        var trait = OfficerProgressionRules.DomesticTraitModifier(officer, focus);
        var gain = Math.Clamp((int)Math.Round((3 + ability / 18 + FacilityCommandBonus(city, focus) - city.Fatigue / 25) * trait.Modifier), 2, 12);
        var effect = focus switch
        {
            "recruit" => $"预计征募{population:N0}人，并降低民心与平均训练",
            "search" => "尝试发现人才；若无人才则小幅提升文化",
            _ => $"预计主要属性 +{Math.Max(2, gain - 1)}～{Math.Min(12, gain + 1)}",
        };
        return $"{city.Name} · {FocusLabel(focus)}\n消耗：本城城务1、势力府库金{gold:N0}、粮{food:N0}{(equipment > 0 ? $"、军备{equipment:N0}" : "")}\n{effect}；有效能力{ability}；{trait.Description}；执行后本城余{Math.Max(0, city.ActionSlots - 1)}/{city.ActionCapacity}城务。";
    }

    public static string GovernanceModeLabel(string value) => value == "delegated" ? "方针委任" : "亲自治理";
    public static string GovernancePolicyLabel(string value) => value switch
    {
        "recovery" => "休养生息",
        "commerce" => "富民通商",
        "agriculture" => "屯田积粮",
        "military" => "整军备战",
        "integration" => "巩固新土",
        _ => "均衡治理",
    };
    public static string CityRoleLabel(string value) => value switch
    {
        "granary" => "粮仓",
        "market" => "商埠",
        "garrison" => "军镇",
        "academy" => "学府",
        "hub" => "枢纽",
        _ => "未定",
    };
    public static string CityStatusLabel(string value) => value switch
    {
        "prosperous" => "繁荣",
        "shortage" => "缺粮",
        "unrest" => "动荡",
        "exhausted" => "疲敝",
        "frontline" => "前线",
        "integrating" => "整合中",
        _ => "安定",
    };

    private bool ExecutePlayerCityCommand(string cityId, string officerId, string focus)
    {
        var city = City(cityId);
        var officer = Officer(officerId);
        if (!CanCityAct(city, officer, State.PlayerFactionId, false, out var error)) return Fail(error);
        if (!ExecuteCityCommand(city!, officer!, focus, State.PlayerFactionId, false, out var message)) return Fail(message);
        return Success(message, "city");
    }

    private bool ExecuteCityCommand(CityData city, ScenarioOfficerData officer, string focus, string factionId, bool delegated, out string message)
    {
        message = string.Empty;
        if (!CanCityAct(city, officer, factionId, delegated, out message)) return false;
        if (!TryGetCommandCost(city, officer, focus, out var goldCost, out var foodCost, out var equipmentCost, out var populationCost))
        {
            message = "未知城务命令。";
            return false;
        }
        var treasury = Treasury(factionId);
        if (treasury.Gold < goldCost)
        {
            message = $"势力府库金不足：需要{goldCost:N0}，现有{treasury.Gold:N0}。";
            return false;
        }
        if (treasury.Food < foodCost)
        {
            message = $"势力府库粮不足：需要{foodCost:N0}，现有{treasury.Food:N0}。";
            return false;
        }
        if (treasury.Equipment < equipmentCost)
        {
            message = $"{Faction(factionId)?.ShortName ?? "势力"}军备不足：需要{equipmentCost:N0}。";
            return false;
        }
        if (populationCost > 0 && city.Population - populationCost < 5000)
        {
            message = $"{city.Name}人口不足，不能继续征募。";
            return false;
        }

        var ability = CommandAbility(officer, focus);
        var facilityBonus = FacilityCommandBonus(city, focus);
        var fatiguePenalty = city.Fatigue / 25;
        var governor = Officer(city.GovernorId);
        var governorBonus = !GovernorHoldsOffice(city, governor) || governor!.Profile.Id == officer.Profile.Id ? 0 : EffectiveAbility(governor, "politics", "civil") / 35;
        var trait = OfficerProgressionRules.DomesticTraitModifier(officer, focus);
        var gain = Math.Clamp((int)Math.Round((3 + ability / 18 + facilityBonus + governorBonus - fatiguePenalty) * trait.Modifier), 2, 12);
        if (focus == "recruit") populationCost = Math.Clamp((int)Math.Round(populationCost * trait.Modifier), 500, 1200);
        var detail = $"有效能力{ability}，设施修正{facilityBonus:+#;-#;0}，疲敝修正{-fatiguePenalty:+#;-#;0}，{trait.Description}";

        switch (focus)
        {
            case "agriculture":
                city.Agriculture = Clamp100(city.Agriculture + gain);
                break;
            case "commerce":
                city.Commerce = Clamp100(city.Commerce + gain);
                break;
            case "patrol":
                city.PublicOrder = Clamp100(city.PublicOrder + gain);
                if (EffectiveAbility(officer, "might", "civil") >= 75 && EffectiveAbility(officer, "charisma", "civil") < 40) city.PublicSupport = Clamp100(city.PublicSupport - 1);
                break;
            case "defense":
                city.Defense = Clamp100(city.Defense + gain);
                city.WallDurability = Clamp100(city.WallDurability + Math.Max(2, gain / 2));
                city.GateDurability = Clamp100(city.GateDurability + Math.Max(2, gain / 2));
                break;
            case "recruit":
                var previousGarrison = Math.Max(1, city.Garrison);
                city.Garrison += populationCost;
                city.Population -= populationCost;
                var supportLoss = Math.Max(2, populationCost / 350);
                if (OfficerProgressionRules.AllTraits(officer).Contains("爱民")) supportLoss = Math.Max(1, supportLoss - 1);
                city.PublicSupport = Clamp100(city.PublicSupport - supportLoss);
                city.Training = Math.Clamp((city.Training * previousGarrison + 35 * populationCost) / (previousGarrison + populationCost), 0, 100);
                break;
            case "train":
                city.Training = Clamp100(city.Training + gain);
                break;
            case "relief":
                city.PublicSupport = Clamp100(city.PublicSupport + gain + 2);
                city.Fatigue = Clamp100(city.Fatigue - 3);
                break;
            case "search":
                var free = State.Officers.FirstOrDefault(item => item.InitialState.Status == "free" && item.InitialState.CityId == city.Id && (factionId != State.PlayerFactionId || !State.DiscoveredOfficerIds.Contains(item.Profile.Id)));
                if (free is null)
                {
                    city.Culture = Clamp100(city.Culture + Math.Max(1, gain / 3));
                    detail += "，未发现人才但提升文化";
                }
                else if (factionId == State.PlayerFactionId)
                {
                    State.DiscoveredOfficerIds.Add(free.Profile.Id);
                    detail += $"，发现{free.Profile.Name}";
                }
                else
                {
                    free.InitialState.FactionId = factionId;
                    free.InitialState.Status = "serving";
                    free.InitialState.Appointment = "reserve";
                    free.InitialState.Loyalty = 60;
                    detail += $"，延揽{free.Profile.Name}";
                }
                break;
        }

        treasury.Gold -= goldCost;
        treasury.Food -= foodCost;
        treasury.Equipment -= equipmentCost;
        city.ActionSlots--;
        city.MonthlyOfficerActionIds.Add(officer.Profile.Id);
        city.Fatigue = Clamp100(city.Fatigue + (focus is "recruit" or "train" ? 4 : 2));
        officer.InitialState.Fatigue = Clamp100(officer.InitialState.Fatigue + 10);
        AwardOfficerExperience(officer, 20, $"完成{FocusLabel(focus)}", $"domestic-{focus}");
        message = $"{officer.Profile.Name}在{city.Name}执行{FocusLabel(focus)}：{detail}。本城余{city.ActionSlots}/{city.ActionCapacity}城务。";
        RecordCityLedger(city, "command", -goldCost, -foodCost, message);
        return true;
    }

    private bool ExecutePlayerFacilityBuild(string cityId, string officerId, string facilityId, int slotIndex)
    {
        var city = City(cityId);
        var officer = Officer(officerId);
        if (!CanCityAct(city, officer, State.PlayerFactionId, false, out var error)) return Fail(error);
        if (!ExecuteFacilityBuild(city!, officer!, facilityId, State.PlayerFactionId, false, out var message, slotIndex)) return Fail(message);
        return Success(message, "city");
    }

    private bool ExecuteFacilityBuild(CityData city, ScenarioOfficerData officer, string facilityId, string factionId, bool delegated, out string message, int slotIndex = -1)
    {
        message = string.Empty;
        if (!CanCityAct(city, officer, factionId, delegated, out message)) return false;
        if (city.ConstructionQueue is not null) { message = $"{city.Name}已有建设工程。"; return false; }
        if (city.Facilities.Count >= city.FacilitySlots) { message = $"{city.Name}设施槽已满。"; return false; }
        if (city.Facilities.Any(item => item.DefinitionId == facilityId)) { message = $"{city.Name}已经建成{FacilityName(facilityId)}。"; return false; }
        var occupiedSlots = FacilitiesBySlot(city);
        if (slotIndex < 0) slotIndex = Enumerable.Range(0, Math.Max(1, city.FacilitySlots)).FirstOrDefault(index => !occupiedSlots.ContainsKey(index), -1);
        if (slotIndex < 0 || slotIndex >= city.FacilitySlots || occupiedSlots.ContainsKey(slotIndex)) { message = "所选地块不可用于建设。"; return false; }
        var definition = FacilityCatalog.GetValueOrDefault(facilityId);
        if (definition is null) { message = "未知设施。"; return false; }
        var treasury = Treasury(factionId);
        if (treasury.Gold < definition.Gold || treasury.Food < definition.Food)
        {
            message = $"势力府库不足：建设{definition.Name}需要金{definition.Gold:N0}、粮{definition.Food:N0}。";
            return false;
        }
        treasury.Gold -= definition.Gold;
        treasury.Food -= definition.Food;
        city.ActionSlots--;
        city.MonthlyOfficerActionIds.Add(officer.Profile.Id);
        officer.InitialState.Fatigue = Clamp100(officer.InitialState.Fatigue + 8);
        city.ConstructionQueue = new ConstructionData { DefinitionId = facilityId, OfficerId = officer.Profile.Id, RemainingMonths = definition.Months, TotalMonths = definition.Months, TargetSlotIndex = slotIndex };
        message = $"{city.Name}开始建设{definition.Name}，预计{definition.Months}个月。本城余{city.ActionSlots}/{city.ActionCapacity}城务。";
        RecordCityLedger(city, "construction", -definition.Gold, -definition.Food, message);
        return true;
    }

    private bool ExecutePlayerFacilityMaintenance(string cityId, string instanceId, bool upgrade)
    {
        var city = City(cityId);
        var facility = city?.Facilities.FirstOrDefault(item => item.Id == instanceId);
        var officer = city is null ? null : Officer(city.GovernorId) ?? State.Officers.Where(item => item.InitialState.FactionId == State.PlayerFactionId && item.InitialState.CityId == city.Id && item.InitialState.Status == "serving").OrderByDescending(item => EffectiveAbility(item, "politics", "civil")).FirstOrDefault();
        if (city is null || facility is null) return Fail("设施不存在。");
        if (!CanCityAct(city, officer, State.PlayerFactionId, false, out var error)) return Fail(error);
        if (city.ConstructionQueue is not null) return Fail($"{city.Name}已有建设工程。");
        var cost = upgrade ? 900 * facility.Level : 300;
        var treasury = Treasury(State.PlayerFactionId);
        if (treasury.Gold < cost) return Fail($"势力府库金不足：需要{cost:N0}，现有{treasury.Gold:N0}。");
        treasury.Gold -= cost;
        city.ActionSlots--;
        city.MonthlyOfficerActionIds.Add(officer!.Profile.Id);
        city.ConstructionQueue = new ConstructionData { DefinitionId = facility.DefinitionId, OfficerId = officer.Profile.Id, RemainingMonths = 1, TotalMonths = 1, Kind = upgrade ? "upgrade" : "repair", TargetInstanceId = facility.Id };
        var message = $"{city.Name}开始{(upgrade ? "升级" : "修缮")}{FacilityName(facility.DefinitionId)}，费用由势力府库承担。";
        RecordCityLedger(city, "construction", -cost, 0, message);
        return Success(message, "city");
    }

    private bool ExecutePlayerTreasuryTransfer(string cityId, bool toCity, int gold, int food)
    {
        return Fail("金粮现为势力通用资源，不再需要中央与城池之间调拨。");
    }

    private bool CanCityAct(CityData? city, ScenarioOfficerData? officer, string factionId, bool delegated, out string error)
    {
        error = string.Empty;
        if (city is null || city.OwnerFactionId != factionId) { error = "只能在所属势力城池执行命令。"; return false; }
        if (officer is null || officer.InitialState.FactionId != factionId || officer.InitialState.CityId != city.Id || officer.InitialState.Status != "serving" || !officer.InitialState.Alive)
        {
            error = $"执行者不在{city.Name}或当前不可用。";
            return false;
        }
        if (city.ActionSlots <= 0) { error = $"{city.Name}本月城务额度已用尽。"; return false; }
        var used = city.MonthlyOfficerActionIds.Count(id => id == officer.Profile.Id);
        var limit = delegated && city.GovernorId == officer.Profile.Id ? 2 : 1;
        if (used >= limit) { error = $"{officer.Profile.Name}本月已完成城务，不能重复执行。"; return false; }
        return true;
    }

    private bool TryGetCommandCost(CityData city, ScenarioOfficerData officer, string focus, out int gold, out int food, out int equipment, out int population)
    {
        gold = focus switch { "recruit" => 500, "relief" => 350, _ => 250 };
        food = focus switch { "relief" => 1200, "train" => Math.Max(400, city.Garrison / 25), "recruit" => 600, _ => 0 };
        equipment = focus == "recruit" ? 300 : 0;
        population = focus == "recruit" ? Math.Clamp(350 + EffectiveAbility(officer, "leadership", "military") * 7, 500, 1000) : 0;
        return focus is "agriculture" or "commerce" or "patrol" or "defense" or "recruit" or "train" or "search" or "relief";
    }

    private int CommandAbility(ScenarioOfficerData officer, string focus) => focus switch
    {
        "agriculture" => EffectiveAbility(officer, "politics", "civil"),
        "commerce" or "relief" => EffectiveAbility(officer, "charisma", "civil"),
        "patrol" => (EffectiveAbility(officer, "might", "military") * 2 + EffectiveAbility(officer, "charisma", "civil")) / 3,
        "defense" or "recruit" or "train" => EffectiveAbility(officer, "leadership", "military"),
        "search" => EffectiveAbility(officer, "intelligence", "civil"),
        _ => 50,
    };

    private static int FacilityCommandBonus(CityData city, string focus)
    {
        var ids = focus switch
        {
            "agriculture" => new[] { "irrigation", "granary" },
            "commerce" => ["market"],
            "defense" => ["walls"],
            "recruit" => ["barracks"],
            "train" => ["drill-ground"],
            "search" => ["academy"],
            "relief" => new[] { "granary", "clinic" },
            _ => Array.Empty<string>(),
        };
        return city.Facilities.Where(item => ids.Contains(item.DefinitionId)).Sum(item => item.Level);
    }

    private ResourceData Treasury(string factionId)
    {
        if (factionId == State.PlayerFactionId) return State.Resources;
        if (!State.FactionTreasuries.TryGetValue(factionId, out var treasury))
        {
            treasury = new ResourceData { Gold = 2500, Food = 8000, Equipment = 900, Prestige = 20 };
            State.FactionTreasuries[factionId] = treasury;
        }
        return treasury;
    }

    private void ResolveCityEconomy()
    {
        foreach (var city in State.Cities)
        {
            var forecast = CityMonthlyForecast(city);
            var treasury = Treasury(city.OwnerFactionId);
            var goldBefore = treasury.Gold;
            var foodBefore = treasury.Food;
            treasury.Gold += forecast.GoldIncome;
            treasury.Food += forecast.FoodIncome;
            var goldPaid = Math.Min(treasury.Gold, forecast.GoldUpkeep);
            var foodPaid = Math.Min(treasury.Food, forecast.FoodUpkeep);
            treasury.Gold -= goldPaid;
            treasury.Food -= foodPaid;
            if (foodPaid < forecast.FoodUpkeep)
            {
                city.PublicSupport = Clamp100(city.PublicSupport - 5);
                city.PublicOrder = Clamp100(city.PublicOrder - 3);
                city.Garrison = Math.Max(1000, city.Garrison - Math.Max(100, city.Garrison / 25));
                State.CampaignTimeline.Add($"第{State.Turn}回合 · {city.Name}粮荒 · 驻军与民心受损");
            }
            city.Status = DetermineCityStatus(city);
            var equipmentOutput = city.Facilities.Where(item => item.DefinitionId == "workshop" && item.Condition >= 50).Sum(item => 80 * item.Level);
            if (equipmentOutput > 0) Treasury(city.OwnerFactionId).Equipment += equipmentOutput;
            var governor = Officer(city.GovernorId);
            if (GovernorHoldsOffice(city, governor)) AwardOfficerExperience(governor!, 12, $"治理{city.Name}", "governor-month");
            city.LastMonthlyReport = $"贡献势力府库金{forecast.GoldIncome:N0}/粮{forecast.FoodIncome:N0}，维护金{goldPaid:N0}/粮{foodPaid:N0}，府库净变化金{treasury.Gold - goldBefore:+#,0;-#,0;0}/粮{treasury.Food - foodBefore:+#,0;-#,0;0}";
            RecordCityLedger(city, "economy", forecast.GoldIncome - goldPaid, forecast.FoodIncome - foodPaid, city.LastMonthlyReport);
        }
        State.Resources.Prestige += Math.Max(1, State.Cities.Count(city => city.OwnerFactionId == State.PlayerFactionId) / 3);
    }

    private void ResolveCityContributions()
    {
        // 金粮已经在各城月结时直接进入势力府库，保留该节点以兼容结算顺序。
    }

    private void ResolveSmartDomesticAi()
    {
        foreach (var faction in State.Factions.Where(item => item.Id != State.PlayerFactionId))
        {
            foreach (var city in State.Cities.Where(city => city.OwnerFactionId == faction.Id))
            {
                city.GovernanceMode = "delegated";
                city.GovernancePolicy = SelectAiGovernancePolicy(city, faction.Id);
                var suggestedRole = SuggestedCityRole(city);
                var canAdjustRole = suggestedRole == "garrison" || city.Status is "stable" or "prosperous" or "frontline";
                if (canAdjustRole && suggestedRole != "unassigned" && city.CityRole != suggestedRole && city.RoleTransitionMonths <= 0)
                {
                    city.CityRole = suggestedRole;
                    city.RoleTransitionMonths = 3;
                }
                RunSmartDomestic(city, faction.Id, city.GovernancePolicy, true);
            }
        }
        var delegatedCities = State.Cities.Where(city => city.OwnerFactionId == State.PlayerFactionId && (city.GovernanceMode == "delegated" || State.Automation.Enabled && State.Automation.Domestic));
        foreach (var city in delegatedCities) RunSmartDomestic(city, State.PlayerFactionId, city.GovernancePolicy, false);
    }

    private string SelectAiGovernancePolicy(CityData city, string factionId)
    {
        var forecast = CityMonthlyForecast(city);
        var pressure = AdjacentEnemyPressure(city);
        var factionCities = State.Cities.Where(item => item.OwnerFactionId == factionId).ToList();
        var treasury = Treasury(factionId);
        var foodCoverage = treasury.Food / (double)Math.Max(1, factionCities.Sum(item => CityMonthlyForecast(item).FoodUpkeep));
        if (city.Status == "integrating") return "integration";
        if (city.PublicOrder < 48 || city.PublicSupport < 48 || city.Fatigue >= 55) return "recovery";
        if (foodCoverage < 4) return "agriculture";
        if (pressure > city.Garrison * .55 || city.Garrison < 4000) return "military";
        if (treasury.Gold < factionCities.Sum(item => CityMonthlyForecast(item).GoldUpkeep) * 3 || forecast.GoldIncome < forecast.GoldUpkeep * 2) return "commerce";
        var strategicBias = Math.Abs(factionId.Sum(character => character) + city.Id.Sum(character => character)) % 4;
        return strategicBias switch { 0 => "commerce", 1 => "agriculture", 2 when pressure > 0 => "military", _ => "balanced" };
    }

    private string SuggestedCityRole(CityData city)
    {
        if (AdjacentEnemyPressure(city) > 0) return "garrison";
        if (city.Agriculture >= city.Commerce + 12) return "granary";
        if (city.Commerce >= city.Agriculture + 10) return "market";
        if (city.Culture >= 65) return "academy";
        var friendlyRoads = State.Roads.Count(road => (road.FromCityId == city.Id && City(road.ToCityId)?.OwnerFactionId == city.OwnerFactionId) || (road.ToCityId == city.Id && City(road.FromCityId)?.OwnerFactionId == city.OwnerFactionId));
        return friendlyRoads >= 3 ? "hub" : "unassigned";
    }

    private void RunSmartDomestic(CityData city, string factionId, string policy, bool nonPlayer)
    {
        var attempted = new HashSet<string>();
        if (TryChooseFacility(city, factionId, policy, out var facilityId, out var builder))
        {
            ExecuteFacilityBuild(city, builder!, facilityId!, factionId, true, out _);
        }
        while (city.ActionSlots > 0)
        {
            var candidates = EvaluateDomesticCandidates(city, factionId, policy)
                .Where(item => !attempted.Contains(item.Focus))
                .OrderByDescending(item => item.Score)
                .ToList();
            if (candidates.Count == 0 || candidates[0].Score < 20) break;
            var executed = false;
            foreach (var candidate in candidates)
            {
                attempted.Add(candidate.Focus);
                if (!ExecuteCityCommand(city, candidate.Officer, candidate.Focus, factionId, true, out var message)) continue;
                State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "ai", Message = $"{Faction(factionId)?.ShortName}·{city.Name}：{message}（评估{candidate.Score:0}）" });
                executed = true;
                break;
            }
            if (!executed) break;
        }
        if (nonPlayer && city.ActionSlots > 0) State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "ai", Message = $"{Faction(factionId)?.ShortName}·{city.Name}保留{city.ActionSlots}点城务：资源或武将条件不足。" });
    }

    private List<DomesticCandidate> EvaluateDomesticCandidates(CityData city, string factionId, string policy)
    {
        var pressure = AdjacentEnemyPressure(city);
        var frontline = pressure > 0;
        var desiredGarrison = frontline ? Math.Max(6500, (int)(pressure * .8)) : 4000;
        var result = new List<DomesticCandidate>();
        var treasury = Treasury(factionId);
        var factionCities = State.Cities.Where(item => item.OwnerFactionId == factionId).ToList();
        var foodUpkeep = factionCities.Sum(item => CityMonthlyForecast(item).FoodUpkeep);
        var goldUpkeep = factionCities.Sum(item => CityMonthlyForecast(item).GoldUpkeep);
        AddCandidate("agriculture", 25 + Math.Max(0, 72 - city.Agriculture) * .9 + (treasury.Food < foodUpkeep * 3 ? 65 : 0) + PolicyBonus(policy, "agriculture") + RoleBonus(city, "agriculture"));
        AddCandidate("commerce", 22 + Math.Max(0, 68 - city.Commerce) * .8 + (treasury.Gold < goldUpkeep * 3 ? 60 : 0) + PolicyBonus(policy, "commerce") + RoleBonus(city, "commerce"));
        AddCandidate("patrol", 15 + Math.Max(0, 65 - city.PublicOrder) * 1.6 + (city.Status == "unrest" ? 45 : 0) + PolicyBonus(policy, "patrol"));
        AddCandidate("relief", 10 + Math.Max(0, 62 - city.PublicSupport) * 1.5 + (city.Status is "shortage" or "integrating" ? 35 : 0) + PolicyBonus(policy, "relief"));
        AddCandidate("defense", 12 + Math.Max(0, 70 - city.Defense) + (frontline ? 55 : 0) + PolicyBonus(policy, "defense") + RoleBonus(city, "defense"));
        AddCandidate("train", city.Garrison < 2000 ? 0 : 10 + Math.Max(0, 72 - city.Training) + (frontline ? 28 : 0) + PolicyBonus(policy, "train"));
        AddCandidate("recruit", city.Garrison >= desiredGarrison ? 0 : 18 + (desiredGarrison - city.Garrison) / 90d + (frontline ? 42 : 0) + PolicyBonus(policy, "recruit"));
        var hasFree = State.Officers.Any(item => item.InitialState.Status == "free" && item.InitialState.CityId == city.Id && (factionId != State.PlayerFactionId || !State.DiscoveredOfficerIds.Contains(item.Profile.Id)));
        AddCandidate("search", 8 + (hasFree ? 58 : Math.Max(0, 60 - city.Culture) * .5) + PolicyBonus(policy, "search") + RoleBonus(city, "search"));
        return result;

        void AddCandidate(string focus, double score)
        {
            if (score <= 0) return;
            var actor = SelectDomesticActor(city, factionId, focus);
            if (actor is not null) result.Add(new DomesticCandidate(focus, score + CommandAbility(actor, focus) / 12d, actor));
        }
    }

    private ScenarioOfficerData? SelectDomesticActor(CityData city, string factionId, string focus)
    {
        return State.Officers
            .Where(item => item.InitialState.FactionId == factionId && item.InitialState.CityId == city.Id && item.InitialState.Status == "serving" && item.InitialState.Alive)
            .Where(item => city.MonthlyOfficerActionIds.Count(id => id == item.Profile.Id) < (city.GovernorId == item.Profile.Id ? 2 : 1))
            .OrderByDescending(item => CommandAbility(item, focus) - item.InitialState.Fatigue / 5)
            .FirstOrDefault();
    }

    private bool TryChooseFacility(CityData city, string factionId, string policy, out string? facilityId, out ScenarioOfficerData? builder)
    {
        facilityId = null;
        builder = null;
        if (city.ConstructionQueue is not null || city.Facilities.Count >= city.FacilitySlots || city.ActionSlots <= 0) return false;
        var ordered = new List<(string Id, double Score)>
        {
            ("granary", (Treasury(factionId).Food < State.Cities.Where(item => item.OwnerFactionId == factionId).Sum(item => CityMonthlyForecast(item).FoodUpkeep) * 3 ? 100 : 30) + PolicyBonus(policy, "agriculture")),
            ("irrigation", Math.Max(0, 75 - city.Agriculture) + PolicyBonus(policy, "agriculture")),
            ("market", Math.Max(0, 72 - city.Commerce) + PolicyBonus(policy, "commerce")),
            ("walls", (AdjacentEnemyPressure(city) > 0 ? 95 : 20) + Math.Max(0, 65 - city.Defense)),
            ("barracks", policy == "military" ? 90 : 20),
            ("drill-ground", policy == "military" && city.Training < 65 ? 85 : 15),
            ("academy", policy == "balanced" && city.Culture < 60 ? 72 : 15),
            ("administration", city.ActionCapacity < 3 ? 78 : 20),
        };
        foreach (var candidate in ordered.OrderByDescending(item => item.Score))
        {
            if (candidate.Score < 75 || city.Facilities.Any(item => item.DefinitionId == candidate.Id)) continue;
            var definition = FacilityCatalog[candidate.Id];
            var treasury = Treasury(factionId);
            if (treasury.Gold < definition.Gold || treasury.Food < definition.Food) continue;
            var actorFocus = candidate.Id switch
            {
                "market" => "commerce",
                "walls" or "barracks" or "drill-ground" => "defense",
                "academy" => "search",
                "administration" => "commerce",
                _ => "agriculture",
            };
            var actor = SelectDomesticActor(city, factionId, actorFocus);
            if (actor is null) return false;
            facilityId = candidate.Id;
            builder = actor;
            return true;
        }
        return false;
    }

    private double AdjacentEnemyPressure(CityData city)
    {
        var pressure = State.Roads
            .Where(road => road.FromCityId == city.Id || road.ToCityId == city.Id)
            .Select(road => City(road.FromCityId == city.Id ? road.ToCityId : road.FromCityId))
            .Where(other => other is not null && other.OwnerFactionId != city.OwnerFactionId)
            .Select(other => (double)other!.Garrison)
            .DefaultIfEmpty(0)
            .Max();
        pressure += State.Armies
            .Where(army => army.TargetCityId == city.Id && army.FactionId != city.OwnerFactionId && army.Status is "marching" or "besieging" or "awaiting-battle")
            .Sum(army => army.Soldiers * (army.Status == "marching" ? .75 : 1));
        return pressure;
    }

    private static double PolicyBonus(string policy, string focus) => policy switch
    {
        "recovery" when focus is "agriculture" or "patrol" or "relief" => 35,
        "commerce" when focus is "commerce" or "patrol" => 38,
        "agriculture" when focus is "agriculture" or "relief" => 42,
        "military" when focus is "defense" or "train" or "recruit" => 42,
        "integration" when focus is "patrol" or "relief" or "defense" => 38,
        "balanced" => 8,
        _ => 0,
    };

    private static double RoleBonus(CityData city, string focus)
    {
        if (city.RoleTransitionMonths > 0) return 0;
        return city.CityRole switch
        {
            "granary" when focus is "agriculture" or "relief" => 18,
            "market" when focus == "commerce" => 18,
            "garrison" when focus is "defense" or "train" or "recruit" => 18,
            "academy" when focus == "search" => 18,
            "hub" when focus is "patrol" or "commerce" => 10,
            _ => 0,
        };
    }

    private void ResetCityCivicCapacity(CityData city)
    {
        var governor = Officer(city.GovernorId);
        var governorBonus = GovernorHoldsOffice(city, governor) && EffectiveAbility(governor!, "politics", "civil") >= 75 ? 1 : 0;
        var administrationBonus = city.Facilities.Any(item => item.DefinitionId == "administration" && item.Condition >= 50) ? 1 : 0;
        var chaosPenalty = city.PublicOrder < 30 || city.Status is "unrest" or "shortage" ? 1 : 0;
        city.ActionCapacity = Math.Clamp(2 + governorBonus + administrationBonus - chaosPenalty, 1, 4);
        city.ActionSlots = city.ActionCapacity;
        city.MonthlyOfficerActionIds.Clear();
        if (city.RoleTransitionMonths > 0) city.RoleTransitionMonths--;
        if (city.IntegrationMonthsRemaining > 0) city.IntegrationMonthsRemaining--;
        if (city.IntegrationMonthsRemaining == 0 && city.Status == "integrating") city.Status = "stable";
        city.Status = DetermineCityStatus(city);
    }

    private static bool GovernorHoldsOffice(CityData city, ScenarioOfficerData? governor) =>
        governor is not null
        && city.GovernorId == governor.Profile.Id
        && governor.InitialState.FactionId == city.OwnerFactionId
        && governor.InitialState.Alive
        && governor.InitialState.Status is not "captive" and not "free";

    private string DetermineCityStatus(CityData city)
    {
        if (city.IntegrationMonthsRemaining > 0) return "integrating";
        var treasury = Treasury(city.OwnerFactionId);
        var factionFoodUpkeep = State.Cities.Where(item => item.OwnerFactionId == city.OwnerFactionId).Sum(item => CityMonthlyForecast(item).FoodUpkeep);
        if (treasury.Food < Math.Max(1, factionFoodUpkeep)) return "shortage";
        if (city.PublicOrder < 40) return "unrest";
        if (city.Fatigue >= 60) return "exhausted";
        if (AdjacentEnemyPressure(city) > 0) return "frontline";
        if (city.PublicOrder >= 75 && city.PublicSupport >= 75 && treasury.Gold >= 1000 && treasury.Food >= factionFoodUpkeep * 3) return "prosperous";
        return "stable";
    }

    private void RecordCityLedger(CityData city, string category, int goldDelta, int foodDelta, string description)
    {
        city.LedgerEntries.Add(new CityLedgerEntryData { Turn = State.Turn, Category = category, GoldDelta = goldDelta, FoodDelta = foodDelta, Description = description });
        if (city.LedgerEntries.Count > 80) city.LedgerEntries.RemoveRange(0, city.LedgerEntries.Count - 80);
    }

    private sealed record DomesticCandidate(string Focus, double Score, ScenarioOfficerData Officer);
}
