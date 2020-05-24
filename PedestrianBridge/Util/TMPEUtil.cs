namespace PedestrianBridge.Util {
    using System.Runtime.CompilerServices;
    using KianCommons;
    public static class TMPEUtil {
        public static bool Active = true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool _SetPedestrianCrossingAllowed(ushort segmentId, bool startNode, bool value) {
            return TrafficManager.Manager.Impl.JunctionRestrictionsManager.Instance.
                SetPedestrianCrossingAllowed(segmentId, startNode, false);
        }

        public static bool SetPedestrianCrossingAllowed(ushort segmentId, bool startNode, bool value) {
            if (!Active)
                return false;
            try {
                return _SetPedestrianCrossingAllowed(segmentId, startNode, value);
            } catch { }

            Log.Info("TMPE not found!");
            Active = false;
            return false;
        }

        public static void BanPedestrianCrossings(ushort segmentId, ushort nodeId) {
            Log.Debug($"BanPedestrianCrossings({segmentId},{nodeId})");

            bool startNode = segmentId.ToSegment().m_startNode == nodeId;
            bool res = SetPedestrianCrossingAllowed(segmentId, startNode, false);
#if DEBUG
            if(Active && !res)
                Log.Info("BanPedestrianCrossings failed");
#endif
        }
    }
}
