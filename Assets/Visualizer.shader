Shader "Hidden/StableFluids/Visualizer"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _VelocityField;
            sampler2D _PressureField;

            half4 frag(v2f_img i) : SV_Target
            {
                half2 col = tex2D(_VelocityField, i.uv).xy + 0.5;
                return half4(LinearToGammaSpace(half3(col, 0.5)), 1);
                //half col = tex2D(_PressureField, i.uv).x * 1000 + 0.5;
                //return half4(LinearToGammaSpace(col), 1);
            }

            ENDCG
        }
    }
}
