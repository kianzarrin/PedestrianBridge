using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework;
using ICities;

namespace PedestrianBridge.Util {
    public static class VectorUtils {
        public static float Angle(this Vector2 v) => Vector2.Angle(v, Vector2.right);

        public static Vector2 Rotate(this Vector2 v, float angle) => Vector2ByAgnle(v.magnitude, angle + v.Angle());
        public static Vector2 Rotate90CCW(this Vector2 v) => new Vector2(-v.y, +v.x);
        public static Vector2 PerpendicularCCW(this Vector2 v) => v.normalized.Rotate90CCW();
        public static Vector2 Rotate90CC(this Vector2 v) => new Vector2(+v.y, -v.x);
        public static Vector2 PerpendicularCC(this Vector2 v) => v.normalized.Rotate90CC();

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
        public static Vector3 ToPos(this Vector2 v2, float h = 0) => new Vector3(v2.x, h, v2.y);
        public static Vector2 ToPoint(this Vector3 v3) => new Vector2(v3.x, v3.z);
        public static float Height(this Vector3 v3) => v3.y;
    }
}
