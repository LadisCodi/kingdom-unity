// Trees and crops in the wind: the sprite's foot stays where it stands and its top leans, more the higher it is
// (the uv's height squared), on a slow wave that drifts across the map in gusts. The rest is a plain sprite: its
// texture times its tint (a tilemap's tile colour).
Shader "Kingdom/Sprite Sway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Amplitude ("Lean at the top, world units", Float) = 0.02
        _Speed ("Sway, radians a second", Float) = 1.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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

            sampler2D _MainTex;
            float _Amplitude;
            float _Speed;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                // The phase rides mostly on the height on the map, so both top corners of one sprite lean together.
                float phase = world.y * 1.7 + world.x * 0.15;
                float t = _Time.y;
                float gust = 0.65 + 0.35 * sin(t * 0.53 + world.x * 0.21 - world.y * 0.13);
                float bend = v.uv.y * v.uv.y;
                world.x += _Amplitude * bend * gust * sin(t * _Speed + phase);
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
