Shader "Custom/Grass"
{
    Properties
    {
        _BaseColor    ("Long grass colour",  Color)   = (0.2, 0.55, 0.1, 1)
        _CutColor     ("Cut grass colour",   Color)   = (0.55, 0.65, 0.2, 1)
        _CutMask      ("Cut mask",           2D)      = "black" {}
        _LawnMin      ("Lawn world min",     Vector)  = (0,0,0,0)
        _LawnSize     ("Lawn world size",    Vector)  = (10,1,10,0)
        _WindStrength ("Wind strength",      Float)   = 0.08
        _WindSpeed    ("Wind speed",         Float)   = 1.2
        _BladeWidth   ("Blade width",        Float)   = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off  // render both sides of each blade quad

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // Per-instance position buffer (set by GrassRenderer.cs)
            StructuredBuffer<float4> _Positions;  // xyz = world pos, w = height scale

            sampler2D _CutMask;
            float4    _LawnMin, _LawnSize;
            float4    _BaseColor, _CutColor;
            float     _WindStrength, _WindSpeed, _BladeWidth;

            struct appdata
            {
                float4 vertex    : POSITION;
                float2 uv        : TEXCOORD0;
                uint   instanceID: SV_InstanceID;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float  cutVal : TEXCOORD1;   // 0 = long, 1 = cut
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Fetch this blade's world position and height scale
                float4 data       = _Positions[v.instanceID];
                float3 worldRoot  = data.xyz;
                float  heightScale = data.w;

                // Sample the cut mask at this blade's UV
                float2 lawnUV = float2(
                    (worldRoot.x - _LawnMin.x) / _LawnSize.x,
                    (worldRoot.z - _LawnMin.z) / _LawnSize.z
                );
                float cutValue = tex2Dlod(_CutMask, float4(lawnUV, 0, 0)).r;

                // Effective height: long grass = full height, cut = 15% height
                float effectiveHeight = lerp(heightScale, heightScale * 0.15, cutValue);

                // v.uv.y goes 0 (root) to 1 (tip) — scale tip up by effective height
                // v.uv.x goes -0.5 to 0.5 — gives blade width
                float3 localOffset = float3(
                    v.uv.x * _BladeWidth,
                    v.uv.y * effectiveHeight,
                    0
                );

                // Wind: only affects the tip (v.uv.y = amount of sway)
                // Cut grass barely sways — multiply wind by (1 - cutValue)
                float windPhase = _Time.y * _WindSpeed + worldRoot.x * 0.5 + worldRoot.z * 0.3;
                float windX = sin(windPhase) * _WindStrength * v.uv.y * (1.0 - cutValue * 0.9);
                float windZ = cos(windPhase * 0.7) * _WindStrength * 0.4 * v.uv.y * (1.0 - cutValue * 0.9);
                localOffset.x += windX;
                localOffset.z += windZ;

                // Place in world space
                float3 worldPos = worldRoot + localOffset;
                o.pos    = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv     = v.uv;
                o.cutVal = cutValue;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Blend colour from long-grass green to cut-grass straw
                float4 col = lerp(_BaseColor, _CutColor, i.cutVal);

                // Darken the base slightly for depth
                col.rgb *= lerp(0.6, 1.0, i.uv.y);

                return col;
            }
            ENDHLSL
        }
    }
}