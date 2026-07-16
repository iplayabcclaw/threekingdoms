namespace ThreeKingdomsSimulator.Godot;

public sealed record TraitDefinition(
    string Name,
    string Quality,
    string Category,
    string Description,
    string[] DomesticFocuses,
    double DomesticModifier,
    string[] BattleStages,
    double BattleModifier,
    string RequiredRoleToken = "",
    string RequiredTroopType = "",
    string RequiredSpecialTroopId = "");

public sealed record SpecialTroopDefinition(
    string Id,
    string Name,
    string BaseTroopType,
    string[] FactionIds,
    string CompatibleTrait,
    string Description);

public sealed record CourtOfficeDefinition(
    string Id,
    string Name,
    string ParentId,
    int Tier,
    string Track,
    int MinimumRank,
    int SalaryAllowance,
    string Effect);

public sealed class FactionCourtEffects
{
    public double GoldIncomeRate { get; set; }
    public double FoodIncomeRate { get; set; }
    public double DomesticActionRate { get; set; }
    public double CombatRate { get; set; }
    public double OfficerHealthRate { get; set; }
    public double TacticRate { get; set; }
    public double TrainingRate { get; set; }
    public double MoraleRate { get; set; }
    public double LogisticsSavingRate { get; set; }
}

public static class OfficerProgressionRules
{
    public static readonly int[] ExperienceThresholds = [0, 200, 450, 750, 1100, 1500, 1950, 2450, 3000, 3600, 4250, 4950, 5700, 6500, 7350, 8250, 9200, 10200, 11250, 12500];
    public static readonly int[] OfficeLevelRequirements = [1, 2, 4, 6, 9, 12, 15, 18];
    public static readonly int[] OfficeMeritRequirements = [0, 200, 600, 1200, 2200, 3600, 5500, 8000];
    public static readonly int[] OfficeSalaries = [0, 10, 22, 40, 65, 100, 150, 220];
    private static readonly int[] PrimaryOfficeBonuses = [0, 1, 2, 2, 3, 4, 5, 6];
    private static readonly int[] SecondaryOfficeBonuses = [0, 0, 0, 1, 1, 2, 3, 4];

    public static readonly IReadOnlyList<CourtOfficeDefinition> CourtOffices =
    [
        new("grand-general", "大将军", "", 1, "military", 3, 100, "统摄诸军：提升全势力军队战力、训练与士气"),
        new("chancellor", "丞相", "", 1, "civil", 3, 100, "总理政务：提升全势力钱粮收入与城务效率"),
        new("strategist-general", "军师将军", "", 1, "civil", 3, 90, "参赞军国：提升全势力战术与计策效果"),
        new("front-general", "前将军", "grand-general", 2, "military", 2, 50, "前军节制：强化全军进攻与训练"),
        new("left-general", "左将军", "grand-general", 2, "military", 2, 45, "攻坚先登：强化全军正面战力"),
        new("right-general", "右将军", "grand-general", 2, "military", 2, 45, "整军励士：强化全军士气与稳定"),
        new("secretariat-director", "尚书令", "chancellor", 2, "civil", 2, 50, "综理章奏：强化城务与金钱收入"),
        new("administration-aide", "治中从事", "chancellor", 2, "civil", 2, 40, "察举百官：强化治理与粮食收入"),
        new("provincial-aide", "别驾从事", "chancellor", 2, "civil", 2, 40, "巡行郡国：强化商贸与地方输送"),
        new("central-strategist", "中军师", "strategist-general", 2, "civil", 2, 45, "中军谋划：强化全军战术效果"),
        new("army-protector", "护军", "strategist-general", 2, "military", 2, 45, "督护诸营：提高武将体力与军心"),
        new("army-major", "军司马", "strategist-general", 2, "military", 2, 40, "参谋军务：提高训练并降低军粮维护"),
    ];

    public static readonly Dictionary<string, TraitDefinition> Traits = new()
    {
        ["忠义"] = new("忠义", "普通", "性格", "逆境中更能维持军令与承诺。", [], 0, ["决胜", "攻城与内城"], .02),
        ["豪勇"] = new("豪勇", "普通", "军事", "担任前锋或主将时强化接战与决胜。", [], 0, ["正面接战", "决胜"], .03, "前锋"),
        ["谨慎"] = new("谨慎", "普通", "军事", "各阶段保持稳定，但不会形成高爆发。", [], 0, ["远程压制", "正面接战", "攻城与内城"], .02),
        ["善谋"] = new("善谋", "普通", "军政", "提高寻访、情报和计策阶段的执行质量。", ["search", "defense"], .04, ["远程压制", "决胜", "攻城与内城"], .03, "军师"),
        ["善政"] = new("善政", "普通", "内政", "提高农业、商业和赈济的实际效果。", ["agriculture", "commerce", "relief"], .04, [], 0),
        ["沉着"] = new("沉着", "普通", "军政", "在巡查、守备和危机阶段保持稳定。", ["patrol", "defense", "relief"], .04, ["正面接战", "攻城与内城"], .025),
        ["爱民"] = new("爱民", "普通", "内政", "改善赈济、巡查，并降低征兵的民心代价。", ["relief", "patrol", "recruit"], .04, [], 0),
        ["野心"] = new("野心", "普通", "性格", "更重视功勋、官职和个人声望。", [], 0, [], 0),
        ["多疑"] = new("多疑", "普通", "性格", "对风险和他人建议更为敏感。", [], 0, [], 0),
        ["贪财"] = new("贪财", "普通", "性格", "对俸禄、赠礼和欠俸尤其敏感。", [], 0, [], 0),
        ["仁德之主"] = new("仁德之主", "传奇", "名将", "仁政行动形成更强的民心恢复。", ["relief", "patrol", "recruit"], .06, ["决胜"], .03),
        ["武圣"] = new("武圣", "传奇", "名将", "统领前军时强化接战与决胜。", [], 0, ["正面接战", "决胜"], .06, "主将"),
        ["万人敌"] = new("万人敌", "传奇", "名将", "前锋突破时形成强烈冲击。", [], 0, ["正面接战"], .06, "前锋"),
        ["常胜护军"] = new("常胜护军", "传奇", "名将", "在接战与撤收阶段维持阵线。", [], 0, ["正面接战", "决胜"], .05),
        ["奸雄"] = new("奸雄", "传奇", "名将", "统筹全军并提高复杂军令的完成度。", ["commerce", "search"], .04, ["远程压制", "决胜"], .05, "主将"),
        ["王佐之才"] = new("王佐之才", "传奇", "名臣", "显著提高治理、商业和人才事务。", ["agriculture", "commerce", "search", "relief"], .06, [], 0),
        ["鬼谋"] = new("鬼谋", "传奇", "名臣", "军师职责下强化计策与决胜判断。", ["search"], .06, ["远程压制", "决胜", "攻城与内城"], .06, "军师"),
        ["飞将"] = new("飞将", "传奇", "名将", "率骑兵接战与决胜时发挥极强冲击。", [], 0, ["正面接战", "决胜"], .06, "", "cavalry"),
        ["陷阵之志"] = new("陷阵之志", "传奇", "名将", "负责陷阵营时强化接战并延缓溃散。", [], 0, ["正面接战", "决胜"], .06, "", "infantry", "trap-camp"),
        ["白马将军"] = new("白马将军", "传奇", "名将", "统率白马义从进行先射和侧翼机动。", [], 0, ["远程压制", "决胜"], .06, "", "cavalry", "white-horse"),
        ["西凉骁骑"] = new("西凉骁骑", "传奇", "名将", "统率西凉铁骑进行冲击与追击。", [], 0, ["正面接战", "决胜"], .06, "", "cavalry", "xiliang-cavalry"),
        ["江东英略"] = new("江东英略", "传奇", "名将", "山地与江东精兵协同时提高决胜能力。", [], 0, ["正面接战", "决胜"], .055, "", "infantry", "danyang-veterans"),
        ["火计都督"] = new("火计都督", "传奇", "名将", "军师职责下强化远程压制和攻城火计。", [], 0, ["远程压制", "攻城与内城"], .06, "军师"),
        ["虎卫之勇"] = new("虎卫之勇", "传奇", "名将", "率虎卫护持主将和正面战线。", [], 0, ["正面接战", "攻城与内城"], .06, "", "infantry", "tiger-guard"),
    };

