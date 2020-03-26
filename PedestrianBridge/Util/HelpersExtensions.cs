namespace PedestrianBridge.Util {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using UnityEngine;
    using ICities;
    using System.Diagnostics;

    public static class HelpersExtensions
    {
        internal static AppMode currentMode => SimulationManager.instance.m_ManagersWrapper.loading.currentMode;
        internal static bool CheckGameMode(AppMode mode)
        {
            try
            {
                if (currentMode == mode)
                    return true;
            }
            catch { }
            return false;
        }
        internal static bool InGame => CheckGameMode(AppMode.Game);
        internal static bool InAssetEditor => CheckGameMode(AppMode.AssetEditor);
        internal static bool IsActive =>
#if DEBUG
            InGame || InAssetEditor;
#else
            InGame;
#endif

        internal static string BIG(string m)
        {
            string mul(string s, int i)
            {
                string ret_ = "";
                while (i-- > 0) ret_ += s;
                return ret_;
            }
            m = "  " + m + "  ";
            int n = 120;
            string stars1 = mul("*", n);
            string stars2 = mul("*", (n - m.Length) / 2);
            string ret = stars1 + "\n" + stars2 + m + stars2 + "\n" + stars1;
            return ret;
        }


        /// <summary>
        /// returns a new calling Clone() on all items.
        /// </summary>
        /// <typeparam name="T">item time must be IClonable</typeparam>
        internal static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable =>
            listToClone.Select(item => (T)item.Clone()).ToList();

        /// <summary>
        /// useful for easily debuggin inline functions
        /// to be used like this example:
        /// TYPE inlinefunctionname(...) => expression
        /// TYPE inlinefunctionname(...) => expression.LogRet("messege");
        /// </summary>
        internal static T LogRet<T>(this T a, string m)
        {
            Log.Debug(m + a);
            return a;
        }

        static Stopwatch ticks = null;
        internal static void LogWait(string m) {
            if (ticks == null) {
                Log.Info(m);
                ticks = Stopwatch.StartNew();
            } else if (ticks.Elapsed.TotalSeconds > .5) {
                Log.Info(m);
                ticks.Reset();
                ticks.Start();
            }
        }


        internal static string CenterString(this string stringToCenter, int totalLength)
        {
            int leftPadding = ((totalLength - stringToCenter.Length) / 2) + stringToCenter.Length;
            return stringToCenter.PadLeft(leftPadding).PadRight(totalLength);
        }

        internal static string ToSTR<T>(this IEnumerable<T> segmentList)
        {
            string ret = "{ ";
            foreach (T segmentId in segmentList)
            {
                ret += $"{segmentId}, ";
            }
            ret.Remove(ret.Length - 2, 2);
            ret += " }";
            return ret;
        }

        internal static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        internal static bool ControlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        internal static bool AltIsPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        public static void AssertNotNull(object obj, string m = "") =>
            Assert(obj != null, " unexpected null " + m);

        public static void AssertEqual(float a, float b, string m = "") =>
            Assert(MathUtil.EqualAprox(a, b), "expected {a} == {b} | " + m);

        internal static void Assert(bool con, string m = "") {
            if (!con) {
                m = "Assertion failed: " + m;
                Log.Error(m);
                throw new System.Exception(m);
            }
        }

    }
}
