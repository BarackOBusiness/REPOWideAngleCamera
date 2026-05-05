using UnityEngine;

namespace WideAngleCamera;

public class CameraManager : MonoBehaviour {
	public static CameraManager Instance;

	public Camera orig;

	public Camera front;
	public Camera back;
	public Camera right;
	public Camera left;
	public Camera up;
	public Camera down;

	private RenderTexture cubemap;

	private void Awake() {
		orig = Camera.main;
		front = transform.Find("Front").GetComponent<Camera>();
		back = transform.Find("Back").GetComponent<Camera>();
		left = transform.Find("Left").GetComponent<Camera>();
		right = transform.Find("Right").GetComponent<Camera>();
		down = transform.Find("Down").GetComponent<Camera>();
		up = transform.Find("Up").GetComponent<Camera>();
	
		back.gameObject.SetActive(true);

		cubemap = new RenderTexture(512, 512, 16);
		cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;

		SetupCams();

		var screen = transform.parent.Find("Projector Screen").GetComponent<MeshRenderer>();
		screen.material.mainTexture = cubemap;
		screen.material.SetFloat("_FOV", WideAnglePlugin.Instance.FieldOfView.Value);
	}

	private void Update() {
		Graphics.CopyTexture(front.targetTexture, 0, cubemap, 4);
		Graphics.CopyTexture(back.targetTexture, 0, cubemap, 5);
		Graphics.CopyTexture(right.targetTexture, 0, cubemap, 0);
		Graphics.CopyTexture(left.targetTexture, 0, cubemap, 1);
		Graphics.CopyTexture(up.targetTexture, 0, cubemap, 3);
		Graphics.CopyTexture(down.targetTexture, 0, cubemap, 2);
	}

	public void SetupCams() {
		foreach (Camera cam in new Camera[]{front, back, right, left, up, down}) {
			SetupCam(cam, 512);
		}
	}

	private void SetupCam(Camera cam, int size) {
		RenderTexture rt = new RenderTexture(size, size, 16);
		cam.targetTexture = rt;
		cam.depth = orig.depth;
		cam.clearFlags = orig.clearFlags;
		cam.cullingMask = orig.cullingMask;
		cam.depthTextureMode = orig.depthTextureMode;
	}
}