    public static readonly Dictionary<string, string[]> SignatureTraitsByOfficer = new()
    {
        ["officer-liu-bei"] = ["仁德之主"], ["officer-guan-yu"] = ["武圣"], ["officer-zhang-fei"] = ["万人敌"],
        ["officer-zhao-yun"] = ["常胜护军"], ["officer-cao-cao"] = ["奸雄"], ["officer-xun-yu"] = ["王佐之才"],
        ["officer-guo-jia"] = ["鬼谋"], ["officer-lu-bu"] = ["飞将"], ["officer-gao-shun"] = ["陷阵之志"],
        ["officer-gongsun-zan"] = ["白马将军"], ["officer-ma-chao"] = ["西凉骁骑"], ["officer-sun-ce"] = ["江东英略"],
        ["officer-zhou-yu"] = ["火计都督"], ["officer-xu-chu"] = ["虎卫之勇"],
    };

    public static readonly Dictionary<string, SpecialTroopDefinition> SpecialTroops = new()
    {
        ["white-horse"] = new("white-horse", "白马义从", "cavalry", ["faction-gongsun-zan"], "白马将军", "骑射、侦察与侧翼机动"),
        ["trap-camp"] = new("trap-camp", "陷阵营", "infantry", ["faction-lu-bu"], "陷阵之志", "正面突破与低士气维持"),
        ["xiliang-cavalry"] = new("xiliang-cavalry", "西凉铁骑", "cavalry", ["faction-ma-teng", "faction-li-jue", "faction-zhang-xiu"], "西凉骁骑", "平原冲击与追击"),
        ["danyang-veterans"] = new("danyang-veterans", "丹阳兵", "infantry", ["faction-liu-bei", "faction-sun-ce"], "江东英略", "山地与复杂地形作战"),
        ["tiger-guard"] = new("tiger-guard", "虎卫", "infantry", ["faction-cao-cao"], "虎卫之勇", "护卫主将与稳定正面"),
    };

    public static void EnsureDefaults(ScenarioOfficerData officer, int scenarioYear)
    {
        var profile = officer.Profile;
        var state = officer.InitialState;
        profile.Traits ??= [];
        profile.GrowthPlan ??= [];
        state.CareerRecords ??= [];
        state.LearnedTraits ??= [];
        state.CourtOfficeId ??= string.Empty;
        state.GrowthBonuses ??= new OfficerAbilitiesData();
        profile.AbilityPotential ??= new OfficerAbilitiesData();
        state.Health = 100;

        if (string.IsNullOrWhiteSpace(profile.GrowthArchetype)) profile.GrowthArchetype = DetermineArchetype(profile.Abilities);
        if (profile.GrowthPlan.Count < 23) profile.GrowthPlan = BuildGrowthPlan(profile.GrowthArchetype);
        EnsurePotential(profile.Abilities, profile.AbilityPotential);
        if (string.IsNullOrWhiteSpace(profile.FameTier)) profile.FameTier = SignatureTraitsByOfficer.ContainsKey(profile.Id) ? "famous" : state.Merit >= 1800 ? "notable" : "ordinary";

        if (state.Level <= 0)
        {
            var age = Math.Max(18, scenarioYear - profile.BirthYear);
            state.Level = Math.Clamp(1 + Math.Max(0, age - 18) / 4 + state.Merit / 1500, 1, 12);
        }
        state.Level = Math.Clamp(state.Level, 1, 20);
        if (state.CareerExperience < ExperienceThresholds[state.Level - 1]) state.CareerExperience = ExperienceThresholds[state.Level - 1];
        if (state.ExperienceTurn <= 0) state.ExperienceTurn = 1;
        if (string.IsNullOrWhiteSpace(state.OfficeTrack))
        {
            state.OfficeTrack = state.Appointment == "ruler" ? "none" : state.Appointment == "general" ? "military" : "civil";
            if (state.OfficeTrack != "none") state.OfficeRank = Math.Min(3, HighestEligibleRank(state.Level, state.Merit));
        }
        state.OfficeRank = Math.Clamp(state.OfficeRank, 0, 7);
        var courtOffice = CourtOffice(state.CourtOfficeId);
        if (courtOffice is null || state.OfficeTrack != courtOffice.Track || state.OfficeRank < courtOffice.MinimumRank
            || !state.Alive || state.Status is "captive" or "free") state.CourtOfficeId = string.Empty;
    }

    public static IReadOnlyList<string> AllTraits(ScenarioOfficerData officer)
    {
        var result = new List<string>(officer.Profile.Traits);
        if (SignatureTraitsByOfficer.TryGetValue(officer.Profile.Id, out var signatures)) result.AddRange(signatures);
        result.AddRange(officer.InitialState.LearnedTraits);
        return result.Distinct().ToList();
    }

