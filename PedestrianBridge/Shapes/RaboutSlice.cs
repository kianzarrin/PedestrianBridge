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
            internal LWrapper.Calc L;
            // Output:
            internal Vector2 ControlPoint => L.ControlPoint1;
            internal Vector2 Point => L.PointL;
            internal Vector2 AltPoint => L.Point2;
            internal bool Ignore;

            /// DirMain first represents the direction of the main segment at the junction.
            /// but then it is modified to represent the direction of the pedestrian bridge
            /// slightly away from the junction.
            internal Vector2 DirMain;


            internal Vector2 DirMinor => L.StartDir2;
            internal Vector2 EndDirMinor => L.EndDir2;
            internal float Offset => (ControlPoint - Point).magnitude;

            public Corner(ushort segmentMainID, ushort segmentMinorID, float HWpath) {
                L = new LWrapper.Calc(segmentMainID, segmentMinorID, HWpath);
                DirMain = L.StartDir1;
            Ignore = false; //!segmentMinor.Info.m_hasPedestrianLanes;
            }
        }

        Corner corner1, corner2;
        Vector2 MiddlePoint;
        Vector2 MDir1, MDir2;
        float Len1 => (MiddlePoint - corner1.Point).magnitude;
        float Len2 => (MiddlePoint - corner2.Point).magnitude;

        bool CalculateMiddlePoint() {
            Vector2 v21 = corner2.Point - corner1.Point;
            float angle1 = VectorUtil.UnsignedAngleRad(v21, corner1.DirMain); // expected Accute
            float angle2 = VectorUtil.UnsignedAngleRad(-v21, corner2.DirMain); // expected Accute
            float c = Mathf.PI * .5f - Epsilon;
            if (angle1>=c || angle2>= c) {
                //Log.Debug("Roundabout angle >= 180");
                return false;
            }

            var b2 = LineUtil.Bezier2ByDir(
                corner1.ControlPoint, corner1.DirMain,
                corner2.ControlPoint, corner2.DirMain);

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

        bool IsBetweenInOut(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor) {
            Util.HelpersExtensions.AssertStack();
            //Log.Debug("IsBetweenInOut() called.");
            const float maxLen = 7 * MPU;
            bool bShort = (corner1.Point - corner2.Point).sqrMagnitude <= maxLen * maxLen;
            if (!bShort) {
                return false; // fast exit
            }

            bool bOneWay = CalculateIsOneWay(segmentID1Minor) && CalculateIsOneWay(segmentID2Minor);

            float angle1 = VectorUtil.UnsignedAngleRad(corner1.DirMinor, corner1.DirMain);
            float angle2 = VectorUtil.UnsignedAngleRad(corner2.DirMinor, corner2.DirMain);
            bool bAccute1 = angle1 < Mathf.PI * .5f + Epsilon;
            bool bAccute2 = angle2 < Mathf.PI * .5f + Epsilon;
            bool bAccute = bAccute1 && bAccute2;

            bool flag1 =
                GetHeadNode(segmentID1Minor) == GetHeadNode(segmentID1Main) &&
                GetTailNode(segmentID2Main) == GetTailNode(segmentID2Minor);
            bool flag2 =
                GetHeadNode(segmentID2Minor) == GetHeadNode(segmentID2Main) &&
                GetTailNode(segmentID1Main) == GetTailNode(segmentID1Minor);
            bool flag = flag1 || flag2;

            bool ret = bOneWay && bAccute && flag;
            if (ret) {
                //Log.Debug("IsBeweenInout() returns true");
            }
            return ret;
        }

        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor,
            NodeWrapper centerNode, NetInfo info) {
            //Log.Debug($"RaboutSlice: main1:{segmentID1Main}, minor1:{segmentID1Minor}, " +
            //    $"main2:{segmentID2Main}, minor2:{segmentID2Minor},");

            bool ignoreAll = IsSplit(segmentID1Minor, segmentID2Minor);
            if (ignoreAll) {
                //Log.Debug("RaboutSlice: Ignoring Split");
                return;
            }

            NetInfo eInfo = info.GetElevated();
            corner1 = new Corner(segmentID1Main, segmentID1Minor, eInfo.m_halfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, eInfo.m_halfWidth);
            ignoreAll = corner1.Ignore & corner2.Ignore;
            ignoreAll = ignoreAll || !CalculateMiddlePoint();
            ignoreAll = ignoreAll || IsBetweenInOut(segmentID1Main, segmentID1Minor, segmentID2Main, segmentID2Minor);
            if (ignoreAll) {
                //Log.Debug($"RaboutSlice: returns silently - {corner1.Ignore}  {corner2.Ignore} ");
                return;
            }

            nodeM = new NodeWrapper(MiddlePoint, 10, eInfo);
            if (corner1.Ignore) {
                node1 = null;
                segment1 = null;
            } else if (Len1 > 2.5f * MPU) {
                node1 = new NodeWrapper(corner1.Point, 0, eInfo);
                segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.DirMain);
            } else if (Len1 > 1f * MPU) {
                node1 = new NodeWrapper(corner1.AltPoint, 0, eInfo);
                segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.EndDirMinor);
            } else {
                node1 = new NodeWrapper(corner1.AltPoint, 0, eInfo);
                segment1 = new SegmentWrapper(nodeM, node1);
            }

            if (corner2.Ignore) {
                node2 = null;
                segment2 = null;
            } else if (Len2 > 2.5f * MPU) {
                node2 = new NodeWrapper(corner2.Point, 0, eInfo);
                segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.DirMain);
            } else if (Len2 > 1f * MPU) {
                node2 = new NodeWrapper(corner2.AltPoint, 0, eInfo);
                segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.EndDirMinor);
            } else {
                node2 = new NodeWrapper(corner2.AltPoint, 0, eInfo);
                segment2 = new SegmentWrapper(nodeM, node2);
            }
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