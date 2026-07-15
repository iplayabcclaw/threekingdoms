using Godot;

namespace ThreeKingdomsSimulator.Godot;

/// <summary>
/// Shared visual language for the whole game.  The interface uses bright xuan-paper
/// surfaces, ink-dark typography, muted jade interaction states, restrained bronze
/// details, and cinnabar for decisive actions.
/// </summary>
public static class GameTheme
{
    public const string EmbeddedFontPath = "res://assets/fonts/NotoSansSC-VF.otf";
    public static readonly Color Ink = Color.FromHtml("#1f2a25");
    public static readonly Color Backdrop = Color.FromHtml("#f0efe7");
    public static readonly Color Panel = Color.FromHtml("#f2ecdf");
    public static readonly Color PanelRaised = Color.FromHtml("#fffaf0");
    // Kept as Paper for compatibility with the existing views; in the bright theme
    // it is the primary ink color used on paper surfaces.
    public static readonly Color Paper = Color.FromHtml("#202b26");
    public static readonly Color Muted = Color.FromHtml("#46534d");
    public static readonly Color Bronze = Color.FromHtml("#765527");
    public static readonly Color Jade = Color.FromHtml("#365f56");
    public static readonly Color Gold = Jade;
    public static readonly Color GoldDim = Color.FromHtml("#9f9173");
    public static readonly Color Cinnabar = Color.FromHtml("#a84e3f");
    public static readonly Color River = Color.FromHtml("#668794");
    public static readonly Color OnAccent = Color.FromHtml("#fff9eb");
    public static readonly Color Success = Color.FromHtml("#4f775f");
    public static readonly Color Danger = Color.FromHtml("#a3483a");

    public static Theme Create()
    {
        var font = GD.Load<Font>(EmbeddedFontPath);
        if (font is null) throw new InvalidOperationException($"内置中文字体加载失败：{EmbeddedFontPath}");
        var theme = new Theme { DefaultFont = font, DefaultFontSize = 16 };

        theme.SetColor("font_color", "Label", Paper);
        theme.SetColor("font_shadow_color", "Label", Colors.Transparent);
        theme.SetConstant("shadow_offset_x", "Label", 0);
        theme.SetConstant("shadow_offset_y", "Label", 1);
        theme.SetConstant("line_spacing", "Label", 4);

        ConfigureButtonTheme(theme, "Button");
        ConfigureButtonTheme(theme, "OptionButton");
        theme.SetConstant("arrow_margin", "OptionButton", 14);
        theme.SetColor("font_color", "CheckButton", Paper);
        theme.SetColor("font_hover_color", "CheckButton", Jade);
        theme.SetColor("font_pressed_color", "CheckButton", Jade);
        theme.SetConstant("h_separation", "CheckButton", 10);
        var transparent = new StyleBoxEmpty { ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6 };
        foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" }) theme.SetStylebox(state, "CheckButton", transparent);

        var input = Box(Color.FromHtml("#fffaf0"), new Color(GoldDim, .95f), 5, 1, 12, 7);
        var inputFocus = Box(Color.FromHtml("#fffdf7"), Jade, 5, 2, 11, 6);
        var inputReadOnly = Box(Color.FromHtml("#e7e7de"), new Color(GoldDim, .6f), 5, 1, 12, 7);
        theme.SetStylebox("normal", "LineEdit", input);
        theme.SetStylebox("focus", "LineEdit", inputFocus);
        theme.SetStylebox("read_only", "LineEdit", inputReadOnly);
        theme.SetColor("font_color", "LineEdit", Paper);
        theme.SetColor("font_placeholder_color", "LineEdit", new Color(Muted, .62f));
        theme.SetColor("caret_color", "LineEdit", Gold);
        theme.SetColor("selection_color", "LineEdit", new Color(Jade, .24f));

        theme.SetStylebox("panel", "PanelContainer", PanelBox());
        theme.SetStylebox("panel", "Panel", PanelBox());
        theme.SetStylebox("panel", "PopupPanel", RaisedBox(8));
        theme.SetStylebox("panel", "TooltipPanel", Box(Color.FromHtml("#fffaf0"), Jade, 6, 1, 12, 8));
        theme.SetColor("font_color", "TooltipLabel", Paper);

        var menu = Box(Color.FromHtml("#fffaf0"), new Color(Bronze, .7f), 7, 1, 8, 7);
        theme.SetStylebox("panel", "PopupMenu", menu);
        theme.SetStylebox("hover", "PopupMenu", Box(Color.FromHtml("#e3ebe5"), new Color(Jade, .75f), 4, 1, 8, 5));
        theme.SetColor("font_color", "PopupMenu", Paper);
        theme.SetColor("font_hover_color", "PopupMenu", Ink);
        theme.SetColor("font_accelerator_color", "PopupMenu", Muted);
        theme.SetConstant("item_start_padding", "PopupMenu", 12);
        theme.SetConstant("item_end_padding", "PopupMenu", 12);
        theme.SetConstant("v_separation", "PopupMenu", 6);

        theme.SetStylebox("scroll", "VScrollBar", Box(new Color(.84f, .85f, .81f, .72f), new Color(GoldDim, .36f), 5));
        theme.SetStylebox("grabber", "VScrollBar", Box(new Color(Jade, .48f), new Color(Jade, .62f), 5));
        theme.SetStylebox("grabber_highlight", "VScrollBar", Box(new Color(Jade, .72f), Jade, 5));
        theme.SetStylebox("grabber_pressed", "VScrollBar", Box(new Color(Cinnabar, .78f), Cinnabar, 5));
        theme.SetConstant("scrollbar_h_separation", "ScrollContainer", 10);
        theme.SetConstant("scrollbar_v_separation", "ScrollContainer", 10);

        theme.SetColor("font_color", "SpinBox", Paper);
        theme.SetColor("font_color", "RichTextLabel", Paper);
        return theme;
    }