    public static int PermanentAbility(ScenarioOfficerData officer, string ability)
    {
        var baseValue = AbilityValue(officer.Profile.Abilities, ability);
        var growth = AbilityValue(officer.InitialState.GrowthBonuses, ability);
        return Math.Clamp(baseValue + growth, 1, 100);
    }

    public static int EffectiveAbility(ScenarioOfficerData officer, string ability, string context)
    {
        var value = PermanentAbility(officer, ability);
        var rank = Math.Clamp(officer.InitialState.OfficeRank, 0, 7);
        if (officer.InitialState.TrackTransitionMonths > 0) rank = Math.Max(0, rank / 2);
        if (context == "civil" && officer.InitialState.OfficeTrack == "civil")
        {
            if (ability == "politics") value += PrimaryOfficeBonuses[rank];
            if (ability == "intelligence") value += SecondaryOfficeBonuses[rank];
            if (ability == "charisma" && rank >= 4) value += SecondaryOfficeBonuses[rank];
        }
        if (context == "military" && officer.InitialState.OfficeTrack == "military")
        {
            if (ability == "leadership") value += PrimaryOfficeBonuses[rank];
            if (ability == "might") value += SecondaryOfficeBonuses[rank];
        }
        return Math.Clamp(value, 1, 110);
    }

    public static FactionCourtEffects FactionCourtInfluence(GameSession state, string factionId)
    {
        var result = new FactionCourtEffects();
        foreach (var officer in state.Officers.Where(item => item.InitialState.FactionId == factionId
            && item.InitialState.Alive
            && item.InitialState.Status is not "captive" and not "free"
            && !string.IsNullOrEmpty(item.InitialState.CourtOfficeId)))
        {
            var office = CourtOffice(officer.InitialState.CourtOfficeId);
            if (office is not null) ApplyCourtInfluence(result, officer, office, true);
        }
        CapCourtInfluence(result);
        return result;
    }

    public static string CourtInfluenceSummary(ScenarioOfficerData officer, CourtOfficeDefinition office)
    {
        var result = new FactionCourtEffects();
        var activeTraits = ApplyCourtInfluence(result, officer, office, true);
        CapCourtInfluence(result);
        var effects = CourtEffectParts(result);
        var traitText = activeTraits.Count == 0 ? "无可转化特性" : $"特性转化：{string.Join('、', activeTraits)}";
        return $"势力效果：{string.Join("，", effects)}\n{traitText}";
    }

    public static double CourtBattleMultiplier(GameSession state, string factionId, string stage)
    {
        var effect = FactionCourtInfluence(state, factionId);
        var tactic = stage is "远程压制" or "决胜" or "攻城与内城" ? effect.TacticRate : 0;
        return 1 + effect.CombatRate + tactic;
    }

    public static double CourtOfficerHealthMultiplier(GameSession state, string factionId) =>
        1 + FactionCourtInfluence(state, factionId).OfficerHealthRate;

    public static double CourtMoraleBonus(GameSession state, string factionId) =>
        FactionCourtInfluence(state, factionId).MoraleRate * 100;

    public static double CourtIncomeMultiplier(GameSession state, string factionId, string resource) =>
        1 + (resource == "food" ? FactionCourtInfluence(state, factionId).FoodIncomeRate : FactionCourtInfluence(state, factionId).GoldIncomeRate);

    public static double CourtDomesticMultiplier(GameSession state, string factionId, string focus)
    {
        var effect = FactionCourtInfluence(state, factionId);
        return 1 + (focus is "defense" or "recruit" or "train" ? effect.TrainingRate : effect.DomesticActionRate);
    }

    public static double CourtLogisticsMultiplier(GameSession state, string factionId) =>
        1 - FactionCourtInfluence(state, factionId).LogisticsSavingRate;

