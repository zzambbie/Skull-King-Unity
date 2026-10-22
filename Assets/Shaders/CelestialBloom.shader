Shader "CROSSION/UI/Celestial Bloom"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float4 local:TEXCOORD0; };
            fixed4 _Color;
            float4 _ClipRect;
            v2f vert(appdata input)
            {
                v2f o;o.local=input.vertex;o.vertex=UnityObjectToClipPos(input.vertex);
                o.color=input.color*_Color;return o;
            }
            fixed4 frag(v2f input):SV_Target
            {
                fixed4 c=input.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(input.local.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
