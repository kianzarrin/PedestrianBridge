using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;
using PedestrianBridge.Util;
using PedestrianBridge.UI;
using PedestrianBridge.Shapes;
using JetBrains.Annotations;
namespace PedestrianBridge.Tool {
    public sealed class PedBridgeTool : KianToolBase {
        UIButton button;

        protected override void Awake() {
            var uiView = UIView.GetAView();
            //button = uiView.AddUIComponent(typeof(ToolButton)) as UIButton;
            button = PedestrianBridgeButton.CreateButton();
            base.Awake();
        }

        public static PedBridgeTool Create() {
            Log.Debug("PedBridgeTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            var tool = toolModControl.GetComponent<PedBridgeTool>() ?? toolModControl.AddComponent<PedBridgeTool>();
            return tool;
        }

        public static PedBridgeTool Instance {
            get {
                GameObject toolModControl = ToolsModifierControl.toolController?.gameObject;
                return toolModControl?.GetComponent<PedBridgeTool>();
            }
        }

        public static void Remove() {
            Log.Debug("PedBridgeTool.Remove()");
            var tool = Instance;
            if (tool != null)
                Destroy(tool);
        }

        protected override void OnDestroy() {
            Log.Debug("PedBridgeTool.OnDestroy()\n" + Environment.StackTrace);
            button?.Hide();
            Destroy(button);
            base.OnDestroy();
        }

        //public override void EnableTool() => ToolsModifierControl.SetTool<PedBridgeTool>();

        protected override void OnEnable() {
            Log.Debug("PedBridgeTool.OnEnable");
            button.Focus();
            base.OnEnable();
            button.Focus();
            button.Invalidate();
        }

        protected override void OnDisable() {
            Log.Debug("PedBridgeTool.OnDisable");
            button?.Unfocus();
            base.OnDisable();
            button?.Unfocus();
            button?.Invalidate();

        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();
            ToolCursor = HoverValid ? NetUtil.netTool.m_upgradeCursor : null;
        }

        PathConnectWrapper? _cachedPathConnectWrapper;
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (!HoverValid)
                return;

            Color color1 = Color.yellow;//  GetToolColor(Input.GetMouseButton(0), false);
            if (RoundaboutUtil.Instance_render.TraverseLoop(HoveredSegmentId, out var segList)) {
                foreach (var segmentID in segList) {
                    NetTool.RenderOverlay(cameraInfo, ref segmentID.ToSegment(), color1, color1);
                }
            } else if (IsSuitableJunction()) {
                foreach (var segmentID in NetUtil.GetCCSegList(HoveredNodeId)) {
                    NetTool.RenderOverlay(cameraInfo, ref segmentID.ToSegment(), color1, color1);
                }
            } else {
                bool cached =
                  _cachedPathConnectWrapper != null &&
                  _cachedPathConnectWrapper?.endSegmentID == HoveredSegmentId &&
                  _cachedPathConnectWrapper?.endNodeID == HoveredNodeId;
                _cachedPathConnectWrapper = cached ?
                    _cachedPathConnectWrapper :
                    new PathConnectWrapper(HoveredNodeId, HoveredSegmentId);
                _cachedPathConnectWrapper?.RenderOverlay(cameraInfo);
            }
        }

        protected override void OnPrimaryMouseClicked() {
            if (!HoverValid)
                return;
            Log.Debug($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");

            if(RoundaboutUtil.Instance_Click.TraverseLoop(HoveredSegmentId,out var segList)) {
                BuildControler.CreateRaboutBridge(RoundaboutUtil.Instance_Click);
            } else if (IsSuitableJunction()) {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    BuildControler.CreateJunctionBridge(HoveredNodeId);
                });
            } else {
                var path = new PathConnectWrapper(HoveredNodeId, HoveredSegmentId);
                if (path.segment != null)
                    path.Create();
            }
        }


        protected override void OnSecondaryMouseClicked() {
            //throw new System.NotImplementedException();
        }

        bool IsSuitableJunction() {
            if (HoveredNodeId == 0)
                return false;
            NetNode node = HoveredNodeId.ToNode();
            if (node.CountSegments() < 3)
                return false;

            if (!node.m_flags.IsFlagSet(NetNode.Flags.OnGround))
                return false;

            return true;
        }

    } //end class
}
