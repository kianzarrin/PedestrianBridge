using ColossalFramework.UI;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class StyleStarCheckBox: UICheckBoxExt{
        public static StyleStarCheckBox Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
            Label = "Star";
            Tooltip = "All paths are connected at the center";
            Refresh();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            ControlCenter.RoundaboutBridgeStyle = RoundaboutBridgeStyleT.Star;
            Refresh();
            StyleInnerCircleCheckBox.Instance.Refresh();

        }

        public void Refresh() {
            isChecked = ControlCenter.RoundaboutBridgeStyle == RoundaboutBridgeStyleT.Star;
            Invalidate();
        }

    }
}
