Shader "BagToTheKey/SpriteTextureMix"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Sprite", 2D) = "white" {}
        [PerRendererData] _BlendTex ("Target Sprite", 2D) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0
        [HideInInspector] _MainTexRect ("Source UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _BlendTexRect ("Target UV Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteTextureMix"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BlendTex);
            SAMPLER(sampler_BlendTex);

            float4 _MainTexRect;
            float4 _BlendTexRect;
            float _Blend;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 sourceSize = max(_MainTexRect.zw, float2(0.00001, 0.00001));
                float2 localUv = (input.uv - _MainTexRect.xy) / sourceSize;
                float2 targetUv = _BlendTexRect.xy + localUv * _BlendTexRect.zw;

                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 target = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, targetUv);
                return lerp(source, target, saturate(_Blend)) * input.color;
            }
            ENDHLSL
        }
    }
}
