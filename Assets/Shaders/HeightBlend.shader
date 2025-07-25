Shader "EndlessWorld/HeightBlendArray"
{
    Properties
    {
        _BiomeTexArr ("Biome Textures", 2DArray) = "" {}
        _Tiling      ("UV Tiling", Float) = 8
        _ChunkSize   ("Chunk Size", Float) = 240
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_BiomeTexArr);
            SAMPLER(sampler_BiomeTexArr);

            float _Tiling;
            float _ChunkSize;

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
                float4 color       : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos   = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS   = TransformWorldToHClip(worldPos);
                OUT.uv            = (worldPos.xz / _ChunkSize) * _Tiling;
                OUT.color         = IN.color;   // rgb = tint, a = biome index
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                int   biomeIdx = (int)(IN.color.a * 255.0 + 0.5);
                half4 tex      = SAMPLE_TEXTURE2D_ARRAY(_BiomeTexArr,
                                                        sampler_BiomeTexArr,
                                                        float3(IN.uv, biomeIdx));
                return tex * half4(IN.color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
