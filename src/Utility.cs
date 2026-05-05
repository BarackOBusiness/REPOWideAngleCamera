using System;
using UnityEngine;
using MonoMod.Cil;

namespace WideAngleCamera;

internal static class Utility {
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
	internal static void ChangeLevel(ILContext il) {
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
}
