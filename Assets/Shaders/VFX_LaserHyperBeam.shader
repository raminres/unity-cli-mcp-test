Shader "Arcade/VFX_LaserHyperBeam"
{
    Properties
    {
        _MainTex ("Beam Gradient", 2D) = "white" {}
        _BaseMap ("Base Map (Fallback)", 2D) = "white" {}
        _CoreColor ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _InnerColor ("Inner Color", Color) = (1.0, 0.35, 0.6, 1.0)
        _OuterColor ("Outer Color", Color) = (1.0, 0.08, 0.28, 0.85)
        _BaseFlareColor ("Base Flare Color", Color) = (1.0, 0.85, 0.3, 1.0)
        _CoreWidth ("Core Width Ratio", Range(0.05, 0.6)) = 0.22
        _PulseSpeed ("Pulse Speed", Float) = 10.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseMap_ST;
                half4 _CoreColor;
                half4 _InnerColor;
                half4 _OuterColor;
                half4 _BaseFlareColor;
                float _CoreWidth;
                float _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float u = input.uv.x;
                float v = input.uv.y;

                // Horizontal distance from beam centerline [0 at center, 1 at edges]
                float distCenter = abs(u - 0.5) * 2.0;

                // Horizontal soft edge falloff
                float edgeFade = 1.0 - smoothstep(0.35, 1.0, distCenter);

                // Core and inner aura masks
                float coreMask = 1.0 - smoothstep(0.0, _CoreWidth, distCenter);
                float innerMask = 1.0 - smoothstep(_CoreWidth, 0.7, distCenter);

                // Vertical gradients: base muzzle flare at paddle deck (v near 0)
                float baseFlare = saturate(1.0 - v * 4.0) * 0.8;

                // High-velocity energy ripple scrolling upwards
                float ripple = sin(v * 28.0 - _Time.y * _PulseSpeed) * 0.12 + 0.88;

                // Leading tip surge glow (v near 1)
                float tipGlow = smoothstep(0.85, 1.0, v) * 0.45;

                // Gradient color blending: outer aura -> inner pink glow -> intense white core
                half3 col = lerp(_OuterColor.rgb, _InnerColor.rgb, innerMask);
                col = lerp(col, _CoreColor.rgb, coreMask);

                // Modulate with explicit texture if provided
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= texCol.rgb;

                col *= ripple;
                col += _BaseFlareColor.rgb * (baseFlare * (1.0 - distCenter));
                col += _CoreColor.rgb * tipGlow;

                // Alpha blending: high opacity in core, soft fade at lateral flanks
                float alpha = saturate(edgeFade * (_OuterColor.a + coreMask * 0.35 + baseFlare * 0.25) * texCol.a);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
