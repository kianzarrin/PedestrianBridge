using ColossalFramework.UI;
using KianCommons;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class StyleInnerCircleCheckBox : UICheckBoxExt {
        public static StyleInnerCircleCheckBox Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
            Label = "Inner roundabout";
            Tooltip = "creates a pedestrian bridge roundabout inside of the roundabout.";
            Refresh();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            ControlCenter.RoundaboutBridgeStyle = RoundaboutBridgeStyleT.InnerCircle;
            Refresh();
            StyleStarCheckBox.Instance.Refresh();
            Log.Debug($"StyleInnerCircleCheckBox.OnClick(): StyleStarCheckBox = {StyleStarCheckBox.Instance.isChecked}");
        }

        public void Refresh() {
            isChecked = ControlCenter.RoundaboutBridgeStyle == RoundaboutBridgeStyleT.InnerCircle;
            Invalidate();
        }

    }
}
