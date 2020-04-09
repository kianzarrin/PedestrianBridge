namespace PedestrianBridge.Util {
    using ColossalFramework;
    using System;
    using static HelpersExtensions;

    public static class  PrefabUtil {

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
            return null;
        }

    }
}
