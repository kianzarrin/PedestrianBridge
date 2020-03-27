
namespace PedestrianBridge.Shape {
    using System.Collections.Generic;
    using System;
    using UnityEngine;
    using Util;
    using Shapes;
    using System.Linq;
    using static Util.NetUtil;
    using static Util.RoundaboutUtil;
    using VectorUtils = Util.VectorUtil;

    public static class BuildControler {

        public static void CreateJunctionBridge(ushort nodeID) {
            if (nodeID.ToNode().CountSegments() < 3)
                throw new NotImplementedException("number of segments is less than 3");
            List<ushort> segList = GetCCSegList(nodeID).ToList();
            int n = segList.Count;
            if (n < 3)
                throw new Exception($"seglist count is ${segList.Count} expected at least 3");

            var nodeList = new List<NodeWrapper>();
            for (int i = 0; i < n; ++i) {
                ushort segID1 = segList[i], segID2 = segList[(i + 1) % n];
                var lwrapper = new LWrapper(segID1, segID2, PrefabUtil.SelectedPrefab);
                //Log.Info($"creating L from segments: {segID1} {segID2}");
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

        public static void CreateRaboutBridge(RoundaboutUtil raboutCalc) {
            NetInfo info = PrefabUtil.SelectedPrefab;

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

