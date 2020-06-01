namespace PedestrianBridge.Shapes {
    using System.Collections.Generic;
    using System;
    using UnityEngine;
    using Util;
    using KianCommons;
    public class RaboutWraper {
        public bool IsValid { get; private set; } = false;
        RoundaboutUtil _raboutCalc;
        public RaboutWraper(RoundaboutUtil raboutCalc, NetInfo info) {
            _raboutCalc = raboutCalc;
            _junctions = raboutCalc.GetJunctions();
            int n = _junctions.Count;
            if (n < 3) {
                Log.Info("Roundabout has too few junctions.");
                return;
            }
            Vector2 centerPoint = raboutCalc.CalculateCenter();
            _center = new NodeWrapper(centerPoint, ControlCenter.Elevation);
            _slices = new List<RaboutSlice>(n);
            _segments = new List<SegmentWrapper>(n - 1);

            for (int i = 0; i < n; ++i) {
                var i2 = (i + 1) % n;
                var slice = new RaboutSlice(
                    _junctions[i].Main2, _junctions[i].Minor,
                    _junctions[i2].Main1, _junctions[i2].Minor,
                    raboutCalc.SegmentList,
                    _center,
                    info);
                _slices.Add(slice);
                if (slice.IsValid) {
                    this.IsValid = true;
                }
            }

            if (_slices.Count == 1)
                return;

            for (int i=0;i<_slices.Count; ++i) {
                var slice1 = _slices[i];
                var slice2 = _slices[(i + 1) % _slices.Count];
                var segment12 = slice1.segment_circle2;
                var segment21 = slice2.segment_circle1;
                if (segment12 == null || segment21 == null)
                    continue;
                var segment = new SegmentWrapper(
                    segment12.EndNode, segment21.EndNode,
                    -segment12.EndDir, -segment21.EndDir);
               _segments.Add(segment);
            }
        }

        List<SegmentWrapper> _segments;
        List<RaboutSlice> _slices;
        NodeWrapper _center;
        List<RoundaboutUtil.JunctionData> _junctions;

        public static void Create(RoundaboutUtil raboutCalc) {
            NetInfo info = PrefabUtil.SelectedPrefab;
            var roundabout = new RaboutWraper(raboutCalc, info);
            roundabout.Create();
        }

        public void Create() {
            if (!IsValid)
                return;
            if(ControlCenter.RoundaboutBridgeStyle == RoundaboutBridgeStyleT.Start)
                _center.Create();
            for (int i = 0; i < _junctions.Count; ++i) {
                if (_slices[i].IsValid) {
                    _slices[i].Create();
                    _junctions[i].BanCrossing();
                }
            }
            foreach (var segment in _segments) segment?.Create();
        }
    }
}