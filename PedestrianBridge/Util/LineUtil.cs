
namespace PedestrianBridge.Util {
    using System;
    using UnityEngine;
    using ColossalFramework.Math;

    public static class LineUtil {
        public static bool IntersectLine(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 center) {
            // Line AB represented as a1x + b1y = c1 
            float a1 = B.y - A.y;
            float b1 = A.x - B.x;
            float c1 = a1 * (A.x) + b1 * (A.y);

            // Line CD represented as a2x + b2y = c2 
            float a2 = D.y - C.y;
            float b2 = C.x - D.x;
            float c2 = a2 * (C.x) + b2 * (C.y);

            float determinant = a1 * b2 - a2 * b1;

            if (HelpersExtensions.Equal(determinant, 0)) {
                // The lines are parallel. This is simplified 
                // by returning a pair of FLT_MAX 
                center = Vector2.zero;
                return false;
            } else {
                center.x = (b2 * c1 - b1 * c2) / determinant;
                center.y = (a1 * c2 - a2 * c1) / determinant;
                return true;
            }
        }
        public static bool Intersect(Vector2 point1, Vector2 dir1, Vector2 point2, Vector2 dir2, out Vector2 center) {
            return IntersectLine(
                point1, point1 + dir1,
                point2, point2 + dir2,
                out center);
        }

        public static float ArcLength(this Bezier3 beizer) {
            float ret = 0;
            float step = 0.1f;
            for (float t = step; t <= 1f; t += step) {
                float len = (beizer.Position(t) - beizer.Position(t - step)).magnitude;
                ret += len;
            }
            return ret;
        }

        public static float ArcLength(this Bezier2 beizer) {
            float ret = 0;
            float step = 0.1f;
            for (float t = step; t <= 1f; t += step) {
                float len = (beizer.Position(t) - beizer.Position(t - step)).magnitude;
                ret += len;
            }
            return ret;
        }


        public static Bezier2 ToCSBezier2(this Bezier3 bezier) {
            return new Bezier2 {
                a = bezier.a.ToCS2D(),
                b = bezier.b.ToCS2D(),
                c = bezier.c.ToCS2D(),
                d = bezier.d.ToCS2D(),
            };
        }

        public static Bezier2 Bezier2ByDir(Vector2 startPos, Vector2 startDir, Vector2 endPos, Vector2 endDir) {
            NetSegment.CalculateMiddlePoints(
                startPos.ToCS3D(), startDir.ToCS3D(),
                endPos.ToCS3D(), endDir.ToCS3D(),
                false, false,
                out Vector3 MiddlePoint1, out Vector3 MiddlePoint2);
            return new Bezier2 {
                a = startPos,
                d = endPos,
                b = MiddlePoint1.ToCS2D(),
                c = MiddlePoint2.ToCS2D()
            };
        }
    }
}
