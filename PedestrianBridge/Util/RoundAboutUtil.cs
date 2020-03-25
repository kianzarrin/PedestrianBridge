namespace PedestrianBridge.Util {
    using CSUtil.Commons;
    using System.Collections.Generic;
    using System;
    using TrafficManager.Manager.Impl;
    using System.Linq;
    using UnityEngine;
    using static NetUtil;
    using static TMPEUtil;
    using ColossalFramework.Math;

    public class RoundaboutUtil {
        public static RoundaboutUtil Instance = new RoundaboutUtil();
        public RoundaboutUtil() {
            segmentList = new List<ushort>();
        }

        private List<ushort> segmentList = null;

        /// <summary>
        /// Traverses around a roundabout. At each
        /// traversed segment, the given `visitor` is notified.
        /// </summary>
        /// <param name="initialSegmentGeometry">Specifies the segment at which the traversal
        ///     should start.</param>
        /// <param name="visitorFun">Specifies the stateful visitor that should be notified as soon as
        ///     a traversable segment (which has not been traversed before) is found.
        /// pass null if you are trying to see if segment is part of a round about.
        /// </param>
        /// <returns>true if its a roundabout</returns>
        public bool TraverseLoop(ushort segmentId, out List<ushort> segList) {
            this.segmentList.Clear();
            bool ret;
            if (segmentId == 0 || !CalculateIsOneWay(segmentId)) {
                ret = false;
            } else {
                ret = TraverseAroundRecursive(segmentId);
            }
            segList = this.segmentList;
            return ret;
        }

        public struct JunctionData {
            public ushort NodeID;
            public ushort Main1;
            public ushort Main2;
            public ushort Minor;

            public void BanCrossing() {
                BanPedestrianCrossings(Main1, NodeID);
                BanPedestrianCrossings(Main2, NodeID);
                BanPedestrianCrossings(Minor, NodeID);
            }

        }

        public Vector2 CalculateCenter() {
            Vector2 pointAcc = Vector3.zero;
            float totalWieght = 0;
            foreach(var segmentID in segmentList) {
                Bezier2 bezier = segmentID.ToSegment().CalculateSegmentBezier3().ToCSBezier2();
                float weitght = segmentID.ToSegment().m_averageLength;
                Log.Debug($"segment:{segmentID} weitght={weitght}");
                for (float t = 0; t < 1; t+=0.1f) {
                    var pos = bezier.Position(t);
                    pointAcc += pos * weitght;
                    totalWieght += weitght;
                }
            }
            var ret = pointAcc / totalWieght;
            return ret;
        }

        public static ushort Get3rdSegment(ushort nodeID, ushort segmentID1, ushort segmentID2) {
            ref NetNode node = ref nodeID.ToNode();
            if (node.CountSegments() != 3)
                return 0;
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = node.GetSegment(i);
                if (segmentID != 0 && segmentID != segmentID1 && segmentID != segmentID2) {
                    return segmentID;
                }
            }
            return 0;
        }
        public List<JunctionData> GetJunctions() {
            int n = segmentList.Count;
            List<JunctionData> ret = new List<JunctionData>();
            for (int i = 0; i < n; ++i) {
                int i2 = (i + 1) % n;
                var nodeID = GetHeadNode(segmentList[i]);
                var minorSegmentID = Get3rdSegment(nodeID, segmentList[i], segmentList[i2]);
                if (minorSegmentID == 0)
                    continue;
                JunctionData junction = new JunctionData {
                    NodeID = nodeID,
                    Main1 = segmentList[i],
                    Main2 = segmentList[i2],
                    Minor = minorSegmentID,
                };
                ret.Add(junction);
            }
            return ret;
        }

        public static bool IsRoundabout(List<ushort> segList, bool semi = false) {
            try {
                int n = segList?.Count ?? 0;
                if (n <= 1)
                    return false;
                int lastN = semi ? n - 1 : n;
                for (int i = 0; i < lastN; ++i) {
                    ushort prevSegmentID = segList[i];
                    ushort nextSegmentID = segList[(i + 1) % n];
                    ushort headNodeID = GetHeadNode(prevSegmentID);
                    bool isRoundabout = IsPartofRoundabout(nextSegmentID, prevSegmentID);
                    if (!isRoundabout) {
                        //Log._Debug($"segments {prevSegmentID} and {nextSegmentID} with node:{headNodeID} are not part of a roundabout");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception e) {
                Log.Error(e.ToString());
                return false;
            }
        }

        private bool TraverseAroundRecursive(ushort segmentId) {
            if (segmentList.Count > 20) {
                return false; // too long. prune
            }
            segmentList.Add(segmentId);
            var nextSegmentId = GetNextSegment(segmentId);

            if(nextSegmentId !=0) {
                bool isRoundabout;
                if (nextSegmentId == segmentList[0]) {
                    isRoundabout = true;
                } else if (Contains(nextSegmentId)) {
                    isRoundabout = false;
                } else {
                    isRoundabout = TraverseAroundRecursive(nextSegmentId);
                }
                if (isRoundabout) {
                    return true;
                } //end if
            }// end if
            segmentList.Remove(segmentId);
            return false;
        }

        private static ushort GetNextSegment(ushort segmentID) {
            ushort headNodeId = GetHeadNode(segmentID);
            ushort ret = GetCWSegList(headNodeId).Where(
                nextSegmentID => IsPartofRoundabout(nextSegmentID, segmentID)).
                FirstOrDefault();
            return ret;
        }

        /// <summary>
        /// Checks wheather the next segmentId looks like to be part of a roundabout.
        /// Assumes prevSegmentId is oneway
        /// </summary>
        /// <param name="nextSegmentId"></param>
        /// <param name="prevSegmentId"></param>
        /// <param name="headNodeId">head node for prevSegmentId</param>
        /// <returns></returns>
        private static bool IsPartofRoundabout(ushort nextSegmentId, ushort prevSegmentId) {
            ushort headNodeId = GetHeadNode(prevSegmentId);
            bool ret = nextSegmentId != 0 && nextSegmentId != prevSegmentId;
            ret &= CalculateIsOneWay(nextSegmentId);
            ret &= headNodeId == GetTailNode(nextSegmentId);
            return ret;
        }

        /// <summary>
        /// returns true if the given segment is attached to the middle of the
        /// path of segmentList by checking for duplicate nodes.
        /// </summary>
        private bool Contains(ushort segmentId) {
            ushort nodeId = GetHeadNode(segmentId);
            foreach (ushort segId in segmentList) {
                if (GetHeadNode(segId) == nodeId) {
                    return true;
                }
            }
            return false;
        }
    } // end class
}//end namespace