using ColossalFramework;
using KianCommons;
using PedestrianBridge.UI;
using PedestrianBridge.Util;

namespace PedestrianBridge {
    public enum RoundaboutBridgeStyleT {
        Star,
        InnerCircle,
        OuterCircle,
    }

    public static class ControlCenter {
        public static NetInfo Info => PrefabUtil.SelectedPrefab;
        public static NetInfo Info1 => Underground ? Info.GetSlope() : Info.GetElevated();
        public static NetInfo Info2 => Underground ? Info.GetTunnel() : Info.GetElevated();
        public static float HalfWidth => System.Math.Max(Info1.m_halfWidth, Info2.m_halfWidth);

        #region style
        static readonly SavedInt style_ = new SavedInt(
            "Style", ModSettings.FILE_NAME, (int)RoundaboutBridgeStyleT.InnerCircle, true);
        public static RoundaboutBridgeStyleT RoundaboutBridgeStyle {
            get => (RoundaboutBridgeStyleT)style_.value;
            set => style_.value = (int)value;
        }
        #endregion

        #region underground
        static readonly SavedBool underground_ = new SavedBool(
            "Underground", ModSettings.FILE_NAME, def:false, autoUpdate: true);
        public static bool Underground {
            get => underground_.value;
            set => underground_.value=value;
        }
        #endregion

        #region elevation
        static readonly SavedInt bridgeElevation_ = new SavedInt(
            "BridgeElevation", ModSettings.FILE_NAME, def: 10, autoUpdate: true);
        static int tunnelElevation_ = -12;
        public static int Elevation {
            get {
                //Log.Debug($"Elevation.Get:bridgeElevation_={bridgeElevation_.value}\n" + System.Environment.StackTrace);
                return Underground ? tunnelElevation_ : bridgeElevation_.value;
            }
            set {
                //Log.Debug("Elevation.Set=>" + value + "\n" + System.Environment.StackTrace);
                if (Underground) tunnelElevation_ = -System.Math.Abs(value);
                else bridgeElevation_.value = System.Math.Abs(value);
            }
        }
        #endregion

        #region slope
        static readonly SavedFloat inverseSlope_ = new SavedFloat(
            "InverseSlopeRatio", ModSettings.FILE_NAME, def: 1f, autoUpdate: true);

        public static float InverseSlopeRatio {
            get {
                //Log.Debug("InverseSlopeRatio.Get=>" + inverseSlope_.value + "\n"+System.Environment.StackTrace);
                return inverseSlope_.value;
            }
            set {
                //Log.Debug("InverseSlopeRatio.Set=>" + value + "\n" + System.Environment.StackTrace);
                inverseSlope_.value = value;
            }
        }

        public static float BaseLength =>
            ControlCenter.InverseSlopeRatio *
            (System.Math.Abs(Elevation) * 0.1f) *
            3 * NetUtil.MPU;
        #endregion

    }
}
