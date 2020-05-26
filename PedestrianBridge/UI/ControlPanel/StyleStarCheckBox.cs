using ColossalFramework.UI;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class StyleStarCheckBox: UICheckBoxExt{
        public override void Awake() {
            base.Awake();
            Label = "Star";
            Tooltip = "All paths are connected at the center";
            Refresh();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            ControlCenter.RoundaboutBridgeStyle = RoundaboutBridgeStyleT.Start;
            Refresh();
            (StyleInnerCircleCheckBox.Instance as StyleInnerCircleCheckBox).Refresh();

        }

        public void Refresh() {
            isChecked = ControlCenter.RoundaboutBridgeStyle == RoundaboutBridgeStyleT.Start;
        }

    }
}
