namespace PedestrianBridge.Shapes {
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

                if (!parallel) {
                    PointL = (HW2 + HWpb + SAFETY_NET) * ratio * StartDir1 +
                             (HW1 + HWpb + SAFETY_NET) * ratio * StartDir2;
                } else {
                    Vector2 normal = StartDir1.Rotate90CCW();
                    float HW = Mathf.Max(HW1, HW2);
                    PointL = (HW + HWpb + SAFETY_NET) * normal;
                }
                PointL += origin;

                Point1 = Point2 = default;
                EndDir1 = EndDir2 = default;

                Travel(ref seg1, bStartNode1, 3 * MPU + HW2 + HWpb + SAFETY_NET, out Point1, out Vector2 tangent1);
                var normal1 = tangent1.Rotate90CCW();
                Point1 += (HW1 + HWpb + SAFETY_NET) * normal1;
                EndDir1 = -tangent1;
                Log.Debug($"Point1-PointL={Point1 - PointL} StartDir1={StartDir1} tangent1={tangent1}");

                Travel(ref seg2, bStartNode2, 3 * MPU + HW1 + HWpb + SAFETY_NET, out Point2, out Vector2 tangent2);
                var normal2 = tangent2.Rotate90CW();
                Point2 += (HW2 + HWpb + SAFETY_NET) * normal2;
                EndDir2 = -tangent2;
            }

            void Travel(ref NetSegment seg, bool startNode, float distance, out Vector2 point, out Vector2 tangent) {
                Bezier2 bezier = seg.CalculateSegmentBezier2(startNode);
                float t = distance / seg.m_averageLength;
                point = bezier.Position(t);
                tangent = bezier.Tangent(t).normalized;
                Log.Debug($"distance={distance} segLen={seg.m_averageLength} t={t} " +
                    $"point={point} tangent={tangent}");

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
