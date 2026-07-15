using System.Text.Json;

namespace ThreeKingdomsSimulator.Godot;

public static class SaveService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static string SavePath(string kind, int slot) => $"user://saves/{kind}-{slot}.json";

    public static void WriteManual(GameSession state, int slot) => Write(state, "manual", Math.Clamp(slot, 1, 10));

    public static void WriteAuto(GameSession state)
    {
        var slot = (state.Turn - 1) % 3 + 1;
        Write(state, "auto", slot);
    }

    public static bool ShouldWriteAuto(GameSession state) => state.AutoSaveFrequency switch
    {
        "off" => false,
        "quarterly" => (state.Turn - 1) % 3 == 0,
        "yearly" => (state.Turn - 1) % 12 == 0,
        _ => true,
    };

    public static GameSession? Load(string kind, int slot)
    {
        var path = SavePath(kind, slot);
        if (!global::Godot.FileAccess.FileExists(path)) return null;
        using var file = global::Godot.FileAccess.Open(path, global::Godot.FileAccess.ModeFlags.Read);
        if (file is null) return null;
        var json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var state = JsonSerializer.Deserialize<GameSession>(json, Options);
            state?.EnsureDomesticDefaults();
            return state;
        }
        catch (JsonException)
        {
            // A partially written or legacy save must not prevent the save screen,
            // navigation, or visual regression suite from opening.
            return null;
        }
    }

    public static List<SaveSummaryData> List()
    {
        var result = new List<SaveSummaryData>();
        foreach (var kind in new[] { "auto", "manual" })
        {
            var max = kind == "auto" ? 3 : 10;
            for (var slot = 1; slot <= max; slot++)
            {
                var state = Load(kind, slot);
                if (state is not null) result.Add(new SaveSummaryData { Kind = kind, Slot = slot, Turn = state.Turn, Year = state.Year, Month = state.Month });
            }
        }
        return result.OrderBy(item => item.Kind).ThenBy(item => item.Slot).ToList();
    }

    private static void Write(GameSession state, string kind, int slot)
    {
        global::Godot.DirAccess.MakeDirRecursiveAbsolute(global::Godot.ProjectSettings.GlobalizePath("user://saves"));
        using var file = global::Godot.FileAccess.Open(SavePath(kind, slot), global::Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(state, Options));
    }
}

public sealed class SaveSummaryData
{
    public string Kind { get; set; } = string.Empty;
    public int Slot { get; set; }
    public int Turn { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}
