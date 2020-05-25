namespace PedestrianBridge.Shapes {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using KianCommons;
    using KianCommons.Math;
    using static KianCommons.Math.MathUtil;
    using static KianCommons.NetUtil;
    using static KianCommons.HelpersExtensions;
    using static Shapes.LWrapper.Calc;

    class RoadSideWrapper {
        internal struct Calc {
            // Output:
            internal Vector2 Point0, Point2;
            internal Vector2 Dir0, Dir2;

            // segment has pedestrian paths and is RoadAI
            internal bool CanConnectPath;

            /// <summary>
            /// Note: invert flag and LHT are ignored.
            /// </summary>
            /// <param name="segmentID">start bridge on the side of this segment.</param>
            /// <param name="t">offset of the bezier starting at startnode of segmentID</param>
            /// <param name="HWpb"></param>
            /// <param name="leftSide">which side of the road to build the bridge WTR to the vector
            /// starting at startNode of segment going to endNode of segment.</param>
            /// <param name="startNode">the direction of the bridge. true if going toward start node of segmentID</param>
            internal Calc(ushort segmentID, float t, float HWpb, bool leftSide = false, bool startNode = false) {
                // Prepration:
                ref NetSegment seg = ref segmentID.ToSegment();
                ushort finalNodeID = startNode ? seg.m_startNode : seg.m_endNode;
                float sideDistance = (seg.Info.m_halfWidth + HWpb + 1);

                Bezier2 bezier = NetUtil.CalculateSegmentBezier2(segmentID, startNode);
                if (startNode)
                    t = 1 - t;
                bezier = bezier.Cut(t, 1f);

                Vector2 origin = bezier.a;
                bool isCW = leftSide ^ startNode;
                bezier.NormalTangent(0, isCW, out Vector2 normal, out Vector2 tangent);
                
                // Main Calculations:
                CanConnectPath = seg.CanConnectPath();

                Dir0 = tangent;
                Point0 = origin + sideDistance * normal;

#pragma warning disable
                Log.Debug(
                    $"RoadSideWrapper.Calc(segmentID:{segmentID}, t:{t}, HWpb:{HWpb}, bLeft:{leftSide} , startNode:{startNode})\n" +
                    "{\n" +
                    $"    finalNodeID={finalNodeID} sideDistance={sideDistance}\n" +
                    $"    Dir0={Dir0} Point0={Point0}\n");
#pragma warning enable

                Travel(bezier, isCW, sideDistance, DEFAULT_LENGTH,
                       ref segmentID, ref finalNodeID,
                       out Point2, out Dir2);
                Dir2 = -Dir2; // end direction should be reversed.

                Log.Debug("}");
            }


            static void Travel(
                Bezier2 bezier, bool bLeft, float sideDistance, float distance,
                ref ushort segmentId, ref ushort finalNodeID,
                out Vector2 point, out Vector2 tangent)
            {
                Log.Debug($"    Travel(bezier:{bezier.a}->{bezier.d},bLeft:{bLeft},sideDistance:{sideDistance},distance:{distance}," +
                    $"segmentId(in):{segmentId},finalNodeID(in):{finalNodeID})");
                ref NetSegment segment = ref segmentId.ToSegment();
                Bezier2 bezierParallel = BezierUtil.CalculateParallelBezier(bezier, sideDistance, bLeft);
                float length = bezierParallel.ArcLength();

                if (distance > length) {
                    ushort segmentId2 = ContinueToNextSegment(segmentId, finalNodeID);
                    if (segmentId2 != 0) {
                        segmentId = segmentId2;
                        Log.Debug("ContinueToNextSegment " + segmentId);
                        bezier = CalculateSegmentBezier2(segmentId, finalNodeID);
                        finalNodeID = segmentId.ToSegment().GetOtherNode(finalNodeID);

                        Travel(
                            bezier, bLeft, sideDistance, distance - length,
                            ref segmentId, ref finalNodeID,
                            out point, out tangent);
                        return;
                    }
                    Log.Debug("    distance > length but could not find next segment");
                }

                distance = Mathf.Clamp(distance, 1f , length - 2);
                point = bezierParallel.Travel2(distance, out tangent);
                Log.Debug($"    distance={distance} length={length} return segmentId:{segmentId},finalNodeID:{finalNodeID} point={point} tangent={tangent}");
            }
        }

        public bool IsValid { get; private set; }

        public RoadSideWrapper(ushort segmentID, float t, NetInfo pathInfo, bool leftSide) {
            NetInfo info1 = Options.Underground ? pathInfo.GetSlope() : pathInfo.GetElevated();

            var calc1 = new RoadSideWrapper.Calc(segmentID, t, info1.m_halfWidth, leftSide: leftSide, startNode: false);
            var calc2 = new RoadSideWrapper.Calc(segmentID, t, info1.m_halfWidth, leftSide: leftSide, startNode: true);

            node0 = node1 = node2 = null;
            segment1 = segment2 = null;

            bool create1 = calc1.CanConnectPath;
            bool create2 =  calc2.CanConnectPath;
            IsValid = create1 | create2;
            if (!IsValid) {
                return;
            }


            if (create1) {
                node0 = new NodeWrapper(calc1.Point0, 10, info1);
                node1 = new NodeWrapper(calc1.Point2, 0, info1);
                segment1 = new SegmentWrapper(node0, node1, calc1.Dir0, calc1.Dir2);
            }

            if (create2) {
                node0 = node0 ?? new NodeWrapper(calc2.Point0, 10, info1);
                node2 = new NodeWrapper(calc2.Point2, 0, info1);
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
