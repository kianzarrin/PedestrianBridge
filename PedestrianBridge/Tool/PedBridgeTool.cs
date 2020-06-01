using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;
using PedestrianBridge.Util;
using PedestrianBridge.UI;
using PedestrianBridge.Shapes;
using KianCommons;
using PedestrianBridge.UI.ControlPanel;

namespace PedestrianBridge.Tool {
    using static KianCommons.UI.RenderUtil;

    public sealed class PedBridgeTool : KianToolBase {
        public static readonly SavedInputKey ActivationShortcut = new SavedInputKey(
            "ActivationShortcut",
            UI.ModSettings.FILE_NAME,
            SavedInputKey.Encode(KeyCode.B, true, false, false),
            true);

        UIButton button;

        protected override void Awake() {
            button = PedestrianBridgeButton.CreateButton();
            base.Awake();
        }

        public static PedBridgeTool Create() {
            Log.Info("PedBridgeTool.Create()");
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
            ControlPanel.Instance?.Open();
            Log.Debug("PedBridgeTool.OnEnable");
            button.Focus();
            base.OnEnable();
            button.Focus();
            button.Invalidate();
        }

        protected override void OnDisable() {
            ControlPanel.Instance?.Close();
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
        Vector3 _cachedHitPos;


        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (!HoverValid)
                return;

            //Log.Debug($"HoveredSegmentId={HoveredSegmentId} HoveredNodeId={HoveredNodeId} HitPos={HitPos}");
            //if (Input.GetKey(KeyCode.LeftAlt)) {
            //    var b = HoveredSegmentId.ToSegment().CalculateSegmentBezier3();
            //    float hw = HoveredSegmentId.ToSegment().Info.m_halfWidth;
            //    var b2d = b.ToCSBezier2();
            //    var b1 = b2d.CalculateParallelBezier(hw * 2, false).TOCSBezier3();
            //    var b2 = b2d.CalculateParallelBezier(hw * 2, true).TOCSBezier3();
            //    b = b2d.TOCSBezier3();
            //    b.Render(cameraInfo, Color.green, hw);
            //    b1.Render(cameraInfo, Color.blue, hw);
            //    b2.Render(cameraInfo, Color.blue, hw);

            //    DrawOverlayCircle(cameraInfo, Color.red, HitPos, 1, true);
            //    return;
            //}


            Color color = Color.yellow;//  GetToolColor(Input.GetMouseButton(0), false);
            if (RoundaboutUtil.Instance_render.TraverseLoop(HoveredSegmentId, out var segList)) {
                foreach (var segmentID in segList) {
                    NetTool.RenderOverlay(cameraInfo, ref segmentID.ToSegment(), color, color);
                }
            } else if (IsSuitableRoadForRoadBridge()) {
                RoadBridgeWrapper.RenderOverlay(cameraInfo, color, HoveredSegmentId, HitPos);
            } else if (IsSuitableJunction()) {
                foreach (var segmentID in NetUtil.GetCCSegList(HoveredNodeId)) {
                    NetTool.RenderOverlay(cameraInfo, ref segmentID.ToSegment(), color, color);
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

            DrawOverlayCircle(cameraInfo, Color.red, HitPos, 1, true);

            if (Input.GetKey(KeyCode.LeftAlt)) 
                RenderGrids(cameraInfo, m_mousePosition, Color.black);
        }

        protected override void OnPrimaryMouseClicked() {
            if (!HoverValid)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if(RoundaboutUtil.Instance_Click.TraverseLoop(HoveredSegmentId,out var segList)) {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    RaboutWraper.Create(RoundaboutUtil.Instance_Click);
                });
            } else if (IsSuitableRoadForRoadBridge()) {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    RoadBridgeWrapper.Create(HoveredSegmentId,HitPos);
                });
            } else if (IsSuitableJunction()) {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    JunctionWrapper.Create(HoveredNodeId);
                });
            } else {
                Singleton<SimulationManager>.instance.AddAction(delegate () {
                    PathConnectWrapper.Create(HoveredNodeId, HoveredSegmentId);
                });
            }
        }

        protected override void OnSecondaryMouseClicked() {
            //throw new System.NotImplementedException();
        }

        bool IsSuitableRoadForRoadBridge() {
            if (!HoveredSegmentId.ToSegment().CanConnectPath())
                return false;
            float minDistance = 1 * NetUtil.MPU + NetUtil.MaxNodeHW(HoveredNodeId);
            if (HoveredNodeId.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                return true;
            var diff = HitPos - HoveredNodeId.ToNode().m_position;
            float diff2 = diff.sqrMagnitude;
            return diff2 > minDistance * minDistance;
        }

        bool IsSuitableJunction() {
            if (HoveredNodeId == 0)
                return false;
            NetNode node = HoveredNodeId.ToNode();
            if (node.CountSegments() < 3)
                return false;

            if (!node.Info.CanConnectPath())
                return false;

            return true;
        }

    } //end class
}
