using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PedestrianBridge {
    using Util;
    public static class TestsExperiments {
        public static void Run() {
            ushort segmentID = 11942;
            if (!NetUtil.IsSegmentValid(segmentID))
                return;

            var vector = GridVector.CreateFromSegment(segmentID);
            Log.Debug($"segmentID:{segmentID} vector:{vector}");

            ushort nodeID = 20084;
            var vector2 = GridVector.CreateFromNode(nodeID);
            Log.Debug($"nodeID:{nodeID} vector2:{vector2}");


        }


    }
}
