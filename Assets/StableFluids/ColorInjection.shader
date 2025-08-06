Shader "Hidden/StableFluids/ColorInjection"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
        _Color("", Color) = (1, 1, 1, 1)
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    float4 _Color;
    float2 _Origin;
    float _Exponent;

    half4 frag(v2f_img i) : SV_Target
    {
        // Source sample
        float4 color = tex2D(_MainTex, i.uv);

        // Injection point
        float2 pos = i.uv - 0.5;
        pos.x *= _MainTex_TexelSize.y * _MainTex_TexelSize.z;

        // Injection intensity based on the distance from the origin
        float alpha = 100 * exp(-_Exponent * distance(_Origin, pos));

        return lerp(color, _Color, saturate(alpha));
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}