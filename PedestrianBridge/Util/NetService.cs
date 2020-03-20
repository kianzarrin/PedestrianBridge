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
            Log.Info(m);
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
            Log.Info($"creating node for {info.name} at position {position.ToString("000.000")}");
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
            Log.Info($"creating segment for {info.name} between nodes {startNodeID} {endNodeID}");
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

        public class NodeWrapper {
            Vector2 point;
            float h;
            NetInfo info;

            public NodeWrapper(Vector2 point, float h, NetInfo info) {
                this.point = point;
                this.h = h;
                this.info = info;
            }

            public void Create() {
                Vector3 pos = point.ToPos(h);
                ID = CreateNode(pos, info);
            }

            public ushort ID;
        }

        public class SegmentWrapper {
            public SegmentWrapper(NodeWrapper startNode, NodeWrapper endNode) {
                this.startNode = startNode;
                this.endNode = endNode;
            }

            public NodeWrapper startNode;
            public NodeWrapper endNode;
            
            public void Create() {
                ID = CreateSegment(startNode.ID, endNode.ID);
            }

            public ushort ID;
        }


        public class Lwrapper {
            public Lwrapper(Vector2 point1, Vector2 point2, Vector2 pointL, float h, NetInfo groundInfo) {
                NetInfo eInfo = groundInfo.GetElevated();
                nodeL = new NodeWrapper(pointL, h+10, eInfo);
                node1 = new NodeWrapper(point1, h, eInfo);
                node2 = new NodeWrapper(point2, h , eInfo);
                segment1 = new SegmentWrapper(nodeL, node1);
                segment2 = new SegmentWrapper(nodeL, node2);
            }

            public NodeWrapper nodeL;
            NodeWrapper node1;
            NodeWrapper node2;

            SegmentWrapper segment1;
            SegmentWrapper segment2;

            public void Create() {
                simMan.AddAction(nodeL.Create);
                simMan.AddAction(node1.Create);
                simMan.AddAction(node2.Create);
                simMan.AddAction(segment1.Create);
                simMan.AddAction(segment2.Create);
            }
        }

        public static Lwrapper CreateL(Vector2 point1, Vector2 pointL, Vector2 point2, float h, NetInfo info){
            Lwrapper lwrapper = new Lwrapper(point1,point2,pointL,h,info);
            lwrapper.Create();
            return lwrapper;
        }

        public static ushort CopyMove(ushort segmentID) {
            Log.Info("CopyMove");
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
