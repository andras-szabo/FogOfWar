Shader "Unlit/StencilMarker"
{
    Properties
    {
		_CurrentVisibilityMap("CurrentVisibility", 2D) = "white" {}
		_TerrainSize("Terrain size", Vector) = (1000, 100, 1000)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend Zero One
		ZWrite Off

		Stencil
		{
			Ref 1
			Comp notequal
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
			float3 _TerrainSize;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
				float4 worldSpaceVertex = mul(unity_ObjectToWorld, v.vertex);
				o.uv = float2(worldSpaceVertex.x / _TerrainSize.x,
							  worldSpaceVertex.z / _TerrainSize.z);

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
