#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
    #define MY_HIGHP_OR_MEDIUMP highp
#else
    #define MY_HIGHP_OR_MEDIUMP mediump
#endif

extern MY_HIGHP_OR_MEDIUMP vec2 tentacle;
extern MY_HIGHP_OR_MEDIUMP float dissolve;
extern MY_HIGHP_OR_MEDIUMP float time;
extern MY_HIGHP_OR_MEDIUMP vec4 texture_details;
extern MY_HIGHP_OR_MEDIUMP vec2 image_details;
extern bool shadow;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_1;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_2;
extern MY_HIGHP_OR_MEDIUMP float real_time;
extern MY_HIGHP_OR_MEDIUMP vec2 mouse_screen_pos;
extern MY_HIGHP_OR_MEDIUMP float hovering;
extern MY_HIGHP_OR_MEDIUMP float screen_scale;

vec4 dissolve_mask(vec4 tex, vec2 local_uv)
{
    if (dissolve < 0.001) {
        return vec4(shadow ? vec3(0.0) : tex.xyz, shadow ? tex.a * 0.3 : tex.a);
    }

    // Lightweight pseudo-random noise dissolve
    float noise = fract(sin(dot(local_uv.xy, vec2(12.9898, 78.233))) * 43758.5453);
    float adjusted_dissolve = dissolve * 1.05;

    if (tex.a > 0.01 && burn_colour_1.a > 0.01 && !shadow) {
        if (noise < adjusted_dissolve + 0.1 && noise > adjusted_dissolve) {
            tex.rgba = burn_colour_1;
        } else if (burn_colour_2.a > 0.01 && noise < adjusted_dissolve + 0.2 && noise > adjusted_dissolve) {
            tex.rgba = burn_colour_2;
        }
    }

    float final_alpha = noise > adjusted_dissolve ? (shadow ? tex.a * 0.3 : tex.a) : 0.0;
    return vec4(shadow ? vec3(0.0) : tex.xyz, final_alpha);
}

vec4 effect(vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords)
{
    // 1. We MUST NOT distort texture_coords directly. 
    // This perfectly samples the sprite from the giant Balatro Atlas without bleeding into white space.
    vec4 tex = Texel(texture, texture_coords);
    
    // 2. Calculate the local UV (0.0 to 1.0 mapping across just this specific card)
    vec2 local_uv = (((texture_coords) * (image_details)) - texture_details.xy * texture_details.ba) / max(texture_details.ba, 0.001);

    // 3. The Void Pulse (Aesthetic color distortion instead of physical distortion)
    vec2 center_uv = local_uv - 0.5;
    float dist_sq = dot(center_uv, center_uv); 
    
    // Create pulsing rings expanding outward
    float ring = sin(dist_sq * 40.0 - (time + real_time) * 6.0);
    
    // Deep purple/magenta void color
    vec3 void_color = vec3(0.5, 0.1, 0.8);
    
    // Darken edges and apply the pulsating ring
    float shade = smoothstep(0.5, 0.0, dist_sq) * (0.8 + 0.2 * ring);
    tex.rgb = mix(tex.rgb, tex.rgb * void_color * 2.5, 1.0 - shade);

    // 4. Safe Dummy Anchor
    // Forces the compiler to keep these variables, but prevents NaN corruption if Lua drops a variable.
    float dummy = tentacle.x + texture_details.x + image_details.x + 
                  mouse_screen_pos.x + hovering + screen_scale;
                  
    if (dummy > 9999999.0) {
        tex.a *= 0.99; 
    }

    // Apply the dissolve mask using the safe local_uv
    return dissolve_mask(tex * colour, local_uv);
}

#ifdef VERTEX
vec4 position(mat4 transform_projection, vec4 vertex_position)
{
    if (hovering <= 0.0) {
        return transform_projection * vertex_position;
    }
    
    // Safe vertex displacement
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy) / max(screen_scale, 0.001);
    float dist_sq = dot(mouse_offset, mouse_offset);
    float scale = -0.005 * hovering * min(dist_sq, 50.0); 

    return transform_projection * vertex_position + vec4(0.0, 0.0, 0.0, scale);
}
#endif

