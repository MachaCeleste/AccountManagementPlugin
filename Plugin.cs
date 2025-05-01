using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AccountManagementPlugin;

[BepInPlugin("com.machaceleste.accountmanagementplugin", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var harmony = new Harmony("com.machaceleste.accountmanagementplugin");
        harmony.PatchAll();
    }
}