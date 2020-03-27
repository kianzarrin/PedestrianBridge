namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;

    public class LWrapper {
        struct Calc {
            // Output:
            internal Vector2 Point1, PointL, Point2;
            internal Vector2 StartDir1, EndDir1, StartDir2, EndDir2;

            // segID2 is positioned CCW WRT segID1
            internal Calc(ushort segID1, ushort segID2, float HWpb) {
                Log.Debug($"LWrapper.Calc: {segID1}, {segID2}");
                // Prepration:
                ref NetSegment seg1 = ref segID1.ToSegment();
                ref NetSegment  seg2 = ref segID2.ToSegment();
                float HW1 = seg1.Info.m_halfWidth;
                float HW2 = seg2.Info.m_halfWidth;
                ushort junctionID = seg1.GetSharedNode(segID2);
                ref NetNode junction = ref junctionID.ToNode();
                Vector2 origin = junction.m_position.ToCS2D();
                bool bStartNode1 = seg1.m_startNode == junctionID;
                bool bStartNode2 = seg2.m_startNode == junctionID;
                Vector2 V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToCS2D();
                Vector2 V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToCS2D();
                StartDir1 = V1.normalized;
                StartDir2 = V2.normalized;

                float angle = VectorUtil.SignedAngleRadCCW(StartDir1, StartDir2);
                float ratio = 1f / Mathf.Sin(angle);
                bool parallel = VectorUtil.AreApprox180(StartDir1, StartDir2);
                Log.Debug($"parallel={parallel} angle={angle} ratio={ratio}");

                ////////////////////////////////////////////////////////////////
                // Main calculations
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
                    float HW = Mathf.Max(HW1, HW2);
                    PointL = (HW + HWpb + SAFETY_NET) * normal;
                }
                PointL += origin;

                Point1 = Point2 = EndDir1 = EndDir2 = default;

                float targetLength = 4 * MPU;
                float weight = 0.3f; // reduce weigth for the results to converge.
                float distance = targetLength + offset1;
                for (int counter = 0; counter < 10; ++counter) {
                    bool res = Travel(segID1, junctionID, distance, out Point1, out Vector2 tangent1);
                    var normal1 = tangent1.Rotate90CCW();
                    Point1 += (HW1 + HWpb + SAFETY_NET) * normal1;
                    EndDir1 = -tangent1;

                    float length = LineUtil.Bezier2ByDir(PointL, StartDir1, Point1, EndDir1).ArcLength();
                    distance = (1-weight) * distance + weight * distance * targetLength / length;
                    float diff = length - targetLength;
                    if(!res || Mathf.Abs(diff) < 0.5 * MPU) 
                        break;
                    
                }

                distance = targetLength + offset2;
                for (int counter = 0; counter < 10; ++counter) {
                    bool res = Travel(segID2, junctionID, distance, out Point2, out Vector2 tangent2);
                    var normal2 = tangent2.Rotate90CW();
                    Point2 += (HW2 + HWpb + SAFETY_NET) * normal2;
                    EndDir2 = -tangent2;

                    float length = LineUtil.Bezier2ByDir(PointL, StartDir2, Point2, EndDir2).ArcLength();
                    distance = (1 - weight) * distance + weight * distance * targetLength / length;
                    float diff = length - targetLength;
                    if (!res || Mathf.Abs(diff) < 0.5 * MPU)
                        break;
                }

            }

            static bool Travel(ushort segmentId, ushort nodeId, float distance, out Vector2 point, out Vector2 tangent) {
                Bezier2 bezier = CalculateSegmentBezier2(segmentId, nodeId);
                float length = bezier.ArcLength();
                if (distance > length) {
                    ushort otherNodeId = segmentId.ToSegment().GetOtherNode(nodeId);
                    ushort nextSegmentId = ContinueToNextSegment(segmentId, otherNodeId);
                    if (nextSegmentId != 0) {
                        return Travel(nextSegmentId, otherNodeId, distance-length, out point, out tangent);
                    }
                    Log.Debug("distance > length but could not find next segment");
                }
                    
                float t = distance < length ? distance / length : 1f;
                point = bezier.Position(t);
                tangent = bezier.Tangent(t).normalized;
                return t < 1f-Epsilon;
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

        // segmentID2 is postioned CCW WRT segmentID1.
        public LWrapper(ushort segmentID1, ushort segmentID2, NetInfo info) {
            NetInfo eInfo = info.GetElevated();
            var calc = new Calc(segmentID1, segmentID2, eInfo.m_halfWidth);

            nodeL = new NodeWrapper(calc.PointL, 10, eInfo);
            node1 = new NodeWrapper(calc.Point1, 0, eInfo);
            node2 = new NodeWrapper(calc.Point2, 0, eInfo);
            segment1 = new SegmentWrapper(nodeL, node1, calc.StartDir1, calc.EndDir1);
            segment2 = new SegmentWrapper(nodeL, node2, calc.StartDir2, calc.EndDir2);
        }

        public NodeWrapper nodeL;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;

        public void Create() {
            nodeL.Create();
            node1.Create();
            node2.Create();
            segment1.Create();
            segment2.Create();
        }
    }
}
