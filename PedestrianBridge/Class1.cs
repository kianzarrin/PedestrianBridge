using ColossalFramework;
using ColossalFramework.Math;
using PedestrianBridge.Util;
using System;
using System.Threading;
using UnityEngine;

namespace KianHoverElements.Utils {
    using static NetTool;
    public class Class1 : ToolBase {

        int m_elevation = 10;
        ControlPoint[] m_controlPoints = new ControlPoint[3];
        int m_controlPointCount =0;
        NetInfo m_prefab => PrefabUtils.Value;
        ToolController m_toolController => ToolsModifierControl.toolController;

        Ray m_mouseRay;
        float m_mouseRayLength;
        bool m_mouseRayValid;
        private float m_lengthTimer = 0f;
        private bool m_lengthChanging;
        bool m_switchingDir = false;
        private object m_cacheLock = new object();
        private ToolErrors m_buildErrors;
        int m_constructionCost;
        private int m_productionRate;

        protected override void OnToolUpdate() {
            base.OnToolUpdate();

        }

        //public override void SimulationStep() {
        //    base.SimulationStep();
        //    NetInfo prefab = m_prefab;
        //    if ((object)prefab != null) {
        //        Vector3 mousePosition = Input.mousePosition;
        //        m_mouseRay = Camera.main.ScreenPointToRay(mousePosition);
        //        m_mouseRayLength = Camera.main.farClipPlane;
        //        m_mouseRayValid = (!m_toolController.IsInsideUI && Cursor.visible);
        //        if (m_lengthTimer > 0f) {
        //            m_lengthTimer = Mathf.Max(0f, m_lengthTimer - Time.deltaTime);
        //        }
        //        prefab.m_netAI.GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, GetElevation(prefab));
        //        //ToolBase.ForceInfoMode(mode, subMode);
        //        if (mode == InfoManager.InfoMode.None && (prefab.m_netAI.GetCollisionLayers() & ItemClass.Layer.MetroTunnels) != 0) {
        //            Singleton<TransportManager>.instance.TunnelsVisible = true;
        //        } else {
        //            Singleton<TransportManager>.instance.TunnelsVisible = false;
        //        }
        //        if (prefab.m_netAI.ShowTerrainTopography()) {
        //            Singleton<TerrainManager>.instance.RenderTopography = true;
        //            Singleton<TerrainManager>.instance.RenderZones = false;
        //            Singleton<NetManager>.instance.RenderDirectionArrows = false;
        //        } else {
        //            Singleton<TerrainManager>.instance.RenderTopography = false;
        //            Singleton<TerrainManager>.instance.RenderZones = true;
        //            Singleton<NetManager>.instance.RenderDirectionArrows = ((prefab.m_vehicleTypes & ~VehicleInfo.VehicleType.Bicycle) != 0);
        //        }
        //    }
        //}

