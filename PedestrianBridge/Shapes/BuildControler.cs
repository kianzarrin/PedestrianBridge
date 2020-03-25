
namespace PedestrianBridge.Shape {
    using System.Collections.Generic;
    using System;
    using UnityEngine;
    using Util;
    using Shapes;
    using static Util.NetUtil;
    using static Util.RoundaboutUtil;
    using VectorUtils = Util.VectorUtil;

    public static class BuildControler {
        public static void CreateJunctionBridge(ushort nodeID) {
            if (nodeID.ToNode().CountSegments() < 3)
                throw new NotImplementedException("number of segments is less than 3");
            List<ushort> segList = GetCWSegList(nodeID);
            if (segList.Count < 3)
                throw new Exception($"seglist count is ${segList.Count} expected at least 3");
            int n = segList.Count;
            var nodeList = new List<NodeWrapper>();
            for (int i = 0; i < n; ++i) {
                ushort segID1 = segList[i], segID2 = segList[(i + 1) % n];
                var lwrapper = new LWrapper(segID1, segID2, PrefabUtil.SelectedPrefab);
                Log.Info($"creating L from segments: {segID1} {segID2}");
                lwrapper.Create();
                nodeList.Add(lwrapper.nodeL);
            }
            for (int i = 0; i < n; ++i) {
                SegmentWrapper segment = new SegmentWrapper(
                    nodeList[i], nodeList[(i + 1) % n]);
                segment.Create();
                TMPEUtil.BanPedestrianCrossings(segList[i], nodeID);
            } // end for
        } // end method


        public static bool CalculateCenter(JunctionData j1, JunctionData j2, out Vector2 center) {
            var point1 = j1.NodeID.ToNode().m_position.ToCS2D();
            var point2 = j2.NodeID.ToNode().m_position.ToCS2D();

            ref NetSegment seg1 = ref j1.Main2.ToSegment();
            ref NetSegment seg2 = ref j2.Main1.ToSegment();

            bool bStartNode1 = seg1.m_startNode == j1.NodeID;
            bool bStartNode2 = seg2.m_startNode == j2.NodeID;

            Vector2 V1 = (bStartNode1 ? seg1.m_startDirection : seg1.m_endDirection).ToCS2D();
            Vector2 V2 = (bStartNode2 ? seg2.m_startDirection : seg2.m_endDirection).ToCS2D();

            return LineUtil.Intersect(point1, V1.Rotate90CW(), point2, V2.Rotate90CW(), out center);
        }

        public static void CreateRaboutBridge(ushort segmentID) {
            NetInfo info = PrefabUtil.SelectedPrefab;

            var raboutCalc = new RoundaboutUtil();
            if (!raboutCalc.TraverseLoop(segmentID, out _))
                return;

            var junctions = raboutCalc.GetJunctions();
            int n = junctions.Count;
            if (n < 3) {
                Log.Info("Roundabout has too few junctions.");
                return;
            }

            Vector2 centerPoint = raboutCalc.CalculateCenter();
            NodeWrapper center = new NodeWrapper(centerPoint, 10, info);
            center.Create();

            for (int i = 0; i < n; ++i) {
                var i2 = (i + 1) % n;
                var slice = new RaboutSlice(
                    junctions[i].Main2, junctions[i].Minor,
                    junctions[i2].Main1, junctions[i2].Minor,
                    center,
                    PrefabUtil.SelectedPrefab);
                slice.Create();
                junctions[i].BanCrossing();
            }
        }

    } // end calss
} // end namespace

