using ColossalFramework.UI;
using UnityEngine;
using KianCommons.UI;
using KianCommons;

namespace PedestrianBridge.UI.ControlPanel {
    public class SlopeSlider : UISliderExt {
        public UILabel Label;
        bool started_ = false;
        public override void Awake() {
            base.Awake();
            this.stepSize = 0.1f * 50f / 3f;
            this.scrollWheelAmount = stepSize;
        }

        public override void Start() {
            base.Start();
            UISprite centerLine = AddUIComponent<UISprite>();
            //TODO add thin center line
            //centerLine.spriteName = "ScrollbarThumb";
            //centerLine.width = 1f;
            //centerLine.height = 22f;
            //centerLine.relativePosition = new Vector2(width / 2- centerLine.width/2, height/2- centerLine.height/2);
            //centerLine.SendToBack();
            //Log.Debug("atlas name = " + atlas.name);
            //Log.Debug("atlas sprites = " + atlas.spriteNames.ToSTR());
            Refresh();
            started_ = true;
        }

        protected override void OnValueChanged() {
            base.OnValueChanged();
            float val = InverseSlope;
            if (started_)
                ControlCenter.InverseSlopeRatio = val;
            Invalidate();
            if (Label != null) {
                var str = (1 / val).ToString("#.#");
                Label.text = $"Slope:({str})";
                Label.Invalidate();
            }
        }

        // convert 0->100 into -3 to 3     
        public float LinearValue {
            get => (value-50)*(3f/50f); 
            set => this.value = value*(50f/3f)+50;
        }

        public float InverseSlope {
            get {
                if (LinearValue > 0)
                    return 1 / (1 + LinearValue); // 1 to 1/4
                else //if value <= 0: 
                    return 1 - LinearValue; // 1 to 4
            }
            set {
                if (value >= 1)
                    LinearValue = 1 - value; // 0 to -3
                else // if value < 1:
                    LinearValue = (1 / value) - 1; // 0 to 3
            }
        }

        public void Refresh() {
            InverseSlope = ControlCenter.InverseSlopeRatio;
        }
    }
}
