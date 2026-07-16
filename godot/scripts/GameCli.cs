using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreeKingdomsSimulator.Godot;

public static class GameCli
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private sealed class Context(ScenarioData scenario, string statePath, bool json)
    {
        public ScenarioData Scenario { get; } = scenario;
        public string StatePath { get; } = statePath;
        public bool Json { get; } = json;
        public GameRuntime Runtime { get; set; } = LoadRuntime(scenario, statePath);
        public string Notice { get; set; } = string.Empty;
        public bool Persist { get; set; }
    }

    public static int Run(ScenarioData scenario, string[] rawArgs)
    {
        try
        {
            var args = rawArgs.ToList();
            var json = TakeFlag(args, "--json");
            var statePath = TakeOption(args, "--state") ?? "user://cli-session.json";
            statePath = Globalize(statePath);
            var context = new Context(scenario, statePath, json);
            context.Runtime.Notice += message => context.Notice = message;
            var result = Execute(context, args);
            if (context.Persist) WriteState(context.StatePath, context.Runtime.State);
            Print(context, true, result);
            return 0;
        }
        catch (CliException exception)
        {
            PrintError(json: rawArgs.Contains("--json"), exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            PrintError(json: rawArgs.Contains("--json"), $"CLI 执行失败：{exception.Message}");
            return 1;
        }
    }

    private static object Execute(Context context, List<string> args)
    {
        if (args.Count == 0 || args[0] is "help" or "--help" or "-h") return Help();
        var command = Pop(args).ToLowerInvariant();
        return command switch
        {
            "reset" or "new" => Reset(context, args),
            "status" => Status(context),
            "factions" => Factions(context, args),
            "map" => MapState(context, args),
            "cities" => Cities(context, args),
            "city" => CityDetails(context, args),
            "officers" => Officers(context, args),
            "officer" => OfficerDetails(context, args),
            "armies" => Armies(context, args),
            "reports" => Reports(context, args),
            "catalog" => Catalog(args),
            "coverage" or "click-map" => Coverage(args),
            "preview" => Preview(context, args),
            "log" => Log(context, args),
            "saves" => Saves(),
            "save" => Save(context, args),
            "load" => Load(context, args),
            "develop" => Mutate(context, () => context.Runtime.DevelopCity(CityId(context, Pop(args)), OfficerId(context, Pop(args)), Pop(args))),
            "build" => Build(context, args),
            "maintain" => Maintain(context, args),
            "city-upgrade" => CityUpgrade(context, args),
            "govern" => Mutate(context, () => context.Runtime.ConfigureCityGovernance(CityId(context, Pop(args)), Pop(args), Pop(args), Pop(args))),
            "appoint" => Appoint(context, args),
            "transfer" => Mutate(context, () => context.Runtime.TransferOfficer(OfficerId(context, Pop(args)), CityId(context, Pop(args)))),
            "recruit" => Mutate(context, () => context.Runtime.RecruitOfficer(OfficerId(context, Pop(args)), OfficerId(context, Pop(args)), Pop(args))),
            "promote" => Mutate(context, () => context.Runtime.PromoteOfficer(OfficerId(context, Pop(args)), Pop(args))),
            "demote" => Mutate(context, () => context.Runtime.DemoteOfficer(OfficerId(context, Pop(args)))),
            "court-appoint" => Mutate(context, () => context.Runtime.AppointCourtOffice(OfficerId(context, Pop(args)), Pop(args))),
            "court-vacate" => Mutate(context, () => context.Runtime.VacateCourtOffice(OfficerId(context, Pop(args)))),
            "pay-arrears" => Mutate(context, () => context.Runtime.PaySalaryArrears(OfficerId(context, Pop(args)))),
            "talent" => Talent(context, args),
            "expedition-options" => ExpeditionOptions(context, args),
            "expedition" => Expedition(context, args),
            "march" => Mutate(context, () => context.Runtime.MarchArmy(ArmyId(context, Pop(args)))),
            "intercept" => Mutate(context, () => context.Runtime.OrderArmyIntercept(ArmyId(context, Pop(args)), ArmyId(context, Pop(args)))),
            "withdraw" => Mutate(context, () => context.Runtime.WithdrawArmy(ArmyId(context, Pop(args)), CityId(context, Pop(args)))),
            "diplomacy" => Diplomacy(context, args),
            "event" => Event(context, args),
            "battle" => Battle(context, args),
            "end-turn" or "end" => Mutate(context, context.Runtime.EndTurn),
            _ => throw new CliException($"未知命令：{command}。执行 help 查看可用命令。"),
        };
    }

    private static object Reset(Context context, List<string> args)
    {
        var faction = TakeOption(args, "--faction") ?? context.Scenario.PlayerFactionId;
        faction = Resolve(context.Scenario.Factions, faction, item => item.Id, item => item.Name, "势力").Id;
        var difficulty = TakeOption(args, "--difficulty") ?? "standard";
        var autoSave = TakeOption(args, "--autosave") ?? "off";
        if (difficulty is not ("standard" or "relaxed" or "hard")) throw new CliException("难度必须是 standard、relaxed 或 hard。");
        if (autoSave is not ("monthly" or "quarterly" or "yearly" or "off")) throw new CliException("自动存档频率必须是 monthly、quarterly、yearly 或 off。");
        EnsureNoArgs(args);
        context.Runtime = new GameRuntime(context.Scenario, new NewGameOptions { PlayerFactionId = faction, Difficulty = difficulty, AutoSaveFrequency = autoSave });
        context.Runtime.Notice += message => context.Notice = message;
        context.Persist = true;
        return new { message = "已新建 CLI 试玩局。", state = Status(context) };
    }

    private static object Status(Context context)
    {
        var state = context.Runtime.State;
        var faction = context.Runtime.Faction(state.PlayerFactionId);
        var cities = state.Cities.Where(item => item.OwnerFactionId == state.PlayerFactionId).ToList();
        return new
        {
            state.Turn,
            date = $"{state.Year}年{state.Month}月",
            factionId = state.PlayerFactionId,
            faction = faction?.Name,
            resources = state.Resources,
            cityCount = cities.Count,
            officerCount = context.Runtime.PlayerOfficers().Count(),
            activeArmies = state.Armies.Count(item => item.FactionId == state.PlayerFactionId && item.Status is "marching" or "besieging" or "retreating" or "awaiting-battle"),
            pendingEvent = PendingEvent(context),
            pendingBattle = PendingBattle(context),
            state.Outcome,
            state.OutcomeMessage,
            statePath = context.StatePath,
        };
    }

    private static object Factions(Context context, List<string> args)
    {
        EnsureNoArgs(args);
        var state = context.Runtime.State;
        return state.Factions.Select(faction =>
        {
            var cities = state.Cities.Where(city => city.OwnerFactionId == faction.Id).ToList();
            var officers = state.Officers.Where(officer => officer.InitialState.FactionId == faction.Id && officer.InitialState.Alive).ToList();
            var resources = faction.Id == state.PlayerFactionId
                ? state.Resources
                : state.FactionTreasuries.GetValueOrDefault(faction.Id) ?? new ResourceData();
            return new
            {
                faction.Id, faction.Name, faction.ShortName, faction.RulerName,
                isPlayer = faction.Id == state.PlayerFactionId,
                isEliminated = cities.Count == 0,
                cityCount = cities.Count,
                cities = cities.Select(city => city.Name).ToList(),
                officerCount = officers.Count,
                servingOfficerCount = officers.Count(officer => officer.InitialState.Status is "serving" or "marching" or "deployed"),
                captiveOfficerCount = officers.Count(officer => officer.InitialState.Status == "captive"),
                resources,
            };
        }).ToList();
    }

    private static object MapState(Context context, List<string> args)
    {
        EnsureNoArgs(args);
        return new
        {
            cities = context.Runtime.State.Cities.Select(city => new
            {
                city.Id, city.Name, factionId = city.OwnerFactionId, faction = context.Runtime.Faction(city.OwnerFactionId)?.Name,
                city.Position, city.Garrison, city.Status,
            }),
            roads = context.Runtime.State.Roads.Select(road => new
            {
                road.Id,
                fromCityId = road.FromCityId,
                from = context.Runtime.City(road.FromCityId)?.Name,
                toCityId = road.ToCityId,
                to = context.Runtime.City(road.ToCityId)?.Name,
                road.Kind, road.Terrain, road.TravelDays,
            }),
            passes = context.Runtime.State.Passes,
            armies = Armies(context, []),
        };
    }

    private static object Cities(Context context, List<string> args)
    {
        var all = TakeFlag(args, "--all");
        EnsureNoArgs(args);
        return context.Runtime.State.Cities.Where(item => all || item.OwnerFactionId == context.Runtime.State.PlayerFactionId)
            .Select(city => new
            {
                city.Id, city.Name, faction = context.Runtime.Faction(city.OwnerFactionId)?.Name, city.Population, city.Garrison,
                city.CityLevel, city.FacilitySlots, nextSlotUnlockLevel = GameRuntime.NextFacilitySlotUnlockLevel(city.CityLevel),
                city.Agriculture, city.Commerce, city.PublicOrder, city.PublicSupport, city.Defense, city.Training,
                actions = $"{city.ActionSlots}/{city.ActionCapacity}", city.Status, city.GovernanceMode, city.GovernancePolicy, city.CityRole,
            }).ToList();
    }

    private static object CityDetails(Context context, List<string> args)
    {
        var city = FindCity(context, Pop(args)); EnsureNoArgs(args);
        var forecast = context.Runtime.CityMonthlyForecast(city);
        var officers = context.Runtime.State.Officers.Where(item => item.InitialState.CityId == city.Id && item.InitialState.Alive)
            .Select(item => new { item.Profile.Id, item.Profile.Name, item.InitialState.Status, item.InitialState.Appointment }).ToList();
        return new
        {
            city,
            owner = context.Runtime.Faction(city.OwnerFactionId)?.Name,
            governor = context.Runtime.Officer(city.GovernorId)?.Profile.Name ?? city.GovernorName,
            monthlyForecast = new { forecast.GoldIncome, forecast.FoodIncome, forecast.GoldUpkeep, forecast.FoodUpkeep },
            priority = context.Runtime.CityPrioritySummary(city),
            officers,
        };
    }

    private static object Officers(Context context, List<string> args)
    {
        var all = TakeFlag(args, "--all");
        var cityValue = TakeOption(args, "--city");
        var cityId = cityValue is null ? null : CityId(context, cityValue);
        EnsureNoArgs(args);
        return context.Runtime.State.Officers
            .Where(item => item.InitialState.Alive && (all || item.InitialState.FactionId == context.Runtime.State.PlayerFactionId) && (cityId is null || item.InitialState.CityId == cityId))
            .Select(item => new
            {
                item.Profile.Id, item.Profile.Name, faction = context.Runtime.Faction(item.InitialState.FactionId)?.Name,
                city = context.Runtime.City(item.InitialState.CityId)?.Name, item.InitialState.Status, item.InitialState.Appointment,
                item.InitialState.Level, item.InitialState.Loyalty, item.InitialState.Merit, item.InitialState.OfficeTrack, item.InitialState.OfficeRank,
                abilities = item.Profile.Abilities,
            }).ToList();
    }

    private static object OfficerDetails(Context context, List<string> args)
    {
        var officer = FindOfficer(context, Pop(args)); EnsureNoArgs(args);
        return new
        {
            officer.Profile,
            officer.InitialState,
            faction = context.Runtime.Faction(officer.InitialState.FactionId)?.Name,
            city = context.Runtime.City(officer.InitialState.CityId)?.Name,
            progression = context.Runtime.OfficerProgressionSummary(officer),
            recruitmentMethod = context.Runtime.RecruitmentMethod(officer.Profile.Id),
        };
    }

    private static object Armies(Context context, List<string> args)
    {
        var all = TakeFlag(args, "--all"); EnsureNoArgs(args);
        return context.Runtime.State.Armies.Where(item => all || item.FactionId == context.Runtime.State.PlayerFactionId).Select(army => new
        {
            army.Id, faction = context.Runtime.Faction(army.FactionId)?.Name,
            commander = context.Runtime.Officer(army.CommanderId)?.Profile.Name,
            source = context.Runtime.City(army.SourceCityId)?.Name, target = context.Runtime.City(army.TargetCityId)?.Name,
            army.TargetArmyId, army.Soldiers, army.Food, army.Composition, army.SpecialTroops, army.RemainingDays, army.TotalDays,
            army.LastMarchTurn, army.Status, army.FormationId, army.Stance, army.Tactic,
        }).ToList();
    }

    private static object Reports(Context context, List<string> args)
    {
        var count = args.Count == 0 ? 10 : Int(Pop(args), "战报条数"); EnsureNoArgs(args);
        return context.Runtime.State.BattleReports.TakeLast(Math.Clamp(count, 1, 100)).Select(report => new
        {
            report.Id, report.Turn, report.CityId, report.CityName, report.BattleType,
            attacker = context.Runtime.Faction(report.AttackerFactionId)?.Name,
            defender = context.Runtime.Faction(report.DefenderFactionId)?.Name,
            report.PlayerSide, report.AttackerBefore, report.AttackerAfter, report.DefenderBefore, report.DefenderAfter,
            report.Result, report.CityCaptured, report.FormationId, report.Stance, report.PrimaryTactic,
            report.DecisionSummary, report.Narrative, report.OfficerContributions, report.PhaseResults,
        }).ToList();
    }

    private static object Log(Context context, List<string> args)
    {
        var count = args.Count == 0 ? 20 : Int(Pop(args), "条数"); EnsureNoArgs(args);
        return context.Runtime.State.Log.TakeLast(Math.Clamp(count, 1, 200)).ToList();
    }

    private static object Saves() => SaveService.List();

    private static object Catalog(List<string> args)
    {
        EnsureNoArgs(args);
        return new
        {
            cityFocuses = new[] { "agriculture", "commerce", "patrol", "defense", "recruit", "train", "search", "relief" },
            governanceModes = new[] { "manual", "delegated" },
            governancePolicies = new[] { "balanced", "recovery", "commerce", "agriculture", "military", "integration" },
            cityRoles = new[] { "unassigned", "granary", "market", "garrison", "academy", "hub" },
            appointments = new[] { "governor", "strategist", "civil", "general", "reserve" },
            facilities = GameRuntime.FacilityCatalog.Select(item => new { id = item.Key, item.Value.Name, item.Value.Gold, item.Value.Food, item.Value.Months }),
            cityUpgrades = GameRuntime.CityUpgradeCatalog.Values,
            formations = new[] { "goose", "wedge", "crane", "shield", "siege-array" }.Select(id => new { id, name = BattleCatalog.FormationName(id) }),
            troopOrders = new[] { "shield-line", "loose-line", "assault-column", "spear-wall", "support-line", "spear-column", "rear-double", "wing-fire", "skirmish", "wing-column", "cavalry-wedge", "reserve", "protected-siege", "gate-column", "wall-pressure" }.Select(id => new { id, name = BattleCatalog.OrderName(id) }),
            stances = new[] { "standard", "cautious", "aggressive" },
            tactics = new[] { "steady-advance", "shield-wall", "feigned-retreat", "night-raid", "fire-attack", "encirclement", "arrow-volley", "cavalry-charge", "fortify-camp", "cut-supply", "siege-ladders", "undermine-walls" }.Select(id => new { id, name = BattleCatalog.TacticName(id) }),
            courtOffices = OfficerProgressionRules.CourtOffices,
            specialTroops = OfficerProgressionRules.SpecialTroops.Values,
        };
    }

    private static object Coverage(List<string> args)
    {
        EnsureNoArgs(args);
        return new
        {
            newGame = new Dictionary<string, string> { ["选择势力/难度/自动存档并开始"] = "factions；reset --faction <势力> --difficulty <难度> --autosave <频率>" },
            worldMap = new Dictionary<string, string> { ["查看天下与道路"] = "map", ["点击城池"] = "city <城>", ["点击军团"] = "armies --all", ["继续前进"] = "march <军团>", ["出击/改令拦截"] = "expedition ... --target-army <敌军>；intercept <我军> <敌军>", ["撤兵"] = "withdraw <军团> <己方城>" },
            domestic = new Dictionary<string, string> { ["执行八类城务"] = "preview develop ...；develop ...", ["建造设施"] = "build ...", ["升级城池/取消升级"] = "city-upgrade <城> <武将>；city-upgrade cancel <城>", ["升级/修缮设施"] = "maintain ... upgrade|repair", ["应用治理方针"] = "govern ..." },
            talent = new Dictionary<string, string> { ["查看人才府"] = "talent status", ["执行招募"] = "talent recruit ...", ["确认调动"] = "talent transfer ...", ["任命"] = "talent appoint ...", ["晋升/转序"] = "talent promote ...", ["降职"] = "talent demote ...", ["朝堂任命/卸任"] = "talent court-appoint|court-vacate ...", ["补发欠俸"] = "talent pay-arrears ..." },
            diplomacy = new Dictionary<string, string> { ["查看关系与来使"] = "diplomacy list", ["查看提案成功率"] = "diplomacy preview ...", ["派遣使者"] = "diplomacy propose ...", ["接受/拒绝提案"] = "diplomacy respond accept|reject" },
            expedition = new Dictionary<string, string> { ["查看出征选项/推荐编制"] = "expedition-options <城>", ["确认出征"] = "expedition ...", ["本回合行军/继续攻城"] = "march <军团>" },
            battle = new Dictionary<string, string> { ["战前布阵"] = "battle configure ...", ["下令开战"] = "battle start", ["速度×1/×2与时间推进"] = "battle advance <秒>，由调用方选择推进量", ["全选我军"] = "battle command <军令> all", ["攻击/移动/固守等实时军令"] = "battle command ...", ["跳过演算"] = "battle skip" },
            events = new Dictionary<string, string> { ["查看随机事件"] = "event show", ["点击事件选项"] = "event choose <选项ID>" },
            reportsAndSaves = new Dictionary<string, string> { ["确认/查看战报"] = "reports", ["保存/覆盖手动档"] = "save <槽位>", ["载入存档"] = "load manual|auto <槽位>", ["查看存档"] = "saves" },
            global = new Dictionary<string, string> { ["结束本月"] = "end-turn", ["查看当前局势"] = "status" },
            uiOnly = new[] { "页面导航、关闭面板、地图/战场镜头缩放、背景音乐、动效开关、退出游戏不改变玩法局势，因此由查询或终止 CLI 进程替代。" },
        };
    }

    private static object Preview(Context context, List<string> args)
    {
        var kind = Pop(args);
        if (kind == "end-turn")
        {
            EnsureNoArgs(args);
            return context.Runtime.PreviewEndTurnResourceDelta();
        }
        if (kind == "develop")
        {
            var city = CityId(context, Pop(args)); var officer = OfficerId(context, Pop(args)); var focus = Pop(args); EnsureNoArgs(args);
            return new { preview = context.Runtime.CityCommandPreview(city, officer, focus) };
        }
        throw new CliException("预估子命令必须是 end-turn 或 develop。");
    }

    private static object Save(Context context, List<string> args)
    {
        var slot = Int(Pop(args), "存档槽"); EnsureNoArgs(args);
        SaveService.WriteManual(context.Runtime.State, slot);
        return new { message = $"已保存到手动档{Math.Clamp(slot, 1, 10)}。" };
    }

    private static object Load(Context context, List<string> args)
    {
        var kind = Pop(args); var slot = Int(Pop(args), "存档槽"); EnsureNoArgs(args);
        if (kind is not ("manual" or "auto")) throw new CliException("存档类型必须是 manual 或 auto。");
        var state = SaveService.Load(kind, slot) ?? throw new CliException("指定存档不存在或无法读取。");
        context.Runtime.Replace(state); context.Persist = true;
        return new { message = context.Notice, state = Status(context) };
    }

    private static object Build(Context context, List<string> args)
    {
        var cityId = CityId(context, Pop(args)); var officerId = OfficerId(context, Pop(args)); var facility = Pop(args);
        var slot = args.Count == 0 ? -1 : Int(Pop(args), "设施地块") - 1; EnsureNoArgs(args);
        return Mutate(context, () => context.Runtime.BuildFacility(cityId, officerId, facility, slot));
    }

    private static object Maintain(Context context, List<string> args)
    {
        var cityId = CityId(context, Pop(args)); var instanceId = Pop(args); var action = Pop(args); EnsureNoArgs(args);
        if (action is not ("upgrade" or "repair")) throw new CliException("维护方式必须是 upgrade 或 repair。");
        return Mutate(context, () => context.Runtime.MaintainFacility(cityId, instanceId, action == "upgrade"));
    }

    private static object CityUpgrade(Context context, List<string> args)
    {
        if (args.Count > 0 && args[0] == "cancel")
        {
            Pop(args); var cityId = CityId(context, Pop(args)); EnsureNoArgs(args);
            return Mutate(context, () => context.Runtime.CancelCityUpgrade(cityId));
        }
        if (args.Count > 0 && args[0] == "reassign")
        {
            Pop(args); var cityId = CityId(context, Pop(args)); var officerId = OfficerId(context, Pop(args)); EnsureNoArgs(args);
            return Mutate(context, () => context.Runtime.ReassignCityUpgrade(cityId, officerId));
        }
        var city = CityId(context, Pop(args)); var officer = OfficerId(context, Pop(args)); EnsureNoArgs(args);
        return Mutate(context, () => context.Runtime.UpgradeCity(city, officer));
    }

    private static object Appoint(Context context, List<string> args)
    {
        var officer = OfficerId(context, Pop(args)); var appointment = Pop(args); EnsureNoArgs(args);
        if (appointment is not ("governor" or "strategist" or "civil" or "general" or "reserve")) throw new CliException("任命类型必须是 governor、strategist、civil、general 或 reserve。");
        return Mutate(context, () => context.Runtime.AppointOfficer(officer, appointment));
    }

    private static object Talent(Context context, List<string> args)
    {
        var action = args.Count == 0 ? "status" : Pop(args);
        if (action is "status" or "list")
        {
            EnsureNoArgs(args);
            return new
            {
                officers = Officers(context, []),
                candidates = RecruitmentCandidates(context),
                courtOffices = OfficerProgressionRules.CourtOffices.Select(office => new
                {
                    office,
                    holder = context.Runtime.PlayerOfficers().FirstOrDefault(item => item.InitialState.CourtOfficeId == office.Id)?.Profile.Name,
                }),
                salary = new { context.Runtime.State.MonthlySalaryDue, context.Runtime.State.MonthlySalaryPaid },
            };
        }
        if (action == "candidates") { EnsureNoArgs(args); return RecruitmentCandidates(context); }
        if (action == "recruit") return Mutate(context, () => context.Runtime.RecruitOfficer(OfficerId(context, Pop(args)), OfficerId(context, Pop(args)), Pop(args)));
        if (action == "transfer") return Mutate(context, () => context.Runtime.TransferOfficer(OfficerId(context, Pop(args)), CityId(context, Pop(args))));
        if (action == "appoint") return Appoint(context, args);
        if (action == "promote") return Mutate(context, () => context.Runtime.PromoteOfficer(OfficerId(context, Pop(args)), Pop(args)));
        if (action == "demote") return Mutate(context, () => context.Runtime.DemoteOfficer(OfficerId(context, Pop(args))));
        if (action == "court-appoint") return Mutate(context, () => context.Runtime.AppointCourtOffice(OfficerId(context, Pop(args)), Pop(args)));
        if (action == "court-vacate") return Mutate(context, () => context.Runtime.VacateCourtOffice(OfficerId(context, Pop(args))));
        if (action == "pay-arrears") return Mutate(context, () => context.Runtime.PaySalaryArrears(OfficerId(context, Pop(args))));
        throw new CliException("人才子命令必须是 status、candidates、recruit、transfer、appoint、promote、demote、court-appoint、court-vacate 或 pay-arrears。");
    }

    private static object RecruitmentCandidates(Context context)
    {
        var actors = context.Runtime.PlayerOfficers().Where(item => item.InitialState.Status == "serving").ToList();
        return context.Runtime.State.Officers.Where(item => context.Runtime.IsRecruitmentCandidate(item.Profile.Id)).Select(candidate =>
        {
            var chances = actors.Select(actor => new
            {
                actorId = actor.Profile.Id,
                actor = actor.Profile.Name,
                chance = context.Runtime.RecruitmentChance(candidate.Profile.Id, actor.Profile.Id),
            }).Where(item => item.chance > 0).OrderByDescending(item => item.chance).ToList();
            return new
            {
                candidateId = candidate.Profile.Id,
                candidate = candidate.Profile.Name,
                method = context.Runtime.RecruitmentMethod(candidate.Profile.Id),
                methodName = GameRuntime.RecruitmentMethodLabel(context.Runtime.RecruitmentMethod(candidate.Profile.Id)),
                faction = context.Runtime.Faction(candidate.InitialState.FactionId)?.Name,
                city = context.Runtime.City(candidate.InitialState.CityId)?.Name,
                candidate.InitialState.Status,
                candidate.InitialState.Loyalty,
                actorChances = chances,
            };
        }).ToList();
    }

    private static object ExpeditionOptions(Context context, List<string> args)
    {
        var city = FindCity(context, Pop(args));
        var requested = TakeOption(args, "--soldiers");
        var total = requested is null ? Math.Min(city.Garrison, 4000) : Math.Min(city.Garrison, Int(requested, "兵力"));
        EnsureNoArgs(args);
        if (city.OwnerFactionId != context.Runtime.State.PlayerFactionId) throw new CliException("出发城必须是己方城池。");
        var recommended = RecommendedComposition(total);
        return new
        {
            source = new { city.Id, city.Name, city.Garrison, city.Training },
            treasury = context.Runtime.State.Resources,
            commanders = context.Runtime.PlayerOfficers().Where(item => item.InitialState.CityId == city.Id && item.InitialState.Status == "serving").Select(item => new
            {
                item.Profile.Id, item.Profile.Name,
                leadership = context.Runtime.EffectiveAbility(item, "leadership", "military"),
                might = context.Runtime.EffectiveAbility(item, "might", "military"),
                item.InitialState.Appointment,
            }),
            targets = context.Runtime.State.Cities.Where(item => item.OwnerFactionId != context.Runtime.State.PlayerFactionId).Select(item => new { item.Id, item.Name, faction = context.Runtime.Faction(item.OwnerFactionId)?.Name, item.Garrison }),
            interceptTargets = context.Runtime.State.Armies.Where(item => item.FactionId != context.Runtime.State.PlayerFactionId && item.Status is "marching" or "besieging").Select(item => new { item.Id, commander = context.Runtime.Officer(item.CommanderId)?.Profile.Name, faction = context.Runtime.Faction(item.FactionId)?.Name, item.Soldiers, item.Status }),
            recommended,
            wholeGarrison = RecommendedComposition(city.Garrison),
            specialTroops = OfficerProgressionRules.SpecialTroops.Values.Where(item => item.FactionIds.Contains(context.Runtime.State.PlayerFactionId)),
        };
    }

    private static Dictionary<string, int> RecommendedComposition(int total)
    {
        total = Math.Max(0, total);
        var archers = total >= 1000 ? Math.Max(500, total / 4 / 500 * 500) : 0;
        return new Dictionary<string, int> { ["infantry"] = Math.Max(0, total - archers), ["archers"] = archers };
    }

    private static object Expedition(Context context, List<string> args)
    {
        var source = CityId(context, Pop(args)); var target = CityId(context, Pop(args)); var commander = OfficerId(context, Pop(args));
        var soldiers = Int(Pop(args), "兵力"); var food = Int(Pop(args), "军粮");
        var deputies = Csv(TakeOption(args, "--deputies")).Select(value => OfficerId(context, value)).ToList();
        var composition = Map(TakeOption(args, "--composition"));
        var special = Map(TakeOption(args, "--special"));
        var targetArmyValue = TakeOption(args, "--target-army");
        var targetArmy = targetArmyValue is null ? "" : ArmyId(context, targetArmyValue);
        EnsureNoArgs(args);
        return Mutate(context, () => context.Runtime.CreateExpedition(source, target, commander, soldiers, food, deputies,
            composition.Count == 0 ? null : composition, special.Count == 0 ? null : special, targetArmy));
    }

    private static object Diplomacy(Context context, List<string> args)
    {
        var action = args.Count == 0 ? "list" : Pop(args);
        if (action == "list")
        {
            EnsureNoArgs(args);
            return context.Runtime.State.Diplomacy.Select(item => new
            {
                item.FactionId, faction = context.Runtime.Faction(item.FactionId)?.Name, item.Relation, item.Trust, item.Treaties,
                pending = context.Runtime.State.AiDiplomaticProposals.FirstOrDefault(p => p.FromFactionId == item.FactionId && p.Status == "pending"),
            }).ToList();
        }
        if (action == "propose")
        {
            var faction = FactionId(context, Pop(args)); var type = Pop(args); var gift = args.Count == 0 ? 0 : Int(Pop(args), "赠礼"); EnsureNoArgs(args);
            return Mutate(context, () => context.Runtime.ProposeDiplomacy(faction, type, gift));
        }
        if (action == "preview")
        {
            var faction = FactionId(context, Pop(args)); var type = Pop(args); var gift = args.Count == 0 ? 0 : Int(Pop(args), "赠礼"); EnsureNoArgs(args);
            return new
            {
                factionId = faction,
                faction = context.Runtime.Faction(faction)?.Name,
                type,
                typeName = GameRuntime.DiplomacyLabel(type),
                gift,
                chance = context.Runtime.DiplomacyChance(faction, type, gift),
                effect = GameRuntime.DiplomacyEffect(type),
                activeTreaty = context.Runtime.HasTreaty(faction, type),
                canExchangeCaptives = context.Runtime.CanExchangeCaptives(faction),
            };
        }
        if (action == "respond")
        {
            var response = Pop(args); EnsureNoArgs(args);
            if (response is not ("accept" or "reject")) throw new CliException("回应必须是 accept 或 reject。");
            return Mutate(context, () => context.Runtime.RespondToAiProposal(response == "accept"));
        }
        throw new CliException("外交子命令必须是 list、propose 或 respond。");
    }

    private static object Event(Context context, List<string> args)
    {
        var action = args.Count == 0 ? "show" : Pop(args);
        if (action == "show") { EnsureNoArgs(args); return PendingEvent(context) ?? new { message = "当前没有待处理事件。" }; }
        if (action == "choose")
        {
            var choice = Pop(args); EnsureNoArgs(args);
            return Mutate(context, () => context.Runtime.ChooseEvent(choice));
        }
        throw new CliException("事件子命令必须是 show 或 choose。");
    }

    private static object Battle(Context context, List<string> args)
    {
        var action = args.Count == 0 ? "show" : Pop(args);
        if (action == "show") { EnsureNoArgs(args); return PendingBattle(context) ?? new { message = "当前没有待处理战斗。" }; }
        if (action == "configure")
        {
            var formation = Pop(args); var orders = StringMap(TakeOption(args, "--orders"));
            var stance = TakeOption(args, "--stance") ?? ""; var tactic = TakeOption(args, "--tactic") ?? ""; EnsureNoArgs(args);
            return Mutate(context, () => context.Runtime.ConfigurePendingBattle(formation, orders, stance, tactic));
        }
        if (action == "start") return Mutate(context, context.Runtime.StartPendingBattle, args);
        if (action == "advance") return AdvanceBattle(context, args);
        if (action == "command")
        {
            var command = Pop(args); var groupValue = Pop(args); var target = TakeOption(args, "--target") ?? "";
            var x = Float(TakeOption(args, "--x") ?? "0", "x"); var y = Float(TakeOption(args, "--y") ?? "0", "y"); EnsureNoArgs(args);
            var pending = context.Runtime.State.PendingBattle;
            var groups = groupValue == "all" && pending is not null
                ? pending.Groups.Where(item => item.Side == pending.PlayerSide && item.FinalSoldiers > 0 && !item.IsRouted).Select(item => item.Id).ToList()
                : Csv(groupValue);
            return Mutate(context, () => context.Runtime.IssueBattleCommand(groups, command, target, x, y));
        }
        if (action is "finish" or "skip") return Mutate(context, context.Runtime.CompletePendingBattle, args);
        if (action == "auto")
        {
            EnsureNoArgs(args);
            var pending = context.Runtime.State.PendingBattle ?? throw new CliException("当前没有待处理战斗。");
            if (pending.Status == "planning" && !context.Runtime.StartPendingBattle()) throw new CliException(context.Notice);
            return Mutate(context, context.Runtime.CompletePendingBattle);
        }
        throw new CliException("战斗子命令必须是 show、configure、start、advance、command、finish、skip 或 auto。");
    }

    private static object AdvanceBattle(Context context, List<string> args)
    {
        var seconds = Float(Pop(args), "推进秒数"); EnsureNoArgs(args);
        if (seconds <= 0) throw new CliException("推进秒数必须大于0。");
        if (!context.Runtime.AdvancePendingBattle(seconds)) throw new CliException("当前没有正在演算的战斗。");
        context.Persist = true;
        var battle = context.Runtime.State.PendingBattle;
        return new
        {
            message = $"战斗已推进{seconds:0.##}秒。",
            elapsed = battle?.Elapsed,
            duration = battle?.Duration,
            status = battle?.Status,
            battle = PendingBattle(context),
        };
    }

    private static object Mutate(Context context, Func<bool> action, List<string>? args = null)
    {
        if (args is not null) EnsureNoArgs(args);
        var ok = action();
        if (!ok) throw new CliException(string.IsNullOrEmpty(context.Notice) ? "操作未执行。" : context.Notice);
        context.Persist = true;
        return new { message = context.Notice, state = BriefState(context) };
    }

    private static object BriefState(Context context) => new
    {
        context.Runtime.State.Turn,
        date = $"{context.Runtime.State.Year}年{context.Runtime.State.Month}月",
        context.Runtime.State.Resources,
        pendingEvent = context.Runtime.State.PendingEvent?.DefinitionId,
        pendingBattle = context.Runtime.State.PendingBattle?.Id,
        context.Runtime.State.Outcome,
    };

    private static object? PendingEvent(Context context)
    {
        var pending = context.Runtime.State.PendingEvent;
        if (pending is null) return null;
        var definition = context.Runtime.State.Events.FirstOrDefault(item => item.Id == pending.DefinitionId);
        return new
        {
            pending.Id,
            definitionId = pending.DefinitionId,
            title = definition?.Title,
            city = context.Runtime.City(pending.CityId)?.Name,
            description = definition?.Description,
            choices = definition?.Choices.Select(item => new { item.Id, item.Label, item.Description }),
        };
    }

    private static object? PendingBattle(Context context)
    {
        var battle = context.Runtime.State.PendingBattle;
        if (battle is null) return null;
        return new
        {
            battle.Id, battle.Status, battle.BattleType, battle.PlayerSide,
            city = context.Runtime.City(battle.CityId)?.Name,
            attacker = context.Runtime.Faction(battle.AttackerFactionId)?.Name,
            defender = context.Runtime.Faction(battle.DefenderFactionId)?.Name,
            battle.AttackerBefore, battle.DefenderBefore,
            formation = battle.PlayerSide == "attacker" ? battle.AttackerFormation.FormationId : battle.DefenderFormation.FormationId,
            stance = battle.PlayerSide == "attacker" ? battle.Stance : battle.DefenderStance,
            tactic = battle.PlayerSide == "attacker" ? battle.PrimaryTactic : battle.DefenderPrimaryTactic,
            friendlyGroups = battle.Groups.Where(item => item.Side == battle.PlayerSide).Select(item => new { item.Id, item.TroopType, item.FinalSoldiers, item.State }),
            enemyGroups = battle.Groups.Where(item => item.Side != battle.PlayerSide).Select(item => new { item.Id, item.TroopType, item.FinalSoldiers, item.State }),
            battle.Result, battle.Summary,
        };
    }

    private static object Help() => new
    {
        usage = "./tools/game-cli.sh [--json] <command> [arguments]",
        notes = new[] { "默认局势保存在 .playtest/cli-session.json，每次成功操作后自动保存。", "城池、势力、武将参数均可使用 ID 或唯一中文名。", "执行 reset 可重新开局；查询命令不会改变游戏。" },
        queries = new[] { "status", "factions", "map", "cities [--all]", "city <城>", "officers [--all] [--city <城>]", "officer <武将>", "talent status|candidates", "armies [--all]", "expedition-options <城> [--soldiers <兵力>]", "reports [条数]", "catalog", "coverage", "preview end-turn", "preview develop <城> <武将> <城务>", "diplomacy list|preview ...", "event show", "battle show", "log [条数]", "saves" },
        session = new[] { "reset [--faction <势力>] [--difficulty standard|relaxed|hard] [--autosave monthly|quarterly|yearly|off]", "save <1-10>", "load manual|auto <槽位>" },
        domestic = new[] { "develop <城> <武将> agriculture|commerce|patrol|defense|recruit|train|search|relief", "build <城> <武将> <设施ID> [槽位]", "city-upgrade <城> <武将>", "city-upgrade cancel <城>", "city-upgrade reassign <城> <武将>", "maintain <城> <设施实例ID> upgrade|repair", "govern <城> manual|delegated <方针> <定位>" },
        talent = new[] { "talent status|candidates", "talent appoint <武将> governor|strategist|civil|general|reserve", "talent transfer <武将> <目标城>", "talent recruit <候选人> <执行者> <任命>", "talent promote <武将> civil|military", "talent demote <武将>", "talent court-appoint <武将> <朝堂职位ID>", "talent court-vacate <武将>", "talent pay-arrears <武将>" },
        military = new[] { "expedition <起点> <目标> <主将> <兵力> <军粮> [--deputies 张辽,典韦] [--composition infantry=2000,archers=1000] [--special danyang-veterans=500] [--target-army <军团>]", "march <军团>", "intercept <我军> <敌军>", "withdraw <军团> <己方城>" },
        diplomacy = new[] { "diplomacy preview <势力> trade|truce|captive-exchange [赠礼]", "diplomacy propose <势力> trade|truce|captive-exchange [赠礼]", "diplomacy respond accept|reject" },
        battleAndTurn = new[] { "event choose <选项ID>", "battle configure <阵型> [--orders infantry=shield-line,...] [--stance <姿态>] [--tactic <战术>]", "battle start", "battle advance <秒>", "battle command <命令> <我方编组ID,...|all> [--target <敌方编组>] [--x N --y N]", "battle finish|skip|auto", "end-turn" },
    };

    private static GameRuntime LoadRuntime(ScenarioData scenario, string statePath)
    {
        var runtime = new GameRuntime(scenario, new NewGameOptions { AutoSaveFrequency = "off" });
        if (!File.Exists(statePath)) return runtime;
        try
        {
            var state = JsonSerializer.Deserialize<GameSession>(File.ReadAllText(statePath), JsonOptions);
            if (state is not null) runtime.Replace(state);
        }
        catch (JsonException exception)
        {
            throw new CliException($"CLI 局势文件损坏：{exception.Message}");
        }
        return runtime;
    }

    private static void WriteState(string statePath, GameSession state)
    {
        var directory = Path.GetDirectoryName(statePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temporary = statePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporary, statePath, true);
    }

    private static string Globalize(string path) => path.StartsWith("user://", StringComparison.Ordinal)
        ? global::Godot.ProjectSettings.GlobalizePath(path)
        : Path.GetFullPath(path);

    private static CityData FindCity(Context context, string value) => Resolve(context.Runtime.State.Cities, value, item => item.Id, item => item.Name, "城池");
    private static ScenarioOfficerData FindOfficer(Context context, string value) => Resolve(context.Runtime.State.Officers, value, item => item.Profile.Id, item => item.Profile.Name, "武将");
    private static FactionData FindFaction(Context context, string value) => Resolve(context.Runtime.State.Factions, value, item => item.Id, item => item.Name, "势力");
    private static ArmyData FindArmy(Context context, string value)
    {
        var exact = context.Runtime.State.Armies.FirstOrDefault(item => item.Id == value);
        if (exact is not null) return exact;
        var matches = context.Runtime.State.Armies.Where(item => context.Runtime.Officer(item.CommanderId)?.Profile.Name == value).ToList();
        return matches.Count == 1 ? matches[0] : throw new CliException(matches.Count == 0 ? $"找不到军团：{value}" : $"军团主将名称不唯一，请使用军团 ID：{value}");
    }
    private static string CityId(Context context, string value) => FindCity(context, value).Id;
    private static string OfficerId(Context context, string value) => FindOfficer(context, value).Profile.Id;
    private static string FactionId(Context context, string value) => FindFaction(context, value).Id;
    private static string ArmyId(Context context, string value) => FindArmy(context, value).Id;

    private static T Resolve<T>(IEnumerable<T> values, string value, Func<T, string> id, Func<T, string> name, string kind)
    {
        var exact = values.FirstOrDefault(item => id(item) == value);
        if (exact is not null) return exact;
        var matches = values.Where(item => name(item) == value).ToList();
        return matches.Count == 1 ? matches[0] : throw new CliException(matches.Count == 0 ? $"找不到{kind}：{value}" : $"{kind}名称不唯一，请使用 ID：{value}");
    }

    private static string Pop(List<string> args)
    {
        if (args.Count == 0) throw new CliException("缺少命令参数。执行 help 查看格式。");
        var value = args[0]; args.RemoveAt(0); return value;
    }

    private static bool TakeFlag(List<string> args, string name)
    {
        var index = args.IndexOf(name);
        if (index < 0) return false;
        args.RemoveAt(index); return true;
    }

    private static string? TakeOption(List<string> args, string name)
    {
        var index = args.IndexOf(name);
        if (index < 0) return null;
        if (index + 1 >= args.Count) throw new CliException($"{name} 缺少值。");
        var value = args[index + 1]; args.RemoveRange(index, 2); return value;
    }

    private static int Int(string value, string label) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
        ? result : throw new CliException($"{label}必须是整数：{value}");
    private static float Float(string value, string label) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
        ? result : throw new CliException($"{label}必须是数字：{value}");
    private static List<string> Csv(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    private static Dictionary<string, int> Map(string? value) => Csv(value).ToDictionary(PairKey, pair => Int(PairValue(pair), PairKey(pair)));
    private static Dictionary<string, string> StringMap(string? value) => Csv(value).ToDictionary(PairKey, PairValue);
    private static string PairKey(string pair) { var index = pair.IndexOf('='); if (index <= 0) throw new CliException($"键值参数格式应为 key=value：{pair}"); return pair[..index]; }
    private static string PairValue(string pair) { var index = pair.IndexOf('='); if (index <= 0 || index == pair.Length - 1) throw new CliException($"键值参数格式应为 key=value：{pair}"); return pair[(index + 1)..]; }
    private static void EnsureNoArgs(List<string> args) { if (args.Count > 0) throw new CliException($"多余参数：{string.Join(' ', args)}"); }

    private static void Print(Context context, bool ok, object result)
    {
        if (context.Json) Console.WriteLine(JsonSerializer.Serialize(new { ok, result }, JsonOptions));
        else
        {
            if (result is string text) Console.WriteLine(text);
            else Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
        }
    }

    private static void PrintError(bool json, string message)
    {
        if (json) Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = message }, JsonOptions));
        else Console.Error.WriteLine($"错误：{message}");
    }

    private sealed class CliException(string message) : Exception(message);
}
