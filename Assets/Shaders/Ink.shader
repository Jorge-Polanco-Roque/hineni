Shader "Hidden/LosQueQuedan/Ink"
{
 // Everything on screen is a character cell. Pass 0 picks, per world cell, the glyph whose shape best matches
 // the image under it (2x3 sub-regions, contrast-enhanced), with geometry contours drawn in as strokes.
 // Pass 1 draws world glyphs and, on its own coarser grid, the interface text written by InkCamera.
 Properties { _MainTex ("Scene", 2D) = "white" {} }
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 _MainTex_TexelSize;
 sampler2D _CameraDepthNormalsTexture;
 sampler2D _Cells;     // R = glyph index, G = scene tone, B = 1 when matched by shape
 sampler2D _UI;        // R = (glyph+1) low byte, G = brightness, B = mode | high bits << 2, A = accent
 sampler2D _CJKGlyphs; // wide characters, two cells each, in a grid of _CJKGrid.x columns
 float4 _CJKGrid;
 sampler2D _Glyphs, _UIGlyphs;
 float4 _Grid;         // world: cols, rows, cell width px, cell height px
 float4 _UIGrid;       // interface: cols, rows, cell width px, cell height px
 float4 _Tone;         // black point, white point, gamma, minimum glyph brightness
 float4 _Shape;        // directional exponent, global exponent, matchable glyphs, atlas glyphs
 float4 _Contour;      // stroke brightness, depth threshold, normal threshold, fade distance (m)
 float4 _Look;         // empty-cell dot brightness, panel dim, reveal 0..1, raw scene 0/1
 float4 _Ramp;         // flat-cell threshold, ramp length
 float _RampGlyph[16];
 float4 _Ink, _Paper, _Accent, _Shadow;
 float4 _Color;        // colour amount, saturation boost
 float4 _Glitch;       // tearing 0..1, time, engraving palette 0/1
 float4 _EngPaper, _EngInk, _EngBlood;
 sampler2D _Mean;      // mip-mapped copy of the frame; its last mip is the average (auto exposure)
 float4 _Exposure;     // adaptation strength, last mip level, local contrast
 float4 _ShapeA[96], _ShapeB[96];

 float lum(float3 c) { return dot(c, float3(.2126, .7152, .0722)); }
 float2 UVFromTop(float2 p) { p /= _MainTex_TexelSize.zw; return float2(p.x, 1 - p.y); }
 // Auto exposure: the white point rises with the frame's average, so close-ups keep their features.
 float White() { return max(_Tone.y, lum(tex2Dlod(_Mean, float4(.5, .5, 0, _Exposure.y)).rgb) * _Exposure.x); }
 // Filmic shoulder: highlights are compressed instead of clipped, so lit skin keeps its modelling.
 float Curve(float l)
 {
  float x = max(0, (l - _Tone.x) / (White() - _Tone.x));
  return pow(1 - exp(-x * 1.35), _Tone.z);
 }
 // Local contrast ("clarity"): each point is pushed away from its neighbourhood average, so eyes, brows
 // and lips separate from skin even when the whole face sits in half-shadow.
 float Tone(float2 p)
 {
  float2 uv = UVFromTop(p);
  float t = Curve(lum(tex2D(_MainTex, uv).rgb));
  float local = Curve(lum(tex2Dlod(_Mean, float4(uv, 0, 3)).rgb));
  return saturate(t + (t - local) * _Exposure.z);
 }
 void Geo(float2 p, out float depth, out float3 normal)
 {
  DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, UVFromTop(p)), depth, normal);
  depth *= _ProjectionParams.z;   // metres
 }
 // Contour strength at a point: depth discontinuities (silhouettes) and normal creases (edges of forms).
 float Contour(float2 p, float2 step)
 {
  float d0, d1, d2; float3 n0, n1, n2;
  Geo(p, d0, n0); Geo(p + float2(step.x, 0), d1, n1); Geo(p + float2(0, step.y), d2, n2);
  float dd = max(abs(d1 - d0), abs(d2 - d0)) / max(d0, .05);
  float nd = max(1 - dot(n0, n1), 1 - dot(n0, n2));
  float e = max(saturate((dd - _Contour.y) * 8), saturate((nd - _Contour.z) * 4));
  return e * saturate(1 - d0 / _Contour.w);
 }
 float Hash(float2 c) { return frac(sin(dot(c, float2(127.1, 311.7))) * 43758.5453); }
 ENDCG
 SubShader
 {
  Cull Off ZWrite Off ZTest Always
  Pass // 0: choose a glyph per world cell
  {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #pragma target 3.5
   float4 frag(v2f_img i) : SV_Target
   {
    float2 cell = floor(float2(i.uv.x, 1 - i.uv.y) * _Grid.xy);
    float2 size = _Grid.zw, origin = cell * size, region = size / float2(2, 3), tap = region * .25;
    float v[6], e[6];
    float avg = 0;
    [unroll] for (int k = 0; k < 6; k++)
    {
     float2 c = origin + (float2(k % 2, k / 2) + .5) * region;
     v[k] = (Tone(c + float2(-tap.x, -tap.y)) + Tone(c + float2(tap.x, -tap.y))
           + Tone(c + float2(-tap.x, tap.y)) + Tone(c + float2(tap.x, tap.y))) * .25;
     avg += v[k];
     v[k] = max(v[k], Contour(c, region * .5) * _Contour.x);   // strokes sit on top of tone
     // One sample just outside the cell, in the direction this sub-region faces.
     float2 dir = float2(k % 2 == 0 ? -1 : 1, k / 2 == 0 ? -1 : (k / 2 == 2 ? 1 : 0));
     e[k] = Tone(c + dir * region);
    }
    avg /= 6;
    float peak = 0;
    [unroll] for (int k = 0; k < 6; k++)
    {
     float m = max(max(v[k], e[k]), 1e-4);
     v[k] = pow(v[k] / m, _Shape.x) * m;   // sharpen boundaries against neighbours
     peak = max(peak, v[k]);
    }
    peak = max(peak, 1e-4);
    float4 a = float4(v[0], v[1], v[2], v[3]);
    float2 b = float2(v[4], v[5]);
    a = pow(a / peak, _Shape.y) * peak;       // sharpen inside the cell
    b = pow(b / peak, _Shape.y) * peak;
    // Flat cells take a quiet tonal ramp; only cells with inner structure are matched by shape.
    float lo = min(min(min(a.x, a.y), min(a.z, a.w)), min(b.x, b.y));
    if (peak - lo < _Ramp.x)
     return float4(_RampGlyph[(int)min(avg * _Ramp.y, _Ramp.y - 1)], avg, 0, 1);
    float best = 1e9, glyph = 0;
    for (int g = 0; g < (int)_Shape.z; g++)
    {
     float4 da = a - _ShapeA[g]; float2 db = b - _ShapeB[g].xy;
     float d = dot(da, da) + dot(db, db);
     if (d < best) { best = d; glyph = g; }
    }
    return float4(glyph, max(avg, peak * .6), 1, 1);
   }
   ENDCG
  }
  Pass // 1: draw
  {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #pragma target 3.5
   float Mask(sampler2D atlas, float glyph, float2 f) { return tex2D(atlas, float2((glyph + f.x) / _Shape.w, 1 - f.y)).a; }
   float4 frag(v2f_img i) : SV_Target
   {
    float2 px = float2(i.uv.x, 1 - i.uv.y) * _MainTex_TexelSize.zw;
    float2 screenPx = px;
    // Tearing: bands of rows slide sideways, glyphs scramble, the frame sometimes flips to its negative.
    float tick = floor(_Glitch.y * 24), band = floor(px.y / (_Grid.w * 2));
    float torn = step(1 - _Glitch.x * .55, Hash(float2(band, tick)));
    px.x += (Hash(float2(band + 3.1, tick)) - .5) * torn * _Glitch.x * 260;
    float2 p = px / _Grid.zw, cell = floor(p), f = frac(p);
    float2 w = tex2D(_Cells, float2((cell.x + .5) / _Grid.x, 1 - (cell.y + .5) / _Grid.y)).rg;
    if (Hash(cell + tick * 1.37) < _Glitch.x * .22) { w.x = floor(Hash(cell * 1.7 + tick) * _Shape.z); w.y = max(w.y, .5); }
    float2 q = px / _UIGrid.zw, uiCell = floor(q), uf = frac(q);
    float4 ui = tex2D(_UI, float2((uiCell.x + .5) / _UIGrid.x, 1 - (uiCell.y + .5) / _UIGrid.y));
    float ub = round(ui.b * 255), mode = fmod(ub, 4), uiGlyph = round(ui.r * 255) + floor(ub / 4) * 256 - 1;

    float grain = 1 - Hash(cell) * .1;                          // hand-set texture, stable over time
    float bright = lerp(_Tone.w, 1, saturate(w.y * 1.75)) * grain;
    float vignette = 1 - smoothstep(.5, 1.05, length((i.uv - .5) * float2(1.15, 1))) * .55;
    // Each glyph carries the hue of the scene under its cell: saturated, with shadows pulled to the shadow tint.
    float2 cuv = float2((cell.x + .5) * _Grid.z, (cell.y + .5) * _Grid.w) * _MainTex_TexelSize.xy;
    cuv.y = 1 - cuv.y;
    float2 o = _Grid.zw * .25 * _MainTex_TexelSize.xy;
    float3 scene = (tex2Dlod(_MainTex, float4(cuv + o, 0, 0)).rgb + tex2Dlod(_MainTex, float4(cuv - o, 0, 0)).rgb
                  + tex2Dlod(_MainTex, float4(cuv + float2(o.x, -o.y), 0, 0)).rgb + tex2Dlod(_MainTex, float4(cuv - float2(o.x, -o.y), 0, 0)).rgb) * .25;
    float3 hue = scene / max(lum(scene), .03);
    hue = clamp(lerp(1, hue, _Color.y), 0, 3);
    float3 tint = lerp(_Shadow.rgb, _Ink.rgb, saturate(w.y * 1.6)) * lerp(1, hue, _Color.x);
    float3 world = tint * bright * vignette * Mask(_Glyphs, w.x, f);
    if (w.x < .5) world = _Ink.rgb * _Look.x * vignette * Mask(_Glyphs, 1, f);   // '.' keeps the canvas alive
    if (_Look.w > .5) world = tex2D(_MainTex, i.uv).rgb;
    if (_Glitch.z > .5)
    {
     // Engraving: bone paper, glyphs in dried blood and soot. Bright things become dense ink.
     // Hard edges: almost nothing is grey. Blood keeps its red; everything else is soot.
     float g = smoothstep(.05, .3, lum(world));
     float red = saturate((hue.r - hue.g) / 2.5);
     float3 bone = _EngPaper.rgb * (1 - smoothstep(.35, 1.0, length((i.uv - .5) * float2(1.15, 1))) * .75);
     world = bone * (1 - g) + g * lerp(_EngInk.rgb, _EngBlood.rgb, red) - _Paper.rgb;
    }
    if (Hash(float2(tick, 5)) < _Glitch.x * .12) world = 1 - world - _Paper.rgb * 2;
    if (mode > .5) world = _Glitch.z > .5 ? _EngPaper.rgb * .97 - _Paper.rgb : world * _Look.y;   // panels are paper in an engraving

    float3 col = world;
    if (uiGlyph >= 0)
    {
     float3 tint = lerp(_Ink.rgb, _Accent.rgb, ui.a) * ui.g;
     if (_Glitch.z > .5) tint = lerp(float3(.12, .02, .02), float3(.5, .03, .02), ui.a) * ui.g - _Paper.rgb;
     float m;
     if (uiGlyph >= 256)
     {
      float k = uiGlyph - 256, col = fmod(k, _CJKGrid.x), row = floor(k / _CJKGrid.x);
      m = tex2D(_CJKGlyphs, float2((col + uf.x) / _CJKGrid.x, 1 - (row + uf.y) / _CJKGrid.y)).a;
     }
     else m = Mask(_UIGlyphs, uiGlyph, uf);
     col = mode > 1.5 ? lerp(tint, _Paper.rgb, m) : lerp(world, tint, m);
    }
    else if (mode > 1.5) col = lerp(_Ink.rgb, _Accent.rgb, ui.a) * ui.g;
    // Cells appear one by one instead of a fade to black.
    if (Hash(cell + 17.3) > _Look.z) col = 0;
    return float4(_Paper.rgb + col, 1);
   }
   ENDCG
  }
 }
}
