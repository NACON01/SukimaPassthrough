Shader "Custom/SelectivePassthroughURP"
{
    Properties
    {
        _InvertedAlpha ("Inverted Alpha", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            // Blend Zero SrcAlpha: output = dst * src.a
            // frag alpha=0 (window open) -> dst.a *= 0 -> alpha hole -> passthrough shows through
            // frag alpha=1 (window closed) -> dst.a unchanged -> VR content stays
            Blend Zero SrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _InvertedAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // _InvertedAlpha=1: window open -> output alpha=0 -> punch hole for passthrough
                // _InvertedAlpha=0: window closed -> output alpha=1 -> preserve VR content
                return half4(0.0h, 0.0h, 0.0h, 1.0h - (half)_InvertedAlpha);
            }
            ENDHLSL
        }
    }
}
