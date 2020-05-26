namespace PedestrianBridge.Util {
    using KianCommons;
    using System;

    public static class PrefabUtil {
        public static NetInfo SelectedPrefab => GetSelectedPath();
        public static NetInfo defaultPrefab => PedestrianPathInfo;
        public static NetInfo PedestrianBridgeInfo =>
            GetInfo("Pedestrian Elevated");
        public static NetInfo PedestrianPathInfo =>
            GetInfo("Pedestrian Pavement");

        static NetInfo GetSelectedPath() {
            NetInfo info = NetUtil.netTool?.m_prefab;
            if (info == null)
                return defaultPrefab;
            //Log.Debug("selected Info is "+ info);
            try {
                //Log.Debug("info.GetElevated().m_netAI is " + info.GetElevated().m_netAI);
                if (info.GetElevated().m_netAI is PedestrianBridgeAI)
                    return info;
            }
            catch (Exception e) {
                Log.Error(e.Message);
            }

            //Log.Debug("no pedestrian path is selected. GetSelectedPath() returns default prefab");
            return defaultPrefab;
        }

        public static NetInfo GetInfo(string name) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log(info.name);
            }
            throw new Exception("NetInfo not found!");
        }

        public static NetInfo GetElevated(this NetInfo info) {
            NetAI ai = info.m_netAI;
            if (ai is PedestrianBridgeAI || ai is RoadBridgeAI)
                return info;

            if (ai is PedestrianPathAI)
                return (ai as PedestrianPathAI).m_elevatedInfo;

            if (ai is RoadAI)
                return (ai as RoadAI).m_elevatedInfo;

            Log.Error($"GetElevated({info} returns null. ai={ai}");
            return null;
        }

        public static NetInfo GetTunnel(this NetInfo info) {
            NetAI ai = info.m_netAI;
            if (ai is PedestrianTunnelAI || ai is RoadTunnelAI)
                return info;


            if (ai is PedestrianPathAI)
                return (ai as PedestrianPathAI).m_tunnelInfo;

            if (ai is RoadAI)
                return (ai as RoadAI).m_tunnelInfo;

            Log.Error($"GetTunnel({info}) returns null. ai={ai}");
            return null;
        }

        public static NetInfo GetSlope(this NetInfo info) {
            NetAI ai = info.m_netAI;
            if (ai is PedestrianTunnelAI || ai is RoadTunnelAI)
                return info;

            if (ai is PedestrianPathAI)
                return (ai as PedestrianPathAI).m_slopeInfo;

            if (ai is RoadAI)
                return (ai as RoadAI).m_slopeInfo;

            Log.Error($"GetSlope({info} returns null. ai={ai}");
            return null;
        }

        public static int GetInfoPrioirty(NetInfo info, NetInfo baseInfo = null) {
            PedestrianPathAI baseAI = baseInfo?.m_netAI as PedestrianPathAI;
            HelpersExtensions.AssertNotNull(baseAI, "baseAI");
            if (info == baseAI.m_info) return 0;
            if (info == baseAI.m_elevatedInfo) return 1;
            if (info == baseAI.m_slopeInfo) return 1;
            if (info == baseAI.m_tunnelInfo) return 2;
            if (info == baseAI.m_bridgeInfo) return 2;
            Log.Error("Unreacahble code");
            return -1;
        }
    }
}
