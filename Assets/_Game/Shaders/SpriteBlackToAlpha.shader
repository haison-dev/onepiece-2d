Shader "OnPiece/SpriteBlackToAlpha"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlackThreshold ("Black Threshold", Range(0, 0.2)) = 0.01
        _Opacity ("Opacity", Range(0, 1)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BlackToAlphaFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            float _BlackThreshold;
            float _Opacity;

            fixed4 BlackToAlphaFrag(v2f IN) : SV_Target
            {
                fixed4 sampleColor = SampleSpriteTexture(IN.texcoord);
                float brightness = max(
                    sampleColor.r,
                    max(sampleColor.g, sampleColor.b)
                );
                float alpha = saturate(
                    (brightness - _BlackThreshold) /
                    max(1.0 - _BlackThreshold, 0.0001)
                );

                clip(alpha - 0.001);

                fixed3 color = sampleColor.rgb / max(alpha, 0.001);
                color *= IN.color.rgb;
                alpha *= sampleColor.a * IN.color.a * _Opacity;

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
