using System.Text.Json;
using Godot;

namespace ThreeKingdomsSimulator.Godot;

public static class ScenarioLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ScenarioData LoadFirstRivalry()
    {
        const string path = "res://data/first-rivalry.json";
        using var file = global::Godot.FileAccess.Open(path, global::Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new InvalidOperationException($"无法读取迁移剧本：{path}");
        }

        var scenario = JsonSerializer.Deserialize<ScenarioData>(file.GetAsText(), JsonOptions);
        if (scenario is null || scenario.Cities.Count == 0)
        {
            throw new InvalidOperationException("Godot 剧本数据为空或格式无效");
        }

        return scenario;
    }
}