        public override void SimulationStep() {
            var netInfo = m_prefab;
            Vector3 position = m_controlPoints[m_controlPointCount].m_position;
            bool bRaycastFailed = false;
            {
                ControlPoint cPoint = default(ControlPoint);
                float elevation = GetElevation(netInfo);
                NetNode.Flags flags;
                NetSegment.Flags ignoreSegmentFlags;
                //if ((m_mode == Mode.Curved || m_mode == Mode.Freeform) && m_controlPointCount == 1) ... else
                {
                    flags = NetNode.Flags.ForbidLaneConnection;
                    ignoreSegmentFlags = NetSegment.Flags.Untouchable;
                }
                Building.Flags ignoreBuildingFlags = (!netInfo.m_snapBuildingNodes) ? Building.Flags.All : Building.Flags.Untouchable;

                if (m_mouseRayValid &&
                    MakeControlPoint(
                        m_mouseRay,
                        m_mouseRayLength,
                        netInfo,
                        flags,
                        ignoreSegmentFlags,
                        ignoreBuildingFlags,
                        elevation,
                        out cPoint)) {
                    if (m_controlPointCount != 0) {
                        ControlPoint oldPoint3 = m_controlPoints[m_controlPointCount - 1];
                        cPoint.m_direction = cPoint.m_position - oldPoint3.m_position;
                        cPoint.m_direction.y = 0f;
                        cPoint.m_direction.Normalize();
                        {
                            float minNodeDistance = netInfo.GetMinNodeDistance();
                            minNodeDistance *= minNodeDistance;
                            float num6 = minNodeDistance;
                            if (cPoint.m_segment != 0 && minNodeDistance < num6) {
                                NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[cPoint.m_segment];
                                cPoint.m_position = netSegment.GetClosestPosition(cPoint.m_position, cPoint.m_direction);
                            }
                        }
                    }
                } else {
                    bRaycastFailed = true;
                }
                m_controlPoints[m_controlPointCount] = cPoint;
            }
            ToolErrors toolErrors;
            int cost;
            int productionRate;
            if (m_controlPointCount == 2) {
                if (Vector3.SqrMagnitude(position - m_controlPoints[m_controlPointCount].m_position) > 1f) {
                    m_lengthChanging = true;
                }
                ControlPoint startPoint = m_controlPoints[m_controlPointCount - 2];
                ControlPoint middlePoint = m_controlPoints[m_controlPointCount - 1];
                ControlPoint endPoint = m_controlPoints[m_controlPointCount];
                toolErrors = CreateNode(netInfo, startPoint, middlePoint, endPoint, m_nodePositionsSimulation, 1000, test: true, visualize: false, autoFix: true, true, invert: false, m_switchingDir, 0, out ushort node, out ushort segment, out cost, out productionRate);
            } else if (m_controlPointCount == 1) {
                if (Vector3.SqrMagnitude(position - m_controlPoints[m_controlPointCount].m_position) > 1f) {
                    m_lengthChanging = true;
                }
                ControlPoint middlePoint2 = m_controlPoints[1];
                //if ((m_mode != Mode.Curved && m_mode != Mode.Freeform) || middlePoint2.m_node != 0 || middlePoint2.m_segment != 0)
                {
                    middlePoint2.m_node = 0;
                    middlePoint2.m_segment = 0;
                    middlePoint2.m_position = (m_controlPoints[0].m_position + m_controlPoints[1].m_position) * 0.5f;
                    toolErrors = CreateNode(netInfo, m_controlPoints[m_controlPointCount - 1], middlePoint2, m_controlPoints[m_controlPointCount], m_nodePositionsSimulation, 1000, test: true, visualize: false, autoFix: true, true, invert: false, m_switchingDir, 0, out ushort _, out ushort _, out cost, out productionRate);
                }
            } else { // m_controlPointCount == 0 
                m_toolController.ClearColliding();
                toolErrors = ToolErrors.None;
                cost = 0;
                productionRate = 0;
            }
            if (bRaycastFailed) {
                toolErrors |= ToolErrors.RaycastFailed;
            }
            while (!Monitor.TryEnter(m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                m_buildErrors = toolErrors;

                m_constructionCost = cost;
                m_productionRate = productionRate;
            }
            finally {
                Monitor.Exit(m_cacheLock);
            }
        }

        private float GetElevation(NetInfo info) {
            if ((object)info == null) {
                return 0f;
            }
            info.m_netAI.GetElevationLimits(out int min, out int max);
            if (min == max) {
                return 0f;
            }
            return Mathf.Clamp(m_elevation, min * 256, max * 256) / 256f * 12f;
        }

        public ControlPoint MakeLonelyControlPoint() {
            ControlPoint cPoint = default(ControlPoint);
            cPoint.m_elevation = GetElevation(m_prefab);
            return cPoint;
        }

        public static bool MakeControlPoint(Ray ray, float rayLength, NetInfo info, NetNode.Flags ignoreNodeFlags, NetSegment.Flags ignoreSegmentFlags, Building.Flags ignoreBuildingFlags, float elevation, out ControlPoint cPoint) {
            cPoint = default(ControlPoint);
            cPoint.m_elevation = elevation;
            ItemClass connectionClass = info.GetConnectionClass();
            RaycastInput input = new RaycastInput(ray, rayLength);
            RaycastOutput output = default(RaycastOutput);
            input.m_buildObject = info;
            input.m_netSnap = elevation + info.m_netAI.GetSnapElevation();
            input.m_ignoreNodeFlags = ignoreNodeFlags;
            input.m_ignoreSegmentFlags = ignoreSegmentFlags;
            input.m_ignoreBuildingFlags = ignoreBuildingFlags;
            input.m_ignoreNodeFlags |= NetNode.Flags.Underground;
            input.m_netService = new RaycastService(connectionClass.m_service, connectionClass.m_subService, connectionClass.m_layer);
            input.m_buildingService = new RaycastService(connectionClass.m_service, connectionClass.m_subService, ItemClass.Layer.None);
            if ((object)info.m_intersectClass != null) {
                input.m_netService2 = new RaycastService(info.m_intersectClass.m_service, info.m_intersectClass.m_subService, info.m_intersectClass.m_layer);
            }
            input.m_ignoreTerrain = false;
            if (ToolBase.RayCast(input, out output)) {
                if (output.m_building != 0) {
                    output.m_netNode = Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].FindNode(connectionClass.m_service, connectionClass.m_subService, connectionClass.m_layer, output.m_hitPos);
                    output.m_building = 0;
                }
                cPoint.m_position = output.m_hitPos;
                cPoint.m_node = output.m_netNode;
                cPoint.m_segment = output.m_netSegment;
                Vector3 position = cPoint.m_position;
                if (cPoint.m_node != 0) {
                    NetNode netNode = Singleton<NetManager>.instance.m_nodes.m_buffer[cPoint.m_node];
                    cPoint.m_position = netNode.m_position;
                    cPoint.m_direction = Vector3.zero;
                    cPoint.m_segment = 0;
                    if (netNode.Info.m_netAI.IsUnderground()) {
                        cPoint.m_elevation = -netNode.m_elevation;
                    } else {
                        cPoint.m_elevation = (int)netNode.m_elevation;
                    }
                } else if (cPoint.m_segment != 0) {
                    NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[cPoint.m_segment];
                    NetNode netNode2 = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_startNode];
                    NetNode netNode3 = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_endNode];
                    if (!NetSegment.IsStraight(netNode2.m_position, netSegment.m_startDirection, netNode3.m_position, netSegment.m_endDirection)) {
                        netSegment.GetClosestPositionAndDirection(cPoint.m_position, out Vector3 pos, out Vector3 dir);
                        if ((pos - netNode2.m_position).sqrMagnitude < 64f) {
                            cPoint.m_position = netNode2.m_position;
                            cPoint.m_direction = netSegment.m_startDirection;
                            cPoint.m_node = netSegment.m_startNode;
                            cPoint.m_segment = 0;
                        } else if ((pos - netNode3.m_position).sqrMagnitude < 64f) {
                            cPoint.m_position = netNode3.m_position;
                            cPoint.m_direction = netSegment.m_endDirection;
                            cPoint.m_node = netSegment.m_endNode;
                            cPoint.m_segment = 0;
                        } else {
                            cPoint.m_position = pos;
                            cPoint.m_direction = dir;
                        }
                    } else {
                        cPoint.m_position = netSegment.GetClosestPosition(cPoint.m_position);
                        cPoint.m_direction = netSegment.m_startDirection;
                        float num = (cPoint.m_position.x - netNode2.m_position.x) * (netNode3.m_position.x - netNode2.m_position.x) + (cPoint.m_position.z - netNode2.m_position.z) * (netNode3.m_position.z - netNode2.m_position.z);
                        float num2 = (netNode3.m_position.x - netNode2.m_position.x) * (netNode3.m_position.x - netNode2.m_position.x) + (netNode3.m_position.z - netNode2.m_position.z) * (netNode3.m_position.z - netNode2.m_position.z);
                        if (num2 != 0f) {
                            cPoint.m_position = LerpPosition(netNode2.m_position, netNode3.m_position, num / num2, info.m_netAI.GetLengthSnap());
                        }
                    }
                    float num3 = (int)netNode2.m_elevation;
                    float num4 = (int)netNode3.m_elevation;
                    if (netNode2.Info.m_netAI.IsUnderground()) {
                        num3 = 0f - num3;
                    }
                    if (netNode3.Info.m_netAI.IsUnderground()) {
                        num4 = 0f - num4;
                    }
                    cPoint.m_elevation = Mathf.Lerp(num3, num4, 0.5f);
                    if ((netNode2.m_elevation > 0 || netNode3.m_elevation > 0) && cPoint.m_elevation != 0f) {
                        cPoint.m_elevation = Mathf.Max(1f, cPoint.m_elevation);
                    }
                } else {
                    float num5 = 8640f;
                    if (Mathf.Abs(cPoint.m_position.x) >= Mathf.Abs(cPoint.m_position.z)) {
                        if (cPoint.m_position.x > num5 - info.m_halfWidth * 3f) {
                            cPoint.m_position.x = num5 + info.m_halfWidth * 0.8f;
                            cPoint.m_position.z = Mathf.Clamp(cPoint.m_position.z, info.m_halfWidth - num5, num5 - info.m_halfWidth);
                            cPoint.m_outside = true;
                        }
                        if (cPoint.m_position.x < info.m_halfWidth * 3f - num5) {
                            cPoint.m_position.x = 0f - num5 - info.m_halfWidth * 0.8f;
                            cPoint.m_position.z = Mathf.Clamp(cPoint.m_position.z, info.m_halfWidth - num5, num5 - info.m_halfWidth);
                            cPoint.m_outside = true;
                        }
                    } else {
                        if (cPoint.m_position.z > num5 - info.m_halfWidth * 3f) {
                            cPoint.m_position.z = num5 + info.m_halfWidth * 0.8f;
                            cPoint.m_position.x = Mathf.Clamp(cPoint.m_position.x, info.m_halfWidth - num5, num5 - info.m_halfWidth);
                            cPoint.m_outside = true;
                        }
                        if (cPoint.m_position.z < info.m_halfWidth * 3f - num5) {
                            cPoint.m_position.z = 0f - num5 - info.m_halfWidth * 0.8f;
                            cPoint.m_position.x = Mathf.Clamp(cPoint.m_position.x, info.m_halfWidth - num5, num5 - info.m_halfWidth);
                            cPoint.m_outside = true;
                        }
                    }
                    cPoint.m_position.y = NetSegment.SampleTerrainHeight(info, cPoint.m_position, timeLerp: false, elevation);
                }
                if (cPoint.m_node != 0) {
                    NetNode netNode4 = Singleton<NetManager>.instance.m_nodes.m_buffer[cPoint.m_node];
                    if ((netNode4.m_flags & ignoreNodeFlags) != 0) {
                        cPoint.m_position = position;
                        cPoint.m_position.y = NetSegment.SampleTerrainHeight(info, cPoint.m_position, timeLerp: false, elevation);
                        cPoint.m_node = 0;
                        cPoint.m_segment = 0;
                        cPoint.m_elevation = elevation;
                    }
                } else if (cPoint.m_segment != 0) {
                    NetSegment netSegment2 = Singleton<NetManager>.instance.m_segments.m_buffer[cPoint.m_segment];
                    if ((netSegment2.m_flags & ignoreSegmentFlags) != 0) {
                        cPoint.m_position = position;
                        cPoint.m_position.y = NetSegment.SampleTerrainHeight(info, cPoint.m_position, timeLerp: false, elevation);
                        cPoint.m_node = 0;
                        cPoint.m_segment = 0;
                        cPoint.m_elevation = elevation;
                    }
                }
                return true;
            }
            return false;
        }
        private static Vector3 LerpPosition(Vector3 refPos1, Vector3 refPos2, float t, float snap) {
            if (snap != 0f) {
                float magnitude = new Vector2(refPos2.x - refPos1.x, refPos2.z - refPos1.z).magnitude;
                if (magnitude != 0f) {
                    t = Mathf.Round(t * magnitude / snap + 0.01f) * (snap / magnitude);
                }
            }
            return Vector3.Lerp(refPos1, refPos2, t);
        }
    }
}
