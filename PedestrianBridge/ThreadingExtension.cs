
namespace PedestrianBridge {
    using System;
    using ICities;
    using UnityEngine;
    using static PedestrianBridge.Util.HelpersExtensions;
    using PedestrianBridge.Tool;

    public class ThreadingExtension : ThreadingExtensionBase{
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            if (ControlIsPressed && Input.GetKeyDown(KeyCode.B)) {
                SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(
                    ()=> PedBridgeTool.Instance.ToggleTool());
            }   

        }
    }
}
