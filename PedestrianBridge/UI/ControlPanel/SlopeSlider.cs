using ColossalFramework.UI;
using ICities;
using KianCommons.UI;
using System;
using UnityEngine;

namespace PedestrianBridge.UI.ControlPanel {
    public class SlopeSlider : UISliderExt {
        public override void Start() {
            base.Start();
            minValue = -1;
            maxValue = 1;
            Refresh();
        }

        protected override void OnValueChanged() {
            float val = InverseSlope;
            ControlCenter.InverseSlopeRatio = val;
            tooltip = (1/val).ToString();
            RefreshTooltip();
        }

        // rename this.value to sliderPos to avoid name confusion.
        public float sliderPos {
            get => value; 
            set => this.value = value;
        }

        public float InverseSlope {
            get {
                if (sliderPos > 0)
                    return 1 / (1 + sliderPos); // 1 to 1/4
                else //if value <= 0: 
                    return 1 - sliderPos; // 1 to 4
            }
            set {
                if (value >= 1)
                    sliderPos = 1 - value; // 0 to -3
                else // if value < 1:
                    sliderPos = (1 / value) - 1; // 0 to 3
            }
        }

        public void Refresh() {
            InverseSlope = ControlCenter.InverseSlopeRatio;
            thumbObject.isEnabled = isEnabled = !ControlCenter.Underground;
        }
    }
}