    private static void ConfigureButtonTheme(Theme theme, string type)
    {
        theme.SetColor("font_color", type, Paper);
        theme.SetColor("font_hover_color", type, Ink);
        theme.SetColor("font_pressed_color", type, OnAccent);
        theme.SetColor("font_focus_color", type, Jade);
        theme.SetColor("font_disabled_color", type, new Color(Muted, .42f));
        theme.SetStylebox("normal", type, ButtonBox("normal"));
        theme.SetStylebox("hover", type, ButtonBox("hover"));
        theme.SetStylebox("pressed", type, ButtonBox("pressed"));
        theme.SetStylebox("focus", type, ButtonBox("focus"));
        theme.SetStylebox("disabled", type, ButtonBox("disabled"));
        theme.SetConstant("outline_size", type, 0);
    }

    public static StyleBoxFlat Box(Color color, Color border, int radius = 6, int width = 1, int horizontalPadding = 0, int verticalPadding = 0)
    {
        return new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = horizontalPadding,
            ContentMarginRight = horizontalPadding,
            ContentMarginTop = verticalPadding,
            ContentMarginBottom = verticalPadding,
            AntiAliasing = true,
        };
    }

    public static StyleBoxFlat PanelBox(int radius = 9)
    {
        var box = Box(new Color(Panel, .965f), new Color(GoldDim, .9f), radius, 1);
        box.ShadowColor = new Color(.18f, .22f, .19f, .12f);
        box.ShadowSize = 5;
        box.ShadowOffset = new Vector2(0, 2);
        return box;
    }

    public static StyleBoxFlat RaisedBox(int radius = 10)
    {
        var box = Box(new Color(PanelRaised, .985f), new Color(Bronze, .68f), radius, 1);
        box.ShadowColor = new Color(.18f, .22f, .19f, .18f);
        box.ShadowSize = 9;
        box.ShadowOffset = new Vector2(0, 4);
        return box;
    }

    public static StyleBoxFlat HeaderBox()
    {
        var box = Box(new Color(PanelRaised, .975f), new Color(Bronze, .64f), 0, 0);
        box.BorderWidthBottom = 1;
        box.ShadowColor = new Color(.18f, .22f, .19f, .16f);
        box.ShadowSize = 7;
        box.ShadowOffset = new Vector2(0, 3);
        return box;
    }

    public static StyleBoxFlat ButtonBox(string state)
    {
        return state switch
        {
            "hover" => Box(Color.FromHtml("#e2ebe5"), Jade, 6, 1, 14, 8),
            "pressed" => Box(Jade, Color.FromHtml("#375a52"), 6, 2, 13, 7),
            "focus" => Box(Color.FromHtml("#fffdf7"), Jade, 6, 2, 13, 7),
            "disabled" => Box(Color.FromHtml("#e4e5de"), new Color(GoldDim, .46f), 6, 1, 14, 8),
            _ => Box(Color.FromHtml("#fbf6ea"), new Color(Bronze, .62f), 6, 1, 14, 8),
        };
    }

    public static Button Button(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(116, 42),
            FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        button.AddThemeColorOverride("font_color", Paper);
        button.AddThemeColorOverride("font_hover_color", Ink);
        button.AddThemeColorOverride("font_pressed_color", OnAccent);
        button.AddThemeColorOverride("font_disabled_color", new Color(Muted, .42f));
        button.AddThemeStyleboxOverride("normal", ButtonBox("normal"));
        button.AddThemeStyleboxOverride("hover", ButtonBox("hover"));
        button.AddThemeStyleboxOverride("pressed", ButtonBox("pressed"));
        button.AddThemeStyleboxOverride("focus", ButtonBox("focus"));
        button.AddThemeStyleboxOverride("disabled", ButtonBox("disabled"));
        return button;
    }
}
