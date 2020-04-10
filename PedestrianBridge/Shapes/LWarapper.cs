namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;
    using static Util.HelpersExtensions;

    /* TODO:
     *  - fix distance convergance DONE!
     *  - extend segment with angle>180 DONE!
     *  - fix Travel such that for straight line delta(input)=delta(output) DONE!
     *  - copy code to point 2 DONE1
     *  - optimisation: Travel along side bezier.
     *  - more optimisations
     */

    public class LWrapper {
        internal struct Calc {
            // Output:
            internal Vector2 Point1, PointL, Point2;
            //internal Vector2 StartDir1, StartDir2;
            internal Vector2 EndDir1, EndDir2;
            internal Vector2 CornerDir1, CornerDir2;
            internal ushort JunctionID;

            // segment has pedestrian paths and is ground road
            internal bool CanConnectPath1;
            internal bool CanConnectPath2;

            // the other node is on the ground and at least one segment connected to it has pedestrian paths.
            internal bool CanConnectPathAtOtherNode1;
            internal bool CanConnectPathAtFinalNode2;

            internal ushort FinalNodeID1, FinalNodeID2;

            public const float DEFAULT_LENGTH = 3 * MPU;

            internal Calc(ushort segID1, ushort segID2, float HWpb) {
                //Log.Debug($"LWrapper.Calc: {segID1}, {segID2}");
                // Prepration:
                ref NetSegment seg1 = ref segID1.ToSegment();
                ref NetSegment  seg2 = ref segID2.ToSegment();
                float HW1 = seg1.Info.m_halfWidth;
                float HW2 = seg2.Info.m_halfWidth;
                JunctionID = seg1.GetSharedNode(segID2);
                ref NetNode junction = ref JunctionID.ToNode();
                Vector2 origin = junction.m_position.ToCS2D();
                bool bStartNode1 = seg1.m_startNode == JunctionID;
                bool bStartNode2 = seg2.m_startNode == JunctionID;
                Vector2 V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToCS2D();
                Vector2 V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToCS2D();
                CornerDir1 = V1.normalized;
                CornerDir2 = V2.normalized;

                // the code is written for seg2 CCW WRT seg1.
                // if CW, some values need inversion.
                bool isCW = seg2.GetRightSegment(JunctionID) == segID1;
                //bool isCCW = seg1.GetRightSegment(JunctionID) == segID2;
                bool isCCW = !isCW; // 
                //Assert(isCCW ^ isCW, $"isCCW ^ isCW: {isCCW} ^ {isCW}"); // if there are only two segments, this assertion will fail.

                float angle = VectorUtil.SignedAngleRadCCW(CornerDir1, CornerDir2);
                if (isCW) angle = -angle;
                float ratio = 1f / Mathf.Sin(angle);
                bool parallel = VectorUtil.AreApprox180(CornerDir1, CornerDir2);
                //Log.Debug($"parallel={parallel} angle={angle} ratio={ratio}");

                ////////////////////////////////////////////////////////////////
                // Main calculations
                CanConnectPath1 = seg1.CanConnectPath();
                CanConnectPath2 = seg2.CanConnectPath();

                FinalNodeID1 = seg1.GetOtherNode(JunctionID);
                FinalNodeID2 = seg2.GetOtherNode(JunctionID);

                if (!parallel) {
                    if (angle < 0) {
                        float d1 = (HW2 + HWpb) * ratio + SAFETY_NET;
                        float d2 = (HW1 + HWpb) * ratio + SAFETY_NET;
                        PointL = d1 * CornerDir1 + d2 * CornerDir2;
                        PointL += origin;
                    } else {
                        angle = VectorUtil.SignedAngleRadCCW(CornerDir1, CornerDir2);
                        if (isCW) angle = -angle;
                        ratio = 1f / Mathf.Sin(angle);
                        CalculateCorner(segID1, JunctionID, isCW, out var PointL1, out CornerDir1);
                        CalculateCorner(segID2, JunctionID, !isCW, out var PointL2, out CornerDir2);
                        PointL = (PointL1 + PointL2) / 2;
                        float d = HWpb * ratio + SAFETY_NET;
                        PointL += d * CornerDir1 + d * CornerDir2;
                    }
                } else {
                    Vector2 normal = CornerDir1.Rotate90CCW();
                    if (isCW) normal = -normal;
                    float HW = Mathf.Max(HW1, HW2);
                    PointL = (HW + HWpb + SAFETY_NET) * normal;
                    PointL += origin;
                }


                const float extend0 = 1 * MPU; // for angles > 180 length should increase.
                float targetLength = DEFAULT_LENGTH;
                if (!parallel && angle < 0) targetLength += extend0; // TODO this should be based on incomming angles.
                else if (!parallel && angle < Mathf.PI) {
                    float dot = Vector2.Dot(CornerDir1, CornerDir2);
                    targetLength += HWpb / (1-dot);
                }
                Log.Debug($"targetLength={targetLength}");
                {
                    ushort finalSegmentID = segID1;
                    FinalNodeID1 = seg1.GetOtherNode(JunctionID);

                    // calculate starting bezier
                    Bezier3 bezier3D = seg1.CalculateSegmentBezier3();
                    if (IsStartNode(finalSegmentID, FinalNodeID1))
                        bezier3D = bezier3D.Invert();
                    float t = bezier3D.GetClosestT(PointL.ToCS3D(bezier3D.a.Height()));
                    bezier3D = bezier3D.Cut(t, 1);
                    Bezier2 bezier = bezier3D.ToCSBezier2();

                    Travel(
                        bezier: bezier,
                        bLeft: isCCW,
                        sideDistance: HW1 + HWpb + 1,
                        distance: targetLength,
                        finalSegmentId: ref finalSegmentID,
                        finalNodeId: ref FinalNodeID1,
                        point: out Point1,
                        tangent: out var tangent);
                    EndDir1 = -tangent;
                    CanConnectPathAtOtherNode1 = CanConnectPathAtNode(FinalNodeID1);
                }

                {
                    ushort finalSegmentID = segID2;
                    FinalNodeID2 = seg2.GetOtherNode(JunctionID);

                    // calculate starting bezier
                    Bezier3 bezier3D = seg2.CalculateSegmentBezier3();
                    if (IsStartNode(finalSegmentID, FinalNodeID2))
                        bezier3D = bezier3D.Invert();
                    float t = bezier3D.GetClosestT(PointL.ToCS3D(bezier3D.a.Height()));
                    bezier3D = bezier3D.Cut(t, 1);
                    Bezier2 bezier = bezier3D.ToCSBezier2();


                    Travel(
                        bezier: bezier,
                        bLeft: !isCCW,
                        sideDistance: HW2 + HWpb + 1,
                        distance: targetLength,
                        finalSegmentId: ref finalSegmentID,
                        finalNodeId: ref FinalNodeID2,
                        point: out Point2,
                        tangent: out var tangent);
                    EndDir2 = -tangent;
                    CanConnectPathAtFinalNode2 = CanConnectPathAtNode(FinalNodeID2);
                }
            }


            // move the point toward the segment with pedestrian lane.
            internal static void MovePointTowardOtherSegment(ushort segmentID, ushort nodeId, bool bLeft, ref Vector2 Point) {
                ushort OtherSementID = bLeft ?
                    segmentID.ToSegment().GetLeftSegment(nodeId) :
                    segmentID.ToSegment().GetRightSegment(nodeId);
                //Log.Debug($"segmentID={segmentID} OtherSementID={OtherSementID} bLeft={bLeft}");

                ref NetSegment OtherSegment = ref OtherSementID.ToSegment();
                Vector2 OtherDir = IsStartNode(OtherSementID, nodeId) ?
                    OtherSegment.m_startDirection.ToCS2D().normalized :
                    OtherSegment.m_endDirection.ToCS2D().normalized;
                float otherHalfWidth = OtherSegment.Info.m_halfWidth;

                Point += (otherHalfWidth/2) * OtherDir;
            }

            // returns true if node has at least one that can connect to path.
            internal static bool CanConnectPathAtNode(ushort nodeID) {
                //if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.OnGround))
                //    return false;
                foreach(ushort segmentID in GetSegmentsCoroutine(nodeID)) {
                    if (segmentID.ToSegment().CanConnectPath())
                        return true;
                }
                return false;
            }


            static void Travel(
                Bezier2 bezier,
                bool bLeft, float sideDistance, float distance,
                ref ushort finalSegmentId, ref ushort finalNodeId,
                out Vector2 point, out Vector2 tangent,
                int level=2
                ) {
                Log.Debug($"    Travel(bLeft:{bLeft},sideDistance:{sideDistance},distance:{distance}," +
                    $"segmentId(in):{finalSegmentId},finalNodeID(in):{finalNodeId}), level={level}");
                ref NetSegment segment = ref finalSegmentId.ToSegment();
                Bezier2 bezierParallel = BezierUtil.CalculateParallelBezier(bezier, sideDistance, bLeft);
                float length = bezierParallel.ArcLength();


                bool forceEnd = !CanConnectPathToSegment(finalSegmentId) && level > 0;
                bool overFlow = distance > length;
                if (overFlow || forceEnd) {
                    ushort nextSegmentId = ContinueToNextSegment(finalSegmentId, finalNodeId);
                    if (nextSegmentId != 0) {
                        finalSegmentId = nextSegmentId;
                        bezier = CalculateSegmentBezier2(finalSegmentId, finalNodeId);
                        finalNodeId = finalSegmentId.ToSegment().GetOtherNode(finalNodeId);
                        Log.Debug("ContinueToNextSegment " + finalSegmentId);
                        Travel(
                            bezier,
                            bLeft, sideDistance, distance - length,
                            ref finalSegmentId, ref finalNodeId,
                            out point, out tangent,
                            level-1);
                        return;
                    }
                    Log.Debug("    overFlow || forceEnd but could not find next segment");
                }

                distance = Mathf.Clamp(distance, 1f, length - 2);
                point = bezierParallel.Travel2(distance, out tangent);
                Log.Debug($"    distance={distance} length={length} return segmentId:{finalSegmentId},finalNodeID:{finalNodeId} point={point} tangent={tangent}");
            }

            internal static ushort ContinueToNextSegment(ushort segmentId, ushort nodeId) {
                ref NetSegment seg = ref segmentId.ToSegment();
                ref NetNode node = ref nodeId.ToNode();

                if (node.CountSegments()!=2) {
                    return 0;
                }
                for(int i=0; i<8; ++i) {
                    ushort nextSegmentID = node.GetSegment(i);
                    if (nextSegmentID != 0 && nextSegmentID != segmentId)
                        return nextSegmentID;
                }
                throw new Exception("Unreachable code");
            }

        }

        public bool Valid { get; private set;}

        public LWrapper(ushort segmentID1, ushort segmentID2, NetInfo info) {
            NetInfo eInfo = info.GetElevated();
            var calc = new Calc(segmentID1, segmentID2, eInfo.m_halfWidth);
            nodeL = node1 = node2 = null;
            segment1 = segment2 = null;

            bool create1 = calc.CanConnectPathAtOtherNode1 && calc.CanConnectPath1;
            bool create2 = calc.CanConnectPathAtFinalNode2 && calc.CanConnectPath2;
            if (!create1 && !create2) {
                return;
            }

            nodeL = new NodeWrapper(calc.PointL, 10, eInfo);

            if (create1) {
                node1 = new NodeWrapper(calc.Point1, 0, eInfo);
                segment1 = new SegmentWrapper(nodeL, node1, calc.CornerDir1, calc.EndDir1);
            }

            if (create2) {
                node2 = new NodeWrapper(calc.Point2, 0, eInfo);
                segment2 = new SegmentWrapper(nodeL, node2, calc.CornerDir2, calc.EndDir2);
            }

            Valid = create1 | create2;
        }

        public NodeWrapper nodeL;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;

        public void Create() {
            nodeL?.Create();
            node1?.Create();
            node2?.Create();
            segment1?.Create();
            segment2?.Create();
        }
    }
}
