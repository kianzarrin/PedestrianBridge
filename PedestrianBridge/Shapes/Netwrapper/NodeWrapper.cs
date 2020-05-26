using KianCommons;
using KianCommons.Math;
using PedestrianBridge.Util;
using System;
using UnityEngine;
using static KianCommons.HelpersExtensions;
using static KianCommons.NetUtil;
using static PedestrianBridge.Util.PrefabUtil;

namespace PedestrianBridge.Shapes {
    public class NodeWrapper {
        public Vector2 point;
        public int elevation;
        public ushort ID { get; private set; }
        public bool IsCreated => ID != 0;
        public bool Queued { get; private set; } = false;

        //public NetInfo BaseInfo;

        public NodeWrapper(Vector2 point, int elevation) {
            this.point = point;
            this.elevation = elevation;
        }

        public void Create() {
            Queued = true;
            simMan.AddAction(_Create);
        }

        void _Create() {
            if (IsCreated) throw new Exception("Node already has been created");

            ID = CreateNode(Get3DPos(), GetFinalInfo());

            ID.ToNode().m_elevation = (byte)Math.Abs(elevation);
            if (elevation == 0)     ID.ToNode().m_flags |= NetNode.Flags.OnGround | NetNode.Flags.Transition;
            else if (elevation < 0) ID.ToNode().m_flags |= NetNode.Flags.Underground;

            ID.ToNode().m_flags &= ~NetNode.Flags.Moveable;
        }

        static ushort CreateNode(Vector3 position, NetInfo info) {
            HelpersExtensions.AssertNotNull(info, "info");
            Log.Info($"creating node for {info.name} at position {position.ToString("000.000")}");
            bool res = netMan.CreateNode(node: out ushort nodeID, randomizer: ref simMan.m_randomizer,
                info: info, position: position, buildIndex: simMan.m_currentBuildIndex);
            if (!res)
                throw new NetServiceException("Node creation failed");
            simMan.m_currentBuildIndex++;
            return nodeID;
        }


        public NetInfo GetFinalInfo() {
            return elevation == 0 ? ControlCenter.Info : ControlCenter.Info1;
        }

        public Vector3 Get3DPos() => Get3DPos(point, elevation);

        public static Vector3 Get3DPos(Vector2 point, int elevation) {
            float terrainH = terrainMan.SampleDetailHeightSmooth(point.ToCS3D(0));
            return point.ToCS3D(terrainH + elevation);
        }
    }
}