    private static List<string> ApplyCourtInfluence(FactionCourtEffects result, ScenarioOfficerData officer, CourtOfficeDefinition office, bool includeTraits)
    {
        var ability = office.Id switch
        {
            "grand-general" or "front-general" or "army-protector" => PermanentAbility(officer, "leadership"),
            "left-general" => PermanentAbility(officer, "might"),
            "right-general" or "administration-aide" => PermanentAbility(officer, "charisma"),
            "chancellor" or "secretariat-director" => PermanentAbility(officer, "politics"),
            _ => PermanentAbility(officer, "intelligence"),
        };
        var abilityScale = Math.Clamp(.75 + (ability - 50) * .01, .70, 1.25);
        switch (office.Id)
        {
            case "grand-general": Add(result, office, "combat", .020 * abilityScale); Add(result, office, "training", .040 * abilityScale); Add(result, office, "morale", .020 * abilityScale); break;
            case "front-general": Add(result, office, "combat", .012 * abilityScale); Add(result, office, "training", .012 * abilityScale); break;
            case "left-general": Add(result, office, "combat", .016 * abilityScale); break;
            case "right-general": Add(result, office, "combat", .005 * abilityScale); Add(result, office, "morale", .030 * abilityScale); break;
            case "chancellor": Add(result, office, "gold", .030 * abilityScale); Add(result, office, "food", .030 * abilityScale); Add(result, office, "domestic", .030 * abilityScale); break;
            case "secretariat-director": Add(result, office, "gold", .012 * abilityScale); Add(result, office, "domestic", .022 * abilityScale); break;
            case "administration-aide": Add(result, office, "food", .012 * abilityScale); Add(result, office, "domestic", .020 * abilityScale); break;
            case "provincial-aide": Add(result, office, "gold", .016 * abilityScale); Add(result, office, "food", .010 * abilityScale); break;
            case "strategist-general": Add(result, office, "combat", .010 * abilityScale); Add(result, office, "tactic", .045 * abilityScale); break;
            case "central-strategist": Add(result, office, "tactic", .028 * abilityScale); break;
            case "army-protector": Add(result, office, "health", .040 * abilityScale); Add(result, office, "morale", .018 * abilityScale); break;
            case "army-major": Add(result, office, "training", .028 * abilityScale); Add(result, office, "logistics", .025 * abilityScale); Add(result, office, "tactic", .010 * abilityScale); break;
        }

        var activeTraits = new List<string>();
        if (!includeTraits) return activeTraits;
        var tierScale = office.Tier == 1 ? 1d : .55;
        foreach (var trait in AllTraits(officer))
        {
            var before = CourtEffectTotal(result);
            switch (trait)
            {
                case "忠义": Add(result, office, "morale", .015 * tierScale); break;
                case "豪勇": Add(result, office, "combat", .020 * tierScale); Add(result, office, "health", .010 * tierScale); break;
                case "谨慎": Add(result, office, "health", .020 * tierScale); Add(result, office, "logistics", .010 * tierScale); break;
                case "善谋": Add(result, office, "tactic", .025 * tierScale); Add(result, office, "domestic", .010 * tierScale); break;
                case "善政": Add(result, office, "gold", .015 * tierScale); Add(result, office, "food", .015 * tierScale); Add(result, office, "domestic", .020 * tierScale); break;
                case "沉着": Add(result, office, "morale", .015 * tierScale); Add(result, office, "health", .015 * tierScale); Add(result, office, "domestic", .010 * tierScale); break;
                case "爱民": Add(result, office, "food", .020 * tierScale); Add(result, office, "domestic", .020 * tierScale); break;
                case "野心": Add(result, office, "training", .015 * tierScale); break;
                case "多疑": Add(result, office, "tactic", .012 * tierScale); break;
                case "贪财": Add(result, office, "gold", .018 * tierScale); break;
                case "仁德之主": Add(result, office, "food", .050 * tierScale); Add(result, office, "domestic", .040 * tierScale); Add(result, office, "morale", .030 * tierScale); break;
                case "武圣": Add(result, office, "combat", .050 * tierScale); Add(result, office, "morale", .030 * tierScale); break;
                case "万人敌": Add(result, office, "combat", .060 * tierScale); Add(result, office, "health", .080 * tierScale); break;
                case "常胜护军": Add(result, office, "health", .080 * tierScale); Add(result, office, "morale", .040 * tierScale); break;
                case "奸雄": Add(result, office, "gold", .030 * tierScale); Add(result, office, "combat", .030 * tierScale); Add(result, office, "tactic", .030 * tierScale); break;
                case "王佐之才": Add(result, office, "gold", .060 * tierScale); Add(result, office, "food", .060 * tierScale); Add(result, office, "domestic", .050 * tierScale); break;
                case "鬼谋": Add(result, office, "tactic", .080 * tierScale); break;
                case "飞将": Add(result, office, "combat", .070 * tierScale); Add(result, office, "health", .060 * tierScale); break;
                case "陷阵之志": Add(result, office, "combat", .050 * tierScale); Add(result, office, "health", .080 * tierScale); break;
                case "白马将军": Add(result, office, "combat", .050 * tierScale); Add(result, office, "training", .030 * tierScale); break;
                case "西凉骁骑": Add(result, office, "combat", .055 * tierScale); Add(result, office, "morale", .025 * tierScale); break;
                case "江东英略": Add(result, office, "combat", .045 * tierScale); Add(result, office, "tactic", .025 * tierScale); break;
                case "火计都督": Add(result, office, "tactic", .070 * tierScale); break;
                case "虎卫之勇": Add(result, office, "health", .100 * tierScale); Add(result, office, "morale", .025 * tierScale); break;
            }
            if (CourtEffectTotal(result) > before + .00001) activeTraits.Add(trait);
        }
        return activeTraits;
    }

    private static bool AllowsCourtEffect(CourtOfficeDefinition office, string key)
    {
        var root = office.Tier == 1 ? office.Id : office.ParentId;
        return root switch
        {
            "grand-general" => key is "combat" or "health" or "training" or "morale" or "logistics" or "tactic",
            "chancellor" => key is "gold" or "food" or "domestic",
            "strategist-general" => key is "combat" or "health" or "training" or "morale" or "logistics" or "tactic",
            _ => false,
        };
    }

    private static void Add(FactionCourtEffects result, CourtOfficeDefinition office, string key, double value)
    {
        if (!AllowsCourtEffect(office, key)) return;
        switch (key)
        {
            case "gold": result.GoldIncomeRate += value; break;
            case "food": result.FoodIncomeRate += value; break;
            case "domestic": result.DomesticActionRate += value; break;
            case "combat": result.CombatRate += value; break;
            case "health": result.OfficerHealthRate += value; break;
            case "tactic": result.TacticRate += value; break;
            case "training": result.TrainingRate += value; break;
            case "morale": result.MoraleRate += value; break;
            case "logistics": result.LogisticsSavingRate += value; break;
        }
    }

    private static double CourtEffectTotal(FactionCourtEffects value) =>
        value.GoldIncomeRate + value.FoodIncomeRate + value.DomesticActionRate + value.CombatRate + value.OfficerHealthRate + value.TacticRate + value.TrainingRate + value.MoraleRate + value.LogisticsSavingRate;

    private static void CapCourtInfluence(FactionCourtEffects value)
    {
        value.GoldIncomeRate = Math.Min(.20, value.GoldIncomeRate);
        value.FoodIncomeRate = Math.Min(.20, value.FoodIncomeRate);
        value.DomesticActionRate = Math.Min(.20, value.DomesticActionRate);
        value.CombatRate = Math.Min(.18, value.CombatRate);
        value.OfficerHealthRate = Math.Min(.25, value.OfficerHealthRate);
        value.TacticRate = Math.Min(.15, value.TacticRate);
        value.TrainingRate = Math.Min(.15, value.TrainingRate);
        value.MoraleRate = Math.Min(.12, value.MoraleRate);
        value.LogisticsSavingRate = Math.Min(.15, value.LogisticsSavingRate);
    }

    private static List<string> CourtEffectParts(FactionCourtEffects value)
    {
        var parts = new List<string>();
        if (value.GoldIncomeRate > 0) parts.Add($"金钱收入 +{value.GoldIncomeRate:P0}");
        if (value.FoodIncomeRate > 0) parts.Add($"粮食收入 +{value.FoodIncomeRate:P0}");
        if (value.DomesticActionRate > 0) parts.Add($"城务效果 +{value.DomesticActionRate:P0}");
        if (value.CombatRate > 0) parts.Add($"全军战力 +{value.CombatRate:P0}");
        if (value.OfficerHealthRate > 0) parts.Add($"武将体力 +{value.OfficerHealthRate:P0}");
        if (value.TacticRate > 0) parts.Add($"战术效果 +{value.TacticRate:P0}");
        if (value.TrainingRate > 0) parts.Add($"整军训练 +{value.TrainingRate:P0}");
        if (value.MoraleRate > 0) parts.Add($"战斗士气 +{value.MoraleRate:P0}");
        if (value.LogisticsSavingRate > 0) parts.Add($"军粮维护 -{value.LogisticsSavingRate:P0}");
        return parts.Count == 0 ? ["暂无有效加成"] : parts;
    }

