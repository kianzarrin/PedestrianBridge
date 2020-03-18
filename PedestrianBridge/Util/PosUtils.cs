using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PedestrianBridge.Util {
    using static VectorUtils;
    public static class PosUtils {
        const float epsilon = 0.01f;
        const float MPU = 8f; //meter per unit

        static bool Equal(float a, float b) => Mathf.Abs(a - b) < epsilon*2f;
        static void AssertEqual(float a, float b, string m = "") =>
            Debug.Assert(Equal(a, b), $"Assertion failed: expected {a} == {b} | " + m);

        public struct MiddleRabout {
            //inputs:
            public Vector2 Junction1, Junction2;// no nodes exist in CC rotation from junction1 to junction2.
            public float HWpb, HWr; // half widths of Pedesterian bridge, roundabout road respectively.

            // outputs: relative position of 3 nodes for the pedastrian bridge v1->vM<-V2
            public Vector2 Vm;

            public Vector2 Calculate() {
                AssertEqual(Junction1.magnitude, Junction2.magnitude, "expected circular roundabout.");
                float HW = HWpb + HWr;
                float r = Junction1.magnitude + HW;
                float angle = (Junction1.Angle() + Junction2.Angle())/2f;
                return Vm = Vector2ByAgnle(r, angle);
            }
        }

        public struct RLR {
            // Input: the two segments that form intersection.
            public ushort segID1, segID2;
            public float HWpb;
            // Output:
            public Vector2 Point1, PointL, Point2;

            //intermidiate
            NetSegment seg1, seg2;
            float HW1, HW2;
            bool bStartNode1, bStartNode2;
            Vector2 V1, V2;
            Vector2 dir1, dir2;
            ushort junctionID;
            NetNode junction;
            Vector2 origin;

            void PreCalc() {
                seg1 = segID1.ToSegment();
                seg2 = segID2.ToSegment();
                HW1 = seg1.Info.m_halfWidth;
                HW2 = seg2.Info.m_halfWidth;
                junctionID = seg1.GetSharedNode(segID2);
                junction = junctionID.ToNode();
                origin = junction.m_position.ToPoint();
                bStartNode1 = seg1.m_startNode == junctionID;
                bStartNode2 = seg2.m_startNode == junctionID;
                V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToPoint();
                V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToPoint();
                dir1 = V1.normalized;
                dir2 = V2.normalized;
            }

            public void Calculate() {
                PreCalc();
                PointL = (HW2+ HWpb+epsilon) * dir1 + (HW1+HWpb+epsilon) * dir2;
                Point1 = PointL + 3 * MPU * dir1;
                Point2 = PointL + 3 * MPU * dir2;

                PointL += origin;
                Point1 += origin;
                Point2 += origin;
            }
        }
    }
}
