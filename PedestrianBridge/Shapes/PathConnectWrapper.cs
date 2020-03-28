using UnityEngine;


namespace PedestrianBridge.Shapes {
    using PedestrianBridge.Util;
    using static PedestrianBridge.Util.NetUtil;
    using static PedestrianBridge.Util.HelpersExtensions;
    using static PedestrianBridge.Util.VectorUtil;
    using static PedestrianBridge.Util.GridUtil;
    using ColossalFramework.Math;

    public struct PathConnectWrapper {
        public Vector2 HitPoint;
        public ushort segmentID;
        public NodeWrapper node1;
        public NodeWrapper node2;
        public SegmentWrapper segment;

        public PathConnectWrapper(ushort endNodeID) {
            segmentID = FindConnactableSegment(endNodeID, out HitPoint);
            Log.Debug($"PathConnectWrapper detected segmentID={segmentID}");

            if (segmentID == 0) {
                node1 = node2 = null;
                segment = null;
                return;
            }
            Vector2 start = endNodeID.ToNode().m_position.ToCS2D();
            Vector2 dir = (HitPoint - start).normalized;

            var pathInfo = PrefabUtil.SelectedPrefab;
            var nodeInfo = endNodeID.ToNode().Info;
            var segmentInfo = segmentID.ToSegment().Info;
            Vector2 startPoint = start + (pathInfo.m_halfWidth + nodeInfo.m_halfWidth + SAFETY_NET) * dir;
            Vector2 endPoint = HitPoint - (pathInfo.m_halfWidth + segmentInfo.m_halfWidth + SAFETY_NET) * dir;

            node1 = new NodeWrapper(startPoint, 0, pathInfo);
            node2 = new NodeWrapper(endPoint , 0, pathInfo);
            segment = new SegmentWrapper(node1, node2);
        }

        public void Create() {
            node1.Create();
            node2.Create();
            segment.Create();
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color, bool alphaBlend = true) {
            Log.Debug("PathConnectWrapper.RenderOverlay() called");
            var a = node1.Get3DPos();
            var b = node2.Get3DPos();
            var segment = new Segment3(a,b);
            float size = PrefabUtil.SelectedPrefab.m_halfWidth*2;
            RenderManager.instance.OverlayEffect.DrawSegment(
                cameraInfo, color,
                segment, size, 1000,
                -1000, +1000, true,
                alphaBlend);
        }

        public static ushort FindConnactableSegment(ushort endNodeID, out Vector2 hitPoint) {
            hitPoint = Vector2.zero;
            if (endNodeID.ToNode().m_elevation != 0) {
                return 0;
            }
            ushort segmentID0 = GetFirstSegment(endNodeID);
            Vector2 dir = -GetSegmentDir(segmentID0, endNodeID).ToCS2D();
            Vector3 pos = endNodeID.ToNode().m_position;
            Vector2 start = pos.ToCS2D();
            foreach (ushort segmentID in ScanDirSegment(start, dir, 10 * MPU)) {
                if (HasNode(segmentID, endNodeID))
                    continue;

                bool onGround = segmentID.ToSegment().Info.m_netAI is RoadAI;
                if (!onGround)
                    continue;

                segmentID.ToSegment().GetClosestPositionAndDirection(pos, out Vector3 hit, out Vector3 _);
                hitPoint = hit.ToCS2D();

                Vector2 diff = hitPoint - start;
                float angle = UnsignedAngleRad(dir, diff) * Mathf.Rad2Deg;
                float distance = diff.magnitude;
                Log.Debug($"segmentID={segmentID} angle={angle} distance={distance}");

                if (angle < 45 && distance <= 10 * MPU)
                    return segmentID;
            }

            return 0;
        }
    }
}
