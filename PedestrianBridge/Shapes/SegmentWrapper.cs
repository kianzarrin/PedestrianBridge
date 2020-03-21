using UnityEngine;
using PedestrianBridge.Util;
using static PedestrianBridge.Util.NetUtil;

namespace PedestrianBridge.Shapes {
    public class SegmentWrapper {
        NodeWrapper startNode;
        NodeWrapper endNode;

        public SegmentWrapper(NodeWrapper startNode, NodeWrapper endNode) {
            this.startNode = startNode;
            this.endNode = endNode;
        }


        public ushort ID;
        public void Create() =>
            simMan.AddAction(_Create);

        void _Create() => ID = CreateSegment(startNode.ID, endNode.ID);

        static ushort CreateSegment(
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

        static ushort CreateSegment(ushort startNodeID, ushort endNodeID, NetInfo info = null) {
            Vector3 startPos = startNodeID.ToNode().m_position;
            Vector3 endPos = endNodeID.ToNode().m_position;
            var dir = endPos - startPos;
            return CreateSegment(startNodeID, endNodeID, dir, -dir, info);
        }

        static ushort CreateSegment(ushort startNodeID, ushort endNodeID, Vector2 middlePoint, NetInfo info = null) {
            Vector3 startPos = startNodeID.ToNode().m_position;
            Vector3 endPos = endNodeID.ToNode().m_position;
            Vector3 middlePos = middlePoint.ToPos();
            Vector3 startDir = middlePos - startPos;
            Vector3 endDir = middlePos - endPos;
            return CreateSegment(startNodeID, endNodeID, startDir, endDir);
        }

        public static float GetClosestHeight(ushort segmentID, Vector3 Pos) =>
            segmentID.ToSegment().GetClosestPosition(Pos).Height();

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

    }
}
