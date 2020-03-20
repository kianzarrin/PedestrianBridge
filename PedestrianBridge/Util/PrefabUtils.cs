namespace PedestrianBridge.Util {
    using ColossalFramework;
    using System;
    using static HelpersExtensions;

    public static class  PrefabUtils {
        static NetTool netTool => Singleton<NetTool>.instance; //ToolsModifierControl.toolController.CurrentTool as NetTool;

        public static NetInfo PedestrianBridgeInfo =>
            GetInfo("Pedestrian Elevated");

        public static NetInfo PedestrianPathInfo =>
            GetInfo("Pedestrian Pavement");

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

        public static NetInfo defaultPrefab => PedestrianPathInfo;

        public static NetInfo Value {
            get {
                Log.Info("KIAN DEBUG POINT A");
                NetInfo prefab = netTool.m_prefab ?? defaultPrefab;
                if (PluginUtils.FineRoadToolDetected)
                    prefab = FineRoadToolSelection(prefab);
                return prefab;
            }
        }

        /* We change the road mode according to Fine Road Tool */
        private static NetInfo FineRoadToolSelection(NetInfo prefab) {
            RoadAI roadAI = prefab.m_netAI as RoadAI;
            if (roadAI != null) {
                Log.Info($"underground:{roadAI.IsUnderground()} overground:{roadAI.IsOverground()}");
                // If the user has manually selected underground/overground mode, we let it be
                if (!roadAI.IsUnderground() && !roadAI.IsOverground()) {
                    switch (FineRoadTool.FineRoadTool.instance.mode) {
                        case FineRoadTool.Mode.Ground:
                            return roadAI.m_info;
                        case FineRoadTool.Mode.Elevated:
                        case FineRoadTool.Mode.Bridge:
                            if (roadAI.m_elevatedInfo != null) {
                                return roadAI.m_elevatedInfo;
                            }
                            break;
                        case FineRoadTool.Mode.Tunnel:
                            if (roadAI.m_tunnelInfo != null) {
                                return roadAI.m_tunnelInfo;
                            }
                            break;
                        case FineRoadTool.Mode.Normal:
                        case FineRoadTool.Mode.Single:
                            break;
                    }
                }
            }

            PedestrianPathAI pedestrianAI = prefab.m_netAI as PedestrianPathAI;
            if (pedestrianAI != null) {
                Log.Info($"underground:{pedestrianAI.IsUnderground()} overground:{pedestrianAI.IsOverground()}");
                // If the user has manually selected underground/overground mode, we let it be
                if (!pedestrianAI.IsUnderground() && !pedestrianAI.IsOverground()) {
                    switch (FineRoadTool.FineRoadTool.instance.mode) {
                        case FineRoadTool.Mode.Ground:
                            return pedestrianAI.m_info;
                        case FineRoadTool.Mode.Elevated:
                        case FineRoadTool.Mode.Bridge:
                            if (pedestrianAI.m_elevatedInfo != null) {
                                return pedestrianAI.m_elevatedInfo;
                            }
                            break;
                        case FineRoadTool.Mode.Tunnel:
                            if (pedestrianAI.m_tunnelInfo != null) {
                                return pedestrianAI.m_tunnelInfo;
                            }
                            break;
                        case FineRoadTool.Mode.Normal:
                        case FineRoadTool.Mode.Single:
                            break;
                    }
                }
            }

            return prefab;
        }
    }
}
