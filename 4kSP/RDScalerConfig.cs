using System;
using System.IO;
using UnityEngine;

namespace FourkSP
{
    // Persists the R&D patch parameters to
    // GameData/4kSP/PluginData/RDScaler.cfg using KSP's native
    // ConfigNode format.
    public static class RDScalerConfig
    {
        // MaxZoom: raises the tech tree zoom cap above stock 100%.
        // PartsScale: scales the parts list tiles (icon + "poss." label)
        // by adjusting GridLayoutGroup cell/spacing and the tile
        // localScale. Column count is reduced automatically so the
        // enlarged tiles still fit the fixed panel width.
        public const float DefaultMaxZoom    = 2.0f;
        public const float DefaultPartsScale = 1.2f;

        // Slider hard caps. Kept generous to cover 8k setups.
        public const float MinZoomLimit  = 1.0f;
        public const float MaxZoomLimit  = 5.0f;
        public const float MinPartsScale = 1.0f;
        public const float MaxPartsScale = 2.5f;

        public static float MaxZoom    = DefaultMaxZoom;
        public static float PartsScale = DefaultPartsScale;

        private static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    KSPUtil.ApplicationRootPath,
                    "GameData/4kSP/PluginData/RDScaler.cfg");
            }
        }

        private const string TAG  = "[4kSP-RD/Cfg]";
        private const string ROOT = "RDSCALER";

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

                var root = ConfigNode.Load(path);
                var node = root != null ? root.GetNode(ROOT) : null;
                if (node == null)
                {
                    Save();
                    return;
                }

                // ConfigNode.TryGetValue takes 'ref' (not 'out'), so the
                // local must be initialised before being passed in.
                float tmp = 0f;
                if (node.TryGetValue("maxZoom", ref tmp))
                    MaxZoom = Mathf.Clamp(tmp, MinZoomLimit, MaxZoomLimit);
                if (node.TryGetValue("partsScale", ref tmp))
                    PartsScale = Mathf.Clamp(tmp, MinPartsScale, MaxPartsScale);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Load err: " + e);
            }
        }

        // Writes current state to disk. Creates the folder if missing.
        public static void Save()
        {
            try
            {
                string path = ConfigPath;
                string dir  = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var root = new ConfigNode();
                var node = root.AddNode(ROOT);
                node.AddValue("maxZoom",    MaxZoom);
                node.AddValue("partsScale", PartsScale);
                node.AddValue("_comment",
                    "maxZoom range [" + MinZoomLimit + "," + MaxZoomLimit +
                    "], partsScale range [" + MinPartsScale + "," + MaxPartsScale + "]");

                root.Save(path);
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " Save err: " + e);
            }
        }

        // Resets in-memory defaults; does not write to disk.
        public static void ResetDefaults()
        {
            MaxZoom    = DefaultMaxZoom;
            PartsScale = DefaultPartsScale;
        }
    }
}
