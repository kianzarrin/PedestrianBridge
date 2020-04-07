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

                float offset1 = 0;
                float offset2 = 0;
                const float extend0 = 1 * MPU; // if angle is too wide, length should increase.
                if (!parallel) {
                    if (angle < 0) {
                        float d1 = (HW2 + HWpb) * ratio + SAFETY_NET;
                        float d2 = (HW1 + HWpb) * ratio + SAFETY_NET;
                        PointL = d1 * CornerDir1 + d2 * CornerDir2;
                        PointL += origin;
                        offset1 = d1 + d2 * Vector2.Dot(CornerDir1, CornerDir2);
                        offset2 = d2 + d1 * Vector2.Dot(CornerDir1, CornerDir2);
                    } else {
                        angle = VectorUtil.SignedAngleRadCCW(CornerDir1, CornerDir2);
                        if (isCW) angle = -angle;
                        ratio = 1f / Mathf.Sin(angle);
                        CalculateCorner(segID1, JunctionID, isCW, out var PointL1, out CornerDir1);
                        CalculateCorner(segID2, JunctionID, !isCW, out var PointL2, out CornerDir2);
                        PointL = (PointL1 + PointL2) / 2;
                        float d = HWpb * ratio + SAFETY_NET;
                        PointL += d * CornerDir1 + d * CornerDir2;

                        offset1 = Vector2.Dot(PointL - origin, CornerDir1);
                        offset2 = Vector2.Dot(PointL - origin, CornerDir2);
                        //Log.Debug($"offset1={offset1} offset2={offset2}");
                    }
                } else {
                    Vector2 normal = CornerDir1.Rotate90CCW();
                    if (isCW) normal = -normal;
                    float HW = Mathf.Max(HW1, HW2);
                    PointL = (HW + HWpb + SAFETY_NET) * normal;
                    PointL += origin;
                }



                Point1 = Point2 = EndDir1 = EndDir2 = default;

                const float lengthError = 0.5f;
                const float weight = 0.5f; // reduce weigth for the results to converge.
                float distance_prev = 0, diff_prev = 0;

                #region point1
                float targetLength = DEFAULT_LENGTH;
                if (!parallel && angle < Epsilon) targetLength += extend0;
                float distance = targetLength + offset1;
                Log.Debug($"1: targetLength={targetLength} distance={distance}");
                for (int counter = 0; counter < 10; ++counter) {
                    Travel(
                        segID1,
                        JunctionID,
                        distance,
                        out Point1,
                        out Vector2 tangent,
                        out ushort  finalSegmentID,
                        out FinalNodeID1,
                        out bool overFlow,
                        out bool forcedEnd);
                    var normal = tangent.Rotate90CCW();
                    if (isCW) normal = -normal;
                    Point1 += (HW1 + HWpb + SAFETY_NET) * normal;
                    EndDir1 = -tangent;

                    if (FinalNodeID1.ToNode().CountSegments()>2)
                    {
                        //Log.Debug("FinalNodeID1.ToNode().CountSegments()>2");
                        // TODO move to end of loop.
                        // if we have moved too far into a corner
                        // then bring back Point1 into the corner.
                        Vector2 otherNodePoint = FinalNodeID1.ToNode().m_position.ToCS2D();
                        CalculateCorner(finalSegmentID, FinalNodeID1, isCCW,
                                        out Vector2 cornerPoint, out Vector2 cornerDir);
                        if ((otherNodePoint - cornerPoint).magnitude >
                            (Point1 - cornerPoint).magnitude) {
                            Point1 = cornerPoint;
                            Point1 += (HWpb + SAFETY_NET) * normal; // TODO this does not work for accute corners.
                            MovePointTowardOtherSegment(finalSegmentID, FinalNodeID1, isCCW, ref Point1);
                            EndDir1 = cornerDir;
                        }
                    }

                    float length = BezierUtil.Bezier2ByDir(PointL, CornerDir1, Point1, EndDir1).ArcLength();
                    Log.Debug($"1: distance={distance} length={length}");
                    if (forcedEnd)
                        break;

                    float diff = length - targetLength;
                    float distance_next;
                    //Log.Debug($"diff={diff}");
                    if (counter == 0) {
                        distance_next = distance -  diff;
                    } else if (diff * diff_prev < 0) {
                        float w = diff_prev / diff;
                        w = -w;
                        w = 1f / (1f + w); // prevDiff α 1/w
                        distance_next = (1-w) * distance + w * distance_prev;
                    } else {
                        distance_next = (1 - weight) * distance + weight * distance*targetLength/length;
                    }
                    diff_prev = diff;
                    distance_prev = distance;
                    distance = distance_next;
                    

                    if (overFlow && diff > 0) {
                        continue;
                    }else if (overFlow || Mathf.Abs(diff) < lengthError) 
                        break;
                    
                }
                #endregion

                #region point2
                targetLength = DEFAULT_LENGTH;
                if (!parallel && angle < 0) targetLength += extend0;

                distance = targetLength + offset2;
                for (int counter = 0; counter < 10; ++counter) {
                    Travel(
                        segID2,
                        JunctionID,
                        distance,
                        out Point2,
                        out Vector2 tangent,
                        out ushort finalSegmentID,
                        out FinalNodeID2,
                        out bool overFlow,
                        out bool forcedEnd);
                    var normal = tangent.Rotate90CW();
                    if (isCW) normal = -normal;
                    Point2 += (HW2 + HWpb + SAFETY_NET) * normal;
                    EndDir2 = -tangent;

                    if (FinalNodeID2.ToNode().CountSegments() > 2)
                    {
                        // TODO move to end of loop.
                        // if we have moved too far into a corner
                        // then bring back Point1 into the corner.
                        Vector2 otherNodePoint = FinalNodeID2.ToNode().m_position.ToCS2D();
                        CalculateOtherCorner(finalSegmentID, FinalNodeID2, isCW,
                                        out Vector2 cornerPointB, out Vector2 cornerDir);
                        if (forcedEnd || (otherNodePoint - cornerPointB).magnitude >
                            (Point2 - cornerPointB).magnitude) {
                            Point2 = cornerPointB;
                            Point2 += (HWpb + SAFETY_NET) * normal; // TODO this does not work for accute corners.

                        CalculateCorner(finalSegmentID, FinalNodeID2, isCW,
                            out Vector2 cornerPointA, out Vector2 cornerDirA);
                            EndDir2 = cornerDirA;
                        }
                    }


                    float length = BezierUtil.Bezier2ByDir(PointL, CornerDir2, Point2, EndDir2).ArcLength();
                    //Log.Debug($"length2={length}");
                    if (forcedEnd)
                        break;


                    float diff = length - targetLength;
                    float distance_next;
                    //Log.Debug($"diff={diff}");
                    if (counter == 0) {
                        distance_next = distance - diff;
                    } else if (diff * diff_prev < 0) {
                        float w = diff_prev / diff;
                        w = -w;
                        w = 1f / (1f + w); // prevDiff α 1/w
                        distance_next = (1 - w) * distance + w * distance_prev;
                    } else {
                        distance_next = (1 - weight) * distance + weight * distance * targetLength / length;
                    }
                    diff_prev = diff;
                    distance_prev = distance;
                    distance = distance_next;

                    if (overFlow && diff > 0) {
                        continue;
                    } else if (overFlow || Mathf.Abs(diff) < lengthError)
                        break;
                }
                #endregion
                CanConnectPathAtOtherNode1 = CanConnectPathAtNode(FinalNodeID1);
                CanConnectPathAtFinalNode2 = CanConnectPathAtNode(FinalNodeID2);
            }

            // move the point toward the segment with pedestrian lane.
            // TODO does not work for 180 angle if the two segments have different half widths.
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

            // returns false if overflow or forceEnd
            // returns true otherwise.
            internal static void Travel(
                ushort segmentId,
                ushort nodeId,
                float distance,
                out Vector2 point,
                out Vector2 tangent,
                out ushort finalSegmentID,
                out ushort finalOtherNodeID,
                out bool overFlow,
                out bool forcedEnd, // result has been forced to continue to the node end because segment does not have pedestrian lanes.
                int level=2) {
                Bezier2 bezier = CalculateSegmentBezier2(segmentId, nodeId);
                float length = bezier.ArcLength();
                ushort otherNodeId = segmentId.ToSegment().GetOtherNode(nodeId);

                bool forceEnd = !CanConnectPathToSegment(segmentId) && level > 0;
                overFlow = distance > length;
                if (overFlow || forceEnd) {
                    ushort nextSegmentId = ContinueToNextSegment(segmentId, otherNodeId);
                    if (nextSegmentId != 0) {
                        //Log.Debug("ContinueToNextSegment");
                        Travel(
                            nextSegmentId,
                            otherNodeId,
                            distance-length,
                            out point,
                            out tangent,
                            out finalSegmentID,
                            out finalOtherNodeID,
                            out overFlow,
                            out forcedEnd,
                            level-1);
                        return;
                    }
                    //Log.Debug("distance > length || forceEnd but could not find next segment");
                }

                forcedEnd = !CanConnectPathToSegment(segmentId);
                finalSegmentID = segmentId;
                finalOtherNodeID = otherNodeId;
                point = bezier.Travel2(distance, out tangent);
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
