Shader "Hidden/StableFluids/Marbling"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
        _Color("", Color) = (1, 1, 1, 1)
        _VelocityField("", 2D) = ""
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    sampler2D _VelocityField;
    float4 _Color;
    float2 _Origin;
    float _Falloff;

    // Pass 0: Color Injection
    half4 frag_injection(v2f_img i) : SV_Target
    {
        // Source sample
        float4 color = tex2D(_MainTex, i.uv);

        // Injection point
        float2 pos = i.uv - 0.5;
        pos.y *= _MainTex_TexelSize.x * _MainTex_TexelSize.w;

        // Injection intensity based on the distance from the origin
        float alpha = 100 * exp(-_Falloff * distance(_Origin, pos));

        return lerp(color, _Color, saturate(alpha));
    }

    // Pass 1: Fluid Advection
    half4 frag_advection(v2f_img i) : SV_Target
    {
        // Velocity field sample
        float2 delta = tex2D(_VelocityField, i.uv).xy;

        // Aspect ratio compensation (width-based normalization)
        delta.y *= _MainTex_TexelSize.y * _MainTex_TexelSize.z;

        return tex2D(_MainTex, i.uv - delta * unity_DeltaTime.x);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        // Pass 0: Color Injection
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_injection
            ENDCG
        }
        
        // Pass 1: Fluid Advection
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_advection
            ENDCG
        }
    }
}