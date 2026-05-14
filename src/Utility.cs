using UnityEngine;

namespace WideAngleCamera;

internal static class Utility {
	internal enum Orientation {
		Front,
		Back,
		Left,
		Right,
		Down,
		Up
	}

	// Generates a copy of the main camera for its post processing layer
	// destroys the children and sets up the render texture and fov
	internal static Transform CopyCamera(GameObject orig, Orientation orient) {
		var clone = GameObject.Instantiate(orig).transform;
		clone.name = orient.ToString();
		foreach (Transform child in clone) {
			GameObject.Destroy(child.gameObject);
		}

		switch (orient) {
			case Orientation.Front:
				clone.localRotation = Quaternion.Euler(0f, 0f, 0f);
				break;
			case Orientation.Back:
				clone.localRotation = Quaternion.Euler(0f, 180f, 0f);
				break;
			case Orientation.Left:
				clone.localRotation = Quaternion.Euler(0f, 270f, 0f);
				break;
			case Orientation.Right:
				clone.localRotation = Quaternion.Euler(0f, 90f, 0f);
				break;
			case Orientation.Up:
				clone.localRotation = Quaternion.Euler(90f, 0f, 0f);
				break;
			case Orientation.Down:
				clone.localRotation = Quaternion.Euler(270f, 0f, 0f);
				break;
		}

		RenderTexture rt = new RenderTexture(512, 512, 16);
		clone.GetComponent<Camera>().targetTexture = rt;
		clone.GetComponent<Camera>().fieldOfView = 90.0f;

		return clone;
	}

	internal static GameObject Projector() {
		Mesh m = new Mesh();

        m.vertices = new Vector3[]
        {
            new Vector3(-1, -1, 0),
            new Vector3( 3, -1, 0),
            new Vector3(-1,  3, 0),
        };

        m.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(2, 0),
            new Vector2(0, 2),
        };

        // Winds from the last vertex to the first because for some reason that faces it towards negative z
        m.triangles = new int[] { 2, 1, 0 };
        m.RecalculateBounds();

        var obj = new GameObject("Projector Screen");
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();

        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        mf.sharedMesh = m;

        return obj;
	}

	public static Vector3 WorldToViewportPoint(Transform cam, Vector3 worldPoint) {
		// Transform to local space, this is the ray to the position
		Vector3 p = cam.InverseTransformPoint(worldPoint);
		float r = p.magnitude;

		// FOV scaling factor
		float s = 1.0f / Mathf.Tan(CameraManager.Instance.FOV * Mathf.Deg2Rad * 0.25f);

		// Map ray onto stereographic image plane
		// using the magnitude in place of 1 in the denominator cancels the need to normalize it apparently
		float u = s * (p.x / (r + p.z));
		float v = s * (p.y / (r + p.z));
		// Aspect ratio correction
		v /= Camera.main.aspect;

		// Figure out whether the position is 'behind' the camera or not
		bool behind = (r + p.z) <= 0f;

		// Viewport coordinates + r as depth analogue
		return new Vector3(
			0.5f + u * 0.5f,
			0.5f + v * 0.5f,
			behind? -r : r
		);
	}

	public static float ExpDecay(float a, float b, float decay, float dt) {
		return b+(a-b)*Mathf.Exp(-decay*dt);
	}
}
