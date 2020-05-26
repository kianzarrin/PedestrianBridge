using ColossalFramework.UI;
using ICities;
using ColossalFramework;

namespace PedestrianBridge.UI {
    using Tool;
    using KianCommons.UI;
    public static class ModSettings {
        public const string FILE_NAME = nameof(PedestrianBridge);
        static ModSettings() {
            // Creating setting file - from SamsamTS
            if (GameSettings.FindSettingsFileByName(FILE_NAME) == null) {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FILE_NAME } });
            }
        }

        public static void OnSettingsUI(UIHelperBase helper) {
            UIHelper group = helper.AddGroup("Pedestrian Bridge") as UIHelper;
            UIPanel panel = group.self as UIPanel;
            var keymappings = panel.gameObject.AddComponent<UIKeymappingsPanel>();
            keymappings.AddKeymapping("Activation Shortcut", PedBridgeTool.ActivationShortcut);
        }
    }
}
