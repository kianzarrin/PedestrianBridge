namespace PedestrianBridge.Shapes {
    using UnityEngine;
    using ColossalFramework;
    using PedestrianBridge.Util;
    using KianCommons;
    using KianCommons.UI;
    using static KianCommons.NetUtil;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.Math.VectorUtil;
    using static KianCommons.GridUtil;
    using ColossalFramework.Math;
    using CSUtil.Commons;
    using Log = KianCommons.Log;
    using KianCommons.Math;

    public struct PathConnectWrapper {
        public Vector2 HitPoint;
        public ushort endSegmentID;
        public ushort endNodeID;
        public ushort HitSegmentID;
        public NodeWrapper node1;
        public NodeWrapper node2;
        public SegmentWrapper segment;
        public bool IsValid =>
            segment != null &&
            endNodeID.ToNode().Info.CanConnectPath() &&
            HitSegmentID.ToSegment().CanConnectPath();

        public PathConnectWrapper(ushort endNodeID, ushort endSegmentID) {
            this.endSegmentID = endSegmentID;
            this.endNodeID = endNodeID;

            HitSegmentID = FindConnactableSegment(endNodeID, endSegmentID, out HitPoint);
            //Log.Debug($"PathConnectWrapper detected segmentID={HitSegmentID}");

            if (HitSegmentID == 0) {
                node1 = node2 = null;
                segment = null;
                return;
            }
            Vector2 start = endNodeID.ToNode().m_position.ToCS2D();
            Vector2 dir = (HitPoint - start).normalized;

            var nodeInfo = endNodeID.ToNode().Info;
            var segmentInfo = HitSegmentID.ToSegment().Info;
            const float endWidth = 8;//meters
            Vector2 startPoint = start + (ControlCenter.HalfWidth + endWidth + SAFETY_NET) * dir;
            Vector2 endPoint = HitPoint - (ControlCenter.HalfWidth + segmentInfo.m_halfWidth + SAFETY_NET) * dir;

            node1 = new NodeWrapper(startPoint, 0);
            node2 = new NodeWrapper(endPoint , 0);
            segment = new SegmentWrapper(node1, node2);
        }

        public static void Create(ushort segmentID, ushort nodeID) {
            var path = new PathConnectWrapper(segmentID, nodeID);
            if (path.IsValid)
                path.Create();
        }

        public void Create() {
            node1?.Create();
            node2?.Create();
            segment?.Create();
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            if (!IsValid)
                return;
            //Log.Debug("PathConnectWrapper.RenderOverlay() called");
            var a = node1.Get3DPos();
            var b = node2.Get3DPos();

            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            RenderManager.instance.OverlayEffect.DrawSegment(
                cameraInfo, Color.blue,
                new Segment3(a, b),
                node1.GetFinalInfo().m_halfWidth * 2, 1000,
                -1000, +1000, true,
                false);
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            RenderManager.instance.OverlayEffect.DrawCircle(
                cameraInfo, Color.yellow,
                NodeWrapper.Get3DPos(HitPoint, 0),
                HitSegmentID.ToSegment().Info.m_halfWidth * 2,
                -1000, +1000, true,
                false);
            RenderUtil.DrawCutSegmentEnd(
                cameraInfo,
                endSegmentID,
                0.5f,
                IsStartNode(endSegmentID, endNodeID),
                Color.yellow,
                false);
        }

        public static ushort FindConnactableSegment(ushort endNodeID, ushort segmentID0, out Vector2 hitPoint) {
            hitPoint = Vector2.zero;
            var flags = endNodeID.ToNode().m_flags;
            bool b = flags.IsFlagSet(NetNode.Flags.End | NetNode.Flags.Bend);
            b &= endNodeID.ToNode().Info.CanConnectPath(); //flags.IsFlagSet(NetNode.Flags.OnGround);
            if (!b) {
                return 0;
            }

            Vector2 dir = -GetSegmentDir(segmentID0, endNodeID).ToCS2D();
            Vector3 pos = endNodeID.ToNode().m_position;
            Vector2 start = pos.ToCS2D();
            float max_distance = 16 * MPU;
            float min_distance = max_distance + MathUtil.Epsilon;
            ushort ret = 0;
            foreach (ushort segmentID in ScanDirSegment(start, dir, max_distance)) {
                if (IsNodeTooCloseToSegment(endNodeID, segmentID)) {
                    Log.Debug($"segment:{segmentID} was ignored because it is too close to endNodeID:{endNodeID}");
                    continue;
                }

                bool onGround = segmentID.ToSegment().Info.m_netAI is RoadAI;
                if (!onGround) {
                    Log.Debug($"segment:{segmentID} was ignored because it is not on ground");
                    continue;
                }

                segmentID.ToSegment().GetClosestPositionAndDirection(pos, out Vector3 hit, out Vector3 _);
                Vector2 _hitPoint = hit.ToCS2D();

                Vector2 diff = _hitPoint - start;
                float angle = UnsignedAngleRad(dir, diff) * Mathf.Rad2Deg;
                float distance = diff.magnitude;
                //Log.Debug($"segmentID={segmentID} angle={angle} distance={distance}");

                if (angle >= 45) {
                    Log.Debug($"segment:{segmentID} was ignored its angle is too much: {angle} degrees");
                    continue;
                }
                if (distance >= min_distance) {
                    Log.Debug($"segment:{segmentID} was ignored distance>min_distance");
                    continue;
                }

                ret = segmentID;
                hitPoint = _hitPoint;
                min_distance = distance;
            }

            return ret;
        }

        /// <summary>returns if there is only one segment distance between given node and segment.</summary>
        static bool IsNodeTooCloseToSegment(ushort nodeID, ushort segmentID) {
            foreach (ushort segID in GetCWSegList(nodeID)) {
                if (GetSharedNode(segID, segmentID) != 0)
                    return true;
            }
            return false;
        }
    }
}
