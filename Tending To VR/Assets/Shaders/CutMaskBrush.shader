Shader "Hidden/CutMaskBrush"
{
    Properties
    {
        _MainTex    ("Main", 2D)         = "black" {}
        _BrushUV    ("Brush UV centre",  Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius("Brush radius",     Float)  = 0.5
        _LawnSize   ("Lawn Size (X, Z)", Vector) = (1.0, 1.0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        // Use MAX blending so painted areas never un-paint
        Blend One One
        BlendOp Max

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _BrushUV;
            float     _BrushRadius;
            float4    _LawnSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION;  float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Existing paint value (so we never erase)
                float existing = tex2D(_MainTex, i.uv).r;

                // Circular brush using actual world-scale distances
                // Scale the UVs by the lawn's physical dimensions to get distance in meters
                float2 scaledUV = i.uv * _LawnSize.xy;
                float2 scaledBrushCenter = _BrushUV.xy * _LawnSize.xy;

                float dist = distance(scaledUV, scaledBrushCenter);
                
                // Falloff from mostly full to 0 at the radius edge edge to make the brush look closer to collider size
                float brush = 1.0 - smoothstep(_BrushRadius * 0.95, _BrushRadius, dist);

                return float4(max(existing, brush), 0, 0, 1);
            }
            ENDHLSL
        }
    }
}