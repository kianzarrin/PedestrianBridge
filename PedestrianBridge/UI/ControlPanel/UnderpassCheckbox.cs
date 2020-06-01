using ColossalFramework.UI;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class UnderpassCheckbox : UICheckBoxExt {
        public override void Awake() {
            base.Awake();
            Label = "Tunnel";
            Tooltip = "Overpass/Underpass";
        }

        public override void Start() {
            base.Start();
            isChecked = ControlCenter.Underground;
        }

        protected override void OnCheckChanged() {
            ControlCenter.Underground = isChecked;
            Invalidate();          
            ElevationSlider.Instance.Refresh();
        }

    }
}
