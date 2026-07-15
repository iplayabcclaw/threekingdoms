namespace ThreeKingdomsSimulator.Godot;

public static class AssetPaths
{
    private const string RuntimeRoot = "res://assets/runtime";
    public static IReadOnlyList<string> AmbientMusic { get; } =
    [
        "res://assets/audio/shan-he-wei-ding-v1.mp3",
        "res://assets/audio/feng-huo-chang-ge-v3.mp3",
    ];
    public const string BattleMusic = "res://assets/audio/jin-ge-po-zhen-v1.mp3";

    public static string OfficerPortrait(string officerId)
    {
        var slug = officerId.StartsWith("officer-", StringComparison.Ordinal) ? officerId[8..] : officerId;
        return $"{RuntimeRoot}/officer-portraits/{slug}-portrait-v1.webp";
    }

    public static string CityMarker(string region, string cityId)
    {
        var family = region switch
        {
            "并州" or "冀州" or "青州" => "north",
            "幽州" => "northeast",
            "扬州" => "jiangnan",
            "荆州" or "交州" or "益州" => "south",
            "凉州" or "雍州" => "xiliang",
            "南中" or "云南" => "nanman",
            _ => "central",
        };
        var explicitSecond = cityId is "city-tianshui" or "city-ye" or "city-beiping" or "city-xuchang" or "city-xiaopei" or "city-jianye" or "city-jiangling" or "city-luoyang";
        var explicitFirst = cityId is "city-wuwei" or "city-changan" or "city-jinyang" or "city-nanpi" or "city-yijing" or "city-ji" or "city-chenliu" or "city-puyang" or "city-xiapi" or "city-shouchun" or "city-xiangyang";
        var variant = explicitSecond ? 2 : explicitFirst ? 1 : cityId.Sum(character => character) % 2 + 1;
        return $"{RuntimeRoot}/map-markers/city-{family}-{variant}-v1.webp";
    }

    public static string PassMarker(string passId) => passId switch
    {
        "pass-yanmen" => $"{RuntimeRoot}/map-markers/pass-northern-v1.webp",
        "pass-hulao" => $"{RuntimeRoot}/map-markers/pass-river-v1.webp",
        "pass-bowang" => $"{RuntimeRoot}/map-markers/pass-southern-v1.webp",
        _ => $"{RuntimeRoot}/map-markers/pass-mountain-v1.webp",
    };

    public static string BattleBackground(string terrain) => $"{RuntimeRoot}/battle/backgrounds/battlefield-{((terrain is "hill" or "mountain" or "river") ? terrain : "plain")}-bg-v1.webp";

    public static string SiegeBackground(string region)
    {
        var family = region switch
        {
            "扬州" or "荆州" or "益州" or "交州" => "jiangnan",
            "凉州" or "雍州" => "xiliang",
            "南中" or "云南" => "nanman",
            _ => "central",
        };
        return $"{RuntimeRoot}/battle/backgrounds/siege-{family}-bg-v1.webp";
    }

    public static string TroopSprite(string troopType) => $"{RuntimeRoot}/battle/troops/{troopType}-battle-sprite-v1.webp";

    public static string MountedOfficerSprite(string spriteId) => $"{RuntimeRoot}/battle/officers/{spriteId}-mounted-sprite-v1.webp";

    public static string FactionFlag() => $"{RuntimeRoot}/map-markers/faction-flag-v1.png";
}