    public static (double Modifier, string Description) DomesticTraitModifier(ScenarioOfficerData officer, string focus)
    {
        var active = AllTraits(officer)
            .Select(name => Traits.GetValueOrDefault(name))
            .Where(rule => rule is not null && rule.DomesticModifier > 0 && rule.DomesticFocuses.Contains(focus))
            .Cast<TraitDefinition>()
            .OrderByDescending(rule => rule.DomesticModifier)
            .ToList();
        var bonus = Math.Min(.15, active.Sum(rule => rule.DomesticModifier));
        return (1 + bonus, active.Count == 0 ? "无特性修正" : string.Join('、', active.Select(rule => $"{rule.Name}+{rule.DomesticModifier:P0}")));
    }

    public static double BattleTraitMultiplier(
        GameSession state,
        IEnumerable<string> officerIds,
        Dictionary<string, string> roles,
        Dictionary<string, int> composition,
        Dictionary<string, int> specialTroops,
        string stage,
        Dictionary<string, string> descriptions)
    {
        var totalSoldiers = Math.Max(1, composition.Values.Sum());
        var contributions = new List<(ScenarioOfficerData Officer, TraitDefinition Rule, double Bonus)>();
        foreach (var officerId in officerIds)
        {
            var officer = state.Officers.FirstOrDefault(item => item.Profile.Id == officerId);
            if (officer is null) continue;
            var role = roles.GetValueOrDefault(officerId, string.Empty);
            foreach (var rule in AllTraits(officer).Select(name => Traits.GetValueOrDefault(name)).Where(rule => rule is not null).Cast<TraitDefinition>())
            {
                if (rule.BattleModifier <= 0 || !rule.BattleStages.Contains(stage)) continue;
                if (!string.IsNullOrEmpty(rule.RequiredRoleToken) && !role.Contains(rule.RequiredRoleToken, StringComparison.Ordinal)) continue;
                var scale = 1d;
                if (!string.IsNullOrEmpty(rule.RequiredTroopType))
                {
                    var share = composition.GetValueOrDefault(rule.RequiredTroopType) / (double)totalSoldiers;
                    if (share < .20 || composition.GetValueOrDefault(rule.RequiredTroopType) < 500) continue;
                    scale = share;
                }
                if (!string.IsNullOrEmpty(rule.RequiredSpecialTroopId))
                {
                    var special = specialTroops.GetValueOrDefault(rule.RequiredSpecialTroopId);
                    if (special < 500 || special / (double)totalSoldiers < .20) continue;
                    scale = special / (double)totalSoldiers;
                }
                contributions.Add((officer, rule, rule.BattleModifier * scale));
            }
        }
        var selected = contributions.OrderByDescending(item => item.Bonus).ToList();
        var total = Math.Min(.15, selected.Sum(item => item.Bonus));
        foreach (var group in selected.GroupBy(item => item.Officer.Profile.Id))
        {
            var additions = group
                .Select(item => $"{item.Rule.Name}+{item.Bonus:P1}")
                .Distinct()
                .ToList();
            var existingItems = descriptions.TryGetValue(group.Key, out var existing)
                ? existing.Split('；', StringSplitOptions.RemoveEmptyEntries).ToList()
                : [];
            foreach (var addition in additions)
            {
                var traitName = addition.Split('+', 2)[0];
                if (existingItems.Any(item => item.StartsWith($"{traitName}+", StringComparison.Ordinal))) continue;
                existingItems.Add(addition);
            }
            descriptions[group.Key] = string.Join('；', existingItems);
        }
        return 1 + total;
    }

    public static int Salary(int rank) => OfficeSalaries[Math.Clamp(rank, 0, 7)];
    public static int Salary(ScenarioOfficerData officer)
    {
        if (officer.InitialState.Appointment == "ruler") return 0;
        return Salary(officer.InitialState.OfficeRank) + (CourtOffice(officer.InitialState.CourtOfficeId)?.SalaryAllowance ?? 0);
    }
    public static CourtOfficeDefinition? CourtOffice(string id) => CourtOffices.FirstOrDefault(item => item.Id == id);
    public static string CourtOfficeName(string id) => CourtOffice(id)?.Name ?? "未入朝堂";
    public static int NextLevelExperience(int level) => level >= 20 ? ExperienceThresholds[^1] : ExperienceThresholds[Math.Clamp(level, 1, 19)];
    public static string OfficeName(string track, int rank) => track switch
    {
        "civil" => new[] { "白身", "从事", "功曹", "别驾", "治中", "尚书", "九卿", "三公" }[Math.Clamp(rank, 0, 7)],
        "military" => new[] { "白身", "军司马", "校尉", "中郎将", "偏将军", "杂号将军", "重号将军", "大将军" }[Math.Clamp(rank, 0, 7)],
        _ => "君主",
    };
    public static string TrackName(string track) => track == "military" ? "武职" : track == "civil" ? "文职" : "君主";
    public static string QualityName(string quality) => quality switch { "legendary" => "传奇", "rare" => "稀有", _ => "普通" };
    public static int HighestEligibleRank(int level, int merit)
    {
        var result = 0;
        for (var rank = 1; rank <= 7; rank++) if (level >= OfficeLevelRequirements[rank] && merit >= OfficeMeritRequirements[rank]) result = rank;
        return result;
    }

    public static List<string> GrowthForLevel(ScenarioOfficerData officer, int level)
    {
        var index = 0;
        for (var current = 2; current < level; current++) index += current % 5 == 0 ? 2 : 1;
        var count = level % 5 == 0 ? 2 : 1;
        return officer.Profile.GrowthPlan.Skip(index).Take(count).ToList();
    }

    public static bool AddGrowth(ScenarioOfficerData officer, string desiredAbility)
    {
        var priorities = new[] { desiredAbility }.Concat(ArchetypePriorities(officer.Profile.GrowthArchetype)).Distinct();
        foreach (var ability in priorities)
        {
            if (PermanentAbility(officer, ability) >= AbilityValue(officer.Profile.AbilityPotential, ability)) continue;
            SetAbilityValue(officer.InitialState.GrowthBonuses, ability, AbilityValue(officer.InitialState.GrowthBonuses, ability) + 1);
            return true;
        }
        officer.InitialState.Merit += 50;
        return false;
    }

