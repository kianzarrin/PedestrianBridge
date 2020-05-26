using ColossalFramework.UI;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class StyleInnerCircleCheckBox : UICheckBoxExt {
        public override void Awake() {
            base.Awake();
            Label = "Inner roundabout";
            Tooltip = "creates a pedestrian bridge roundabout inside of the roundabout.";
            Refresh();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            ControlCenter.RoundaboutBridgeStyle = RoundaboutBridgeStyleT.InnerCircle;
            Refresh();
            (StyleStarCheckBox.Instance as StyleStarCheckBox).Refresh();
        }

        public void Refresh() {
            isChecked = ControlCenter.RoundaboutBridgeStyle == RoundaboutBridgeStyleT.InnerCircle;
        }

    }
}
