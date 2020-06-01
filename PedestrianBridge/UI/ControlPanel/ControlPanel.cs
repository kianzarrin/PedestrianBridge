namespace PedestrianBridge.UI.ControlPanel {
    using ColossalFramework;
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using UnityEngine;

    public class ControlPanel : UIAutoSizePanel {
        public static readonly SavedFloat SavedX = new SavedFloat(
            "PanelX", ModSettings.FILE_NAME, 87, true);
        public static readonly SavedFloat SavedY = new SavedFloat(
            "PanelY", ModSettings.FILE_NAME, 58, true);

        #region Instanciation
        public static ControlPanel Instance { get; private set; }


        public static ControlPanel Create() {
            var uiView = UIView.GetAView();
            ControlPanel panel = uiView.AddUIComponent(typeof(ControlPanel)) as ControlPanel;
            return panel;
        }

        public static void Release() {
            Destroy(Instance);
        }

        #endregion Instanciation

        public override void Awake() {
            base.Awake();
            Instance = this;
            isVisible = false;
        }

        public override void Start() {
            base.Start();
            Log.Debug("ControlPanel started");

            width = 250;
            name = "ControlPanel";
            backgroundSprite = "MenuPanel2";
            absolutePosition = new Vector3(SavedX, SavedY);


            {
                var dragHandle_ = AddUIComponent<UIDragHandle>();
                dragHandle_.width = width;
                dragHandle_.height = 42;
                dragHandle_.relativePosition = Vector3.zero;
                dragHandle_.target = parent;

                var lblCaption = dragHandle_.AddUIComponent<UILabel>();
                lblCaption.text = "Overpass builder";
                lblCaption.relativePosition = new Vector3(65, 14, 0);

                var sprite = dragHandle_.AddUIComponent<UISprite>();
                sprite.size = new Vector2(40, 40);
                sprite.relativePosition = new Vector3(5, 2.5f, 0);
                sprite.atlas = TextureUtil.GetAtlas(PedestrianBridgeButton.ATLAS_NAME);
                sprite.spriteName = PedestrianBridgeButton.PedestrianBridgeIconPressed;
            }


            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Roundabout Styles:";
                panel.AddUIComponent<StyleStarCheckBox>();
                panel.AddUIComponent<StyleInnerCircleCheckBox>();
            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Elevation";
                label.tooltip = "Height of the pedestrian overpass.";
                var slider = panel.AddUIComponent<ElevationSlider>();
                slider.Label = label;

            }

            {
                var panel = AddPanel();
                panel.AddUIComponent<UnderpassCheckbox>();
            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Slope";
                label.tooltip = "steepness of pedesterian paths.";
                var slider = panel.AddUIComponent<SlopeSlider>();
                slider.Label = label;
            }

            //{
            //    var panel = AddPanel();
            //    var button = panel.AddUIComponent<UIResetButton>();
            //}
        }

        UIAutoSizePanel AddPanel() {
            int pad_horizontal = 10;
            int pad_vertical = 5;
            UIAutoSizePanel panel = AddUIComponent<UIAutoSizePanel>();
            panel.width = width - pad_horizontal * 2;
            panel.autoLayoutPadding =
                new RectOffset(pad_horizontal, pad_horizontal, pad_vertical, pad_vertical);
            return panel;
        }

        protected override void OnPositionChanged() {
            base.OnPositionChanged();
            Log.Debug("OnPositionChanged called");

            Vector2 resolution = GetUIView().GetScreenResolution();

            absolutePosition = new Vector2(
                Mathf.Clamp(absolutePosition.x, 0, resolution.x - width),
                Mathf.Clamp(absolutePosition.y, 0, resolution.y - height));

            SavedX.value = absolutePosition.x;
            SavedY.value = absolutePosition.y;
            Log.Debug("absolutePosition: " + absolutePosition);
        }

        public void Open() {
            Show();
            Refresh();
        }

        public void Close() {
            Hide();
        }

        public void Refresh() {
            RefreshSizeRecursive();
        }
    }
}

