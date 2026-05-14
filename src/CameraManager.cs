using UnityEngine;

namespace WideAngleCamera;

public class CameraManager : MonoBehaviour {
	public static CameraManager Instance;

	public enum Projection {
		Stereographic,
		Equisolid
	}

	private Shader stereographic;
	private Shader equisolid;

	private Camera front;
	private Camera back;
	private Camera left;
	private Camera right;
	private Camera down;
	private Camera up;

	private RenderTexture cubemap;
	private Material screen;

	private void Awake() {
		Instance = this;

		front = transform.Find("Front").GetComponent<Camera>();
		back = transform.Find("Back").GetComponent<Camera>();
		left = transform.Find("Left").GetComponent<Camera>();
		right = transform.Find("Right").GetComponent<Camera>();
		down = transform.Find("Down").GetComponent<Camera>();
		up = transform.Find("Up").GetComponent<Camera>();

		cubemap = new RenderTexture(512, 512, 16);
		cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;

		screen = transform.parent.Find("Projector Screen").GetComponent<MeshRenderer>().material;
		screen.mainTexture = cubemap;
		FOV = WideAnglePlugin.Instance.FieldOfView.Value;

		// Cache these for switching
		stereographic = screen.shader;
		equisolid = WideAnglePlugin.Bundle.LoadAsset<Shader>("Assets/Shaders/Equisolid.shader");
	}

	private void Update() {
		Graphics.CopyTexture(front.targetTexture, 0, cubemap, 4);
		if (back.gameObject.activeSelf)
			Graphics.CopyTexture(back.targetTexture, 0, cubemap, 5);
		Graphics.CopyTexture(right.targetTexture, 0, cubemap, 0);
		Graphics.CopyTexture(left.targetTexture, 0, cubemap, 1);
		Graphics.CopyTexture(up.targetTexture, 0, cubemap, 2);
		Graphics.CopyTexture(down.targetTexture, 0, cubemap, 3);
	}

	public bool RenderBackface {
		get;
		internal set {
			back.gameObject.SetActive(value);
			field = value;
		}
	}

	public Projection Mode {
		get;
		internal set {
			switch (value) {
				case Projection.Stereographic:
					screen.shader = stereographic;
					break;
				case Projection.Equisolid:
					screen.shader = equisolid;
					break;
			}
			field = value;
		}
	}

	public float FOV {
		get {
			return screen.GetFloat("_FOV");
		}
		internal set {
			screen.SetFloat("_FOV", value);
		}
	}

	public float FarClipPlane {
		get;
		internal set {
			foreach (var camera in new Camera[]{ front, back, left, right, down, up }) {
				camera.farClipPlane = value;
			}
			field = value;
		}
	}
}
