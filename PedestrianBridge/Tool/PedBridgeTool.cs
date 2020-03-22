using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;
using PedestrianBridge.Util;
using PedestrianBridge.UI;
using PedestrianBridge.Shape;
using JetBrains.Annotations;

namespace PedestrianBridge.Tool {
    using static NetTool;
    public sealed class PedBridgeTool : KianToolBase {
        UIButton button;

        public PedBridgeTool() : base() {
            var uiView = UIView.GetAView();
            button = (ToolButton)uiView.AddUIComponent(typeof(ToolButton));
            button.eventClicked += (_, __) => {
                ToggleTool();
            };
        }

        public static PedBridgeTool Create() {
            Log.Debug("PedBridgeTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            var tool = toolModControl.GetComponent<PedBridgeTool>() ?? toolModControl.AddComponent<PedBridgeTool>();
            return tool;
        }

        public static void Remove() {
            Log.Debug("PedBridgeTool.Remove()");
            GameObject toolModControl = ToolsModifierControl.toolController?.gameObject;
            var tool = toolModControl?.GetComponent<PedBridgeTool>();
            if (tool != null)
                Destroy(tool);
        }

        protected override void OnDestroy() {
            Log.Debug("PedBridgeTool.OnDestroy()\n" + Environment.StackTrace);
            Destroy(button);
            base.OnDestroy();
        }

        //public override void EnableTool() => ToolsModifierControl.SetTool<PedBridgeTool>();

        protected override void OnEnable() {
            Log.Debug("PedBridgeTool.OnEnable");
            base.OnEnable();
            button.Focus();
        }

        protected override void OnDisable() {
            Log.Debug("PedBridgeTool.OnDisable");
            button.Unfocus();
            base.OnDisable();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (HoveredSegmentId == 0 || HoveredNodeId == 0)
                return;

            Color color1 = GetToolColor(Input.GetMouseButton(0), false);
            Color color2 = GetToolColor(Input.GetMouseButton(1), false);
            if (Condition())
                DrawNodeCircle(cameraInfo, HoveredNodeId, color1);
        }

        bool Condition() {
            if (HoveredSegmentId == 0 || HoveredNodeId == 0)
                return false;
            NetNode.Flags nodeFlags = HoveredNodeId.ToNode().m_flags;
            NetNode node = HoveredNodeId.ToNode();
            if (node.CountSegments() != 4)
                return false;
            return true;
        }

        protected override void OnPrimaryMouseClicked() {
            Log.Debug($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (HoveredSegmentId == 0 || HoveredNodeId == 0)
                return;
            if (Condition()) {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    BuildControler.CreateJunctionBridge(HoveredNodeId);
                });
            }
        }

        protected override void OnSecondaryMouseClicked() {
            throw new System.NotImplementedException();
        }

    } //end class
}
