using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;

namespace PedestrianBridge.Util {
    public class NetServiceException : Exception {
        public NetServiceException(string m) : base(m) { }
        public NetServiceException() : base() { }
        public NetServiceException(string m, Exception e) : base(m, e) { }
    }

    public static class NetUtil {
        public const float SAFETY_NET = 0.02f;

        public static NetManager netMan => Singleton<NetManager>.instance;
        public static NetTool netTool => Singleton<NetTool>.instance;
        public static SimulationManager simMan => Singleton<SimulationManager>.instance;
        public static TerrainManager terrainMan => TerrainManager.instance;

        public const float MPU = 8f; // meter per unit
        internal static ref NetNode ToNode(this ushort id) => ref netMan.m_nodes.m_buffer[id];
        internal static ref NetSegment ToSegment(this ushort id) => ref netMan.m_segments.m_buffer[id];
        internal static ref NetLane ToLane(this uint id) => ref netMan.m_lanes.m_buffer[id];


        internal static Bezier3 CalculateSegmentBezier3(this ref NetSegment seg) {
            Bezier3 bezier = new Bezier3 {
                a = seg.m_startNode.ToNode().m_position,
                d= seg.m_endNode.ToNode().m_position,
            };
            NetSegment.CalculateMiddlePoints(
                bezier.a, seg.m_startDirection,
                bezier.d, seg.m_endDirection,
                false, false,
                out bezier.b,
                out bezier.c);
            return bezier;
        }

        #region copied from TMPE
        public static bool LHT => TrafficDrivesOnLeft;
        public static bool RHT => !LHT;
        public static bool TrafficDrivesOnLeft =>
            Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic
                == SimulationMetaData.MetaBool.True;

        public static bool IsStartNode(ushort segmentId, ushort nodeId) =>
            segmentId.ToSegment().m_startNode == nodeId;

        public static bool IsSegmentValid(ushort segmentId) {
            if (segmentId != 0) {
                return (segmentId.ToSegment().m_flags &
                (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) ==
                NetSegment.Flags.Created;
            }
            return false;
        }

        public static ushort GetHeadNode(ref NetSegment segment) {
            // tail node>-------->head node
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            invert = invert ^ LHT;
            if (invert) {
                return segment.m_startNode;
            } else {
                return segment.m_endNode;
            }
        }

        public static ushort GetHeadNode(ushort segmentId) =>
            GetHeadNode(ref segmentId.ToSegment());

        public static ushort GetTailNode(ref NetSegment segment) {
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            invert = invert ^ LHT;
            if (!invert) {
                return segment.m_startNode;
            } else {
                return segment.m_endNode;
            }//endif
        }

        public static ushort GetTailNode(ushort segmentId) =>
            GetTailNode(ref segmentId.ToSegment());

        public static bool CalculateIsOneWay(ushort segmentId) {
            int forward = 0;
            int backward = 0;
            segmentId.ToSegment().CountLanes(
                segmentId,
                NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle,
                VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Train |
                VehicleInfo.VehicleType.Tram | VehicleInfo.VehicleType.Metro |
                VehicleInfo.VehicleType.Monorail,
                ref  forward,
                ref backward);
            return (forward == 0) ^ (backward == 0);
        }

        #endregion

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

        public struct NodeSegments{
            public ushort[] segments;
            public int count;
            void Add(ushort segmentID) {
                segments[count++] = segmentID;
            }

            public NodeSegments(ushort nodeID) {
                segments = new ushort[8];
                count = 0;

                ushort segmentID = GetFirstSegment(nodeID);
                Add(segmentID);
                while (true) {
                    segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                    if (segmentID == segments[0])
                        break;
                    else
                        Add(segmentID);
                }
            }
        }

        /// <summary>
        /// returns a counter-clockwise list of segments of the given node ID.
        /// </summary>
        public static List<ushort> GetCCSegList(ushort nodeID) {
            ushort segmentID = GetFirstSegment(nodeID);
            List<ushort> segList = new List<ushort>();
            segList.Add(segmentID);

            // add the rest of the segments.
            while (true) {
                segmentID = segmentID.ToSegment().GetRightSegment(nodeID);
                if (segmentID == segList[0])
                    break;
                else
                    segList.Add(segmentID);
            }
            return segList;
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
