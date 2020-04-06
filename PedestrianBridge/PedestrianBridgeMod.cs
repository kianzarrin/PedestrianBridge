using ICities;
using JetBrains.Annotations;
using System;
using PedestrianBridge.Util;

namespace PedestrianBridge {
    public class PedestrianBridgeMod : IUserMod {
        public static Version ModVersion => typeof(PedestrianBridgeMod).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);
        public string Name => "Automatic Pedestrian Bridge" + VersionString;
        public string Description => "use Ctrl+B to activate. " +
            "Automatically builds pedestrian bridges over junctions and roundabouts with one click";
        
        public void OnEnabled() {
            if (HelpersExtensions.InGame)
                LoadTool.Load();
#if DEBUG
            TestsExperiments.Run();
#endif
        }

        public void OnDisabled() {
            LoadTool.Release();
        }
    }

    public static class LoadTool {
        public static void Load() {
            TMPEUtil.Active = true;
            Tool.PedBridgeTool.Create();
        }
        public static void Release() {
            Tool.PedBridgeTool.Remove();
        }
    }

    public class LoadingExtention : LoadingExtensionBase {
        public override void OnLevelLoaded(LoadMode mode) {
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
                LoadTool.Load();
        }

        public override void OnLevelUnloading() {
            LoadTool.Release();
        }
    }



} // end namesapce
