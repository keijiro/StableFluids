Shader "Hidden/StableFluids/FluidAdvection"
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

    half4 frag(v2f_img i) : SV_Target
    {
        // Velocity field sample
        float2 delta = tex2D(_VelocityField, i.uv).xy;

        // Aspect ratio compensation
        delta.x *= _MainTex_TexelSize.x * _MainTex_TexelSize.w;

        return tex2D(_MainTex, i.uv - delta * unity_DeltaTime.x);
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
