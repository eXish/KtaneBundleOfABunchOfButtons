Shader "KT/Custom/Masks/Mask 172" {
    Properties {
    }

    SubShader {
        Tags { "RenderType"="Transparent" }
        ColorMask 0
        Stencil {
            ref 172
            Comp Always
            Pass replace
        }
        LOD 150

        ZWrite Off

        CGPROGRAM
        // Mobile improvement: noforwardadd
        // http://answers.unity3d.com/questions/1200437/how-to-make-a-conditional-pragma-surface-noforward.html
        // http://gamedev.stackexchange.com/questions/123669/unity-surface-shader-conditinally-noforwardadd
        #pragma surface surf Lambert

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = fixed4(1,1,1,1);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
}