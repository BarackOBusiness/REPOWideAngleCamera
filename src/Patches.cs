using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WideAngleCamera;

internal static class Patches {
	[HarmonyPatch(typeof(SpectateCamera), nameof(SpectateCamera.Awake))]
	[HarmonyPostfix]
	static void Awake(SpectateCamera __instance, ref Camera ___TopCamera) {
		// Reset the Top Camera to what it should be
		___TopCamera = Camera.main.transform.Find("Camera Top").GetComponent<Camera>();
	}

	[HarmonyPatch(typeof(RenderTextureMain), nameof(RenderTextureMain.Start))]
	[HarmonyPostfix]
	static void Start(ref List<Camera> ___cameras) {
		// Remove all cameras from this list that either are not the main camera or the top camera
		___cameras.RemoveAll(cam => cam.transform.parent == null ||
		(cam.transform.parent.name != "Camera Main" && cam.transform.parent.name != "Tilt"));
	}
}
