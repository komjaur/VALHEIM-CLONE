Shader "EndlessWorld/HeightBlend"
{
    Properties
    {
        _Sand  ("Sand Texture" , 2D) = "white" {}
        _Grass ("Grass Texture", 2D) = "white" {}
        _Stone ("Stone Texture", 2D) = "white" {}
        _Tiling("UV Tiling"    , Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float selector     : TEXCOORD1; // 0,0.5,1 from G
            };

            TEXTURE2D(_Sand);  SAMPLER(sampler_Sand);
            TEXTURE2D(_Grass); SAMPLER(sampler_Grass);
            TEXTURE2D(_Stone); SAMPLER(sampler_Stone);
            float _Tiling;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv * _Tiling;
                OUT.selector = IN.color.g; // we stored biome flag in vertex G
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 sand  = SAMPLE_TEXTURE2D(_Sand , sampler_Sand , IN.uv);
                half4 grass = SAMPLE_TEXTURE2D(_Grass, sampler_Grass, IN.uv);
                half4 stone = SAMPLE_TEXTURE2D(_Stone, sampler_Stone, IN.uv);

                // linear blend: if selector=0 → sand
                //               if selector=0.5 → grass
                //               if selector=1   → stone
                half mid = saturate((IN.selector - 0.0) / 0.5);   // 0 → 1 across sand→grass
                half top = saturate((IN.selector - 0.5) / 0.5);   // 0 → 1 across grass→stone

                half4 col = lerp(sand, grass, mid);      // first cross-fade
                col       = lerp(col , stone, top);      // second cross-fade
                return col;
            }
            ENDHLSL
        }
    }
}
