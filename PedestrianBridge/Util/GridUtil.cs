using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace PedestrianBridge.Util {
    using static NetUtil;
    using static HelpersExtensions;
    using static VectorUtil;
    using static GridUtil;

    public struct GridVector {
        public int x;
        public int y;
        public GridVector(int x, int y) {
            this.x = x;
            this.y = y;
        }
        public GridVector(Vector2 v) {
            x = ConvertGrid(v.x);
            y = ConvertGrid(v.y);
        }
        public GridVector(Vector3 v) {
            x = ConvertGrid(v.x);
            y = ConvertGrid(v.z);
        }

        public static GridVector CreateFromSegment(ushort segmentID) {
            var a = segmentID.ToSegment().m_startNode.ToNode().m_position;
            var b = segmentID.ToSegment().m_endNode.ToNode().m_position;
            var c = (a + b) * 0.5f;
            return new GridVector(c);
        }

        public static GridVector CreateFromNode(ushort nodeID) {
            var a = nodeID.ToNode().m_position;
            return new GridVector(a);
        }

        public int Index => y* GRID_LENGTH + x;

        public static GridVector up = new GridVector(0, 1);
        public static GridVector down = new GridVector(0, -1);
        public static GridVector left = new GridVector(-1, 0);
        public static GridVector right = new GridVector(1, 0);
        public static GridVector zero = new GridVector(0, 0);

        public static GridVector operator +(GridVector lhs, GridVector rhs) =>
            new GridVector(lhs.x + rhs.x, lhs.y + rhs.y);

        public override string ToString() {
            return $"GridVector<{Index}>({x}, {y})";
        }

    }

    public static class GridUtil {
        public const float GRID_SIZE= 64f;
        public const int GRID_LENGTH = 270;
        public static int ConvertGrid(float a) =>
            Mathf.Clamp((int)(a / GRID_SIZE + (GRID_LENGTH/2)), 0, GRID_LENGTH-1); //270/2=135 279-1=269

        public static IEnumerable<ushort> ScanDirSegment(Vector2 start, Vector2 dir, float dist) {
            foreach (GridVector grid in ScaDir(start, dir, dist)) {
                int index = grid.Index;
                //Log.Debug($"grid={grid} dist={dist}");
                ushort segmentID = 0;
                if (index >= 0 && index < netMan.m_segmentGrid.Length) 
                    segmentID = netMan.m_segmentGrid[index];
                while (segmentID != 0) {
                    yield return segmentID;
                    segmentID = segmentID.ToSegment().m_nextGridSegment;
                }
            }
        }

        public static IEnumerable<GridVector> ScaDir(Vector2 start, Vector2 dir, float dist) {
            GridVector startGrid = new GridVector(start);
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) {
                foreach (GridVector point in scanX(startGrid, dir, dist)) 
                    yield return point;
            } else {
                foreach (GridVector point in scanY(startGrid, dir, dist)) 
                    yield return point;
            } 
        } 

        static IEnumerable<GridVector> scanX(GridVector start, Vector2 dir, float dist) {
            float ratioYX = Mathf.Abs(dir.y / dir.x);
            dist /= GRID_SIZE;
            int distx =  Mathf.CeilToInt(dist / Mathf.Sqrt(1 + ratioYX * ratioYX)); // dist = sqrt(distx^2 + (distx*ratioYX)^2)
            int signx = (int)Mathf.Sign(dir.x);
            int signy = (int)Mathf.Sign(dir.y);

            for (int x = 0; x <= distx; ++x) {
                int y = Mathf.RoundToInt(x * ratioYX);

                GridVector grid = new GridVector(start.x + x*signx, start.y + y*signy);
                yield return grid;
                yield return grid + GridVector.up;
                yield return grid + GridVector.down;
            }
        }

        static IEnumerable<GridVector> scanY(GridVector start, Vector2 dir, float dist) {
            float ratioXY = Mathf.Abs(dir.x / dir.y);
            dist /= GRID_SIZE;
            int disty = Mathf.CeilToInt(dist / Mathf.Sqrt(1 + ratioXY * ratioXY));
            int signx = (int)Mathf.Sign(dir.x);
            int signy = (int)Mathf.Sign(dir.y);

            for (int y = 0; y <= disty; ++y) {
                int x = Mathf.RoundToInt(y * ratioXY);

                GridVector grid = new GridVector(start.x + x * signx, start.y + y * signy);
                yield return grid;
                yield return grid + GridVector.left;
                yield return grid + GridVector.right;
            }
        }
    }
}
