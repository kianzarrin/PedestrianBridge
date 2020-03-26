using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PedestrianBridge {
    using Util;
    public static class TestsExperiments {
        public static void Run() {
            Vector2 v1, v2;
            v1 = new Vector2(0, 1);
            v2 = new Vector2(-1, -1);
            float angle = Vector2.Angle(v1, v2);
            float angle2 = Vector2.Angle(v2,v1);
            Log.Debug($"angle={angle} angle2={angle2}");
            v2.Normalize();
            angle = Vector2.Angle(v1, v2);
            angle2 = Vector2.Angle(v2, v1);
            Log.Debug($"Normalized: angle={angle} angle2={angle2}");
        }


    }
}
