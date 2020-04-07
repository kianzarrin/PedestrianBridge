namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using static Util.MathUtil;
    using static Util.NetUtil;
    using static Util.HelpersExtensions;
    using static Shapes.LWrapper.Calc;

    class RoadSideWrapper {
        internal struct Calc {
            // Output:
            internal Vector2 Point0, Point2;
            internal Vector2 Dir0, Dir2;

            // segment has pedestrian paths and is RoadAI
            internal bool CanConnectPath;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="segmentID"></param>
            /// <param name="Position">Poistion on segmentID Bezier</param>
            /// <param name="tangent">tangent of Position on segmentID going toward the end node</param>
            /// <param name="startNode"></param>
            /// <param name="HWpb"></param>
            internal Calc(ushort segmentID, float t, float HWpb, bool bLeft = false, bool startNode = false) {
                // Prepration:
                ref NetSegment seg = ref segmentID.ToSegment();
                ushort finalNodeID = startNode ? seg.m_startNode : seg.m_endNode;
                float sideDistance = (seg.Info.m_halfWidth + HWpb + SAFETY_NET);

                Bezier2 bezier = NetUtil.CalculateSegmentBezier2(segmentID, startNode);
                bezier = bezier.Cut(t, 1f);

                Vector2 origin = bezier.a;
                bezier.NormalTangent(0, bLeft, out Vector2 normal, out Vector2 tangent);
                
                // Main Calculations:
                CanConnectPath = seg.CanConnectPath();

                Dir0 = tangent;
                Point0 = origin + sideDistance * normal;

#pragma warning disable
                Log.Debug(
                    $"RoadSideWrapper.Calc(segmentID:{segmentID}, t:{t}, HWpb:{HWpb}, bLeft:{bLeft} , startNode:{startNode})\n" + "{" +
                    $"  finalNodeID={finalNodeID} sideDistance={sideDistance}\n" +
                    $"  Dir0={Dir0} Point0={Point0}\n" +
                    "}");
#pragma warning enable

                Travel(bezier, bLeft, sideDistance, DEFAULT_LENGTH,
                       ref segmentID, ref finalNodeID,
                       out Point2, out Dir2);
                Dir2 = -Dir2; // end direction should be reversed.
            }

            // returns false if overflow or forceEnd
            // returns true otherwise.
            static void Travel(
                Bezier2 bezier, bool leftSide, float sideDistance, float distance,
                ref ushort segmentId, ref ushort finalNodeID,
                out Vector2 point, out Vector2 tangent)
            {
                ref NetSegment segment = ref segmentId.ToSegment();
                float length = bezier.ArcLength();
                if (distance > length) {
                    segmentId = ContinueToNextSegment(segmentId, finalNodeID);
                    if (segmentId != 0) {
                        Log.Debug("ContinueToNextSegment");
                        bezier = CalculateSegmentBezier2(segmentId, finalNodeID);
                        finalNodeID = segmentId.ToSegment().GetOtherNode(finalNodeID);

                        Travel(
                            bezier, leftSide, sideDistance, distance - length,
                            ref segmentId, ref finalNodeID,
                            out point, out tangent);
                        return;
                    }
                    //Log.Debug("distance > length || forceEnd but could not find next segment");
                }
                bezier.NormalTangent(0, leftSide, out Vector2 normalStart, out Vector2 tangentStart);
                bezier.NormalTangent(1, leftSide, out Vector2 normalEnd, out Vector2 tangentEnd);

                Bezier2 bezierParallel = BezierUtil.Bezier2ByDir(
                    bezier.a + sideDistance * normalStart, tangentStart,
                    bezier.d + sideDistance * normalEnd, tangentEnd);

                point = bezierParallel.Travel2(distance, out tangent);
            }
        }

        public bool IsValid { get; private set; }

        public RoadSideWrapper(ushort segmentID, float t, NetInfo pathInfo, bool leftSide) {
            NetInfo eInfo = pathInfo.GetElevated();
            var calc1 = new RoadSideWrapper.Calc(segmentID, t, pathInfo.m_halfWidth, bLeft: leftSide, startNode: false);
            var calc2 = new RoadSideWrapper.Calc(segmentID, t, pathInfo.m_halfWidth, bLeft: leftSide, startNode: true);

            node0 = node1 = node2 = null;
            segment1 = segment2 = null;

            bool create1 = calc1.CanConnectPath;
            bool create2 = false;// calc2.CanConnectPath;
            IsValid = create1 | create2;
            if (!IsValid) {
                return;
            }

            node0 = new NodeWrapper(calc1.Point0, 10, eInfo);

            if (create1) {
                node1 = new NodeWrapper(calc1.Point2, 0, eInfo);
                segment1 = new SegmentWrapper(node0, node1, calc1.Dir0, calc1.Dir2);
            }

            if (create2) {
                node2 = new NodeWrapper(calc2.Point2, 0, eInfo);
                segment2 = new SegmentWrapper(node0, node2, calc2.Dir0, calc2.Dir2);
            }
         }

        public NodeWrapper node0;
        NodeWrapper node1;
        NodeWrapper node2;

        SegmentWrapper segment1;
        SegmentWrapper segment2;

        public void Create() {
            node0?.Create();
            node1?.Create();
            node2?.Create();
            segment1?.Create();
            segment2?.Create();
        }

    }
}
