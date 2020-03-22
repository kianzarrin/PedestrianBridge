using ColossalFramework;
using System;
using System.Collections.Generic;

namespace PedestrianBridge.Util {
    public class NetServiceException : Exception {
        public NetServiceException(string m) : base(m) { }
        public NetServiceException() : base() { }
        public NetServiceException(string m, Exception e) : base(m, e) { }
    }

    public static class NetUtil {
        public static NetManager netMan => Singleton<NetManager>.instance;
        public static NetTool netTool => Singleton<NetTool>.instance;
        public static SimulationManager simMan => Singleton<SimulationManager>.instance;
        public static TerrainManager terrainMan => TerrainManager.instance;

        public const float MPU = 8f; // meter per unit
        internal static ref NetNode ToNode(this ushort id) => ref netMan.m_nodes.m_buffer[id];
        internal static ref NetSegment ToSegment(this ushort id) => ref netMan.m_segments.m_buffer[id];
        internal static ref NetLane ToLane(this int id) => ref netMan.m_lanes.m_buffer[id];


        public static ushort GetFirstSegment(ushort nodeID) {
            NetNode node = nodeID.ToNode();
            ushort segmentID = 0;
            int i;
            for (i = 0; i < 8; ++i) {
                segmentID = node.GetSegment(i);
                if (segmentID != 0)
                    break;
            }
            return segmentID;
        }
        /// <summary>
        /// returns a clock-wise list of segments of the given node ID.
        /// </summary>
        public static List<ushort> GetCWSegList(ushort nodeID) {
            ushort segmentID = GetFirstSegment(nodeID);
            List<ushort> segList = new List<ushort>();
            segList.Add(segmentID);

            // add the rest of the segments.
            while (true) {
                segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                if (segmentID == segList[0])
                    break;
                else
                    segList.Add(segmentID);

            }
            return segList;
        }
    }
}