    public static string AbilityName(string ability) => ability switch { "leadership" => "统率", "might" => "武力", "intelligence" => "智力", "politics" => "政治", "charisma" => "魅力", _ => ability };

    private static string DetermineArchetype(OfficerAbilitiesData ability)
    {
        var values = new Dictionary<string, int> { ["commander"] = ability.Leadership, ["warrior"] = ability.Might, ["strategist"] = ability.Intelligence, ["administrator"] = ability.Politics, ["notable"] = ability.Charisma };
        var top = values.OrderByDescending(item => item.Value).Take(2).ToList();
        return top[0].Value - top[1].Value <= 4 ? "versatile" : top[0].Key;
    }

    private static List<string> BuildGrowthPlan(string archetype)
    {
        var priorities = ArchetypePriorities(archetype);
        var plan = new List<string>();
        for (var level = 2; level <= 20; level++)
        {
            plan.Add(priorities[(level * 3 + plan.Count) % Math.Min(3, priorities.Length)]);
            if (level % 5 == 0) plan.Add(priorities[(level + 1) % Math.Min(3, priorities.Length)]);
        }
        return plan;
    }

    private static string[] ArchetypePriorities(string archetype) => archetype switch
    {
        "warrior" => ["might", "leadership", "charisma", "intelligence", "politics"],
        "strategist" => ["intelligence", "politics", "leadership", "charisma", "might"],
        "administrator" => ["politics", "intelligence", "charisma", "leadership", "might"],
        "notable" => ["charisma", "politics", "intelligence", "leadership", "might"],
        "versatile" => ["leadership", "intelligence", "politics", "charisma", "might"],
        _ => ["leadership", "intelligence", "charisma", "might", "politics"],
    };

    private static void EnsurePotential(OfficerAbilitiesData basis, OfficerAbilitiesData potential)
    {
        if (potential.Leadership <= 0) potential.Leadership = Math.Min(100, basis.Leadership + 8);
        if (potential.Might <= 0) potential.Might = Math.Min(100, basis.Might + 8);
        if (potential.Intelligence <= 0) potential.Intelligence = Math.Min(100, basis.Intelligence + 8);
        if (potential.Politics <= 0) potential.Politics = Math.Min(100, basis.Politics + 8);
        if (potential.Charisma <= 0) potential.Charisma = Math.Min(100, basis.Charisma + 8);
    }

    private static int AbilityValue(OfficerAbilitiesData ability, string key) => key switch
    {
        "leadership" => ability.Leadership, "might" => ability.Might, "intelligence" => ability.Intelligence, "politics" => ability.Politics, "charisma" => ability.Charisma, _ => 0,
    };

    private static void SetAbilityValue(OfficerAbilitiesData ability, string key, int value)
    {
        switch (key) { case "leadership": ability.Leadership = value; break; case "might": ability.Might = value; break; case "intelligence": ability.Intelligence = value; break; case "politics": ability.Politics = value; break; case "charisma": ability.Charisma = value; break; }
    }
}

public sealed partial class GameRuntime
{
    public int PermanentAbility(ScenarioOfficerData officer, string ability) => OfficerProgressionRules.PermanentAbility(officer, ability);
    public int EffectiveAbility(ScenarioOfficerData officer, string ability, string context) => OfficerProgressionRules.EffectiveAbility(officer, ability, context);
    public int CitySalaryDue(string cityId) => State.Officers.Where(item => item.InitialState.CityId == cityId && item.InitialState.Status == "serving" && item.InitialState.Appointment != "ruler").Sum(OfficerProgressionRules.Salary);

    public bool AppointCourtOffice(string officerId, string officeId)
    {
        var officer = Officer(officerId);
        var office = OfficerProgressionRules.CourtOffice(officeId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || officer.InitialState.Appointment == "ruler" || office is null) return Fail("朝堂职位或任命人选无效。");
        if (officer.InitialState.Status != "serving") return Fail("只有当前在任且未出征的武将可以进入朝堂。");
        if (officer.InitialState.OfficeTrack != office.Track) return Fail($"{office.Name}只接受{OfficerProgressionRules.TrackName(office.Track)}序列武将。");
        if (officer.InitialState.OfficeRank < office.MinimumRank) return Fail($"{office.Name}需要至少{OfficerProgressionRules.OfficeName(office.Track, office.MinimumRank)}官阶。");
        if (officer.InitialState.CourtOfficeId == office.Id) return Fail($"{officer.Profile.Name}已经担任{office.Name}。");
        var currentHolder = State.Officers.FirstOrDefault(item => item.InitialState.FactionId == State.PlayerFactionId && item.InitialState.CourtOfficeId == office.Id);
        var previousOffice = OfficerProgressionRules.CourtOffice(officer.InitialState.CourtOfficeId);
        if (currentHolder is not null) return Fail($"{office.Name}已有{currentHolder.Profile.Name}任职，请先卸任现任。");
        if (previousOffice is not null) return Fail($"{officer.Profile.Name}已担任{previousOffice.Name}，请先卸任后再改任。");
        officer.InitialState.CourtOfficeId = office.Id;
        return Success($"{officer.Profile.Name}出任{office.Name}；月俸增至{OfficerProgressionRules.Salary(officer)}金。{OfficerProgressionRules.CourtInfluenceSummary(officer, office).Replace('\n', ' ')}", "talent");
    }

    public bool VacateCourtOffice(string officerId)
    {
        var officer = Officer(officerId);
        var office = officer is null ? null : OfficerProgressionRules.CourtOffice(officer.InitialState.CourtOfficeId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || office is null) return Fail("该武将没有可卸任的朝堂职位。");
        officer.InitialState.CourtOfficeId = string.Empty;
        return Success($"{officer.Profile.Name}卸任{office.Name}，月俸调整为{OfficerProgressionRules.Salary(officer)}金。", "talent");
    }

    public bool PromoteOfficer(string officerId, string track)
    {
        var officer = Officer(officerId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId) return Fail("只能晋升己方武将。");
        return PromoteOfficerInternal(officer, track, true);
    }

    public bool DemoteOfficer(string officerId)
    {
        var officer = Officer(officerId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || officer.InitialState.Appointment == "ruler") return Fail("该武将不能降职。");
        if (officer.InitialState.Status == "marching") return Fail("武将调动途中不能升降官阶。");
        if (officer.InitialState.OfficeRank <= 0) return Fail("该武将当前已是白身。");
        officer.InitialState.OfficeRank--;
        var courtOffice = OfficerProgressionRules.CourtOffice(officer.InitialState.CourtOfficeId);
        if (courtOffice is not null && officer.InitialState.OfficeRank < courtOffice.MinimumRank) officer.InitialState.CourtOfficeId = string.Empty;
        officer.InitialState.LastPromotionTurn = State.Turn;
        return Success($"{officer.Profile.Name}降为{OfficerProgressionRules.OfficeName(officer.InitialState.OfficeTrack, officer.InitialState.OfficeRank)}，月俸调整为{OfficerProgressionRules.Salary(officer)}金。", "talent");
    }

