using ColossalFramework.Plugins;
using ICities;

namespace PedestrianBridge.Util {
    using KianCommons;
    public static class PluginUtil {
        public static bool FineRoadToolDetected => PluginDetected("FineRoadTool");
        static bool PluginDetected(string name) {
            try {
                foreach (PluginManager.PluginInfo current in PluginManager.instance.GetPluginsInfo()) {
                    IUserMod mod = current.userModInstance as IUserMod;

                    Log.Info("checking plugin ... " + current.name);
                    Log.Info("checking mod ... " + mod.Name);

                    if (current.isEnabled && mod.GetType().Assembly.FullName.Contains(name)) {
                        Log.Info("found plugin" + name);
                        return true;
                    }
                }
            }
            catch { }
            Log.Info("PLUGING NOT FOUND: " + name);
            return false;
        } // end FineRoadToolDetected
    } // end PluginUtils
} // end namesapce

