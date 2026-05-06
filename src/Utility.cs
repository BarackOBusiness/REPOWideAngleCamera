using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
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
				clone.localRotation = Quaternion.Euler(270f, 0f, 0f);
				break;
			case Orientation.Down:
				clone.localRotation = Quaternion.Euler(90f, 0f, 0f);
				break;
		}

		RenderTexture rt = new RenderTexture(512, 512, 16);
		clone.GetComponent<Camera>().targetTexture = rt;
		clone.GetComponent<Camera>().fieldOfView = 90.0f;

		return clone;
	}

	public static GameObject Projector() {
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

        // Now create the projector which will be returned
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

	public static float ExpDecay(float a, float b, float decay, float dt) {
		return b+(a-b)*Mathf.Exp(-decay*dt);
	}
}

internal static class Hooks {
	private const BindingFlags Public = BindingFlags.Public;
	private const BindingFlags Private = BindingFlags.NonPublic;
	private const BindingFlags Instance = BindingFlags.Instance;

	internal static ILHook _RunManagerHook;
	internal static ILHook _RenderTextureMainHook;
	internal static ILHook _EnvironmentDirectorHook;

	internal static void Hook() {
		_RunManagerHook = new ILHook(GetMethod<RunManager>("ChangeLevel", Public | Instance), RunManager_ChangeLevel);
		_RenderTextureMainHook = new ILHook(GetMethod<RenderTextureMain>("Start", Private | Instance), RenderTextureMain_Start);
		_EnvironmentDirectorHook = new ILHook(GetMethod<EnvironmentDirector>("Setup", Public | Instance), EnvironmentDirector_Setup);
	}

	internal static void Unhook() {
		_RunManagerHook.Dispose();
		_RenderTextureMainHook.Dispose();
		_EnvironmentDirectorHook.Dispose();
	}

	private static MethodInfo GetMethod<T>(string name, BindingFlags flags) {
		return typeof(T).GetMethod(name, flags);
	}

	// Trigger an event whenever a level is loading
	private static void RunManager_ChangeLevel(ILContext il) {
		ILCursor cursor = new ILCursor(il).Goto(0);
				
		if (cursor.TryGotoNext(moveType: MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchCall<RunManager>("RestartScene")
		)) {
			cursor.EmitDelegate(() => {
				WideAnglePlugin.levelLoaded.Invoke();
			});
		}
	}

	// Update camera list construction to account for all the new ones that shouldn't be appended
	private static void RenderTextureMain_Start(ILContext il) {
		ILCursor cursor = new ILCursor(il).Goto(0);

		if (cursor.TryGotoNext(moveType: MoveType.After,
			x => x.MatchBr(out _),
			x => x.MatchLdloc(0),
			x => x.MatchLdloc(1),
			x => x.MatchLdelemRef(),
			x => x.MatchStloc(2)
		)) {
			cursor.RemoveRange(4);
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_2);
			cursor.EmitDelegate((RenderTextureMain self, Camera cam) => {
				if (cam.transform.parent != null && (cam.transform.parent.name == "Camera Main" || cam.transform.parent.name == "Tilt")) {
					self.cameras.Add(cam);
				}
			});
		}
	}

	// EnvironmentDirector sets far clip distance of subcameras instead of main one
	private static void EnvironmentDirector_Setup(ILContext il) {
		ILCursor cursor = new ILCursor(il).Goto(0);

		if (cursor.TryGotoNext(
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<EnvironmentDirector>("MainCamera"),
			x => x.MatchCall<UnityEngine.RenderSettings>("get_fogEndDistance"),
			x => x.MatchLdcR4(1),
			x => x.MatchAdd(),
			x => x.MatchCallvirt<Camera>("set_farClipPlane")
		)) {
			cursor.Index++;
			cursor.RemoveRange(5);
			cursor.EmitDelegate((EnvironmentDirector self) => {
				if (CameraManager.Instance != null) {
					CameraManager.Instance.FarClipPlane = RenderSettings.fogEndDistance + 1f;
				} else {
					self.MainCamera.farClipPlane = RenderSettings.fogEndDistance + 1f;
				}
			});
		}
	}
}
