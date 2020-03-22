using ColossalFramework.UI;
using System;
using UnityEngine;

/* A lot of copy-pasting from Crossings mod by Spectra. The sprites are partly copied as well. */

namespace PedestrianBridge.UI {
    public class UIPanelButton : UIButton {
        public static UIPanelButton Instance;

        private const float BUTTON_HORIZONTAL_POSITION = 79;
        const int SIZE = 31;
        const string PedestrianBridgeButtonBg = "PedestrianBridgeButtonBg";
        const string PedestrianBridgeButtonBgPressed = "PedestrianBridgeButtonBgPressed";
        const string PedestrianBridgeButtonBgHovered = "PedestrianBridgeButtonBgHovered";
        const string PedestrianBridgeIcon = "PedestrianBridgeIcon";
        const string PedestrianBridgeIconPressed = "PedestrianBridgeIconPressed";

        public UIPanelButton() {
            Instance = this;
        }

        public override void Start() {
            name = "PedestrianBridgeButton";
            playAudioEvents = true;
            tooltip = "Pedestrian Bridge Builder";

            var roadsOptionPanel = UIUtils.Instance.FindComponent<UIComponent>("RoadsOptionPanel", null, UIUtils.FindOptions.NameContains);
            var builtinTabstrip = UIUtils.Instance.FindComponent<UITabstrip>("ToolMode", roadsOptionPanel, UIUtils.FindOptions.None);
            UIButton uibutton = (UIButton)builtinTabstrip.tabs[0];

            string[] spriteNames = new string[]
            {
                PedestrianBridgeButtonBg,
                PedestrianBridgeButtonBgPressed,
                PedestrianBridgeButtonBgHovered,
                PedestrianBridgeIcon,
                PedestrianBridgeIconPressed
            };
            if (ResourceLoader.GetAtlas("PedestrianBridgeUI") == UIView.GetAView().defaultAtlas) {
                atlas = ResourceLoader.CreateTextureAtlas("sprites.png", "PedestrianBridgeUI", uibutton.atlas.material, SIZE, SIZE, spriteNames);
            } else {
                atlas = ResourceLoader.GetAtlas("PedestrianBridgeUI");
            }

            normalBgSprite = focusedBgSprite = disabledBgSprite = PedestrianBridgeButtonBg;
            hoveredBgSprite  = PedestrianBridgeButtonBgHovered;
            pressedBgSprite  = PedestrianBridgeButtonBgPressed;

            normalFgSprite = disabledFgSprite = hoveredFgSprite = pressedFgSprite = PedestrianBridgeIcon;
            focusedFgSprite = PedestrianBridgeIconPressed;

            relativePosition = new Vector3(BUTTON_HORIZONTAL_POSITION, 38f); //RABOUT:23,38     Crossings:94,38
            size = new Vector2(SIZE, SIZE);
        }

        //protected override void OnClick(UIMouseEventParameter p)
        //{
        //    base.OnClick(p);
        //    //Debug.Log("Clicked!!");
        //    if (UIWindow2.instance == null)
        //        return;
        //    UIWindow2.instance.enabled = !UIWindow2.instance.enabled;
        //}

        /*public void FocusSprites()
        {
            Debug.Log("focus icon");
            focusedBgSprite = "PedestrianBridgeButtonBgPressed";
            normalBgSprite = "PedestrianBridgeButtonBgPressed";
            normalFgSprite = (disabledFgSprite = (hoveredFgSprite = "PedestrianBridgeIconPressed"));
            focusedFgSprite = "PedestrianBridgeIconPressed";
            size = new Vector2(33, 33);
            size = new Vector2(32, 32);
        }

        public void UnfocusSprites()
        {
            Debug.Log("unfocus icon");
            focusedBgSprite = "PedestrianBridgeButtonBg";
            normalBgSprite = "PedestrianBridgeButtonBg";
            normalFgSprite = (disabledFgSprite = (hoveredFgSprite = "PedestrianBridgeIcon"));
            focusedFgSprite = "PedestrianBridgeIcon";
            size = new Vector2(33, 33);
            size = new Vector2(32, 32);
        }*/

        public static UIButton CreateButton() {
            var roadsOptionPanel = UIUtils.Instance.FindComponent<UIComponent>("RoadsOptionPanel", null, UIUtils.FindOptions.NameContains);
            if (roadsOptionPanel && !Instance) {
                try {
                    return roadsOptionPanel.AddUIComponent<UIPanelButton>();
                }
                catch {
                }
            }
            Debug.LogWarning("Failed to create UIPanelButton!");
            return null;
        }
    }


    public class UIUtils {
        // Token: 0x17000006 RID: 6
        // (get) Token: 0x06000038 RID: 56 RVA: 0x00004CF0 File Offset: 0x00002EF0
        public static UIUtils Instance {
            get {
                bool flag = UIUtils.instance == null;
                if (flag) {
                    UIUtils.instance = new UIUtils();
                }
                return UIUtils.instance;
            }
        }

        // Token: 0x06000039 RID: 57 RVA: 0x00004D20 File Offset: 0x00002F20
        private void FindUIRoot() {
            this.uiRoot = null;
            foreach (UIView uiview in UnityEngine.Object.FindObjectsOfType<UIView>()) {
                bool flag = uiview.transform.parent == null && uiview.name == "UIView";
                if (flag) {
                    this.uiRoot = uiview;
                    break;
                }
            }
        }

        // Token: 0x0600003A RID: 58 RVA: 0x00004D84 File Offset: 0x00002F84
        public string GetTransformPath(Transform transform) {
            string text = transform.name;
            Transform parent = transform.parent;
            while (parent != null) {
                text = parent.name + "/" + text;
                parent = parent.parent;
            }
            return text;
        }

        // Token: 0x0600003B RID: 59 RVA: 0x00004DD0 File Offset: 0x00002FD0
        public T FindComponent<T>(string name, UIComponent parent = null, UIUtils.FindOptions options = UIUtils.FindOptions.None) where T : UIComponent {
            bool flag = this.uiRoot == null;
            if (flag) {
                this.FindUIRoot();
                bool flag2 = this.uiRoot == null;
                if (flag2) {
                    return default(T);
                }
            }
            foreach (T t in UnityEngine.Object.FindObjectsOfType<T>()) {
                bool flag3 = (options & UIUtils.FindOptions.NameContains) > UIUtils.FindOptions.None;
                bool flag4;
                if (flag3) {
                    flag4 = t.name.Contains(name);
                } else {
                    flag4 = (t.name == name);
                }
                bool flag5 = !flag4;
                if (!flag5) {
                    bool flag6 = parent != null;
                    Transform transform;
                    if (flag6) {
                        transform = parent.transform;
                    } else {
                        transform = this.uiRoot.transform;
                    }
                    Transform parent2 = t.transform.parent;
                    while (parent2 != null && parent2 != transform) {
                        parent2 = parent2.parent;
                    }
                    bool flag7 = parent2 == null;
                    if (!flag7) {
                        return t;
                    }
                }
            }
            return default(T);
        }

        // Token: 0x04000024 RID: 36
        private static UIUtils instance = null;

        // Token: 0x04000025 RID: 37
        private UIView uiRoot = null;

        // Token: 0x02000010 RID: 16
        [Flags]
        public enum FindOptions {
            // Token: 0x04000034 RID: 52
            None = 0,
            // Token: 0x04000035 RID: 53
            NameContains = 1
        }
    }
}
