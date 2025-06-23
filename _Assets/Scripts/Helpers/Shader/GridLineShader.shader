Shader "Hidden/GridLineShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // For transparency
            ZWrite Off // Don't write to depth buffer, so it draws over other things
            Cull Off // Don't cull anything

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0,1,0,0.5); // Green with some transparency
            }
            ENDCG
        }
    }
}