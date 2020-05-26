using ColossalFramework.PlatformServices;
using KianCommons;
using PedestrianBridge.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PedestrianBridge {
    public enum RoundaboutBridgeStyleT {
        Start,
        InnerCircle,
        OuterCircle,
    }

    public static class ControlCenter {


        public static NetInfo Info => PrefabUtil.SelectedPrefab;
        public static NetInfo Info1 => Underground ? Info.GetSlope() : Info.GetElevated();
        public static NetInfo Info2 => Underground ? Info.GetTunnel() : Info.GetElevated();
        public static float HalfWidth => System.Math.Max(Info1.m_halfWidth, Info2.m_halfWidth);

        public static RoundaboutBridgeStyleT RoundaboutBridgeStyle { get; set; } = RoundaboutBridgeStyleT.InnerCircle;
        public static bool Underground { get; set; } = false;

        static int _bridgeElevation = 9;
        static int _tunnelElevation = -12;
        public static int Elevation {
            get => Underground ? _tunnelElevation : _bridgeElevation;
            set {
                if (Underground) _tunnelElevation = value;
                else _bridgeElevation = value;
            }
        }

        static float _inverseSlopeRatio = 1f; 
        public static float InverseSlopeRatio {
            get => _inverseSlopeRatio * (System.Math.Abs(Elevation) * 0.1f); // 1 => h=10 and length=3
            set => _inverseSlopeRatio = value;
        }
        public static float BaseLength => ControlCenter.InverseSlopeRatio * 3 * NetUtil.MPU;


    }
}
