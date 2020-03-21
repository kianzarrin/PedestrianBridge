using System.Collections.Generic;
using System;

namespace PedestrianBridge.Shape {
    using Util;
    using static Util.NetUtil;
    using Shapes;
    public static class BuildControler {
        static float HWpb => info.GetElevated().m_halfWidth;
        static NetInfo info => PrefabUtils.defaultPrefab;



        public static void CreateJunctionBridge(ushort nodeID) {
            if (nodeID.ToNode().CountSegments() != 4)
                throw new NotImplementedException("number of segments is not 4");
            List<ushort> segList = GetCWSegList(nodeID);
            if (segList.Count != 4)
                throw new Exception($"seglist count is ${segList.Count} expected 4");
            int n = segList.Count;
            var nodeList = new List<NodeWrapper>();
            for (int i = 0; i < n; ++i) {
                ushort segID1 = segList[i], segID2 = segList[(i + 1) % n];
                var lwrapper = new LWrapper(segID1, segID2, PrefabUtils.defaultPrefab);
                Log.Info($"creating L from segments: {segID1} {segID2}");
                lwrapper.Create();
                nodeList.Add(lwrapper.nodeL);
            }
            for (int i = 0; i < n; ++i) {
                SegmentWrapper segment = new SegmentWrapper(
                    nodeList[i], nodeList[(i + 1) % n]);
                segment.Create();
            } // end for
        } // end method

    } // end calss
} // end namespace
