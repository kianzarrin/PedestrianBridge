using ColossalFramework;
using ColossalFramework.Math;
using PedestrianBridge.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PedestrianBridge.Util {
    public static class RenderUtil {
        /// <summary>
        /// Draws a half sausage at segment end.
        /// </summary>
        /// <param name="segmentId"></param>
        /// <param name="cut">The lenght of the highlight [0~1] </param>
        /// <param name="bStartNode">Determines the direction of the half sausage.</param>
        public static void DrawCutSegmentEnd(RenderManager.CameraInfo cameraInfo,
                       ushort segmentId,
                       float cut,
                       bool bStartNode,
                       Color color,
                       bool alpha = false) {
            if (segmentId == 0) {
                return;
            }
            ref NetSegment segment = ref segmentId.ToSegment();
            float width = segment.Info.m_halfWidth;

            NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
            bool IsMiddle(ushort nodeId) => (nodeBuffer[nodeId].m_flags & NetNode.Flags.Middle) != 0;

            Bezier3 bezier;
            bezier.a = segment.m_startNode.ToNode().m_position;
            bezier.d = segment.m_endNode.ToNode().m_position;

            NetSegment.CalculateMiddlePoints(
                bezier.a,
                segment.m_startDirection,
                bezier.d,
                segment.m_endDirection,
                IsMiddle(segment.m_startNode),
                IsMiddle(segment.m_endNode),
                out bezier.b,
                out bezier.c);

            if (bStartNode) {
                bezier = bezier.Cut(0, cut);
            } else {
                bezier = bezier.Cut(1 - cut, 1);
            }

            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawBezier(
                cameraInfo,
                color,
                bezier,
                width * 2f,
                bStartNode ? 0 : width,
                bStartNode ? width : 0,
                -1f,
                1280f,
                false,
                alpha);
        }

        public static void RenderSegmnetOverlay(
            RenderManager.CameraInfo cameraInfo,
            ushort segmentId,
            Color color,
            bool alphaBlend = false)
        {
            if (segmentId == 0) return;
        
            ref NetSegment segment = ref segmentId.ToSegment();
            float hw = segment.Info.m_halfWidth;

            NetNode[] nodeBuffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
            bool IsMiddle(ushort nodeId) => (nodeBuffer[nodeId].m_flags & NetNode.Flags.Middle) != 0;

            Bezier3 bezier;
            bezier.a = segment.m_startNode.ToNode().m_position;
            bezier.d = segment.m_endNode.ToNode().m_position;

            NetSegment.CalculateMiddlePoints(
                bezier.a,
                segment.m_startDirection,
                bezier.d,
                segment.m_endDirection,
                IsMiddle(segment.m_startNode),
                IsMiddle(segment.m_endNode),
                out bezier.b,
                out bezier.c);

            bezier.Render(cameraInfo, color, hw, alphaBlend);


        }

        public static void DrawNodeCircle(RenderManager.CameraInfo cameraInfo,
                           ushort nodeId,
                           Color color,
                           bool alphaBlend = false) {
            float r = 8;
            Vector3 pos = nodeId.ToNode().m_position;
            DrawOverlayCircle(cameraInfo, color, pos, r, alphaBlend);
        }

        public static void DrawOverlayCircle(RenderManager.CameraInfo cameraInfo,
                               Color color,
                               Vector3 position,
                               float raduis,
                               bool alphaBlend =false) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                color,
                position,
                raduis * 2,
                position.y - 100f,
                position.y + 100f,
                false,
                alphaBlend);
        }

        public static void Render(this Bezier3 bezier, RenderManager.CameraInfo cameraInfo, Color color, float hw, bool alphaBlend=false) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            RenderManager.instance.OverlayEffect.DrawBezier(
                cameraInfo, color,
                bezier, hw * 2,
                hw, hw, -1, 1024, false, alphaBlend);

        }

        public static void RenderLine(Segment3 line, RenderManager.CameraInfo cameraInfo, Color color) {
            Singleton<ToolManager>.instance.m_drawCallData.m_overlayCalls++;
            RenderManager.instance.OverlayEffect.DrawSegment(cameraInfo, color, line, 0,
                0,
                -1, 1024, false, true);
        }

        public static void RenderLine(Vector2 a, Vector2 b, RenderManager.CameraInfo cameraInfo, Color color) {
            var line = new Segment3 {
                a = NodeWrapper.Get3DPos(a, 0),
                b = NodeWrapper.Get3DPos(b, 0),
            };
            RenderLine(line, cameraInfo, color);
        }



        public static void RenderGrid(RenderManager.CameraInfo cameraInfo, GridVector grid, Color color) {
            var corner0 = grid.GetGirdStartCorner();
            var corner3 = new GridVector(grid.x + 1, grid.y + 1).GetGirdStartCorner();
            var corner1 = new Vector2(corner0.x, corner3.y);
            var corner2 = new Vector2(corner3.x, corner0.y);
            RenderLine(corner0, corner1, cameraInfo, color);
            RenderLine(corner0, corner2, cameraInfo, color);
            RenderLine(corner3, corner1, cameraInfo, color);
            RenderLine(corner3, corner2, cameraInfo, color);
        }

        public static void RenderGrids(RenderManager.CameraInfo cameraInfo, Vector3 pos, Color color) {
            var grid = new GridVector(pos);
            for (int dx = -2; dx <= +2; ++dx) {
                for (int dy = -2; dy <= +2; ++dy) {
                    var grid2 = grid;
                    grid2.x += dx;
                    grid2.y += dy;
                    RenderGrid(cameraInfo, grid2, color);
                }
            }
        }


    }
}
