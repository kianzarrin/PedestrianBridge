using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace PedestrianBridge.Util {
    using static VectorUtils;
    using static HelpersExtensions;

    public static class PosUtils {




        public struct MiddleRabout {
            //inputs:
            public Vector2 Junction1, Junction2;// no nodes exist in CC rotation from junction1 to junction2.
            public float HWpb, HWr; // half widths of Pedesterian bridge, roundabout road respectively.

            // outputs: relative position of 3 nodes for the pedastrian bridge v1->vM<-V2
            public Vector2 Vm;

            public Vector2 Calculate() {
                AssertEqual(Junction1.magnitude, Junction2.magnitude, "expected circular roundabout.");
                float HW = HWpb + HWr;
                float r = Junction1.magnitude + HW;
                float angle = (Junction1.Angle() + Junction2.Angle())/2f;
                return Vm = Vector2ByAgnle(r, angle);
            }
        }

 
    }
}
