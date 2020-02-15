﻿Shader "Unlit/StencilMarker"
{
    Properties
    {
		_CurrentVisibilityMap("CurrentVisibility", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend Zero One
		ZWrite Off

		Stencil
		{
			Ref 1
			Comp always
			Pass replace
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
            };

            struct v2f
            {
				float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			sampler2D _CurrentVisibilityMap;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
				float4 worldSpaceVertex = mul(unity_ObjectToWorld, v.vertex);
				o.uv = float2(worldSpaceVertex.x / 1000.0, 
							  worldSpaceVertex.z / 1000.0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_CurrentVisibilityMap, i.uv);

				if (col.a <= 0.0)
				{
					discard;
				}

				return 0;
            }
            ENDCG
        }
    }
}
