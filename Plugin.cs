using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using REPOPingMod.Input;
using REPOPingMod.Networking;
using REPOPingMod.PingSystem;
using UnityEngine;

namespace REPOPingMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.repomods.pingsystem";
        public const string PluginName = "REPO Ping System";
        public const string PluginVersion = "0.3.1";

        internal static new ManualLogSource Logger;
        internal static PingConfig PingConfig;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"{PluginName} v{PluginVersion} loading...");

            PingConfig = new PingConfig(Config);

            try
            {
                var modObject = new GameObject("REPOPingMod");
                modObject.hideFlags = HideFlags.HideAndDontSave;
                Logger.LogInfo("[DEBUG] GameObject created with HideAndDontSave");

                PingAudio.Initialize(modObject);
                Logger.LogInfo("[DEBUG] PingAudio initialized");

                modObject.AddComponent<PingManager>();
                Logger.LogInfo("[DEBUG] PingManager added");

                var netSync = modObject.AddComponent<PingNetSync>();
                Logger.LogInfo("[DEBUG] PingNetSync added");

                netSync.Initialize();
                Logger.LogInfo("[DEBUG] PingNetSync initialized");

                var radialWheel = modObject.AddComponent<RadialWheel>();
                Logger.LogInfo("[DEBUG] RadialWheel added");

                var controller = modObject.AddComponent<PingController>();
                controller.Initialize(radialWheel);
                Logger.LogInfo($"[DEBUG] PingController added, enabled={controller.enabled}, go.active={modObject.activeInHierarchy}");

                // Initialize arm pointing system with Harmony patches
                var harmony = new Harmony(PluginGUID);
                ArmPointing.Initialize(harmony);

                Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"{PluginName} v{PluginVersion} FAILED to load: {ex}");
            }
        }
    }
}
