#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
    #define MY_HIGHP_OR_MEDIUMP highp
#else
    #define MY_HIGHP_OR_MEDIUMP mediump
#endif

// Strictly matched precision across both shader stages
extern MY_HIGHP_OR_MEDIUMP vec2 phantom;
extern MY_HIGHP_OR_MEDIUMP float dissolve;
extern MY_HIGHP_OR_MEDIUMP float time;
extern MY_HIGHP_OR_MEDIUMP vec4 texture_details;
extern MY_HIGHP_OR_MEDIUMP vec2 image_details;
extern bool shadow;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_1;
extern MY_HIGHP_OR_MEDIUMP vec4 burn_colour_2;
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
    // 1. Sample the exact texture atlas coordinate
    vec4 tex = Texel(texture, texture_coords);
    
    // 2. Isolate local UV (0.0 to 1.0 over the card sprite) to prevent atlas bleed
    vec2 local_uv = (((texture_coords) * (image_details)) - texture_details.xy * texture_details.ba) / max(texture_details.ba, 0.001);

    // 3. Phantom Grid Noise Logic
    // Define how many square blocks fit across the card
    float grid_density = 28.0; 
    
    // Floor the UVs to group pixels into solid squares
    vec2 block_uv = floor(local_uv * grid_density);
    
    // Create a time variable that steps rigidly (creates a flickering static effect rather than sliding)
    float time_step = floor(time * 12.0);
    
    // Generate pseudo-random static noise per block
    float block_noise = fract(sin(dot(block_uv + time_step, vec2(12.9898, 78.233))) * 43758.5453);
    
    // Define the Ash and White colors
    vec3 ash = vec3(0.45, 0.45, 0.47); // Dark, slightly cool grey
    vec3 white = vec3(1.0, 1.0, 1.0);
    vec3 noise_color = mix(ash, white, block_noise);
    
    // Calculate the brightness (luminance) of the original card art so we don't erase the drawing
    float lum = tex.r * 0.299 + tex.g * 0.587 + tex.b * 0.114;
    
    // Blend the original art with the shifting ash blocks
    tex.rgb = mix(tex.rgb, noise_color * lum * 1.8, 0.90);

    // 4. Safe Dummy Anchor
    // Prevents GLES from culling "unused" variables and crashing Lua
    float dummy = phantom.x + texture_details.x + image_details.x + 
                  mouse_screen_pos.x + hovering + screen_scale;
                  
    if (dummy > 9999999.0) {
        tex.a *= 0.99; 
    }

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

