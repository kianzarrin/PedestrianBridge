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

        public override void OnCheckChanged(UIComponent component, bool value) {
            base.OnCheckChanged(component, value);
            ControlCenter.Underground = isChecked;
            ElevationSlider.Instance.Refresh();
        }
    }
}
