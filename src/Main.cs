using BepInEx;

namespace WideAngleCamera;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}

// General layout of how I think this mod is going to work:
// We're going to IL hook a RunManager method, match for after levelCurrent is set,
// then it's going to invoke an event which is defined in this plugin to signal
// that a load is complete. Once this is done, we can begin standard practice;
// instantiating the wide angle cam from assets, initializing the handler script,
// generating the triangle and setting up the main camera to view it orthographically
