namespace PedestrianBridge.Shapes {
    using ColossalFramework.Math;
    using ColossalFramework;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;
    using static Util.VectorUtil;

    public class RaboutSlice {
        public struct Corner {
            internal LWrapper.Calc L { get; private set; }
            // Output:
            internal Vector2 Point => L.PointL;
            internal Vector2 AltPoint => L.Point2;

            /// DirMain first represents the direction of the main segment at the junction.
            /// but then it is modified to represent the direction of the pedestrian bridge
            /// slightly away from the junction.
            internal Vector2 DirMain => L.CornerDir1;

            internal Vector2 DirMinor => L.CornerDir2;
            internal Vector2 EndDirMinor => L.EndDir2;

            internal bool CanConnectPathAtJunction => L.CanConnectPath2;
            internal bool CanConnectPathAtFinalNode => L.CanConnectPathAtFinalNode2;
            internal ushort FinalNodeID => L.FinalNodeID2;

            public Corner(ushort segmentMainID, ushort segmentMinorID, float HWpath) {
                L = new LWrapper.Calc(segmentMainID, segmentMinorID, HWpath);
            }
        }

        Corner corner1, corner2;
        Vector2 MiddlePoint;
        Vector2 MDir1, MDir2;
        float Len1 => (MiddlePoint - corner1.Point).magnitude;
        float Len2 => (MiddlePoint - corner2.Point).magnitude;

        // returns false if angle>180
        bool CalculateMiddlePoint() {
            Vector2 v21 = corner2.Point - corner1.Point;
            float angle1 = VectorUtil.UnsignedAngleRad(v21, corner1.DirMain); // expected Accute
            float angle2 = VectorUtil.UnsignedAngleRad(-v21, corner2.DirMain); // expected Accute
            float c = Mathf.PI * .5f - Epsilon;
            if (angle1>=c || angle2>= c) {
                Log.Debug("Roundabout angle >= 180");
                return false;
            }

            var b2 = BezierUtil.Bezier2ByDir(
                corner1.Point, corner1.DirMain,
                corner2.Point, corner2.DirMain);

            MiddlePoint = b2.Travel2(b2.ArcLength() * .5f, out MDir2);
            MDir1 = -MDir2;

            return true;
        }

        bool IsSplit => corner1.FinalNodeID == corner2.FinalNodeID;

        static bool IsIntersectionOnGround(ushort segmentID1, ushort segmentID2) {
            ref NetNode node = ref GetSharedNode(segmentID1, segmentID2).ToNode();
            //return node.m_flags.IsFlagSet(NetNode.Flags.OnGround);
            return node.Info.m_netAI is RoadAI; // fix Roundabout builder.
        }

        // typicall in highway connections 
        bool IsBetweenInOut(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor) {
            Util.HelpersExtensions.AssertStack();
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

        public const float MIN_LEN = 2.5f;
        public const float DESIRED_LEN = 3.2f;
        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor,
            NodeWrapper centerNode, NetInfo info) {
            Log.Debug($"RaboutSlice: main1:{segmentID1Main}, minor1:{segmentID1Minor}, " +
                $"main2:{segmentID2Main}, minor2:{segmentID2Minor},");

            bool onGround =
                IsIntersectionOnGround(segmentID1Main, segmentID1Minor) &
                IsIntersectionOnGround(segmentID2Main, segmentID2Minor);
            if (!onGround) {
                Log.Debug($"this RaboutSlice is ignored because its not on the ground.");
                return;
            }

            NetInfo eInfo = info.GetElevated();
            corner1 = new Corner(segmentID1Main, segmentID1Minor, eInfo.m_halfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, eInfo.m_halfWidth);
            if (IsSplit)
                return;
            bool angleTooWide = !CalculateMiddlePoint();
            bool ignoreAll = angleTooWide || IsBetweenInOut(segmentID1Main, segmentID1Minor, segmentID2Main, segmentID2Minor);
            if (ignoreAll) {
                Log.Debug($"this RaboutSlice is ignored. angleTooWide={angleTooWide} ");
                return;
            }

            nodeM = new NodeWrapper(MiddlePoint, 10, eInfo);

            float len1 = Len1;
            Log.Debug($"len1={len1} corner1.CanConnectPathAtJunction={corner1.CanConnectPathAtJunction}");
            if ((len1 > MIN_LEN * MPU) && corner1.CanConnectPathAtJunction) {
                node1 = new NodeWrapper(corner1.Point, 0, eInfo);
                segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.DirMain);
            } else if (corner1.CanConnectPathAtFinalNode) {
                if (len1 > 1f * MPU) {
                    node1 = new NodeWrapper(corner1.AltPoint, 0, eInfo);
                    segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.EndDirMinor);
                } else {
                    node1 = new NodeWrapper(corner1.AltPoint, 0, eInfo);
                    segment1 = new SegmentWrapper(nodeM, node1);
                }
            }

            float len2 = Len2;
            Log.Debug($"len2={len2} corner2.CanConnectPathAtJunction={corner2.CanConnectPathAtJunction}");
            if ((len2 > MIN_LEN * MPU) && corner2.CanConnectPathAtJunction) {
                node2 = new NodeWrapper(corner2.Point, 0, eInfo);
                segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.DirMain);
            } else if (corner2.CanConnectPathAtFinalNode) {
                if (len2 > 1f * MPU) {
                    node2 = new NodeWrapper(corner2.AltPoint, 0, eInfo);
                    segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.EndDirMinor);
                } else {
                    node2 = new NodeWrapper(corner2.AltPoint, 0, eInfo);
                    segment2 = new SegmentWrapper(nodeM, node2);
                }
            }

            if (segment1 == null && segment2 == null) {
                nodeM = null;
            } else if (segment1 == null && len2 < DESIRED_LEN && len2*2 >= DESIRED_LEN)  {
                // if space is tight and segment1 is null then
                // make space by moving nodeM to node1 if that would be enough.
                nodeM.point = corner1.Point;
                node2.point = corner2.Point;
                segment2.startDir = corner1.DirMain;
                segment2.endDir = corner2.DirMain;
            } else if (segment2 == null && len1 < DESIRED_LEN && len1 * 2 >= DESIRED_LEN) {
                // if space is tight and segment2 is null then
                // make space by moving nodeM to node1 if that would be enough.
                nodeM.point = corner2.Point;
                node1.point = corner1.Point;
                segment1.startDir = corner2.DirMain;
                segment1.endDir = corner1.DirMain;
            }

            if (nodeM != null) {
                segment3 = new SegmentWrapper(nodeM, centerNode);
                this.centerNode = centerNode;
            }
        }

        public bool IsValid => nodeM != null;
        public NodeWrapper nodeM;
        NodeWrapper node1;
        NodeWrapper node2;
        NodeWrapper centerNode;

        SegmentWrapper segment1;
        SegmentWrapper segment2;
        SegmentWrapper segment3;

        public void Create() {
            //Log.Debug($"{nodeM != null} {node1 != null} {node2 != null} {segment1 != null} {segment2 != null} {segment3 != null}");
            //if (centerNode != null && !centerNode.IsCreated)
            //    centerNode.Create(); // TODO fix Create action already exists but not created yet
            nodeM?.Create();
            node1?.Create();
            node2?.Create();
            segment1?.Create();
            segment2?.Create();
            segment3?.Create();
        }
    }
}