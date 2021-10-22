Shader "Unlit/ViewCone"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0.5)
        _StripeWidth ("Stripe Width", Range(0.1, 2)) = 1
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define USE_WORLD_POS 0

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 wPos : TEXCOORD1;
                float4 lPos : TEXCOORD2;
            };

            float4 _Color;
            float _StripeWidth;

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.wPos = mul(UNITY_MATRIX_M, v.vertex);
                o.lPos = v.vertex;
                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                #if USE_WORLD_POS
                // For world position
                float wRemainder = ((i.wPos.x + i.wPos.z) % _StripeWidth);
                wRemainder += _StripeWidth * (wRemainder < 0);
                float wBrightStripe = wRemainder > (_StripeWidth / 2);
                float brightStripe = wBrightStripe;
                #else
                // For local Space
                float lRemainder = ((i.lPos.x + i.lPos.z) % _StripeWidth);
                lRemainder += _StripeWidth * (lRemainder < 0);
                float lBrightStripe = lRemainder > (_StripeWidth / 2);
                float brightStripe = lBrightStripe;
                #endif

                return lerp(0.5, 1, brightStripe) * _Color;
            }
            ENDCG
        }
    }
}
