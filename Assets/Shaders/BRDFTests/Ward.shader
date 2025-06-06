Shader "Terakoya RP/Ward"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) =  (0.5, 0.5, 0.5, 1.0)
        _Metallic("Metallic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _AlphaX("Alpha X", Range(0.0, 1.0)) = 0.5
        _AlphaY("Alpha Y", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DestBlend("Dest Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "CustomLit"
            }

            Blend [_SrcBlend] [_DestBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma multi_compile_instancing

            #pragma shader_feature _CLIPPING

            #pragma vertex WardPassVertex
            #pragma fragment WardPassFragment
            #include "WardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "CustomShaderGUI"
}
