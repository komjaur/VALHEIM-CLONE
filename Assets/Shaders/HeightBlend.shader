Shader "EndlessWorld/VertexColor"
{
    Properties
    {
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

            float _ChunkSize;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos   = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS   = TransformWorldToHClip(worldPos);
                OUT.color         = IN.color;   // rgb = tint, a = biome index
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(IN.color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
