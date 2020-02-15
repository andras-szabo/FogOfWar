Shader "Unlit/FogOfWarStencilMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off ZWrite Off ZTest Always

		Stencil
		{
			Ref 1
			Comp NotEqual
			Pass Keep
		}
	
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 col = tex2D(_MainTex, i.uv) * fixed4(1.0, 1.0, 1.0, 0.5f);
				fixed4 col = fixed4(0.2, 0.2, 0.2, 0.75);
                return col;
            }
            ENDCG
        }
    }
}
