namespace ThreeKingdomsSimulator.Godot;

public sealed class ScenarioData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string PlayerFactionId { get; set; } = string.Empty;
    public ResourceData Resources { get; set; } = new();
    public List<FactionData> Factions { get; set; } = [];
    public List<CityData> Cities { get; set; } = [];
    public List<ScenarioOfficerData> Officers { get; set; } = [];
    public List<RoadData> Roads { get; set; } = [];
    public List<PassData> Passes { get; set; } = [];
    public List<EventDefinitionData> Events { get; set; } = [];
}

public sealed class NewGameOptions
{
    public string PlayerFactionId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "standard";
    public string AutoSaveFrequency { get; set; } = "monthly";
}

public sealed class ResourceData
{
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Prestige { get; set; }
    public int Equipment { get; set; }
}

public sealed class FactionData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string RulerName { get; set; } = string.Empty;
    public string Color { get; set; } = "#777777";
    public string Pattern { get; set; } = "sun";
}

public sealed class CityData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string OwnerFactionId { get; set; } = string.Empty;
    public string GovernorId { get; set; } = string.Empty;
    public string GovernorName { get; set; } = string.Empty;
    public MapPosition Position { get; set; } = new();
    public int Population { get; set; }
    public int Agriculture { get; set; }
    public int Commerce { get; set; }
    public int PublicOrder { get; set; }
    public int PublicSupport { get; set; }
    public int Defense { get; set; }
    public int Culture { get; set; }
    public int Fatigue { get; set; }
    public int Training { get; set; }
    public int FacilitySlots { get; set; } = 4;
    public List<FacilityInstanceData> Facilities { get; set; } = [];
    public ConstructionData? ConstructionQueue { get; set; }
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Garrison { get; set; }
    public int WallDurability { get; set; } = 100;
    public int GateDurability { get; set; } = 100;
    public int InnerControl { get; set; } = 100;
    public int ActionCapacity { get; set; } = 2;
    public int ActionSlots { get; set; } = 2;
    public int IntelligenceAge { get; set; }
    public string Status { get; set; } = "stable";
    public string GovernanceMode { get; set; } = "manual";
    public string GovernancePolicy { get; set; } = "balanced";
    public string CityRole { get; set; } = "unassigned";
    public int RoleTransitionMonths { get; set; }
    public int IntegrationMonthsRemaining { get; set; }
    public List<string> MonthlyOfficerActionIds { get; set; } = [];
    public List<CityLedgerEntryData> LedgerEntries { get; set; } = [];
    public string LastMonthlyReport { get; set; } = string.Empty;
}

public sealed class CityLedgerEntryData
{
    public int Turn { get; set; }
    public string Category { get; set; } = string.Empty;
    public int GoldDelta { get; set; }
    public int FoodDelta { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class FacilityInstanceData
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public int SlotIndex { get; set; } = -1;
    public int Level { get; set; } = 1;
    public int Condition { get; set; } = 100;
}

public sealed class ConstructionData
{
    public string DefinitionId { get; set; } = string.Empty;
    public string OfficerId { get; set; } = string.Empty;
    public int RemainingMonths { get; set; }
    public int TotalMonths { get; set; }
    public string Kind { get; set; } = "build";
    public string? TargetInstanceId { get; set; }
    public int TargetSlotIndex { get; set; } = -1;
}

public sealed class ScenarioOfficerData
{
    public OfficerProfileData Profile { get; set; } = new();
    public OfficerStateData InitialState { get; set; } = new();
}

public sealed class OfficerProfileData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CourtesyName { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public OfficerAbilitiesData Abilities { get; set; } = new();
    public OfficerAbilitiesData AbilityPotential { get; set; } = new();
    public string GrowthArchetype { get; set; } = string.Empty;
    public List<string> GrowthPlan { get; set; } = [];
    public string FameTier { get; set; } = string.Empty;
    public List<string> Traits { get; set; } = [];
    public List<string> Ideals { get; set; } = [];
}

public sealed class OfficerAbilitiesData
{
    public int Leadership { get; set; }
    public int Might { get; set; }
    public int Intelligence { get; set; }
    public int Politics { get; set; }
    public int Charisma { get; set; }
}

public sealed class OfficerStateData
{
    public string Id { get; set; } = string.Empty;
    public string? FactionId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public int Loyalty { get; set; }
    public int Merit { get; set; }
    public int Level { get; set; }
    public int CareerExperience { get; set; }
    public OfficerAbilitiesData GrowthBonuses { get; set; } = new();
    public Dictionary<string, int> CareerRecords { get; set; } = [];
    public List<string> LearnedTraits { get; set; } = [];
    public int ExperienceTurn { get; set; }
    public int ExperienceEarnedThisTurn { get; set; }
    public int Health { get; set; }
    public int Fatigue { get; set; }
    public string Status { get; set; } = "serving";
    public bool Alive { get; set; } = true;
    public string? ArmyId { get; set; }
    public string Appointment { get; set; } = "reserve";
    public string OfficeTrack { get; set; } = string.Empty;
    public int OfficeRank { get; set; }
    public string CourtOfficeId { get; set; } = string.Empty;
    public int LastPromotionTurn { get; set; } = -99;
    public int TrackTransitionMonths { get; set; }
    public string TravelTargetCityId { get; set; } = string.Empty;
    public int TravelTotalDays { get; set; }
    public int TravelRemainingDays { get; set; }
    public int SalaryArrears { get; set; }
    public int SalaryArrearsMonths { get; set; }
}

public sealed class RoadData
{
    public string Id { get; set; } = string.Empty;
    public string FromCityId { get; set; } = string.Empty;
    public string ToCityId { get; set; } = string.Empty;
    public string Kind { get; set; } = "trunk";
    public string Terrain { get; set; } = "plain";
    public int TravelDays { get; set; }
    public List<MapPosition> Waypoints { get; set; } = [];
}

public sealed class PassData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoadId { get; set; } = string.Empty;
    public MapPosition Position { get; set; } = new();
    public int DefenseBonus { get; set; }
}

public sealed class MapPosition
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class EventDefinitionData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Condition { get; set; } = "always";
    public int Weight { get; set; }
    public int CooldownTurns { get; set; }
    public List<EventChoiceData> Choices { get; set; } = [];
}

public sealed class EventChoiceData
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EventEffectData> Effects { get; set; } = [];
}

public sealed class EventEffectData
{
    public string Type { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string? Metric { get; set; }
    public int Amount { get; set; }
}
