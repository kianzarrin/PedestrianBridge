using ColossalFramework.UI;
using ICities;
using KianCommons;
using KianCommons.UI;
using System;
using System.Reflection.Emit;
using UnityEngine;

namespace PedestrianBridge.UI.ControlPanel {
    public class ElevationSlider:UISliderExt {
        public static ElevationSlider Instance { get; private set; }
        public UILabel Label;
        bool started_ = false;

        public override void Awake() {
            base.Awake();
            Instance = this;
            this.stepSize = 100f / diff;
            this.scrollWheelAmount = stepSize;
        }

        public override void Start() {
            base.Start();
            Refresh();
            started_ = true;
        }

        const int low = 9;
        const int high = 30;
        const int diff = high - low;
        public float LinearValue {
            get => this.value * ( diff / 100f ) + low;
            set => this.value = (value - low) * (100f / diff);
        }

        protected override void OnValueChanged() {
            int val = Mathf.RoundToInt(LinearValue);
            base.OnValueChanged();
            if (started_)
                ControlCenter.Elevation = val;
            Invalidate();
            if (Label != null) {
                Label.text = $"Elevation:({val})";
                Label.Invalidate();
            }
        }

        public void Refresh() {
            LinearValue = Math.Abs(ControlCenter.Elevation);
            thumbObject.isEnabled = isEnabled = !ControlCenter.Underground;
            if (ControlCenter.Underground) 
                Label.textColor = Color.grey;
            else
                Label.textColor = Color.white;
            Invalidate();
        }
    }
}
