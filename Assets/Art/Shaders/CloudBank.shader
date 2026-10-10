// THE CLOUD BANK (the web's fog/fogLayer.ts): every undiscovered cell, and everything past the province's edge, is one
// tileable cloud texture laid across the plane, drifting, cut to a mask of one texel a cell. The bank thins inside a
// fogged cell toward a seen neighbour, the tallest puffs lasting longest, so its edge is the outline of the clouds and
// not a line. Under it lies the bank's flat floor tone, so a bank cell is opaque.
//
// The web's numbers are in its projected plane (a cell 128 px wide) and in gamma space; the colour math runs in gamma
// here too, so the cut reads the texture's light as the web does.
Shader "Kingdom/Cloud Bank"
{
    Properties
    {
        _MainTex ("Cloud", 2D) = "white" {}
        _Mask ("Mask (one texel a cell, 1 under the bank)", 2D) = "white" {}
        _Floor ("Floor under the bank", Color) = (0.737, 0.761, 0.969, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // One repeat of the texture spans this many web pixels; a cell is 128 of them across.
            #define CLOUD_PX 900.0
            // How far into a fogged cell, in cells, the bank takes to reach full thickness.
            #define EDGE_CELLS 0.55
            // One repeat of the drift every DRIFT_S seconds; the clock wraps at twice that, a whole repeat of every layer.
            #define DRIFT_S 600.0
            #define BOIL 0.025

            sampler2D _MainTex;
            sampler2D _Mask;
            fixed4 _Floor;
            // (origin x, origin y, width, height) of the mask, in tilemap cells.
            float4 _MaskRect;
            // World to continuous tilemap cell: (m00, m01, m10, m11), and the world origin of cell (0, 0).
            float4 _WorldToCell;
            float4 _CellOrigin;
            // Web pixels a world unit, and the clock in seconds (wrapped by the view).
            float _PixelsPerUnit;
            float _BankTime;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 world : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            float fogAt(float2 c)
            {
                return tex2Dlod(_Mask, float4((c - _MaskRect.xy + 0.5) / _MaskRect.zw, 0, 0)).r;
            }

            float3 gammaOf(float3 c)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return c;
                #else
                return LinearToGammaSpace(c);
                #endif
            }

            // The texture is imported without sRGB, so it samples the web's own (gamma) values.
            float3 sampleCloud(float2 uv)
            {
                return tex2D(_MainTex, uv).rgb;
            }

            float3 cloudAt(float2 proj)
            {
                float2 uv = proj / CLOUD_PX;
                float d = _BankTime / DRIFT_S;
                float2 boil = sampleCloud(uv * 0.5 + float2(d * 2.0, d)).rg - 0.5;
                return sampleCloud(uv + float2(d, 0.0) + boil * BOIL);
            }

            // The texture's light is its height: sunlit tops stand tallest.
            float cloudHeight(float3 col)
            {
                return saturate((dot(col, float3(0.299, 0.587, 0.114)) - 0.68) / 0.3);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 local = i.world - _CellOrigin.xy;
                float2 p = float2(dot(_WorldToCell.xy, local), dot(_WorldToCell.zw, local));
                float2 c = floor(p);
                if (fogAt(c) < 0.5) discard;

                // How far, in cells, to the nearest ground the player can see.
                float d = 2.0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        float2 n = c + float2(dx, dy);
                        if (fogAt(n) < 0.5) d = min(d, length(max(abs(p - n - 0.5) - 0.5, 0.0)));
                    }
                }

                float th = (1.0 - smoothstep(0.0, EDGE_CELLS, d)) * 1.1;
                // The web's plane: x right, y down, a cell 128 px across.
                float2 proj = float2(i.world.x, -i.world.y) * _PixelsPerUnit;
                float3 col = cloudAt(proj);
                float cut = cloudHeight(col) - th;
                float a = smoothstep(-0.05, 0.05, cut);
                // A soft shadow under the outline, so the edge is clean.
                col *= lerp(0.88, 1.0, smoothstep(0.0, 0.25, cut));
                float3 shown = lerp(gammaOf(_Floor.rgb), col, a);
                #ifdef UNITY_COLORSPACE_GAMMA
                return fixed4(shown, 1);
                #else
                return fixed4(GammaToLinearSpace(shown), 1);
                #endif
            }
            ENDCG
        }
    }
}
