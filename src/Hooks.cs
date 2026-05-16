using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace WideAngleCamera;

internal static class Hooks {
	private const BindingFlags Public = BindingFlags.Public;
	private const BindingFlags Private = BindingFlags.NonPublic;
	private const BindingFlags Static = BindingFlags.Static;
	private const BindingFlags Instance = BindingFlags.Instance;

	private static ManualLogSource _logger;
	private static List<ILHook> _hooks;

	internal static void Hook(BepInEx.Logging.ManualLogSource logger) {
		_logger = logger;
		_hooks = new List<ILHook>();
		// Now start patching all the methods
		_logger.LogInfo("Hooking `EnvironmentDirector::FogLogic`");
		_hooks.Add(new ILHook(GetMethod<EnvironmentDirector>("FogLogic", Private | Instance), EnvironmentDirector_FogLogic));
		_logger.LogInfo("Hooking `ValuableDiscoverGraphic::RendererBoundsInScreenSpace`");
		_hooks.Add(new ILHook(GetMethod<ValuableDiscoverGraphic>("RendererBoundsInScreenSpace", Private | Instance), ValuableDiscoverGraphic_RendererBoundsInScreenSpace));
		_logger.LogInfo("Hooking `SemiFunc::OnScreen`");
		_hooks.Add(new ILHook(typeof(SemiFunc).GetMethod("OnScreen", Public | Static), SemiFunc_OnScreen));
		_logger.LogInfo("Hooking `CrystalBallValuable::StateActive`");
		_hooks.Add(new ILHook(GetMethod<CrystalBallValuable>("StateActive", Private | Instance), CrystalBall_StateActive));
		_logger.LogInfo("Hooking `CrystalBallValuable::StateIdle`");
		_hooks.Add(new ILHook(GetMethod<CrystalBallValuable>("StateIdle", Private | Instance), CrystalBall_StateIdle));
	}

	internal static void Unhook() {
		foreach (var hook in _hooks) {
			hook.Dispose();
		}
	}

	private static MethodInfo GetMethod<T>(string name, BindingFlags flags) {
		return typeof(T).GetMethod(name, flags);
	}

	// EnvironmentDirector sets far clip distance of subcameras instead of main one
	private static void EnvironmentDirector_FogLogic(ILContext il) {
		ILCursor cursor = new ILCursor(il).Goto(0);

		// Separate these fucking things and make sure wide angle cam exists before trying to set it
		if (cursor.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<EnvironmentDirector>("FogStartDistance"),
			x => x.MatchCall<RenderSettings>("set_fogStartDistance")
		)) {
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit<EnvironmentDirector>(OpCodes.Ldfld, "FogEndDistance");
			cursor.EmitDelegate((float FogEndDistance) => {
				if (RenderSettings.fogEndDistance != FogEndDistance)
					RenderSettings.fogEndDistance = FogEndDistance;
			});
		}

		// Add assignment of farClipPlane to wide angle cam's far plane
		// on environment setup
		if (cursor.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<EnvironmentDirector>("MainCamera"),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<EnvironmentDirector>("FogEndDistance"),
			x => x.MatchLdcR4(1),
			x => x.MatchAdd(),
			x => x.MatchCallvirt<Camera>("set_farClipPlane")
		)) {
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit<EnvironmentDirector>(OpCodes.Ldfld, "FogEndDistance");
			cursor.EmitDelegate((float FogEndDistance) => {
				if (CameraManager.Instance != null) {
					CameraManager.Instance.FarClipPlane = FogEndDistance + 1f;
				}
			});
		}
	}

	// Replace assignments to screenSpaceCorners[0-7] with stereographic world-to-viewport-space
	private static void ValuableDiscoverGraphic_RendererBoundsInScreenSpace(ILContext il) {
		ILCursor cursor = new ILCursor(il);

		for (int i = 0; i < 8; i++) {
			// Matches loading a screenSpaceCorners element onto the stack
			if (cursor.TryGotoNext(MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<ValuableDiscoverGraphic>("screenSpaceCorners"),
				x => x.MatchLdcI4(i)
			)) {
				cursor.RemoveRange(2);
				cursor.Index += 22; // Seek to after the Vector3 creation
				cursor.Remove();
				cursor.EmitDelegate((Vector3 bound) => {
					return Utility.WorldToViewportPoint(CameraManager.Instance.transform, bound);
				});
			}
		}
	}

	// Replace the call to WorldToScreenPoint with a wrapper around the stereographic WorldToViewpointPort
	private static void SemiFunc_OnScreen(ILContext il) {
		ILCursor cursor = new ILCursor(il);

		if (cursor.TryGotoNext(
			x => x.MatchLdsfld<CameraUtils>("Instance"),
			x => x.MatchLdfld<CameraUtils>("MainCamera"),
			x => x.MatchLdarg(0),
			x => x.MatchCallvirt<Camera>("WorldToScreenPoint"),
			x => x.MatchStloc(0)
		)) {
			cursor.RemoveRange(4);
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate((Vector3 position) => {
				Vector3 point = Vector3.zero;
				if (CameraManager.Instance != null) {
					point = Utility.WorldToViewportPoint(CameraManager.Instance.transform, position);
					point.x *= (float)Screen.width;
					point.y *= (float)Screen.height;
				} else {
					point = Camera.main.WorldToScreenPoint(position);
				}
				return point;
			});
		}
	}

	private static void CrystalBall_StateActive(ILContext il) {
		ILCursor cursor = new ILCursor(il);

		if (cursor.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<CrystalBallValuable>("activeLocal"),
			x => x.MatchBrtrue(out _)
		)) {
			cursor.EmitDelegate(() => {
				CameraManager.Instance.Mode = CameraManager.Projection.Equisolid;
			});
		}
	}

	private static void CrystalBall_StateIdle(ILContext il) {
		ILCursor cursor = new ILCursor(il);

		if (cursor.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<CrystalBallValuable>("activeLocal"),
			x => x.MatchBrfalse(out _)
		)) {
			cursor.EmitDelegate(() => {
				CameraManager.Instance.Mode = CameraManager.Projection.Stereographic;
			});
		}
	}
}
