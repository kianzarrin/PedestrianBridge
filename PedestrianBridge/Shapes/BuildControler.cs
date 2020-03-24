
namespace PedestrianBridge.Shape {
    using System.Collections.Generic;
    using System;
    using UnityEngine;
    using Util;
    using Shapes;
    using static Util.NetUtil;
    using static Util.RoundaboutUtil;
    using ColossalFramework.Math;


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
                var lwrapper = new LWrapper(segID1, segID2, PrefabUtils.SelectedPrefab);
                Log.Info($"creating L from segments: {segID1} {segID2}");
                lwrapper.Create();
                nodeList.Add(lwrapper.nodeL);
            }
            for (int i = 0; i < n; ++i) {
                SegmentWrapper segment = new SegmentWrapper(
                    nodeList[i], nodeList[(i + 1) % n]);
                segment.Create();
                TMPEUtils.BanPedestrianCrossings(segList[i], nodeID);
            } // end for
        } // end method

        public static void CreateRaboutBridge(ushort segmentID) {
            var util = new RoundaboutUtil();
            if (!util.TraverseLoop(segmentID, out _))
                return;

            var junctions = util.GetJunctions();
            int n = junctions.Count;
            if (n < 3)
                return;

            NodeWrapper center = null;
            NetInfo info = PrefabUtils.SelectedPrefab;

            for (int i = 0; i < n; ++i) {
                var i2 = (i + 1) % n;
                var slice = new RaboutSlice(
                    junctions[i].Main2, junctions[i].Minor,
                    junctions[i2].Main1, junctions[i2].Minor,
                    PrefabUtils.SelectedPrefab);
                if (i == 0)
                    center = new NodeWrapper(slice.CalculateCenter(), 10, info);
                slice.Create();
                center.Create();
                SegmentWrapper segment = new SegmentWrapper(center, slice.nodeM);
                segment.Create();
            }
        }

    } // end calss
} // end namespace

