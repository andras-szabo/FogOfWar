Shader "Custom/VisibilityMask"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_CurrentVisibility("Current visibility", 2D) = "white" {}

		_CamBottomLeft("CamBottomLeft", Vector) = (0, 0, 0)
		_CamTopLeft("CamTopLeft", Vector) = (0, 1, 0)
		_CamTopRight("CamTopRight", Vector) = (1, 1, 0)
		_CamBottomRight("CamBottomRight", Vector) = (1, 0, 0)

		_TerrainSize("Terrain size", Vector) = (1000, 1000, 0)
	}

		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

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
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float2 uv_depth : TEXCOORD1;
				};

				v2f vert(appdata v)
				{
					v2f o;

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.uv_depth = v.uv.xy;

					return o;
				}

				sampler2D _MainTex;
				sampler2D _Mask;
				sampler2D _CurrentVisibility;
				sampler2D_float _CameraDepthTexture;

				float3 _CamBottomLeft;
				float3 _CamTopLeft;
				float3 _CamTopRight;
				float3 _CamBottomRight;

				float3 _TerrainSize;
		
				float3 ViewportToRay(float2 uv)
				{
					float3 horizontalBottom = lerp(_CamBottomLeft, _CamBottomRight, uv.x);
					float3 horizontalTop = lerp(_CamTopLeft, _CamTopRight, uv.x);
					float3 direction = lerp(horizontalBottom, horizontalTop, uv.y);

					return direction;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					// _ProjectionParams.y is the cam's near plane
					// _ProjectionParams.z is the far plane
					float3 viewportToWorldRayDirection = ViewportToRay(i.uv) / _ProjectionParams.y;

					float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv_depth));
					float linearDepth = Linear01Depth(rawDepth);
					float rayLength = linearDepth * _ProjectionParams.z;	

					float3 worldSpaceVisibilityMapPos = _WorldSpaceCameraPos.xyz + viewportToWorldRayDirection * rayLength;
					float2 customUV = float2(worldSpaceVisibilityMapPos.x / _TerrainSize.x,
											 worldSpaceVisibilityMapPos.z / _TerrainSize.z);

					if (customUV.x < 0.0 || customUV.x > 1.0 || customUV.y < 0.0 || customUV.y > 1.0 || linearDepth >= 1.0)
					{
						return tex2D(_MainTex, i.uv);
					}

					fixed4 discoveryFactor = tex2D(_Mask, customUV);
					fixed4 currentFactor = tex2D(_CurrentVisibility, customUV);
					fixed4 originalPixelColor = tex2D(_MainTex, i.uv);

					fixed4 colour = lerp(fixed4(0, 0, 0, 1), originalPixelColor, discoveryFactor.a);
					fixed4 multiplier = lerp(fixed4(0.5, 0.5, 0.5, 1), fixed4(1, 1, 1, 1), currentFactor.a);

					return colour * multiplier;
				}

				ENDCG
			}
		}
}
