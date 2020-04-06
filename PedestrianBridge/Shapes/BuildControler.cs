namespace PedestrianBridge.Shapes {
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
            var junction = new JunctionWrapper(nodeID);
            if (junction.Valid) {
                junction.Create();
            }
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

            var slices = new List<RaboutSlice>(n);
            bool bCreate = false;
            for (int i = 0; i < n; ++i) {
                var i2 = (i + 1) % n;
                var slice = new RaboutSlice(
                    junctions[i].Main2, junctions[i].Minor,
                    junctions[i2].Main1, junctions[i2].Minor,
                    center,
                    PrefabUtil.SelectedPrefab);
                slices.Add(slice);
                bCreate |= slice.nodeM != null;
            }
            if (!bCreate) {
                Log.Info("Could not create roundabout");
                return;
            }

            center.Create(); // this must be created before any slice.
            for (int i = 0; i < n; ++i) {
                slices[i].Create();
                junctions[i].BanCrossing();
            }

        }

    } // end calss
} // end namespace