    public bool PaySalaryArrears(string officerId)
    {
        var officer = Officer(officerId);
        if (officer?.InitialState.FactionId != State.PlayerFactionId || officer.InitialState.SalaryArrears <= 0) return Fail("该武将没有可补发的欠俸。");
        if (State.Resources.Gold < officer.InitialState.SalaryArrears) return Fail($"势力府库不足，补发需要{officer.InitialState.SalaryArrears}金。");
        var paid = officer.InitialState.SalaryArrears;
        State.Resources.Gold -= paid;
        officer.InitialState.SalaryArrears = 0;
        officer.InitialState.SalaryArrearsMonths = 0;
        officer.InitialState.Loyalty = Math.Min(100, officer.InitialState.Loyalty + 2);
        State.PayrollLedgerEntries.Add(new PayrollLedgerEntryData { Turn = State.Turn, FactionId = State.PlayerFactionId, OfficerId = officerId, PayerId = "faction-treasury", Due = paid, Paid = paid, Description = $"补发{officer.Profile.Name}欠俸{paid}金" });
        return Success($"已补发{officer.Profile.Name}欠俸{paid}金，忠诚有所恢复。", "talent");
    }

    public string OfficerProgressionSummary(ScenarioOfficerData officer)
    {
        var state = officer.InitialState;
        var traits = OfficerProgressionRules.AllTraits(officer).Select(name => OfficerProgressionRules.Traits.TryGetValue(name, out var rule) ? $"{name}（{rule.Quality}）" : name);
        var next = state.Level >= 20 ? "已满级" : $"下级阅历 {state.CareerExperience}/{OfficerProgressionRules.NextLevelExperience(state.Level)}";
        var nextRank = Math.Min(7, state.OfficeRank + 1);
        var promotion = state.OfficeRank >= 7 ? "已达最高官阶" : $"下阶要求 等级{OfficerProgressionRules.OfficeLevelRequirements[nextRank]} / 功勋{OfficerProgressionRules.OfficeMeritRequirements[nextRank]}";
        return $"等级{state.Level}　{next}　功勋 {state.Merit}\n" +
               $"{OfficerProgressionRules.TrackName(state.OfficeTrack)}·{OfficerProgressionRules.OfficeName(state.OfficeTrack, state.OfficeRank)}　朝堂 {OfficerProgressionRules.CourtOfficeName(state.CourtOfficeId)}　月俸 {OfficerProgressionRules.Salary(officer)}　欠俸 {state.SalaryArrears}\n" +
               $"统{PermanentAbility(officer, "leadership")} 武{PermanentAbility(officer, "might")} 智{PermanentAbility(officer, "intelligence")} 政{PermanentAbility(officer, "politics")} 魅{PermanentAbility(officer, "charisma")}\n" +
               $"{promotion}\n特性：{string.Join('、', traits)}";
    }

