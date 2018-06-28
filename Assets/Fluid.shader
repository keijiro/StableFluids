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

        float2 p = (i.uv - 0.5) * float2(_MainTex_TexelSize.y * _MainTex_TexelSize.z, 1);
        float2 o = float2(sin(time * 3.13), sin(time * 1.32)) * 0.3;

        float d = distance(o, p);
        float3 c = saturate(sin(time * float3(2.72, 5.12, 4.98)) + 0.5);
        float param = saturate(2 / exp(100 * d));

        float2 vel = tex2D(_VelocityField, i.uv).xy;
        float3 src = tex2D(_MainTex, i.uv - vel * deltaTime).xyz;
        return half4(lerp(src, c, param), 1);
    }

    half4 frag_render(v2f_img i) : SV_Target
    {
        half3 rgb = tex2D(_MainTex, i.uv).rgb;
        rgb = sin(rgb * float3(3.43, 4.43, 3.84) + _Time.y * float3(1.32, 1.23, 0.94)) * 0.5 + 0.5;
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
