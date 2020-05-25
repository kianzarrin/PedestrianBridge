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

    public static class Options {
        public static RoundaboutBridgeStyleT RoundaboutBridgeStyle { get; set; }
        public static bool Underground { get; set; }
        public static byte Elevation { get; set; }
        public static float Slope { get; set; }
    }
}
