using PedestrianBridge.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PedestrianBridge {
    public enum RoundaboutBridgeStyleT {
        CenterNode,
        InnerCircle,
        OuterCircle,
    }

    public static class ControlCenter {
        public static NetInfo Info => PrefabUtil.SelectedPrefab;
        public static NetInfo Info1 => Underground ? Info.GetSlope() : Info.GetElevated();
        public static NetInfo Info2 => Underground ? Info.GetTunnel() : Info1;
        public static float HalfWidth => System.Math.Max(Info1.m_halfWidth, Info2.m_halfWidth);

        public static RoundaboutBridgeStyleT RoundaboutBridgeStyle { get; set; } = RoundaboutBridgeStyleT.CenterNode;
        public static bool Underground { get; set; } = true;
        public static int Elevation { get; set; } = -9;
        public static float SlopeRatio { get; set; } = 1; // 1 = 10m in 3 units.
    }
}
