using System;
using UnityEngine;
using PedestrianBridge.Util;
using static PedestrianBridge.Util.PrefabUtil;
using static KianCommons.NetUtil;
using KianCommons;
using KianCommons.Math;
using static KianCommons.HelpersExtensions;

namespace PedestrianBridge.Shapes {
    public class NodeWrapper {
        public Vector2 point;
        public int elevation;
        public  NetInfo info; // TODO pass base info.
        public ushort ID { get; private set; }
        public bool IsCreated => ID != 0;

        public NodeWrapper(Vector2 point, int elevation, NetInfo info) {
            this.point = point;
            this.elevation = elevation;
            //if (elevation >= 1)
            //    info = info.GetElevated();
            //else if (elevation <= -9)
            //    info = info.GetsTunnel();
            //else if (elevation <= -1)
            //    info = info.GetSlope();
            //AssertNotNull(info,"info elevation="+elevation);

            this.info = info;
        }

        public void Create() =>
            simMan.AddAction(_Create);

        void _Create() {
            if (IsCreated)
                throw new Exception("Node already has been created");
            Vector3 pos = Get3DPos();
            ID = CreateNode(pos, info);
            ID.ToNode().m_elevation = (byte)Math.Abs(elevation);
            if (elevation == 0) {
                ID.ToNode().m_flags |= NetNode.Flags.Transition | NetNode.Flags.OnGround;
            }else if(elevation < 0) {
                ID.ToNode().m_flags |= NetNode.Flags.Underground;
            } else {

            }
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

        public Vector3 Get3DPos() => Get3DPos(point, elevation);
        public static Vector3 Get3DPos(Vector2 point, int elevation) {
            float terrainH = terrainMan.SampleDetailHeightSmooth(point.ToCS3D(0));
            return point.ToCS3D(terrainH + elevation);
        }
    }
}
