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
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 worldSpacePosition : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
            };

			sampler2D _CurrentVisibilityMap;
			sampler2D_float _CameraDepthTexture;
			float3 _TerrainSize;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				o.uv = float2(o.worldSpacePosition.x / _TerrainSize.x,
							  o.worldSpacePosition.z / _TerrainSize.z);
				o.screenPos = ComputeScreenPos(o.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float3 fromCameraToFragment = i.worldSpacePosition - _WorldSpaceCameraPos;
				fromCameraToFragment = normalize(fromCameraToFragment);
				float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.screenPos.xy/i.screenPos.w));
				float linearDepth = Linear01Depth(rawDepth);
				float rayLength = linearDepth * _ProjectionParams.z;

				float3 worldSpaceVisibilityMapPos = _WorldSpaceCameraPos.xyz + fromCameraToFragment * rayLength;
				float2 customUV = float2(worldSpaceVisibilityMapPos.x / _TerrainSize.x,
										 worldSpaceVisibilityMapPos.z / _TerrainSize.z);

				fixed4 col = tex2D(_CurrentVisibilityMap, customUV);

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
