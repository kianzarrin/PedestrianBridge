namespace PedestrianBridge.Shapes {
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.HelpersExtensions;
    using static Util.NetUtil;
    using static Util.VectorUtil;
    using VectorUtils = Util.VectorUtil;

    public class RaboutSlice {
        public struct Corner {
            // Output:
            internal Vector2 Point;
            internal Vector2 ControlPointA;

            /// DirMain first represents the direction of the main segment at the junction.
            /// but then it is modified to represent the direction of the pedestrian bridge
            /// slightly away from the junction.
            internal Vector2 DirMain, DirMinor;
            internal float Offset => (ControlPointA - Point).magnitude;
            //bool bLeft;

            public Corner(ushort segmentMainID, ushort segmentMinorID, float HWpath) {
                // Prepration:
                ref NetSegment segmentMain = ref segmentMainID.ToSegment();
                ref NetSegment segmentMinor = ref segmentMinorID.ToSegment();

                ushort junctionID = segmentMain.GetSharedNode(segmentMinorID);
                ref NetNode junction = ref junctionID.ToNode();

                Vector2 origin = junction.m_position.ToCS2D();

                bool bStartNodeMain = segmentMain.m_startNode == junctionID;
                bool bStartNodeMinor = segmentMinor.m_startNode == junctionID;

                DirMain = (bStartNodeMain ? segmentMain.m_startDirection : segmentMain.m_endDirection).ToCS2D().normalized;
                DirMinor = (bStartNodeMinor ? segmentMinor.m_startDirection : segmentMinor.m_endDirection).ToCS2D().normalized;


                float HWMain = segmentMain.Info.m_halfWidth;
                float HWMInor = segmentMinor.Info.m_halfWidth;
                float angle = Vector2.Angle(DirMain, DirMinor);
                angle *= Mathf.Deg2Rad;
                float ratio = 1f / Mathf.Sin(angle);
                HWMain *= ratio;
                HWMInor *= ratio;




                ////////////////////////////////////////////////////////////////
                // Main calculations
                Point = (HWMInor + HWpath + Epsilon) * DirMain + (HWMain + HWpath + Epsilon) * DirMinor;
                Point += origin;

                ControlPointA = (HWMain + HWpath + Epsilon) * DirMinor;
                ControlPointA += origin;

                // old code:
                //ushort otherNodeID = segmentMain.GetOtherNode(junctionID);
                //Vector2 otherPoint = otherNodeID.ToNode().m_position.ToCS2D();
                //Vector2 otherDir = (!bStartNodeMain ? segmentMain.m_startDirection : segmentMain.m_endDirection).ToCS2D().normalized;
                //bLeft = segmentMinorID == segmentMain.GetLeftSegment(junctionID);
                //Vector2 otherNormal = bLeft ? otherDir.Rotate90CCW() : otherDir.Rotate90CW();
                //ControlPointB = (HWMain + HWpath + Epsilon) * otherNormal;
            }
        }

        Corner corner1, corner2;
        Vector2 MiddlePoint;
        Vector2 MDir1, MDir2;
        void CalculateMiddlePoint() {
            var b2 = LineUtil.Bezier2ByDir(
                corner1.ControlPointA, corner1.DirMain,
                corner2.ControlPointA, corner2.DirMain);

            MiddlePoint = b2.Position(0.5f);
            MDir2 = b2.Tangent(0.5f);
            MDir1 = -MDir2;

            // re-adjust offset direction.
            corner1.DirMain = b2.Tangent(corner1.Offset / b2.ArcLength());
            corner2.DirMain = -b2.Tangent(1f - corner2.Offset / b2.ArcLength());
        }

        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor,
            NodeWrapper centerNode, NetInfo info) {
            NetInfo eInfo = info.GetElevated();
            corner1 = new Corner(segmentID1Main, segmentID1Minor, eInfo.m_halfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, eInfo.m_halfWidth);
            CalculateMiddlePoint();

            nodeM = new NodeWrapper(MiddlePoint, 10, eInfo);
            node1 = new NodeWrapper(corner1.Point, 0, eInfo);
            node2 = new NodeWrapper(corner2.Point, 0, eInfo);
            segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.DirMain);
            segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.DirMain);
            segment3 = new SegmentWrapper(nodeM, centerNode);
        }

        public NodeWrapper nodeM;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;
        SegmentWrapper segment3;

        public void Create() {
            nodeM.Create();
            node1.Create();
            node2.Create();
            segment1.Create();
            segment2.Create();
            segment3.Create();
        }
    }
}