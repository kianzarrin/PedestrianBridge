namespace PedestrianBridge.Shapes {
    using ColossalFramework.Math;
    using ColossalFramework;
    using System;
    using UnityEngine;
    using Util;
    using KianCommons;
    using KianCommons.Math;
    using static KianCommons.Math.MathUtil;
    using static KianCommons.NetUtil;
    using static KianCommons.Math.VectorUtil;
    using System.Collections.Generic;
    using System.Linq;

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
        public Vector2 MDir1, MDir2;
        IEnumerable<ushort> _segmentIDs;
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
            KianCommons.HelpersExtensions.AssertStack();
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

        public static ushort GetClosestSegment(Vector2 point, IEnumerable<ushort> segmentList, out Vector3 HitPos) {
            HitPos = Vector3.zero;
            float min_dist = float.MaxValue;
            ushort min_segmentID = 0;
            var pos = Get3DPos(point);
            foreach (ushort segmentID in segmentList) {
                // TODO, uncommnet for optimisation.
                //GridVector segmetnGrid = GridVector.CreateFromSegment(segmentID);
                //if ((segmetnGrid - posGrid).MangitudeSquare > 2)
                //    continue; // too far away.
                Vector3 HitPosCurrent = segmentID.ToSegment().GetClosestPosition(pos);
                var dist = (point - HitPosCurrent.ToCS2D()).sqrMagnitude;
                if (dist < min_dist) {
                    min_dist = dist;
                    min_segmentID = segmentID;
                    HitPos = HitPosCurrent;
                }
            }
            return min_segmentID;
        }

        public static Vector2 MirrorPoint(Vector2 point, IEnumerable<ushort> segmentList) {
            HelpersExtensions.AssertNotNull(segmentList, "segmentList");
            HelpersExtensions.Assert(segmentList.Count() > 0, "segmentList.Count()>0");
            ushort closestSegmentID = GetClosestSegment(point, segmentList, out Vector3 hitpos);
            if (closestSegmentID != 0) {
                var diff = hitpos.ToCS2D() - point;
                return hitpos.ToCS2D() + diff;
            } else {
                throw new Exception("unreachable code");
            }
        }

        public static NodeWrapper MirrorNode(NodeWrapper node, IEnumerable<ushort> segmentList) {
            var mirror_point = MirrorPoint(node.point, segmentList);
            return new NodeWrapper(mirror_point, node.elevation);
        }

        public const float MIN_LEN = 2.5f;
        public const float DESIRED_LEN = 3.2f;
        public RaboutSlice(
            ushort segmentID1Main, ushort segmentID1Minor,
            ushort segmentID2Main, ushort segmentID2Minor,
            IEnumerable<ushort> segmentIDs,
            NodeWrapper centerNode, NetInfo info) {
            Log.Debug($"RaboutSlice: main1:{segmentID1Main}, minor1:{segmentID1Minor}, " +
                $"main2:{segmentID2Main}, minor2:{segmentID2Minor},");
            this._segmentIDs = segmentIDs;
            bool onGround =
                IsIntersectionOnGround(segmentID1Main, segmentID1Minor) &
                IsIntersectionOnGround(segmentID2Main, segmentID2Minor);
            if (!onGround) {
                Log.Debug($"this RaboutSlice is ignored because its not on the ground.");
                return;
            }


            corner1 = new Corner(segmentID1Main, segmentID1Minor, ControlCenter.HalfWidth);
            corner2 = new Corner(segmentID2Main, segmentID2Minor, ControlCenter.HalfWidth);
            if (IsSplit)
                return;
            bool angleTooWide = !CalculateMiddlePoint();
            bool ignoreAll = angleTooWide || IsBetweenInOut(segmentID1Main, segmentID1Minor, segmentID2Main, segmentID2Minor);
            if (ignoreAll) {
                Log.Debug($"this RaboutSlice is ignored. angleTooWide={angleTooWide} ");
                return;
            }

            nodeM = new NodeWrapper(MiddlePoint, ControlCenter.Elevation);

            float len1 = Len1;
            Log.Debug($"len1={len1} corner1.CanConnectPathAtJunction={corner1.CanConnectPathAtJunction}");
            if ((len1 > MIN_LEN * MPU) && corner1.CanConnectPathAtJunction) {
                node1 = new NodeWrapper(corner1.Point, 0);
                segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.DirMain);
            } else if (corner1.CanConnectPathAtFinalNode) {
                if (len1 > 1f * MPU) {
                    node1 = new NodeWrapper(corner1.AltPoint, 0);
                    segment1 = new SegmentWrapper(nodeM, node1, MDir1, corner1.EndDirMinor);
                } else {
                    node1 = new NodeWrapper(corner1.AltPoint, 0);
                    segment1 = new SegmentWrapper(nodeM, node1);
                }
            }

            float len2 = Len2;
            Log.Debug($"len2={len2} corner2.CanConnectPathAtJunction={corner2.CanConnectPathAtJunction}");
            if ((len2 > MIN_LEN * MPU) && corner2.CanConnectPathAtJunction) {
                node2 = new NodeWrapper(corner2.Point, 0);
                segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.DirMain);
            } else if (corner2.CanConnectPathAtFinalNode) {
                if (len2 > 1f * MPU) {
                    node2 = new NodeWrapper(corner2.AltPoint, 0);
                    segment2 = new SegmentWrapper(nodeM, node2, MDir2, corner2.EndDirMinor);
                } else {
                    node2 = new NodeWrapper(corner2.AltPoint, 0);
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
                segment2.StartDir = corner1.DirMain;
                segment2.EndDir = corner2.DirMain;
            } else if (segment2 == null && len1 < DESIRED_LEN && len1 * 2 >= DESIRED_LEN) {
                // if space is tight and segment2 is null then
                // make space by moving nodeM to node1 if that would be enough.
                nodeM.point = corner2.Point;
                node1.point = corner1.Point;
                segment1.StartDir = corner2.DirMain;
                segment1.EndDir = corner1.DirMain;
            }

            if (nodeM == null)
                return;
            switch (ControlCenter.RoundaboutBridgeStyle) {
                case RoundaboutBridgeStyleT.Star:
                    segment3 = new SegmentWrapper(centerNode, nodeM);
                    this.centerNode = centerNode;
                    float center_h = centerNode.Get3DPos().Height();
                    float middle_h = nodeM.Get3DPos().Height();
                    if (ControlCenter.Underground) {
                        if (center_h > middle_h)
                            centerNode.elevation += (byte)Mathf.RoundToInt(middle_h - center_h);
                    } else {
                        if (center_h < middle_h)
                            centerNode.elevation += (byte)Mathf.RoundToInt(middle_h - center_h);
                    }
                    break;
                case RoundaboutBridgeStyleT.InnerCircle:
                    centerNode = null;
                    nodeM_mirrored = MirrorNode(nodeM, _segmentIDs); // calculate based on corner1.L.pointL1 and corner2.L.pointL1
                    var point1_mirrored = MirrorPoint(corner1.Point, segmentIDs);
                    node1_mirrored = new NodeWrapper(point1_mirrored, ControlCenter.Elevation); // TODO use corner1.L.pointL1 for more accurate circle.
                    var point2_mirrored = MirrorPoint(corner2.Point, segmentIDs);
                    node2_mirrored = new NodeWrapper(point2_mirrored, ControlCenter.Elevation);

                    segment3 = new SegmentWrapper(nodeM_mirrored, nodeM);
                    segment_circle1 = new SegmentWrapper(nodeM_mirrored, node1_mirrored, MDir1, corner1.DirMain);
                    segment_circle2 = new SegmentWrapper(nodeM_mirrored, node2_mirrored, MDir2, corner2.DirMain);
                    break;
                case RoundaboutBridgeStyleT.OuterCircle:
                    // move node1 node2
                    // segment1 and segment2 nodeM dir has to rotate 60 degrees.
                    break;
            }
        }

        public bool IsValid => nodeM != null;
        public NodeWrapper nodeM;
        NodeWrapper node1;
        NodeWrapper node2;
        NodeWrapper centerNode;
        public NodeWrapper nodeM_mirrored;
        NodeWrapper node1_mirrored;
        NodeWrapper node2_mirrored;

        SegmentWrapper segment1;
        SegmentWrapper segment2;
        SegmentWrapper segment3; // connected to center or mirror
        public SegmentWrapper segment_circle1;
        public SegmentWrapper segment_circle2;

        public void Create() {
            //Log.Debug($"{nodeM != null} {node1 != null} {node2 != null} {segment1 != null} {segment2 != null} {segment3 != null}");
            //if (centerNode != null && !centerNode.IsCreated)
            //    centerNode.Create(); // TODO fix Create action already exists but not created yet

            var Nodes = new[] { nodeM, node1, node2, nodeM_mirrored, node1_mirrored, node2_mirrored };
            var Segments = new[] { segment1, segment2, segment3, segment_circle1, segment_circle2 };
            foreach (var node in Nodes) node?.Create();
            foreach (var segment in Segments) segment?.Create();
        }
    }
}