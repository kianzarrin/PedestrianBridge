using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System;

using static PedestrianBridge.Util.HelpersExtensions;
using PedestrianBridge.Util;

namespace PedestrianBridge.Tool {
    public abstract class KianToolBase : DefaultTool
    {
        public bool ToolEnabled => ToolsModifierControl.toolController.CurrentTool == this;

        protected override void OnDestroy() {
            DisableTool();
            base.OnDestroy();
        }

        protected abstract void OnPrimaryMouseClicked();
        protected abstract void OnSecondaryMouseClicked();

        public void ToggleTool()
        {
            if (!ToolEnabled)
                EnableTool();
            else
                DisableTool();
        }

        private void EnableTool()
        {
            Log.Info("EnableTool: called");
            //WorldInfoPanel.HideAllWorldInfoPanels();
            //GameAreaInfoPanel.Hide();
            ToolsModifierControl.toolController.CurrentTool = this;
        }

        private void DisableTool()
        {
            Log.Info("DisableTool: called");
            ToolsModifierControl.SetTool<DefaultTool>();
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();
            DetermineHoveredElements();

            if (Input.GetMouseButtonDown(0))
            {
                OnPrimaryMouseClicked();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                OnSecondaryMouseClicked();
            }
        }

        public override void SimulationStep()
        {
            base.SimulationStep();
            DetermineHoveredElements();
        }

        public ushort HoveredNodeId { get; private set; } = 0;
        public ushort HoveredSegmentId { get; private set; } = 0;

        private bool DetermineHoveredElements()
        {
            if (UIView.IsInsideUI() || !Cursor.visible)
            {
                return false;
            }

            HoveredSegmentId = 0;
            HoveredNodeId = 0;

            // find currently hovered node
            RaycastInput nodeInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_netService = {
                        // find road segments
                        m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels,
                        m_service = ItemClass.Service.Road
                    },
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.None
            };

            if (RayCast(nodeInput, out RaycastOutput nodeOutput))
            {
                HoveredNodeId = nodeOutput.m_netNode;
            }

            HoveredSegmentId = GetSegmentFromNode();

            if (HoveredSegmentId != 0) {
                Debug.Assert(HoveredNodeId != 0, "unexpected: HoveredNodeId == 0");
                return true;
            }

            // find currently hovered segment
            var segmentInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_netService = {
                    // find road segments
                    m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels,
                    m_service = ItemClass.Service.Road
                },
                m_ignoreTerrain = true,
                m_ignoreSegmentFlags = NetSegment.Flags.None
            };

            if (RayCast(segmentInput, out RaycastOutput segmentOutput))
            {
                HoveredSegmentId = segmentOutput.m_netSegment;
            }


            if (HoveredNodeId <= 0 && HoveredSegmentId > 0)
            {
                // alternative way to get a node hit: check distance to start and end nodes
                // of the segment
                ushort startNodeId = HoveredSegmentId.ToSegment().m_startNode;
                ushort endNodeId = HoveredSegmentId.ToSegment().m_endNode;

                var vStart = segmentOutput.m_hitPos - startNodeId.ToNode().m_position;
                var vEnd = segmentOutput.m_hitPos - endNodeId.ToNode().m_position;

                float startDist = vStart.magnitude;
                float endDist = vEnd.magnitude;

                if (startDist < endDist && startDist < 75f)
                {
                    HoveredNodeId = startNodeId;
                }
                else if (endDist < startDist && endDist < 75f)
                {
                    HoveredNodeId = endNodeId;
                }
            }
            return HoveredNodeId != 0 || HoveredSegmentId != 0;
        }

        static float GetAgnele(Vector3 v1, Vector3 v2) {
            float ret = Vector3.Angle(v1, v2);
            if (ret > 180) ret -= 180; //future proofing
            ret = Math.Abs(ret);
            return ret;
        }

        internal ushort GetSegmentFromNode() {
            bool considerSegmentLenght = true;
            ushort minSegId = 0;
            if (HoveredNodeId != 0) {
                NetNode node = HoveredNodeId.ToNode();
                Vector3 dir0 = node.m_position - m_mousePosition;
                float min_angle = float.MaxValue;
                for (int i = 0; i < 8; ++i) {
                    ushort segmentId = node.GetSegment(i);
                    if (segmentId == 0)
                        continue;
                    NetSegment segment = segmentId.ToSegment();
                    Vector3 dir;
                    if (segment.m_startNode == HoveredNodeId) {
                        dir = segment.m_startDirection;

                    } else {
                        dir = segment.m_endDirection;
                    }
                    float angle = GetAgnele(-dir,dir0);
                    if(considerSegmentLenght)
                        angle *= segment.m_averageLength;
                    if (angle < min_angle) {
                        min_angle = angle;
                        minSegId = segmentId;
                    }
                }
            }
            return minSegId;
        }

        public void DrawNodeCircle(RenderManager.CameraInfo cameraInfo,
                           ushort nodeId,
                           Color color,
                           bool alpha = false) {
            float r = 8;
            Vector3 pos = nodeId.ToNode().m_position;
            DrawOverlayCircle(cameraInfo, color, pos, r * 2, alpha);
        }

        private void DrawOverlayCircle(RenderManager.CameraInfo cameraInfo,
                               Color color,
                               Vector3 position,
                               float width,
                               bool alpha) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                color,
                position,
                width,
                position.y - 100f,
                position.y + 100f,
                false,
                alpha);
        }
    }
}
