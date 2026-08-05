using System;
using System.Collections.Generic;
using System.IO;
using FourkSP;
using UnityEngine;

namespace _4kSP_RnD
{
    // Persists the mod-window scaler settings to
    // GameData/4kSP/PluginData/ModWindowScaler.cfg using KSP's native
    // ConfigNode format. Follows the same Load()/Save() shape as
    // RDScalerConfig (4kSP-RnD), including per-assembly overrides which
    // don't map to a single slider and are edited by hand or from the log
    // produced when LogWindows is enabled (see ModWindowScalerUI).
    public static class ModWindowScalerConfig
    {
        public const float DefaultScale = 1.5f;
        public const float MinScale = 1.0f;
        public const float MaxScale = 3.0f;

        public static bool Enabled = true;
        public static bool UseStockUIScale = true;   // true: follow GameSettings.UI_SCALE
        public static float Scale = DefaultScale;     // used only when UseStockUIScale = false
        public static bool KeepOnScreen = true;
        public static bool LogWindows = false;

        // Per-assembly scale overrides. A value of 1 acts as an exclude
        // (the assembly's windows are left untouched).
        public static readonly Dictionary<string, float> Overrides =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public static float CurrentScale()
        {
            return UseStockUIScale ? GameSettings.UI_SCALE : Scale;
        }

        // Combines the global on/off switch above (this cfg file, applies
        // pre-game-load too, e.g. MainMenu) with the per-save Difficulty
        // Settings toggle (FourkSP._4kSP.modWindowScalerEnabled, only
        // meaningful once a game is loaded). Both must be true for windows
        // to be scaled. This is what Patch_IMGUI_Window actually checks.
        public static bool EffectiveEnabled
        {
            get
            {
                if (!Enabled) return false;
                Game game = HighLogic.CurrentGame;
                if (game == null) return true;
                return game.Parameters.CustomParams<_4kSP>().modWindowScalerEnabled;
            }
        }

        public static float ScaleFor(string assemblyName)
        {
            float s;
            if (assemblyName != null && Overrides.TryGetValue(assemblyName, out s))
                return s;
            return CurrentScale();
        }

        private static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    KSPUtil.ApplicationRootPath,
                    "GameData/4kSP/PluginData/ModWindowScaler.cfg");
            }
        }

        private const string TAG = "[4kSP-ModWindows/Cfg]";
        private const string ROOT = "MODWINDOWSCALER";

        // Loads the file; creates it with defaults if missing.
        public static void Load()
        {
            try
            {
                string path = ConfigPath;

                if (!File.Exists(path))
                {
                    Save();
                    return;
                }

                ConfigNode root = ConfigNode.Load(path);
                ConfigNode node = root != null ? root.GetNode(ROOT) : null;
                if (node == null)
                {
                    Save();
                    return;
                }

                bool b = false;
                if (node.TryGetValue("enabled", ref b)) Enabled = b;
                if (node.TryGetValue("useStockUIScale", ref b)) UseStockUIScale = b;
                if (node.TryGetValue("keepOnScreen", ref b)) KeepOnScreen = b;
                if (node.TryGetValue("logWindows", ref b)) LogWindows = b;

                float f = 0f;
                if (node.TryGetValue("scale", ref f))
                    Scale = Mathf.Clamp(f, MinScale, MaxScale);

                Overrides.Clear();
                foreach (ConfigNode ov in node.GetNodes("OVERRIDE"))
                {
                    string name = ov.GetValue("assembly");
                    string val = ov.GetValue("scale");
                    float parsed;
                    if (!string.IsNullOrEmpty(name) && val != null
                        && float.TryParse(val, out parsed))
                    {
                        Overrides[name.Trim()] = parsed;
                    }
                }
                foreach (string ex in node.GetValues("exclude"))
                {
                    if (!string.IsNullOrEmpty(ex)) Overrides[ex.Trim()] = 1f;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Load err: " + e);
            }
        }

        // Writes current state to disk. Creates the folder if missing.
        // Note: Overrides is intentionally NOT rewritten here, so hand
        // edits / exclude comments in the file survive a Save() triggered
        // from the settings window (which only touches the scalar fields).
        public static void Save()
        {
            try
            {
                string path = ConfigPath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                ConfigNode root;
                ConfigNode node;
                if (File.Exists(path))
                {
                    // Preserve OVERRIDE / exclude entries and comments already
                    // on disk; only the scalar fields are (re)written.
                    root = ConfigNode.Load(path) ?? new ConfigNode();
                    node = root.GetNode(ROOT);
                    if (node == null)
                    {
                        node = root.AddNode(ROOT);
                    }
                    else
                    {
                        node.RemoveValues("enabled");
                        node.RemoveValues("useStockUIScale");
                        node.RemoveValues("scale");
                        node.RemoveValues("keepOnScreen");
                        node.RemoveValues("logWindows");
                    }
                }
                else
                {
                    root = new ConfigNode();
                    node = root.AddNode(ROOT);
                }

                node.AddValue("enabled", Enabled);
                node.AddValue("useStockUIScale", UseStockUIScale);
                node.AddValue("scale", Scale);
                node.AddValue("keepOnScreen", KeepOnScreen);
                node.AddValue("logWindows", LogWindows);

                root.Save(path);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Save err: " + e);
            }
        }

        public static void ResetDefaults()
        {
            Enabled = true;
            UseStockUIScale = true;
            Scale = DefaultScale;
            KeepOnScreen = true;
            LogWindows = false;
        }
    }
}