    internal void AwardOfficerExperience(ScenarioOfficerData officer, int amount, string source, string? record = null)
    {
        if (!officer.InitialState.Alive || officer.InitialState.Level >= 20 || amount <= 0) return;
        if (officer.InitialState.ExperienceTurn != State.Turn)
        {
            officer.InitialState.ExperienceTurn = State.Turn;
            officer.InitialState.ExperienceEarnedThisTurn = 0;
        }
        var granted = Math.Min(amount, Math.Max(0, 200 - officer.InitialState.ExperienceEarnedThisTurn));
        if (granted <= 0) return;
        officer.InitialState.ExperienceEarnedThisTurn += granted;
        officer.InitialState.CareerExperience += granted;
        if (!string.IsNullOrEmpty(record)) officer.InitialState.CareerRecords[record] = officer.InitialState.CareerRecords.GetValueOrDefault(record) + 1;
        while (officer.InitialState.Level < 20 && officer.InitialState.CareerExperience >= OfficerProgressionRules.ExperienceThresholds[officer.InitialState.Level])
        {
            officer.InitialState.Level++;
            var growthText = new List<string>();
            foreach (var ability in OfficerProgressionRules.GrowthForLevel(officer, officer.InitialState.Level))
            {
                if (OfficerProgressionRules.AddGrowth(officer, ability)) growthText.Add(OfficerProgressionRules.AbilityName(ability));
                else growthText.Add("功勋+50");
            }
            State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "talent", Message = $"{officer.Profile.Name}因{source}升至{officer.InitialState.Level}级：{string.Join('、', growthText)}。" });
        }
    }

    private bool PromoteOfficerInternal(ScenarioOfficerData officer, string track, bool manual)
    {
        if (officer.InitialState.Appointment == "ruler" || track is not ("civil" or "military")) return manual ? Fail("君主或官职序列无效。") : false;
        if (officer.InitialState.Status == "marching") return manual ? Fail("武将调动途中不能升降官阶。") : false;
        if (officer.InitialState.Status is "captive" or "free" || !officer.InitialState.Alive) return manual ? Fail("该武将当前不能晋升。") : false;
        if (officer.InitialState.OfficeTrack != track && officer.InitialState.OfficeRank > 0)
        {
            if (officer.InitialState.OfficeRank > 3) return manual ? Fail("四阶以上转序须先降至三阶并积累对应履历。") : false;
            officer.InitialState.OfficeTrack = track;
            officer.InitialState.CourtOfficeId = string.Empty;
            officer.InitialState.TrackTransitionMonths = 1;
            officer.InitialState.LastPromotionTurn = State.Turn;
            return manual ? Success($"{officer.Profile.Name}转入{OfficerProgressionRules.TrackName(track)}，适应期1个月，期间官职加成减半。", "talent") : true;
        }
        var nextRank = officer.InitialState.OfficeRank + 1;
        if (nextRank > 7) return manual ? Fail("该武将已经达到最高官阶。") : false;
        if (officer.InitialState.Level < OfficerProgressionRules.OfficeLevelRequirements[nextRank] || officer.InitialState.Merit < OfficerProgressionRules.OfficeMeritRequirements[nextRank])
            return manual ? Fail($"晋升需要等级{OfficerProgressionRules.OfficeLevelRequirements[nextRank]}、功勋{OfficerProgressionRules.OfficeMeritRequirements[nextRank]}。") : false;
        if (State.Turn - officer.InitialState.LastPromotionTurn < 3) return manual ? Fail("该武将距离上次升降未满3个月。") : false;
        var factionId = officer.InitialState.FactionId!;
        var used = State.Officers.Count(item => item.InitialState.FactionId == factionId && item.InitialState.OfficeRank >= nextRank && item.InitialState.Appointment != "ruler");
        var limit = OfficeRankLimit(factionId, nextRank);
        if (used >= limit) return manual ? Fail($"{nextRank}阶以上官职名额已满（{used}/{limit}）。") : false;
        var projectedSalary = State.Officers.Where(item => item.InitialState.FactionId == factionId).Sum(OfficerProgressionRules.Salary) + OfficerProgressionRules.Salary(nextRank) - OfficerProgressionRules.Salary(officer.InitialState.OfficeRank);
        var monthlyIncome = State.Cities.Where(item => item.OwnerFactionId == factionId).Sum(item => CityMonthlyForecast(item).GoldIncome);
        var sustainableBudget = Math.Max(1, monthlyIncome + Math.Max(0, Treasury(factionId).Gold) / 12);
        if (projectedSalary > sustainableBudget * .35) return manual ? Fail($"晋升后月俸预计占可持续预算{projectedSalary / (double)sustainableBudget:P0}，超过35%严重风险线。") : false;
        officer.InitialState.OfficeTrack = track;
        officer.InitialState.OfficeRank = nextRank;
        officer.InitialState.LastPromotionTurn = State.Turn;
        var message = $"{officer.Profile.Name}晋升{OfficerProgressionRules.OfficeName(track, nextRank)}，月俸{OfficerProgressionRules.Salary(officer)}金。";
        if (!manual) State.Log.Add(new LogEntryData { Turn = State.Turn, Category = "talent", Message = $"{Faction(factionId)?.ShortName}·{message}" });
        return manual ? Success(message, "talent") : true;
    }

    private int OfficeRankLimit(string factionId, int rank)
    {
        var cities = State.Cities.Count(item => item.OwnerFactionId == factionId);
        return rank switch { <= 2 => int.MaxValue, 3 => cities * 2, 4 => 1 + cities / 2, 5 => 1 + cities / 4, 6 => cities >= 6 ? cities / 6 : 0, 7 => cities >= 12 ? 1 : 0, _ => 0 };
    }

    private void ResolveOfficerSalaries()
    {
        State.MonthlySalaryDue = 0;
        State.MonthlySalaryPaid = 0;
        foreach (var faction in State.Factions)
        {
            var officers = State.Officers.Where(item => item.InitialState.FactionId == faction.Id && item.InitialState.Status is "serving" or "deployed" or "marching" && item.InitialState.Appointment != "ruler" && OfficerProgressionRules.Salary(item) > 0).ToList();
            if (officers.Count == 0) continue;
            var treasury = Treasury(faction.Id);
            var (_, paid) = PayPayrollGroup(officers, faction.Id, "faction-treasury", treasury.Gold);
            treasury.Gold -= paid;
        }
        if (State.PayrollLedgerEntries.Count > 240) State.PayrollLedgerEntries.RemoveRange(0, State.PayrollLedgerEntries.Count - 240);
    }

    private (int Due, int Paid) PayPayrollGroup(List<ScenarioOfficerData> officers, string factionId, string payerId, int available)
    {
        var due = officers.Sum(OfficerProgressionRules.Salary);
        var budget = Math.Min(Math.Max(0, available), due);
        var remaining = budget;
        var remainingDue = due;
        var paidTotal = 0;
        foreach (var officer in officers.OrderBy(item => item.Profile.Id))
        {
            var officerDue = OfficerProgressionRules.Salary(officer);
            var paid = remainingDue <= 0 ? 0 : Math.Min(officerDue, (int)Math.Round(remaining * officerDue / (double)remainingDue));
            remaining -= paid;
            remainingDue -= officerDue;
            paidTotal += paid;
            ApplySalaryResult(officer, factionId, payerId, officerDue, paid);
        }
        return (due, paidTotal);
    }

    private void ApplySalaryResult(ScenarioOfficerData officer, string factionId, string payerId, int due, int paid)
    {
        var ratio = due <= 0 ? 1 : paid / (double)due;
        if (paid < due)
        {
            officer.InitialState.SalaryArrears += due - paid;
            officer.InitialState.SalaryArrearsMonths++;
            if (ratio < .5) officer.InitialState.Loyalty = Math.Max(0, officer.InitialState.Loyalty - 1);
            if (ratio <= 0 || officer.InitialState.SalaryArrearsMonths >= 2 && ratio < .75) officer.InitialState.Loyalty = Math.Max(0, officer.InitialState.Loyalty - 1);
            if (OfficerProgressionRules.AllTraits(officer).Any(item => item is "贪财" or "野心") && officer.InitialState.SalaryArrearsMonths >= 2) officer.InitialState.Loyalty = Math.Max(0, officer.InitialState.Loyalty - 1);
        }
        else officer.InitialState.SalaryArrearsMonths = 0;
        if (factionId == State.PlayerFactionId) { State.MonthlySalaryDue += due; State.MonthlySalaryPaid += paid; }
        State.PayrollLedgerEntries.Add(new PayrollLedgerEntryData { Turn = State.Turn, FactionId = factionId, OfficerId = officer.Profile.Id, PayerId = payerId, Due = due, Paid = paid, Description = $"{officer.Profile.Name}俸禄{paid}/{due}金" });
    }

    private void ResolveAiPromotions()
    {
        if (State.Turn % 3 != 0) return;
        foreach (var faction in State.Factions.Where(item => item.Id != State.PlayerFactionId))
        {
            var candidate = State.Officers
                .Where(item => item.InitialState.FactionId == faction.Id && item.InitialState.Appointment != "ruler" && item.InitialState.Status == "serving")
                .OrderByDescending(item => item.InitialState.Merit)
                .ThenByDescending(item => item.InitialState.Level)
                .FirstOrDefault(item => item.InitialState.OfficeRank < OfficerProgressionRules.HighestEligibleRank(item.InitialState.Level, item.InitialState.Merit));
            if (candidate is null) continue;
            var track = candidate.InitialState.Appointment == "general" ? "military" : "civil";
            PromoteOfficerInternal(candidate, track, false);
        }
    }
}
