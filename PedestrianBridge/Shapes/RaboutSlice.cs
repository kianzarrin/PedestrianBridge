namespace PedestrianBridge.Shapes {
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;
    using static Util.VectorUtil;

    public class RaboutSlice {
        public struct Corner {
            // Output:
            internal Vector2 Point;
            internal Vector2 ControlPointA;
            internal bool Ignore;

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
                Point = (HWMInor + HWpath + SAFETY_NET) * DirMain + (HWMain + HWpath + SAFETY_NET) * DirMinor;
                Point += origin;

                ControlPointA = (HWMain + HWpath + SAFETY_NET) * DirMinor;
                ControlPointA += origin;

                Ignore = false; //!segmentMinor.Info.m_hasPedestrianLanes;
            }
        }

        Corner corner1, corner2;
        Vector2 MiddlePoint;
        Vector2 MDir1, MDir2;
        bool CalculateMiddlePoint() {
            Vector2 v21 = corner2.Point - corner1.Point;
            float angle1 = VectorUtil.UnsignedAngleRad(v21, corner1.DirMain); // expected Accute
            float angle2 = VectorUtil.UnsignedAngleRad(-v21, corner2.DirMain); // expected Accute
            float c = Mathf.PI * .5f - Epsilon;
            if (angle1>=c || angle2>= c) {
                Log.Info("Roundabout angle >= 180");
                return false;
            }

            var b2 = LineUtil.Bezier2ByDir(
                corner1.ControlPointA, corner1.DirMain,
                corner2.ControlPointA, corner2.DirMain);

            MiddlePoint = b2.Position(0.5f);
            MDir2 = b2.Tangent(0.5f);
            MDir1 = -MDir2;

            // re-adjust offset direction.
            corner1.DirMain = b2.Tangent(corner1.Offset / b2.ArcLength());
            corner2.DirMain = -b2.Tangent(1f - corner2.Offset / b2.ArcLength());
            return true;
        }

        bool IsSplit(ushort segmentID1Minor, ushort segmentID2Minor) {
            bool oneway1 = CalculateIsOneWay(segmentID1Minor);
            bool oneway2 = CalculateIsOneWay(segmentID2Minor);
            bool b1 = GetHeadNode(segmentID1Minor) == GetTailNode(segmentID2Minor);
            bool b2 = GetHeadNode(segmentID2Minor) == GetTailNode(segmentID1Minor);
            bool ret = oneway1 & oneway2 & (b1 | b2);
            return ret;
        }

        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor,
            NodeWrapper centerNode, NetInfo info) {
            Log.Info($"RaboutSlice: main1:{segmentID1Main}, minor1:{segmentID1Minor}, " +
                $"main2:{segmentID2Main}, minor2:{segmentID2Minor},");

            bool ignoreAll = IsSplit(segmentID1Minor, segmentID2Minor);
            if (ignoreAll) {
                Log.Info("RaboutSlice: Ignoring Split");
                return;
            }

            NetInfo eInfo = info.GetElevated();
            corner1 = new Corner(segmentID1Main, segmentID1Minor, eInfo.m_halfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, eInfo.m_halfWidth);

            ignoreAll = corner1.Ignore & corner2.Ignore;
            ignoreAll = ignoreAll || !CalculateMiddlePoint();
            if (ignoreAll) {
                Log.Info($"RaboutSlice: returns silently - {corner1.Ignore}  {corner2.Ignore} ");
                return;
            }

            nodeM = new NodeWrapper(MiddlePoint, 10, eInfo);
            node1 = corner1.Ignore ? null: new NodeWrapper(corner1.Point, 0, eInfo);
            node2 = corner2.Ignore ? null : new NodeWrapper(corner2.Point, 0, eInfo);
            segment1 = corner1.Ignore ? null : new SegmentWrapper(nodeM, node1, MDir1, corner1.DirMain);
            segment2 = corner2.Ignore ? null : new SegmentWrapper(nodeM, node2, MDir2, corner2.DirMain);
            segment3 = new SegmentWrapper(nodeM, centerNode);
        }

        public NodeWrapper nodeM;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;
        SegmentWrapper segment3;

        public void Create() {
            nodeM?.Create();
            node1?.Create();
            node2?.Create();
            segment1?.Create();
            segment2?.Create();
            segment3?.Create();
        }
    }
}