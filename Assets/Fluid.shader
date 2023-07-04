// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// https://github.com/keijiro/StableFluids

Shader "Hidden/StableFluids"
{
    Properties
    {
        _MainTex("", 2D) = ""
        _VelocityField("", 2D) = ""

        _Phase1("", float) = 0
        _Phase2("", float) = 0
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    sampler2D _VelocityField;

    float2 _ForceOrigin;
    float _ForceExponent;

    float2 dim = float2(512,512);
    float _Phase1;
    float _Phase2;

    half4 frag_advect(v2f_img i) : SV_Target
    {
        // Time parameters
        // float time = _Time.y; // Dye is unused
        // float deltaTime = unity_DeltaTime.x; // Deltatime is moved into Phase1 and Pahse2

        // Aspect ratio coefficients
        float2 aspect = float2(_MainTex_TexelSize.y * _MainTex_TexelSize.z, 1);
        float2 aspect_inv = float2(_MainTex_TexelSize.x * _MainTex_TexelSize.w, 1);

        // Color advection with the velocity field
        float2 delta1 = tex2D(_VelocityField, i.uv).xy * aspect_inv * _Phase1;
        float2 delta2 = tex2D(_VelocityField, i.uv).xy * aspect_inv * _Phase2;


        // Blend
        float HalfCycle = 5;
        float flowLerp = ( abs( HalfCycle - _Phase1 ) / HalfCycle );
        float2 offset = float2(0,0);
        offset.x = lerp(delta1.x, delta2.x, flowLerp);
        offset.y = lerp(delta1.y, delta2.y, flowLerp);

        // Sample color at previous position
        float3 color = tex2D(_MainTex, i.uv - offset).xyz;

        // Dye (injection color)
        // float3 dye = saturate(sin(time * float3(2.72, 5.12, 4.98)) + 0.5);

        // // Blend dye with the color from the buffer.
        // float2 pos = (i.uv - 0.5) * aspect;
        // float amp = exp(-_ForceExponent * distance(_ForceOrigin, pos));
        // color = lerp(color, dye, saturate(amp * 100));

        // // Base Texture
        // color = tex2D(_MainTex, i.uv).xyz;

        return half4(color, 1);
    }

    half4 frag_render(v2f_img i) : SV_Target
    {
        half3 rgb = tex2D(_MainTex, i.uv).rgb;

        // Mixing channels up to get slowly changing false colors
        //rgb = sin(float3(3.43, 4.43, 3.84) * rgb +
        //          float3(0.12, 0.23, 0.44) * _Time.y) * 0.5 + 0.5;

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
