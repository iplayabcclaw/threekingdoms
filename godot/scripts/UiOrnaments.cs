using Godot;

namespace ThreeKingdomsSimulator.Godot;

/// <summary>
/// Mounts generated monochrome ink-wash corner assets behind panel content.
/// The ornament bitmaps contain no authoritative UI information and never
/// receive mouse input.
/// </summary>
public static class UiOrnaments
{
    public const string PinePath = "res://assets/runtime/ui/ink-pine-corner-v1.png";
    public const string PeachPath = "res://assets/runtime/ui/ink-peach-corner-v1.png";

    public static void AttachInkCorners(Control parent, float size = 230f, float opacity = .10f)
    {
        var overlay = new Control
        {
            Name = "InkCornerOrnaments",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = true,
        };
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(overlay);
        parent.MoveChild(overlay, 0);

        overlay.AddChild(Corner(PinePath, size, opacity, false));
        overlay.AddChild(Corner(PeachPath, size, opacity, true));
    }

    private static TextureRect Corner(string path, float size, float opacity, bool bottomRight)
    {
        var texture = GD.Load<Texture2D>(path);
        if (texture is null) throw new InvalidOperationException($"水墨角花素材加载失败：{path}");
        var ornament = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, opacity),
        };
        if (bottomRight)
        {
            ornament.AnchorLeft = 1f;
            ornament.AnchorTop = 1f;
            ornament.AnchorRight = 1f;
            ornament.AnchorBottom = 1f;
            ornament.OffsetLeft = -size;
            ornament.OffsetTop = -size;
        }
        else
        {
            ornament.OffsetRight = size;
            ornament.OffsetBottom = size;
        }
        return ornament;
    }
}
