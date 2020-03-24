namespace PedestrianBridge.Shapes {
    using UnityEngine;
    using Util;
    using static Util.HelpersExtensions;
    using static Util.NetUtil;

    public class LWrapper {
        struct Calc {
            // Output:
            internal Vector2 Point1, PointL, Point2;

            internal Calc(ushort segID1, ushort segID2, float HWpb) {
                // Prepration:
                ref NetSegment seg1 = ref segID1.ToSegment();
                ref NetSegment  seg2 = ref segID2.ToSegment();
                float HW1 = seg1.Info.m_halfWidth;
                float HW2 = seg2.Info.m_halfWidth;
                ushort junctionID = seg1.GetSharedNode(segID2);
                ref NetNode junction = ref junctionID.ToNode();
                Vector2 origin = junction.m_position.ToPoint();
                bool bStartNode1 = seg1.m_startNode == junctionID;
                bool bStartNode2 = seg2.m_startNode == junctionID;
                Vector2 V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToPoint();
                Vector2 V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToPoint();
                Vector2 dir1 = V1.normalized;
                Vector2 dir2 = V2.normalized;

                float angle = Vector2.Angle(dir1, dir2);
                angle *= Mathf.Deg2Rad;
                float ratio = 1f / Mathf.Sin(angle);
                HW1 *= ratio;
                HW2 *= ratio;

                ////////////////////////////////////////////////////////////////
                // Main calculations
                PointL = (HW2 + HWpb + Epsilon) * dir1 + (HW1 + HWpb + Epsilon) * dir2;
                Point1 = PointL + 3 * MPU * dir1;
                Point2 = PointL + 3 * MPU * dir2;

                PointL += origin;
                Point1 += origin;
                Point2 += origin;
            }
        }

        // segmentID2 must be to the left of segmentID1 (when going toward the intersection)
        public LWrapper(ushort segmentID1, ushort segmentID2, NetInfo info) {
            NetInfo eInfo = info.GetElevated();
            var calc = new Calc(segmentID1, segmentID2, eInfo.m_halfWidth);

            nodeL = new NodeWrapper(calc.PointL, 10, eInfo);
            node1 = new NodeWrapper(calc.Point1, 0, eInfo);
            node2 = new NodeWrapper(calc.Point2, 0, eInfo);
            segment1 = new SegmentWrapper(nodeL, node1);
            segment2 = new SegmentWrapper(nodeL, node2);
        }

        public NodeWrapper nodeL;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;

        public void Create() {
            nodeL.Create();
            node1.Create();
            node2.Create();
            segment1.Create();
            segment2.Create();
        }
    }
}
