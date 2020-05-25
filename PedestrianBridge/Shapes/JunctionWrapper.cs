namespace PedestrianBridge.Shapes {
    using System.Collections.Generic;
    using Util;
    using System.Linq;
    using KianCommons;
    using static KianCommons.NetUtil;

    public class JunctionWrapper {
        public const int MIN_SEGMENT_COUNT = 2;
        public ushort NodeID { get; private set; } = 0;
        public bool IsValid { get; private set; } = false;

        private int _count;
        private List<LWrapper> _corners;
        private List<ushort> _segList;
        private List<SegmentWrapper> _overPasses;

        public JunctionWrapper(ushort nodeID, NetInfo pathInfo) {
            NodeID = nodeID;
            _segList = GetCCSegList(nodeID).ToList();
            _count = _segList.Count;
            _corners = new List<LWrapper>(_count);
            _overPasses = new List<SegmentWrapper>(_count-1);
            if (_count < MIN_SEGMENT_COUNT) {
                Log.Debug("number of segments is less than " + MIN_SEGMENT_COUNT);
                return;
            }

            for (int i = 0; i < _count; ++i) {
                ushort segID1 = _segList[i], segID2 = _segList[(i + 1) % _count];
                var corner = new LWrapper(segID1, segID2, pathInfo);
                //Log.Info($"created L from segments: {segID1} {segID2}");
                if (corner.Valid) {
                    _corners.Add(corner);
                    this.IsValid = true;
                }
            }

            if (_count < 2)
                return;
            NetInfo info2 = Options.Underground ? pathInfo.GetTunnel() : pathInfo.GetElevated();
            for (int i = 0; i < _count; ++i) {
                var startNode = _corners[i].nodeL;
                var endNode = _corners[(i + 1) % _count].nodeL;
                if (startNode != null && endNode != null) {
                    if (!(_count == 2 && i == 1))
                        continue;
                    SegmentWrapper segment = new SegmentWrapper(
                        startNode, endNode);
                        segment.Create();
                    segment.Info = info2;
                    _overPasses.Add(segment);

                }
            } 
        }

        public static void Create(ushort nodeID) {
            var junction = new JunctionWrapper(nodeID, PrefabUtil.SelectedPrefab);
            if (junction.IsValid)
                junction.Create();
        }

        public void Create() {
            foreach(var corner in _corners)
                corner.Create();
            foreach (var overpass in _overPasses)
                overpass?.Create();

            for (int i = 0; i < _count; ++i)
                TMPEUtil.BanPedestrianCrossings(_segList[i], NodeID);
        }
    }
}
