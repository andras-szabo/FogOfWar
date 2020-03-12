using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapFrustumView : MaskableGraphic
{
	private Mesh mesh;
	private Camera mainCam;
	private Vector3[] vertices;

	protected override void Start()
	{
		mesh = CreateDefaultMesh();
		mainCam = Camera.main;
		canvasRenderer.SetMaterial(material, null);
	}

	private Mesh CreateDefaultMesh()
	{
		vertices = new Vector3[]
		{
			new Vector3(-0.5f, -0.5f, 1f),
			new Vector3(-0.5f, 0.5f, 1f),
			new Vector3(0.5f, 0.5f, 1f),
			new Vector3(0.5f, -0.5f, 1f)
		};

		var triangles = new int[]
		{
			0, 1, 2,
			2, 3, 0
		};

		Mesh defaultMesh = new Mesh();

		defaultMesh.vertices = vertices;
		defaultMesh.triangles = triangles;

		return defaultMesh;
	}

	private void Update()
	{
		//Graphics.DrawMesh(mesh, transform.position, transform.rotation, material, 0);

		//Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0);
		//Graphics.DrawMesh(mesh, mainCam.transform.position + mainCam.transform.forward,
		//Quaternion.LookRotation(mainCam.transform.forward), material, 0);
		canvasRenderer.SetMesh(mesh);
	}

	public void Setup(Vector3[] camFrustumCornerWorldSpaceVectors, Vector3 camPos, Vector3 terrainOffset, Vector3 terrainSize)
	{
		// Assumption: frustum corner vectors are sent in some logical order, such
		// that we can connect each to the next one, with wraparound, to get the
		// final image.

		float camHeight = camPos.y - terrainOffset.y;
		Vector3 down = new Vector3(0f, -1f, 0f);

		Vector3[] frustumViewWorldPositions = new Vector3[4];

		var mapSize = VM20.DiscoveryMap.mapSize;

		for (int i = 0; i < 4; ++i)
		{
			Vector3 cornerVector = camFrustumCornerWorldSpaceVectors[i].normalized;

			float cosAngleToVertical = Mathf.Max(0.01f, Vector3.Dot(cornerVector, down));
			float frustumCornerToGroundDistance = camHeight / cosAngleToVertical;

			Vector3 groundHitPosition = camPos + (frustumCornerToGroundDistance * cornerVector) - terrainOffset;
			frustumViewWorldPositions[i] = groundHitPosition;
		}

		RectTransform rt = GetComponent<RectTransform>();
		float xFactor = rt.rect.width;
		float yFactor = rt.rect.height;

		for (int i = 0; i < 4; ++i)
		{
			var w = frustumViewWorldPositions[i];
			float x = w.x / mapSize.x - 0.5f;
			float y = w.z / mapSize.y - 0.5f;
			vertices[i] = new Vector3(x * xFactor, y * yFactor);
		}

		mesh.vertices = vertices;
	}
}
