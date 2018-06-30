// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// https://github.com/keijiro/StableFluids

Shader "Hidden/StableFluids"
{
    Properties
    {
        _MainTex("", 2D) = ""
        _VelocityField("", 2D) = ""
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    sampler2D _VelocityField;

    half4 frag_advect(v2f_img i) : SV_Target
    {
        float time = _Time.y;
        float deltaTime = unity_DeltaTime.x;

        // Injection color
        float3 inject = saturate(sin(time * float3(2.72, 5.12, 4.98)) + 0.5);

        // Injection point
        float2 p = (i.uv - 0.5) * float2(_MainTex_TexelSize.y * _MainTex_TexelSize.z, 1);
        float2 o = float2(sin(time * 3.13), sin(time * 1.32)) * 0.3;
        float param = saturate(2 / exp(100 * distance(o, p)));

        // Color advection with the velocity field
        float2 vel = tex2D(_VelocityField, i.uv).xy;
        float3 src = tex2D(_MainTex, i.uv - vel * deltaTime).xyz;
        return half4(lerp(src, inject, param), 1);
    }

    half4 frag_render(v2f_img i) : SV_Target
    {
        half3 rgb = tex2D(_MainTex, i.uv).rgb;

        // Mixing channels up to get slowly changing false colors
        rgb = sin(float3(3.43, 4.43, 3.84) * rgb +
                  float3(0.12, 0.23, 0.44) * _Time.y) * 0.5 + 0.5;

        return half4(GammaToLinearSpace(rgb), 1);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_advect
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_render
            ENDCG
        }
    }
}
