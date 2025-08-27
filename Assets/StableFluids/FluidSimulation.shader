Shader "Hidden/StableFluids/FluidSimulation"
{
    Properties
    {
        _MainTex ("", 2D) = ""
        _W ("", 2D) = ""
        _P ("", 2D) = ""
        _X ("", 2D) = ""
        _B ("", 2D) = ""
        _ForceField ("", 2D) = ""
    }

HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// Unity built-in time uniform
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

// Uniforms
Texture2D _MainTex;
SamplerState sampler_MainTex;

Texture2D _W;
Texture2D _P;
Texture2D _X;
Texture2D _B;
Texture2D _ForceField;

int _TexWidth, _TexHeight;
float _Alpha, _Beta;

// Common helpers
int2 PixelCoord(float2 uv)
{
    return int2(uv * float2(_TexWidth, _TexHeight));
}

int2 ClampInner(int2 p)
{
    return clamp(p, int2(1, 1), int2(_TexWidth - 2, _TexHeight - 2));
}

// Procedural vertex shader
void VertexProcedural(uint vertexID : SV_VertexID,
                      out float4 positionCS : SV_POSITION,
                      out float2 uv : TEXCOORD0)
{
    positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    uv = GetFullScreenTriangleTexCoord(vertexID);
}

// Pass 0: Apply Force Field
half4 FragmentApplyForce(float4 positionCS : SV_POSITION,
                         float2 uv : TEXCOORD0) : SV_Target
{
    float2 velocity = _MainTex[PixelCoord(uv)].xy;
    float2 force = _ForceField[PixelCoord(uv)].xy;
    return half4(velocity + force, 0, 1);
}

// Pass 1: Velocity Advection
half4 FragmentAdvect(float4 positionCS : SV_POSITION,
                     float2 uv : TEXCOORD0) : SV_Target
{
    float2 vel = _MainTex[PixelCoord(uv)].xy;
    vel.y *= (float)_TexWidth / _TexHeight;

    float2 uv_prev = uv - vel * unity_DeltaTime.x;
    float2 adv = _MainTex.SampleLevel(sampler_MainTex, uv_prev, 0).xy;

    return half4(adv, 0, 1);
}

// Pass 2: Projection setup
half4 FragmentPSetup(float4 positionCS : SV_POSITION,
                     float2 uv : TEXCOORD0) : SV_Target
{
    int2 ip = PixelCoord(uv);

    int2 pL = ClampInner(ip - int2(1, 0));
    int2 pR = ClampInner(ip + int2(1, 0));
    int2 pD = ClampInner(ip - int2(0, 1));
    int2 pU = ClampInner(ip + int2(0, 1));

    float2 vL = _MainTex[pL].xy;
    float2 vR = _MainTex[pR].xy;
    float2 vD = _MainTex[pD].xy;
    float2 vU = _MainTex[pU].xy;

    float div = ((vR.x - vL.x) + (vU.y - vD.y)) * _TexWidth * 0.5;

    return half4(div, 0, 0, 1);
}

// Pass 3: Jacobi (scalar)
half4 FragmentJacobi1(float4 positionCS : SV_POSITION,
                      float2 uv : TEXCOORD0) : SV_Target
{
    int2 ip = PixelCoord(uv);

    int2 pL = ClampInner(ip - int2(1, 0));
    int2 pR = ClampInner(ip + int2(1, 0));
    int2 pD = ClampInner(ip - int2(0, 1));
    int2 pU = ClampInner(ip + int2(0, 1));

    float xl = _X[pL].x;
    float xr = _X[pR].x;
    float xd = _X[pD].x;
    float xu = _X[pU].x;

    float b = _B[ip].x;

    float x = (xl + xr + xd + xu + _Alpha * b) / _Beta;

    return half4(x, 0, 0, 1);
}

// Pass 4: Jacobi (vector)
half4 FragmentJacobi2(float4 positionCS : SV_POSITION,
                      float2 uv : TEXCOORD0) : SV_Target
{
    int2 ip = PixelCoord(uv);

    int2 pL = ClampInner(ip - int2(1, 0));
    int2 pR = ClampInner(ip + int2(1, 0));
    int2 pD = ClampInner(ip - int2(0, 1));
    int2 pU = ClampInner(ip + int2(0, 1));

    float2 xl = _X[pL].xy;
    float2 xr = _X[pR].xy;
    float2 xd = _X[pD].xy;
    float2 xu = _X[pU].xy;

    float2 b = _B[ip].xy;

    float2 x = (xl + xr + xd + xu + _Alpha * b) / _Beta;

    return half4(x, 0, 1);
}

// Pass 5: Projection finish
half4 FragmentPFinish(float4 positionCS : SV_POSITION,
                      float2 uv : TEXCOORD0) : SV_Target
{
    int2 ip = PixelCoord(uv);
    int W = _TexWidth, H = _TexHeight;

    int2 pL = ClampInner(ip - int2(1, 0));
    int2 pR = ClampInner(ip + int2(1, 0));
    int2 pD = ClampInner(ip - int2(0, 1));
    int2 pU = ClampInner(ip + int2(0, 1));

    float p1 = _P[pL].x;
    float p2 = _P[pR].x;
    float p3 = _P[pD].x;
    float p4 = _P[pU].x;

    float2 w = _W[ip].xy;

    float2 u = w - float2(p2 - p1, p4 - p3) * (float)W * 0.5;

    bool left   = ip.x == 0;
    bool right  = ip.x == (W - 1);
    bool bottom = ip.y == 0;
    bool top    = ip.y == (H - 1);

    if (left || right) u.x = -u.x;
    if (bottom || top) u.y = -u.y;

    return half4(u, 0, 1);
}

ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: ApplyForce
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentApplyForce
            ENDHLSL
        }
        // 1: Advect
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentAdvect
            ENDHLSL
        }
        // 2: PSetup
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentPSetup
            ENDHLSL
        }
        // 3: Jacobi1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentJacobi1
            ENDHLSL
        }
        // 4: Jacobi2
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentJacobi2
            ENDHLSL
        }
        // 5: PFinish
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexProcedural
            #pragma fragment FragmentPFinish
            ENDHLSL
        }
    }
}