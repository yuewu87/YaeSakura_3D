Shader "YaeSakura/AnimeToon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RampTex ("Ramp (toon2/toon3)", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.55, 0.5, 0.6, 1)
        _Brightness ("Brightness", Range(0.5, 2)) = 1.0
        _RimColor ("Rim Color", Color) = (0.8, 0.85, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 6)) = 3.0
        _OutlineColor ("Outline Color", Color) = (0.1, 0.1, 0.15, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.03)) = 0.005
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        // ── Outline Pass ──
        Pass
        {
            Name "Outline"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 normal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float2 offset = TransformViewToProjection(normal.xy);
                o.pos = UnityObjectToClipPos(v.vertex + v.normal * _OutlineWidth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        // ── Main Pass ──
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back

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
                float3 viewDir : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            sampler2D _RampTex;
            float4 _MainTex_ST;
            float4 _ShadowColor;
            float _Brightness;
            float4 _RimColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float3 worldNormal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(i.viewDir);

                // Ramp: use NdotL to sample ramp texture for smooth toon shading
                float NdotL = dot(worldNormal, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5; // 0→1
                fixed4 ramp = tex2D(_RampTex, float2(halfLambert, 0.5));

                // Shadow
                fixed shadow = SHADOW_ATTENUATION(i);
                float shadowMask = lerp(0.6, 1.0, shadow);
                float3 litColor = tex.rgb * ramp.rgb * _Brightness * shadowMask;
                float3 shadowCol = tex.rgb * _ShadowColor.rgb * 0.7;
                float3 diffuse = lerp(shadowCol, litColor, step(0.1, halfLambert * shadowMask));

                // Rim light
                float rim = 1.0 - saturate(dot(worldNormal, viewDir));
                rim = pow(rim, _RimPower);
                diffuse += _RimColor.rgb * rim * 0.3;

                return fixed4(diffuse, 1);
            }
            ENDCG
        }

        // Shadow caster
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
