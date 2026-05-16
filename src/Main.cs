using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace WideAngleCamera;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class WideAnglePlugin : BaseUnityPlugin
{
	internal static WideAnglePlugin Instance { get; private set; }
	internal static AssetBundle Bundle { get; private set; }
	private Harmony Patcher;

	internal const int Resolution = 512;

	internal ConfigEntry<float> FieldOfView;
	internal ConfigEntry<bool> RenderBackface;

	private void Awake() {
		Instance = this;

		// Prevent the plugin from being destroyed
		transform.parent = null;
		gameObject.hideFlags = HideFlags.HideAndDontSave;

		FieldOfView = Config.Bind(
			"",
			"Field of View",
			145f,
			new ConfigDescription("The angle of visibility of the major axis of your display in degrees, generally this will be horizontal FOV.", new AcceptableValueRange<float>(60f, 270f))
		);
		RenderBackface = Config.Bind(
			"",
			"Render Backface",
			true,
			"Whether or not to render the back view, affects performance and maximum possible FOV"
		);

		try {
			string bundleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Bundle = AssetBundle.LoadFromFile($"{bundleDir}\\WideAngleAssets");

			Hooks.Hook(Logger);
			Patcher = new Harmony(MyPluginInfo.PLUGIN_GUID);
			Patcher.PatchAll(typeof(Patches));
			Logger.LogInfo("Harmony instance created; patched `RenderTextureMain::Start` and `SpectateCamera::Awake`");
			SceneManager.sceneLoaded += OnSceneLoad;
			this.Config.SettingChanged += OnSettingChanged;

			Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
		} catch (Exception err) {
			Logger.LogError($"{MyPluginInfo.PLUGIN_GUID} failed to load: {err}");
		}
	}

	private void OnDestroy() {
		this.Config.SettingChanged -= OnSettingChanged;
		SceneManager.sceneLoaded -= OnSceneLoad;
		Patcher.UnpatchSelf();
		Hooks.Unhook();
	}

	// What the fuck, all roads lead to the scenemanager
	private void OnSceneLoad(Scene scene, LoadSceneMode mode) {
		if (scene.name == "Reload" || SemiFunc.IsMainMenu() || SemiFunc.RunIsLobbyMenu()) return;
		// Camera setup
		Transform camParent = Camera.main.transform;
		var front = Utility.CopyCamera(camParent.gameObject, Utility.Orientation.Front);
		var back = Utility.CopyCamera(front.gameObject, Utility.Orientation.Back);
		var left = Utility.CopyCamera(front.gameObject, Utility.Orientation.Left);
		var right = Utility.CopyCamera(front.gameObject, Utility.Orientation.Right);
		var down = Utility.CopyCamera(front.gameObject, Utility.Orientation.Down);
		var up = Utility.CopyCamera(front.gameObject, Utility.Orientation.Up);
		// Create and setup fullscreen triangle
		GameObject screen = Utility.Projector();
		screen.GetComponent<MeshRenderer>().material = new Material(Bundle.LoadAsset<Shader>("Assets/Shaders/Stereographic.shader"));
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
		cam.AddComponent<CameraManager>().RenderBackface = RenderBackface.Value;

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
	}

	private void OnSettingChanged(object sender, SettingChangedEventArgs arg) {
		if (SemiFunc.IsMainMenu() || SemiFunc.RunIsLobbyMenu() || !(bool)CameraManager.Instance)
			return;

		// Work remains to be done
		if (arg.ChangedSetting == RenderBackface)
			CameraManager.Instance.RenderBackface = RenderBackface.Value;
	}
}
