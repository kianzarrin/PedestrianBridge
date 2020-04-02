namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;
    using static Util.HelpersExtensions;

    public class LWrapper {
        internal struct Calc {
            // Output:
            internal Vector2 Point1, PointL, Point2;
            internal Vector2 StartDir1, EndDir1, StartDir2, EndDir2;
            internal ushort JunctionID;
            internal Vector2 ControlPoint1;

            // segment has pedestrian paths and junction is on ground
            internal bool CanConnectPathAtJunction1;
            internal bool CanConnectPathAtJunction2;

            // the other node is on the ground and at least one segment connected to it has pedestrian paths.
            internal bool CanConnectPathAtOtherNode1;
            internal bool CanConnectPathAtFinalNode2;

            internal ushort FinalNodeID1, FinalNodeID2;

            internal Calc(ushort segID1, ushort segID2, float HWpb) {
                Log.Debug($"LWrapper.Calc: {segID1}, {segID2}");
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
                StartDir1 = V1.normalized;
                StartDir2 = V2.normalized;

                // the code is written for seg2 CCW WRT seg1.
                // if CW, some values need inversion.
                bool isCCW = seg1.GetRightSegment(JunctionID) == segID2;
                bool isCW = seg2.GetRightSegment(JunctionID) == segID1;
                Assert(isCCW ^ isCW, $"isCCW ^ isCW: {isCCW} ^ {isCW}");

                float angle = VectorUtil.SignedAngleRadCCW(StartDir1, StartDir2);
                if (isCW) angle = -angle;
                float ratio = 1f / Mathf.Sin(angle);
                bool parallel = VectorUtil.AreApprox180(StartDir1, StartDir2);
                Log.Debug($"parallel={parallel} angle={angle} ratio={ratio}");

                ////////////////////////////////////////////////////////////////
                // Main calculations
                // boleans:
                CanConnectPathAtJunction1 = seg1.Info.m_hasPedestrianLanes &&
                                            JunctionID.ToNode().m_flags.IsFlagSet(NetNode.Flags.OnGround);
                CanConnectPathAtJunction2 = seg2.Info.m_hasPedestrianLanes &&
                                            JunctionID.ToNode().m_flags.IsFlagSet(NetNode.Flags.OnGround);

                FinalNodeID1 = seg1.GetOtherNode(JunctionID);
                FinalNodeID2 = seg2.GetOtherNode(JunctionID);
                Log.Debug($"DEBUG> FinalNodeID1={FinalNodeID1} FinalNodeID2={FinalNodeID2}");
                // Control points:
                {
                    Vector2 normal = StartDir1.Rotate90CCW();
                    if (isCW) normal = -normal;
                    ControlPoint1 =  (HW1 + HWpb + SAFETY_NET) * normal;
                    ControlPoint1 += origin;
                }
                float offset1 = 0;
                float offset2 = 0;
                if (!parallel) {
                    float d1 = (HW2 + HWpb) * ratio + SAFETY_NET;
                    float d2 = (HW1 + HWpb) * ratio + SAFETY_NET;
                    PointL = d1 * StartDir1 + d2 * StartDir2;
                    offset1 = d1 + d2 * Vector2.Dot(StartDir1, StartDir2);
                    offset2 = d2 + d1 * Vector2.Dot(StartDir1, StartDir2);
                } else {
                    Vector2 normal = StartDir1.Rotate90CCW();
                    if (isCW) normal = -normal;
                    float HW = Mathf.Max(HW1, HW2);
                    PointL = (HW + HWpb + SAFETY_NET) * normal;
                }
                PointL += origin;


                Point1 = Point2 = EndDir1 = EndDir2 = default;

                float targetLength = 4 * MPU;
                float weight = 0.3f; // reduce weigth for the results to converge.
                float distance = targetLength + offset1;
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

                    {
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

                    if (forcedEnd)
                        break;

                    float length = LineUtil.Bezier2ByDir(PointL, StartDir1, Point1, EndDir1).ArcLength();
                    distance = (1-weight) * distance + weight * distance * targetLength / length;
                    float diff = length - targetLength;
                    if (overFlow && diff > 0) {
                        continue;
                    }else if (overFlow || Mathf.Abs(diff) < 0.5 * MPU) 
                        break;
                    
                }


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

                    {
                        // TODO move to end of loop.
                        // if we have moved too far into a corner
                        // then bring back Point1 into the corner.
                        Vector2 otherNodePoint = FinalNodeID2.ToNode().m_position.ToCS2D();
                        CalculateCorner(finalSegmentID, FinalNodeID2, isCW,
                                        out Vector2 cornerPoint, out Vector2 cornerDir);
                        if (forcedEnd || (otherNodePoint - cornerPoint).magnitude >
                            (Point2 - cornerPoint).magnitude) {
                            Point2 = cornerPoint;
                            Point2 += (HWpb + SAFETY_NET) * normal; // TODO this does not work for accute corners.
                            Log.Debug($"DEBUG> finalSegmentID={finalSegmentID} finalOtherNodeID={FinalNodeID2} isCW={isCW}");
                            MovePointTowardOtherSegment(finalSegmentID, FinalNodeID2, isCW, ref Point2);
                            EndDir2 = cornerDir;
                        }
                    }

                    if (forcedEnd)
                        break;

                    float length = LineUtil.Bezier2ByDir(PointL, StartDir2, Point2, EndDir2).ArcLength();
                    distance = (1 - weight) * distance + weight * distance * targetLength / length;
                    float diff = length - targetLength;
                    if (overFlow && diff > 0) {
                        continue;
                    } else if (overFlow || Mathf.Abs(diff) < 0.5 * MPU)
                        break;
                }
                CanConnectPathAtOtherNode1 = CanConnectPathAtNode(FinalNodeID1);
                CanConnectPathAtFinalNode2 = CanConnectPathAtNode(FinalNodeID2);
                Log.Debug($"DEBUG 2> FinalNodeID1={FinalNodeID1} FinalNodeID2={FinalNodeID2}");
            }

            // move the point toward the segment with pedestrian lane.
            // TODO does not work for 180 angle if the two segments have different half widths.
            static void MovePointTowardOtherSegment(ushort segmentID, ushort nodeId, bool bLeft, ref Vector2 Point) {
                ushort OtherSementID = bLeft ?
                    segmentID.ToSegment().GetLeftSegment(nodeId) :
                    segmentID.ToSegment().GetRightSegment(nodeId);
                Log.Debug($"DEBUG> segmentID={segmentID} OtherSementID={OtherSementID} bLeft={bLeft}");

                ref NetSegment OtherSegment = ref OtherSementID.ToSegment();
                Vector2 OtherDir = IsStartNode(OtherSementID, nodeId) ?
                    OtherSegment.m_startDirection.ToCS2D().normalized :
                    OtherSegment.m_endDirection.ToCS2D().normalized;
                float otherHalfWidth = OtherSegment.Info.m_halfWidth;

                Point += (otherHalfWidth/2) * OtherDir;
            }

            // returns true if node is on ground and has at least one segment with pedestrian lanes.
            static bool CanConnectPathAtNode(ushort nodeID) {
                if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.OnGround))
                    return false;
                foreach(ushort segmentID in GetSegmentsCoroutine(nodeID)) {
                    if (segmentID.ToSegment().Info.m_hasPedestrianLanes)
                        return true;
                }
                return false;
            }

            // returns false if overflow or forceEnd
            // returns true otherwise.
            static void Travel(
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

                bool hasPedestrianLanes = segmentId.ToSegment().Info.m_hasPedestrianLanes;
                bool forceEnd = !hasPedestrianLanes && level > 0;
                overFlow = distance > length;
                if (overFlow || forceEnd) {
                    ushort nextSegmentId = ContinueToNextSegment(segmentId, otherNodeId);
                    if (nextSegmentId != 0) {
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
                    Log.Debug("distance > length || forceEnd but could not find next segment");
                }

                forcedEnd = !hasPedestrianLanes;
                float t = (overFlow || forcedEnd) ? 1f : distance / length;
                point = bezier.Position(t);
                tangent = bezier.Tangent(t).normalized;
                finalSegmentID = segmentId;
                finalOtherNodeID = otherNodeId;
            }

            static ushort ContinueToNextSegment(ushort segmentId, ushort nodeId) {
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

        public LWrapper(ushort segmentID1, ushort segmentID2, NetInfo info) {
            NetInfo eInfo = info.GetElevated();
            var calc = new Calc(segmentID1, segmentID2, eInfo.m_halfWidth);
            nodeL = node1 = node2 = null;
            segment1 = segment2 = null;

            bool create1 = calc.CanConnectPathAtOtherNode1 && calc.CanConnectPathAtJunction1;
            bool create2 = calc.CanConnectPathAtFinalNode2 && calc.CanConnectPathAtJunction2;
            if (!create1 && !create2) {
                return;
            }

            nodeL = new NodeWrapper(calc.PointL, 10, eInfo);

            if (create1) {
                node1 = new NodeWrapper(calc.Point1, 0, eInfo);
                segment1 = new SegmentWrapper(nodeL, node1, calc.StartDir1, calc.EndDir1);
            }

            if (create2) {
                node2 = new NodeWrapper(calc.Point2, 0, eInfo);
                segment2 = new SegmentWrapper(nodeL, node2, calc.StartDir2, calc.EndDir2);
            }
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
