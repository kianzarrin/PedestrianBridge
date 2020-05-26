using ColossalFramework.UI;
using KianCommons.UI;

namespace PedestrianBridge.UI.ControlPanel {
    public class UnderpassCheckbox : UICheckBoxExt {
        public override void Awake() {
            base.Awake();
            Label = "Tunnel";
            Tooltip = "Overpass/Underpass";
            isChecked = ControlCenter.Underground;
        }

        protected override void OnCheckChanged() {
            ControlCenter.Underground = isChecked;
            Invalidate();          
            (ElevationSlider.Instance as ElevationSlider).Refresh();
        }

    }
}
