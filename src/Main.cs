using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace WideAngleCamera;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class WideAnglePlugin : BaseUnityPlugin
{
	internal static WideAnglePlugin Instance { get; private set; } = null!;
	internal static AssetBundle Bundle { get; private set; } = null!;

	internal static UnityEvent levelLoaded;

	internal ConfigEntry<float> FieldOfView;
	internal ConfigEntry<bool> DebugActive;

	private bool shouldSetup = false;

	private void Awake() {
		Instance = this;

		// Prevent the plugin from being destroyed
		this.transform.parent = null;
		this.gameObject.hideFlags = HideFlags.HideAndDontSave;

		FieldOfView = Config.Bind(
			"",
			"Field of View",
			145f,
			new ConfigDescription("The angle of visibility of the major axis of your display in degrees, generally this will be horizontal FOV.", new AcceptableValueRange<float>(60f, 300f))
		);
		DebugActive = Config.Bind(
			"Debug",
			"Projection Enabled",
			true,
			"Whether or not to enable the camera projection"
		);

		try {
			string bundleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Bundle = AssetBundle.LoadFromFile($"{bundleDir}\\WideAngleAssets");

			// Event gets invoked via code injection into RunManager::ChangeLevel
			// it tells us when we're entering gameplay and should modify the camera
			levelLoaded = new UnityEvent();
			levelLoaded.AddListener(() => {
				shouldSetup = true;
			});

			// The scene will reload following our level change event, this is the modification time
			SceneManager.sceneLoaded += OnSceneLoad;

			Hooks.Hook();

			Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
		} catch (Exception err) {
			Logger.LogError($"{MyPluginInfo.PLUGIN_GUID} failed to load: {err}");
		}
	}

	private void OnDestroy() {
		Hooks.Unhook();
	}

	// What the fuck, all roads lead to the scenemanager
	private void OnSceneLoad(Scene scene, LoadSceneMode mode) {
		if (!shouldSetup || !DebugActive.Value || scene.name == "Reload" || SemiFunc.RunIsLobbyMenu()) return;
		// Camera setup
		Transform camParent = Camera.main.transform;
		var front = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Front);
		var back = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Back);
		var left = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Left);
		var right = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Right);
		var down = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Down);
		var up = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Up);
		// Create and setup fullscreen triangle
		GameObject screen = Utility.Projector();
		screen.GetComponent<MeshRenderer>().material = new Material(Bundle.LoadAsset<Shader>("Assets/Stereographic/Projection.shader"));
		screen.transform.SetParent(camParent, false);
		screen.transform.localPosition = new Vector3(0f, 0f, 0.5f);
		screen.layer = 31;

		// Instantiate and setup camera system
		var cam = new GameObject("Wide Angle Camera");
		front.transform.SetParent(cam.transform, false);
		back.transform.SetParent(cam.transform, false);
		left.transform.SetParent(cam.transform, false);
		right.transform.SetParent(cam.transform, false);
		down.transform.SetParent(cam.transform, false);
		up.transform.SetParent(cam.transform, false);
		cam.transform.SetParent(camParent, false);
		cam.AddComponent<CameraManager>();

		// Configure main camera to view only triangle
		Camera.main.nearClipPlane = 0.0f;
		Camera.main.farClipPlane = 1.0f;
		Camera.main.cullingMask = 1 << 31;
		Camera.main.orthographic = true;
		Camera.main.orthographicSize = 1f;
		Camera.main.useOcclusionCulling = false;
		Camera.main.clearFlags = CameraClearFlags.Nothing;
		// Fix up extras caused by the great instantiation of 2026
		Destroy(Camera.main.GetComponent<PostProcessLayer>());
		CameraGlitch.Instance = Camera.main.transform.Find("Glitch").GetComponent<CameraGlitch>();

		// Set this flag back in case we go to the main menu
		shouldSetup = false;
	}
}
