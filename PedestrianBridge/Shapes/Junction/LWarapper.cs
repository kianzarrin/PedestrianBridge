namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;

    using KianCommons.Math;
    using static KianCommons.Math.MathUtil;
    using static KianCommons.NetUtil;
    using static KianCommons.HelpersExtensions;
    using KianCommons;

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
            DoubleControlPoint2D CornerPoint;
            ControlPoint2D CornerPoint1, CornerPoint2;// Dir1=tangent, Dir2=normal

            internal ControlPoint2D Point1, Point2;

            internal ushort JunctionID;

            // segment has pedestrian paths and is ground road
            internal bool CanConnectPath1;
            internal bool CanConnectPath2;

            // the other node is on the ground and at least one segment connected to it has pedestrian paths.
            internal bool CanConnectPathAtOtherNode1;
            internal bool CanConnectPathAtFinalNode2;

            internal ushort FinalNodeID1, FinalNodeID2;

            internal Calc(ushort segID1, ushort segID2, float HWpb) {
                //Log.Debug($"LWrapper.Calc: {segID1}, {segID2}");
                // Prepration:
                ref NetSegment seg1 = ref segID1.ToSegment();
                ref NetSegment  seg2 = ref segID2.ToSegment();
                float HW1 = seg1.Info.m_halfWidth;
                float HW2 = seg2.Info.m_halfWidth;
                JunctionID = seg1.GetSharedNode(segID2);
                ref NetNode junction = ref JunctionID.ToNode();
                Vector2D origin = junction.m_position.To2D();
                bool bStartNode1 = seg1.m_startNode == JunctionID;
                bool bStartNode2 = seg2.m_startNode == JunctionID;

                // the code is written for seg2 CCW WRT seg1.
                // if CW, some values need inversion.
                bool isCW = seg2.GetRightSegment(JunctionID) == segID1;
                //bool isCCW = seg1.GetRightSegment(JunctionID) == segID2;
                bool isCCW = !isCW; 
                //Assert(isCCW ^ isCW, $"isCCW ^ isCW: {isCCW} ^ {isCW}"); // if there are only two segments, this assertion will fail.

                var cornerPoint1 = CalculateCorner(segID1, JunctionID, isCW);
                CornerPoint1 = cornerPoint1.ControlPoint1;
                CornerPoint1.Point += cornerPoint1.Dir2 * HWpb;

                var cornerPoint2 = CalculateCorner(segID2, JunctionID, !isCW);
                CornerPoint2 = cornerPoint2.ControlPoint2;
                CornerPoint2.Point += cornerPoint2.Dir2 * HWpb;





                float angle = VectorUtil.SignedAngleRadCCW(CornerPoint1.Dir, CornerPoint2.Dir);
                if (isCW) angle = -angle;
                float ratio = 1f / Mathf.Sin(angle); // TODO optimize
                bool parallel = VectorUtil.AreApprox180(CornerPoint1.Dir, CornerPoint2.Dir);
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
                        CornerPoint1.Point = origin + d2* CornerPoint.Dir2

                        CornerPoint.Point = d1 * CornerPoint.Dir1 + d2 * CornerPoint.Dir2;
                        CornerPoint.Point += origin;
                    } else {
                        angle = VectorUtil.SignedAngleRadCCW(CornerPoint.Dir1, CornerPoint.Dir2);
                        if (isCW) angle = -angle;
                        ratio = 1f / Mathf.Sin(angle);
                        CornerPoint1 = CalculateCorner(segID1, JunctionID, isCW);
                        CornerPoint2 = CalculateCorner(segID2, JunctionID, !isCW);
                        float d = HWpb * ratio + SAFETY_NET;
                        CornerPoint1.Point += d * CornerPoint2.Dir;
                        CornerPoint2.Point += d * CornerPoint1.Dir;
                        CornerPoint.Point = (CornerPoint1.Point + CornerPoint2.Point) / 2;
                    }
                } else {
                    CornerPoint = new DoubleControlPoint2D {
                        Dir1= cornerPoint1.Dir1;;
                    };
                    Vector2D normal = CornerPoint.Dir1.Rotate90CCW();
                    if (isCW) normal = -normal;
                    float HW = Mathf.Max(HW1, HW2);
                    CornerPoint.Point = (HW + HWpb + SAFETY_NET) * normal;
                    CornerPoint.Point += origin;
                    CornerPoint1 = CornerPoint2 = default;

                }


                // TODO push targetLength calculations to above if-else code.
                const float extend0 = 1 * MPU; // for angles > 180 length should increase.
                float targetLength = ControlCenter.BaseLength;
                if (!parallel && angle < 0) targetLength += extend0; // TODO this should be based on incomming angles.
                else if (!parallel && angle < Mathf.PI) {
                    float dot = Vector2D.Dot(CornerPoint.Dir1, CornerPoint.Dir2);

                    // Note sin(angle) does not work so good here.
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
                    float t = bezier3D.GetClosestT(CornerPoint.Point.ToCS3D(bezier3D.a.Height()));
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
                    float t = bezier3D.GetClosestT(CornerPoint.Point.ToCS3D(bezier3D.a.Height()));
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="bezier"></param>
            /// <param name="bLeft">left side when going toward <paramref name="finalNodeId"/></param>
            /// <param name="sideDistance"></param>
            /// <param name="distance"></param>
            /// <param name="finalSegmentId"></param>
            /// <param name="finalNodeId"></param>
            /// <param name="point"></param>
            /// <param name="tangent"></param>
            /// <param name="level"></param>
            static void Travel(
                Bezier2 bezier,
                bool bLeft, float sideDistance, float distance,
                ref ushort finalSegmentId, ref ushort finalNodeId,
                out Vector2D point, out Vector2D tangent,
                int level=2
                ) {
                Log.Debug($"    Travel(bLeft:{bLeft},sideDistance:{sideDistance},distance:{distance}," +
                    $"segmentId(in):{finalSegmentId},finalNodeID(in):{finalNodeId}, level={level})");
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
                        Log.Debug("    ContinueToNextSegment " + finalSegmentId);
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

                if (!CanConnectPathToSegment(finalSegmentId)) {
                    Log.Debug($"    could not connect path to final segment. calculating next corner ...");
                    // direction from this segment point from next segment
                    NetUtil.CalculateCorner(finalSegmentId, finalNodeId, bLeft, out _, out tangent);
                    tangent = -tangent;
                    finalSegmentId = bLeft ?
                        finalSegmentId.ToSegment().GetLeftSegment(finalNodeId) :
                        finalSegmentId.ToSegment().GetRightSegment(finalNodeId);
                    NetUtil.CalculateCorner(finalSegmentId, finalNodeId, !bLeft, out point, out _);
                } else {
                    distance = Mathf.Clamp(distance, 1f, length - 2); // avoid getting to close to bezier end.
                    point = bezierParallel.Travel2(distance, out tangent);
                    if (DistanceToNodeEdge(point, finalNodeId) - ControlCenter.HalfWidth < -2*SAFETY_NET) {
                        Log.Debug($"    point is inside node. calculating corner ...");
                        NetUtil.CalculateCorner(finalSegmentId, finalNodeId, bLeft, out point, out tangent);
                        tangent = -tangent;
                    }
                }
                Log.Debug($"    distance={distance} length={length} return segmentId:{finalSegmentId},finalNodeID:{finalNodeId} point={point} tangent={tangent}");

            }

            // negative if the point is inside the node.
            static float DistanceToNodeEdge(Vector2D point, ushort nodeID) {
                float dist = (point - nodeID.ToNode().m_position.To2D()).magnitude;
                return dist - NetUtil.MaxNodeHW(nodeID);
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

        public LWrapper(ushort segmentID1, ushort segmentID2) {
            var calc = new Calc(segmentID1, segmentID2, ControlCenter.HalfWidth);
            nodeL = node1 = node2 = null;
            segment1 = segment2 = null;

            bool create1 = calc.CanConnectPathAtOtherNode1 && calc.CanConnectPath1;
            bool create2 = calc.CanConnectPathAtFinalNode2 && calc.CanConnectPath2;
            if (!create1 && !create2) {
                return;
            }

            nodeL = new NodeWrapper(calc.CornerPoint.Point, ControlCenter.Elevation);

            if (create1) {
                node1 = new NodeWrapper(calc.Point1, 0);
                segment1 = new SegmentWrapper(
                    nodeL, node1,
                    calc.CornerPoint.Dir1, calc.EndDir1);
            }

            if (create2) {
                node2 = new NodeWrapper(calc.Point2, 0);
                segment2 = new SegmentWrapper(
                    nodeL, node2,
                    calc.CornerPoint.Dir2, calc.EndDir2);
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
