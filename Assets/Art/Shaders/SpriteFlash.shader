// A tap's white flash, as the web draws it: the sprite added onto itself (the canvas's 'lighter'), up to _Boost
// times at the flash's peak. The renderer's alpha is how much of the flash is left.
Shader "Kingdom/Sprite Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Boost ("Copies added at the peak", Float) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Boost;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return fixed4(tex.rgb * i.color.rgb * tex.a * i.color.a * _Boost, 0);
            }
            ENDCG
        }
    }
}
