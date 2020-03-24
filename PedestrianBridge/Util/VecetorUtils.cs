using System;
using UnityEngine;
using ColossalFramework.Math;

namespace PedestrianBridge.Util {
    public static class VectorUtils {
        public static float Angle(this Vector2 v) => Vector2.Angle(v, Vector2.right);

        public static Vector2 Rotate(this Vector2 v, float angle) => Vector2ByAgnle(v.magnitude, angle + v.Angle());
        public static Vector2 Rotate90CCW(this Vector2 v) => new Vector2(-v.y, +v.x);
        public static Vector2 PerpendicularCCW(this Vector2 v) => v.normalized.Rotate90CCW();
        public static Vector2 Rotate90CW(this Vector2 v) => new Vector2(+v.y, -v.x);
        public static Vector2 PerpendicularCC(this Vector2 v) => v.normalized.Rotate90CW();

        public static Vector2 Extend(this Vector2 v, float magnitude) => NewMagnitude(v, magnitude + v.magnitude);
        public static Vector2 NewMagnitude(this Vector2 v, float magnitude) => magnitude * v.normalized;

        /// <param name="angle">angle in degrees with Vector.right</param>
        public static Vector2 Vector2ByAgnle(float magnitude, float angle) {
            angle *= Mathf.Deg2Rad;
            return new Vector2(
                x: magnitude * Mathf.Cos(angle),
                y: magnitude * Mathf.Sin(angle)
                );
        }
        /// returns rotated vector counter clockwise
        ///
        public static Vector3 ToCS3D(this Vector2 v2, float h = 0) => new Vector3(v2.x, h, v2.y);
        public static Vector2 ToCS2D(this Vector3 v3) => new Vector2(v3.x, v3.z);
        public static float Height(this Vector3 v3) => v3.y;


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

            if (HelpersExtensions.Equal(determinant,0)) {
                // The lines are parallel. This is simplified 
                // by returning a pair of FLT_MAX 
                center = Vector2.zero;
                return false;
            } else {
                center.x = (b2 * c1 - b1 * c2) / determinant;
                center.y = (a1 * c2  - a2 * c1) / determinant;
                return true;
            }
        }
        public static bool Intersect(Vector2 point1, Vector2 dir1, Vector2 point2, Vector2 dir2, out Vector2 center) {
            return IntersectLine(
                point1, point1 + dir1,
                point2, point2 + dir2,
                out center);
        }

    }
}
