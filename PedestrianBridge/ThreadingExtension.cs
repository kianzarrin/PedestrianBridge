
namespace PedestrianBridge {
    using System;
    using ICities;
    using UnityEngine;
    using static PedestrianBridge.Util.HelpersExtensions;
    using PedestrianBridge.Tool;

    public class ThreadingExtension : ThreadingExtensionBase{
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            var tool = ToolsModifierControl.toolController?.CurrentTool;
            if (ControlIsPressed && Input.GetKeyDown(KeyCode.B)) {
                bool flag = tool == null || tool is PedBridgeTool ||
                    tool.GetType() == typeof(DefaultTool) || tool is NetTool || tool is BuildingTool ||
                    tool.GetType().FullName.Contains("Roundabout");
                if (flag) {
                    SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(
                        () => PedBridgeTool.Instance.ToggleTool());
                }
            }   

        }
    }
}
