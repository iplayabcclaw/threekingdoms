shader_type canvas_item;

uniform vec4 faction_color : source_color = vec4(0.72, 0.35, 0.24, 1.0);
uniform float flag_phase = 0.0;

void fragment() {
    float cloth = smoothstep(0.18, 0.34, UV.x);
    vec2 waved_uv = UV;
    waved_uv.y += sin(TIME * 4.2 + UV.x * 9.0 + flag_phase) * 0.009 * cloth;
    waved_uv.x += sin(TIME * 3.1 + UV.y * 6.0 + flag_phase) * 0.004 * cloth;
    vec4 tex = texture(TEXTURE, waved_uv);
    float maximum = max(tex.r, max(tex.g, tex.b));
    float minimum = min(tex.r, min(tex.g, tex.b));
    float saturation = maximum - minimum;
    float luminance = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
    float neutral_cloth = smoothstep(0.38, 0.72, luminance) * (1.0 - smoothstep(0.10, 0.32, saturation));
    vec3 dyed_cloth = faction_color.rgb * (0.42 + luminance * 0.78);
    vec3 final_color = mix(tex.rgb, dyed_cloth, neutral_cloth * 0.94);
    COLOR = vec4(final_color, tex.a);
}
