using KianCommons;
using KianCommons.Math;
using System;
using UnityEngine;
using static KianCommons.NetUtil;

namespace PedestrianBridge.Shapes {
    public class SegmentWrapper {
        public NodeWrapper StartNode;
        public NodeWrapper EndNode;
        public Vector2 StartDir;
        public Vector2 EndDir;
        //public NetInfo BaseInfo;

        public SegmentWrapper(NodeWrapper startNode, NodeWrapper endNode) {
            this.StartNode = startNode;
            this.EndNode = endNode;
            StartDir = EndDir = Vector2.zero;
        }

        public SegmentWrapper(NodeWrapper startNode, NodeWrapper endNode, Vector2 startDir, Vector2 endDir) {
            this.StartNode = startNode;
            this.EndNode = endNode;
            this.StartDir = startDir;
            this.EndDir = endDir;
        }

        public ushort ID;
        public void Create() =>
            simMan.AddAction(_Create);

        void _Create() {
            if (StartDir == Vector2.zero)
                ID = CreateSegment(StartNode.ID, EndNode.ID, GetFinalNetInfo());
            else {
                ID = CreateSegment(StartNode.ID, EndNode.ID, StartDir.ToCS3D(), EndDir.ToCS3D(), GetFinalNetInfo());
            }
        }

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

        //static ushort CreateSegment(ushort startNodeID, ushort endNodeID, Vector2 middlePoint, NetInfo info = null) {
        //    Vector3 startPos = startNodeID.ToNode().m_position;
        //    Vector3 endPos = endNodeID.ToNode().m_position;
        //    Vector3 middlePos = middlePoint.ToCS3D(); 
        //    Vector3 startDir = middlePos - startPos; // TODO set y to 0
        //    Vector3 endDir = middlePos - endPos; // TODO set y to 0
        //    return CreateSegment(startNodeID, endNodeID, startDir, endDir, info);
        //}

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

        private NetInfo GetFinalNetInfo() {
            int e1 = StartNode.elevation;
            int e2 = EndNode.elevation;
            HelpersExtensions.Assert(!(e1 == 0 && e2 < 0), "Underground road is oppostie way arround");
            int nElevated = 0;
            if (e1 != 0) nElevated++;
            if (e2 != 0) nElevated++;

            switch (nElevated) {
                case 0: return ControlCenter.Info;
                case 1: return ControlCenter.Info1;
                case 2: return ControlCenter.Info2;
                default: throw new Exception("Unreachable code");
            }
        }

    }
}
