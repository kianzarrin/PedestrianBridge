namespace PedestrianBridge.Shapes {
    using System.Collections.Generic;
    using System;
    using UnityEngine;
    using Util;
    public class RoundAboutWrapper {
        public bool IsValid { get; private set; } = false;
        RoundaboutUtil _raboutCalc;
        public RoundAboutWrapper(RoundaboutUtil raboutCalc, NetInfo info) {
            _raboutCalc = raboutCalc;
            _junctions = raboutCalc.GetJunctions();
            int n = _junctions.Count;
            if (n < 3) {
                Log.Info("Roundabout has too few junctions.");
                return;
            }

            Vector2 centerPoint = raboutCalc.CalculateCenter();
            _center = new NodeWrapper(centerPoint, 10, info);

            _slices = new List<RaboutSlice>(n);
            for (int i = 0; i < n; ++i) {
                var i2 = (i + 1) % n;
                var slice = new RaboutSlice(
                    _junctions[i].Main2, _junctions[i].Minor,
                    _junctions[i2].Main1, _junctions[i2].Minor,
                    _center,
                    PrefabUtil.SelectedPrefab);
                _slices.Add(slice);
                if (slice.IsValid) {
                    this.IsValid = true;
                }
            }
        }

        List<RaboutSlice> _slices;
        NodeWrapper _center;
        List<RoundaboutUtil.JunctionData> _junctions;


        public static void Create(RoundaboutUtil raboutCalc) {
            NetInfo info = PrefabUtil.SelectedPrefab;
            var roundabout = new RoundAboutWrapper(raboutCalc, info);
            roundabout.Create();
        }

        public void Create() {
            if (!IsValid)
                return;
            _center.Create();
            for (int i = 0; i < _junctions.Count; ++i) {
                if (_slices[i].IsValid) {
                    _slices[i].Create();
                    _junctions[i].BanCrossing();
                }
            }
        }
    }
}
