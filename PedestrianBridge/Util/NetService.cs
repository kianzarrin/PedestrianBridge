using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using ColossalFramework;
using UnityEngine;
using System.Diagnostics;
using System.Timers;
using static PedestrianBridge.Util.PrefabUtils;

namespace PedestrianBridge.Util {
    public static class NetService {
        public static NetManager netMan => Singleton<NetManager>.instance;
        public static SimulationManager simMan => Singleton<SimulationManager>.instance;
        public static NetTool netTool = Singleton<NetTool>.instance;

        public static void LogSegmentDetails(ushort segmentID) {
            var segment = segmentID.ToSegment();
            var startPos = segment.m_startNode.ToNode().m_position;
            var endPos = segment.m_endNode.ToNode().m_position;
            var startDir = segment.m_startDirection;
            var endDir = segment.m_endDirection;
            var end2start = endPos - startPos;
            var end2start_normal = end2start; end2start_normal.y = 0; end2start_normal.Normalize();
            var f = "000.000";
            string m = $"segment:{segmentID},\n" +
                $"startPos:{startPos.ToString(f)}, endPos:{endPos.ToString(f)},\n" +
                $"startDir:{startDir.ToString(f)}, endDir:{endDir.ToString(f)},\n" +
                $"end2start={end2start.ToString(f)}, end2start_normal={end2start_normal.ToString(f)}";
            Helpers.Log(m);
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




        public class NetServiceException : Exception {
            public NetServiceException(string m) : base(m) { }
            public NetServiceException() : base() { }
            public NetServiceException(string m, Exception e) : base(m, e) { }
        }

        public static ushort CreateNode(Vector3 position, NetInfo info = null) {
            info = info ?? PedestrianBridgeInfo;
            Helpers.Log($"creating node for {info.name} at position {position.ToString("000.000")}");
            bool res = netMan.CreateNode(node: out ushort nodeID, randomizer: ref simMan.m_randomizer,
                info: info, position: position, buildIndex: simMan.m_currentBuildIndex);
            if (!res)
                throw new NetServiceException("Node creation failed");
            simMan.m_currentBuildIndex++;
            return nodeID;

        }

        public static ushort CreateSegment(
            ushort startNodeID, ushort endNodeID,
            Vector3 startDir, Vector3 endDir,
            NetInfo info = null) {
            info = info ?? startNodeID.ToNode().Info;
            Helpers.Log($"creating segment for {info.name} between nodes {startNodeID} {endNodeID}");
            var bi = simMan.m_currentBuildIndex;
            startDir.y = endDir.y = 0;
            startDir.Normalize(); endDir.Normalize();
            bool res = netMan.CreateSegment(
                segment: out ushort segmentID, randomizer: ref simMan.m_randomizer, info: info,
                startNode: startNodeID, endNode: endNodeID, startDirection: startDir, endDirection: endDir,
                buildIndex: bi, modifiedIndex: bi, invert: false);
            if (!res)
                throw new NetServiceException("Segment creation failed");
            simMan.m_currentBuildIndex++;
            return segmentID;
        }

        public static ushort CreateSegment(ushort startNodeID, ushort endNodeID, NetInfo info = null) {
            Vector3 startPos = startNodeID.ToNode().m_position;
            Vector3 endPos = endNodeID.ToNode().m_position;
            var dir = endPos - startPos;
            return CreateSegment(startNodeID, endNodeID, dir, -dir, info);
        }

        public static ushort CreateSegment(ushort startNodeID, ushort endNodeID, Vector2 middlePoint, NetInfo info = null) {
            Vector3 startPos = startNodeID.ToNode().m_position;
            Vector3 endPos = endNodeID.ToNode().m_position;
            Vector3 middlePos = middlePoint.ToPos();
            Vector3 startDir = middlePos - startPos;
            Vector3 endDir = middlePos - endPos;
            return CreateSegment(startNodeID, endNodeID, startDir, endDir);
        }


        public static float GetClosestHeight(this ushort segmentID, Vector3 Pos) =>
            segmentID.ToSegment().GetClosestPosition(Pos).Height();

        public static void SetGroundNode(ushort nodeID) {
            ref NetNode node = ref nodeID.ToNode();
            //node.m_elevation = 0;
            //node.m_building = 0;
            //node.m_flags &= ~NetNode.Flags.Moveable;
            //node.m_flags |= NetNode.Flags.Transition | NetNode.Flags.OnGround;
            node.UpdateNode(nodeID);
            netMan.UpdateNode(nodeID);
        }

        public static Vector3 Get3DPos(this Vector2 point, float elevation) {
            float terrainH = Singleton<TerrainManager>.instance.SampleFinalHeightSmooth(point.x, point.y);
            return point.ToPos(terrainH);
        }

        public static ushort CreateL(Vector2 point1, Vector2 pointL, Vector2 point2, float h, NetInfo info){
            //NetInfo eInfo = info.GetElevated();
            float hBridge = 10;

            Vector3 pos1 = point1.ToPos(h+hBridge);
            Vector3 pos2 = point2.ToPos(h + hBridge);
            Vector3 posL = pointL.ToPos(h + hBridge);

            ushort nodeIDL = CreateNode(posL, info);
            ushort nodeID1 = CreateNode(pos1, info);
            ushort nodeID2 = CreateNode(pos2, info);

            lock (GroundNodes) {
                //GroundNodes.Queue(nodeID1,50);
                //GroundNodes.Queue(nodeID2,50);
            }
            CreateSegment(nodeIDL, nodeID1);
            CreateSegment(nodeIDL, nodeID2);
            
            return nodeIDL;
        }

        class NodeTime {
            public NodeTime(ushort nodeID, float ms=1) {
                NodeID = nodeID;
                ticks = Stopwatch.StartNew();
                this.ms = ms;
            }
            public ushort NodeID;
            public Stopwatch ticks;
            public float ms;
            public bool IsTime => ticks.ElapsedMilliseconds >= ms;
        }

        class NodeList : List<NodeTime> {
            public void Queue(ushort nodeID, float ms=1) => Add(new NodeTime(nodeID,ms));
        }
        static NodeList GroundNodes = new NodeList();

        public class Threading : ThreadingExtensionBase {
            public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
                lock (GroundNodes) {
                    for(int i=0;i< GroundNodes.Count;) {
                        var item = GroundNodes[i];
                        if (item.IsTime) {
                            SetGroundNode(item.NodeID);
                            GroundNodes.Remove(item);
                        } else {
                            i++;
                        }
                    }
                }
            }
        }

        public static ushort CopyMove(ushort segmentID) {
            Helpers.Log("CopyMove");
            Vector3 move = new Vector3(70, 0, 70);
            var segment = segmentID.ToSegment();
            var startPos = segment.m_startNode.ToNode().m_position + move;
            var endPos = segment.m_endNode.ToNode().m_position + move;
            NetInfo info = GetInfo("Basic Road");
            ushort nodeID1 = CreateNode(startPos, info);
            ushort nodeID2 = CreateNode(endPos, info);
            return CreateSegment(nodeID1, nodeID2);
        }
    }
}
