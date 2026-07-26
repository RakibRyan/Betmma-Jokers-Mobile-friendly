#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
    #define MY_HIGHP_OR_MEDIUMP highp
#else
    #define MY_HIGHP_OR_MEDIUMP mediump
#endif

// Keep all uniform attachments intact so Steamodded doesn't crash when binding variables
extern MY_HIGHP_OR_MEDIUMP vec2 cooldown;
extern MY_HIGHP_OR_MEDIUMP number dissolve;
extern MY_HIGHP_OR_MEDIUMP number time;
extern MY_HIGHP_OR_MEDIUMP vec4 texture_details;
extern MY_HIGHP_OR_MEDIUMP vec2 image_details;
extern bool shadow;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_1;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_2;
extern float percentage;
extern MY_HIGHP_OR_MEDIUMP vec2 mouse_screen_pos;
extern MY_HIGHP_OR_MEDIUMP float hovering;
extern MY_HIGHP_OR_MEDIUMP float screen_scale;

// Highly efficient, mobile-safe math variant of the dissolve routine
vec4 dissolve_mask(vec4 tex, vec2 texture_coords, vec2 uv)
{
    if (dissolve < 0.001) {
        return vec4(shadow ? vec3(0.0) : tex.xyz, shadow ? tex.a * 0.3 : tex.a);
    }

    float adjusted_dissolve = (dissolve * dissolve * (3.0 - 2.0 * dissolve)) * 1.02 - 0.01;
    vec2 floored_uv = floor(uv * texture_details.ba) / max(texture_details.b, texture_details.a);
    
    // Efficient procedural pseudo-random value for crisp digital pixel breakdown
    float simple_noise = fract(sin(dot(floored_uv, vec2(12.9898, 78.233))) * 43758.5453);
    
    if (tex.a > 0.01 && burn_colour_1.a > 0.01 && !shadow && simple_noise < adjusted_dissolve + 0.1 && simple_noise > adjusted_dissolve) {
        tex.rgba = mix(burn_colour_1.rgba, burn_colour_2.rgba, step(0.05, simple_noise - adjusted_dissolve));
    }

    return vec4(shadow ? vec3(0.0) : tex.xyz, simple_noise > adjusted_dissolve ? (shadow ? tex.a * 0.3 : tex.a) : 0.0);
}

vec4 effect( vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords )
{
    vec4 tex = Texel(texture, texture_coords);
    vec2 uv = (((texture_coords) * (image_details)) - texture_details.xy * texture_details.ba) / texture_details.ba;

    // Optimized Desaturation: Replaced slow HSL conversion block with a fast matrix dot product
    float grayscale = dot(tex.rgb, vec3(0.2126, 0.7152, 0.0722));
    tex.rgb = mix(tex.rgb, vec3(grayscale), 0.15); 

    // Apply the cooling down overlay partition
    if (1.0 - uv.y < percentage) {
        tex.rgb *= 0.4;
    }

    return dissolve_mask(tex * colour, texture_coords, uv);
}

#ifdef VERTEX
vec4 position( mat4 transform_projection, vec4 vertex_position )
{
    if (hovering <= 0.0) {
        return transform_projection * vertex_position;
    }
    float mid_dist = length(vertex_position.xy - 0.5 * love_ScreenSize.xy) / length(love_ScreenSize.xy);
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy) / screen_scale;
    float scale = 0.2 * (-0.03 - 0.3 * max(0.0, 0.3 - mid_dist))
                * hovering * (length(mouse_offset) * length(mouse_offset)) / (2.0 - mid_dist);

    return transform_projection * vertex_position + vec4(0.0, 0.0, 0.0, scale);
}
#endif
