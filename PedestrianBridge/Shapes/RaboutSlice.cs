namespace PedestrianBridge.Shapes {
    using System;
    using UnityEngine;
    using Util;
    using static Util.HelpersExtensions;
    using static Util.NetUtil;
    using static Util.VectorUtils;


    public class RaboutSlice {
        public struct Corner {
            // Output:
            internal Vector2 Point;
            internal Vector2 Dir1, Dir2;
            public Corner(ushort segID1, ushort segID2, float HWpath) {
                // Prepration:
                ref NetSegment seg1 = ref segID1.ToSegment();
                ref NetSegment seg2 = ref segID2.ToSegment();


                // intermidiate
                float HW1 = seg1.Info.m_halfWidth;
                float HW2 = seg2.Info.m_halfWidth;
                ushort junctionID = seg1.GetSharedNode(segID2);
                ref NetNode junction = ref junctionID.ToNode();
                Vector2 origin = junction.m_position.ToPoint();
                bool bStartNode1 = seg1.m_startNode == junctionID;
                bool bStartNode2 = seg2.m_startNode == junctionID;
                Vector2 V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToPoint();
                Vector2 V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToPoint();
                Dir1 = V1.normalized;
                Dir2 = V2.normalized;

                float angle = Vector2.Angle(Dir1, Dir2);
                angle *= Mathf.Deg2Rad;
                float ratio = 1f / Mathf.Sin(angle);
                HW1 *= ratio;
                HW2 *= ratio;

                ////////////////////////////////////////////////////////////////
                // Main calculations
                Point = (HW2 + HWpath + Epsilon) * Dir1 + (HW1 + HWpath + Epsilon) * Dir2;
                Point += origin;
            }
        }

        Corner corner1, corner2;
        Vector2 MiddlePoint;
        Vector2 MDir1, MDir2;

        public Vector2 CalculateCenter() {
            bool b = VectorUtils.Intersect(
                corner1.Point, corner1.Dir1.Rotate90CW(),
                corner2.Point, corner2.Dir1.Rotate90CW(),
               out var center);

            if (!b)
                throw new Exception("could not find center of roundabout");

            return center;
        }

        void CalculateMiddlePoint() {
            bool b = VectorUtils.Intersect(
                corner1.Point, corner1.Dir1,
                corner2.Point, corner2.Dir1,
                out MiddlePoint);
            if (!b)
                throw new Exception("Could not calculate middle point");
            MDir1 = corner2.Dir1 - corner1.Dir1;
            MDir2 = -MDir1;
        }

        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor, NetInfo info) {
            NetInfo eInfo = info.GetElevated();

            corner1 = new Corner(segmentID1Main, segmentID1Minor, eInfo.m_halfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, eInfo.m_halfWidth);
            CalculateMiddlePoint();

            nodeM = new NodeWrapper(MiddlePoint, 10, eInfo);
            node1 = new NodeWrapper(corner1.Point, 0, eInfo);
            node2 = new NodeWrapper(corner2.Point, 0, eInfo);
            segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.Dir1);
            segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.Dir1);
        }

        public NodeWrapper nodeM;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;

        public void Create() {
            nodeM.Create();
            node1.Create();
            node2.Create();
            segment1.Create();
            segment2.Create();
        }
    }
}