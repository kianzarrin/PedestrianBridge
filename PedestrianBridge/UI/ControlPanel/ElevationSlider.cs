using ICities;
using KianCommons.UI;
using System;
using UnityEngine;

namespace PedestrianBridge.UI.ControlPanel {
    public class ElevationSlider:UISliderExt {
        public override void Start() {
            base.Start();
            minValue = 6;
            maxValue = 30;
            Refresh();
        }

        protected override void OnValueChanged() {
            int val = Mathf.RoundToInt(value);
            value = val;
            ControlCenter.Elevation = val;
            base.OnValueChanged();
        }

        public void Refresh() {
            value = Math.Abs(ControlCenter.Elevation);
            thumbObject.isEnabled = isEnabled = !ControlCenter.Underground;
            Invalidate();
        }
    }
}
