Shader "Hidden/StableFluids/Marbling"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
        _VelocityField("", 2D) = ""{}
    }

HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Unity built-in time uniform
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

TEXTURE2D(_VelocityField);
SAMPLER(sampler_VelocityField);

float4 _Color;
float2 _Origin;
float _Falloff;

void VertexProcedural(uint vertexID : SV_VertexID,
                      out float4 positionCS : SV_POSITION,
                      out float2 uv : TEXCOORD0)
{
    // Generate triangle vertices procedurally
    // Using a large triangle that covers the entire screen
    float2 position = float2((vertexID << 1) & 2, vertexID & 2) * 2.0 - 1.0;
    positionCS = float4(position.x, -position.y, 0, 1);
    uv = position * 0.5 + 0.5;
}

half4 FragmentInjection(float4 positionCS : SV_POSITION,
                        float2 uv : TEXCOORD0) : SV_Target
{
    // Source sample
    float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

    // Injection point
    float2 pos = uv - 0.5;
    pos.y *= _MainTex_TexelSize.x * _MainTex_TexelSize.w;

    // Injection intensity based on the distance from the origin
    float dist = distance(_Origin, pos);
    float alpha = 100 * exp(-_Falloff * dist);

    return lerp(color, _Color, saturate(alpha));
}

half4 FragmentAdvection(float4 positionCS : SV_POSITION,
                        float2 uv : TEXCOORD0) : SV_Target
{
    // Velocity field sample
    float2 velocity = SAMPLE_TEXTURE2D(_VelocityField, sampler_VelocityField, uv).xy;

    // Aspect ratio compensation (width-based normalization)
    velocity.y *= _MainTex_TexelSize.y * _MainTex_TexelSize.z;

    // Sample from advected position
    float2 advectedUV = uv - velocity * unity_DeltaTime.x;
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, advectedUV);
}

ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Color Injection
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentInjection
            ENDHLSL
        }

        // Pass 1: Fluid Advection
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentAdvection
            ENDHLSL
        }
    }
}