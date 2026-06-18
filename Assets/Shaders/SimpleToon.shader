Shader "YaeSakura/SimpleToon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RampTex ("Ramp", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.4, 0.3, 0.4, 1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            sampler2D _RampTex;
            float4 _MainTex_ST;
            float4 _ShadowColor;
            float _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Simple toon ramp: dot(worldNormal, lightDir) → 0 to 1
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = dot(normalize(i.worldNormal), lightDir);

                // 2-step ramp: lit > 0.3 = full, shadow = dark
                float ramp = NdotL > 0.15 ? 1.0 : 0.5;

                // Apply shadow
                fixed shadow = SHADOW_ATTENUATION(i);
                ramp *= lerp(0.65, 1.0, shadow);

                fixed4 col = tex * ramp * _Brightness;
                col.rgb = lerp(_ShadowColor.rgb, col.rgb, ramp);

                return col;
            }
            ENDCG
        }

        // Shadow caster pass
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { V2F_SHADOW_CASTER; };

            v2f vert(appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
            fixed4 frag(v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i); }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
