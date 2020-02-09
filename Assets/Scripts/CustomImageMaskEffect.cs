using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomImageMaskEffect : MonoBehaviour
{
	public Material maskMaterial;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, maskMaterial);
	}
}
