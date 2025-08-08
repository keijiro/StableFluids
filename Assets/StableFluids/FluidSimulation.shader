Shader "Hidden/StableFluids/FluidSimulation"
{
    Properties
    {
        _MainTex ("", 2D) = ""
        _W ("", 2D) = ""
        _P ("", 2D) = ""
        _X ("", 2D) = ""
        _B ("", 2D) = ""
    }

    CGINCLUDE

#include "UnityCG.cginc"

// Uniforms

Texture2D _MainTex;
SamplerState sampler_MainTex;

Texture2D _W;
Texture2D _P;
Texture2D _X;
Texture2D _B;

int _TexWidth, _TexHeight;
float _DeltaTime;
float _Alpha, _Beta;
float2 _ForceOrigin, _ForceVector;
float _ForceExponent;

// Common helpers

int2 PixelCoord(float2 uv)
{
    return int2(uv * float2(_TexWidth, _TexHeight));
}

int2 ClampInner(int2 p)
{
    return clamp(p, int2(1, 1), int2(_TexWidth - 2, _TexHeight - 2));
}

// Pass 0: Velocity Advection
half4 frag_advect(v2f_img i) : SV_Target
{
    float2 vel = _MainTex[PixelCoord(i.uv)].xy;
    vel.y *= (float)_TexWidth / _TexHeight;

    float2 uv_prev = i.uv - vel * _DeltaTime;
    float2 adv = _MainTex.SampleLevel(sampler_MainTex, uv_prev, 0).xy;

    return half4(adv, 0, 1);
}

// Pass 1: Add Force
half4 frag_force(v2f_img i) : SV_Target
{
    float2 pos = i.uv - 0.5;
    pos.y *= (float)_TexHeight / _TexWidth;

    float amp = exp(-_ForceExponent * distance(_ForceOrigin, pos));

    float2 v = _MainTex[PixelCoord(i.uv)].xy + _ForceVector * amp;

    return half4(v, 0, 1);
}

// Pass 2: Projection setup
half4 frag_psetup(v2f_img i) : SV_Target
{
    int2 ip = PixelCoord(i.uv);

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
half4 frag_jacobi1(v2f_img i) : SV_Target
{
    int2 ip = PixelCoord(i.uv);

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
half4 frag_jacobi2(v2f_img i) : SV_Target
{
    int2 ip = PixelCoord(i.uv);

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
half4 frag_pfinish(v2f_img i) : SV_Target
{
    int2 ip = PixelCoord(i.uv);
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

    if (left || right || bottom || top) u = -u;

    return half4(u, 0, 1);
}

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Advect
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_advect
            ENDCG
        }
        // 1: Force
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_force
            ENDCG
        }
        // 2: PSetup
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_psetup
            ENDCG
        }
        // 3: Jacobi1
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_jacobi1
            ENDCG
        }
        // 4: Jacobi2
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_jacobi2
            ENDCG
        }
        // 5: PFinish
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag_pfinish
            ENDCG
        }
    }
}
