using ClickThroughFix;
using KSP.UI.Screens;
using ToolbarControl_NS;
using UnityEngine;

namespace _4kSP_ModWindows
{
    // Toolbar button + settings window, mirroring RDSceneScaler's UX
    // (4kSP-RnD) but persistent across every scene rather than scoped to
    // one, since third-party mod windows can appear in the VAB/SPH,
    // Space Center, flight and tracking station alike (MechJeb, PartInfo,
    // WaypointManager, ResearchBodies, ...). Reuses the base mod's
    // toolbar icons (4kSP-38 / 4kSP-24) rather than shipping new art.
    //
    // Per-assembly overrides/excludes are edited in
    // GameData/4kSP/PluginData/ModWindowScaler.cfg (see the OVERRIDE /
    // exclude keys documented in that file's header comment) rather than
    // through this window: they don't map cleanly to a single slider, and
    // LogWindows below tells you exactly which assembly name to use.
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ModWindowScalerUI : MonoBehaviour
    {
        private const string TB_ICON_BIG = "4kSP/PluginData/4kSP-38";
        private const string TB_ICON_SMALL = "4kSP/PluginData/4kSP-24";
        private const string TB_BTN_ID = "4kSPModWindowsBtn";
        private const string TB_TOOLTIP = "4kSP Mod Window Scaler";

        private static readonly ApplicationLauncher.AppScenes TB_SCENES =
            ApplicationLauncher.AppScenes.SPACECENTER
            | ApplicationLauncher.AppScenes.FLIGHT
            | ApplicationLauncher.AppScenes.MAPVIEW
            | ApplicationLauncher.AppScenes.TRACKSTATION
            | ApplicationLauncher.AppScenes.VAB
            | ApplicationLauncher.AppScenes.SPH;

        private ToolbarControl _toolbar;
        private bool _windowShown;
        private int _windowId;
        private Rect _windowRect = new Rect(200, 200, 340, 260);

        private bool _tmpEnabled;
        private bool _tmpUseStock;
        private float _tmpScale;
        private bool _tmpKeepOnScreen;
        private bool _tmpLogWindows;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _windowId = Random.Range(10000, 2000000) + GetType().Name.GetHashCode();
            AddToolbarButton();
        }

        void OnDestroy()
        {
            if (_toolbar != null)
            {
                _toolbar.OnDestroy();
                Destroy(_toolbar);
                _toolbar = null;
            }
        }

        void AddToolbarButton()
        {
            if (_toolbar != null) return;

            GameObject go = new GameObject("4kSPModWindowsToolbar");
            DontDestroyOnLoad(go);
            _toolbar = go.AddComponent<ToolbarControl>();
            _toolbar.AddToAllToolbars(
                OnToolbarTrue, OnToolbarFalse,
                TB_SCENES,
                "4kSP",
                TB_BTN_ID,
                TB_ICON_BIG, TB_ICON_SMALL,
                TB_TOOLTIP);
        }

        void OnToolbarTrue()
        {
            _tmpEnabled = ModWindowScalerConfig.Enabled;
            _tmpUseStock = ModWindowScalerConfig.UseStockUIScale;
            _tmpScale = ModWindowScalerConfig.Scale;
            _tmpKeepOnScreen = ModWindowScalerConfig.KeepOnScreen;
            _tmpLogWindows = ModWindowScalerConfig.LogWindows;
            _windowShown = true;
        }

        void OnToolbarFalse()
        {
            _windowShown = false;
        }

        void ApplyTmp()
        {
            ModWindowScalerConfig.Enabled = _tmpEnabled;
            ModWindowScalerConfig.UseStockUIScale = _tmpUseStock;
            ModWindowScalerConfig.Scale = _tmpScale;
            ModWindowScalerConfig.KeepOnScreen = _tmpKeepOnScreen;
            ModWindowScalerConfig.LogWindows = _tmpLogWindows;
        }

        void OnGUI()
        {
            if (!_windowShown) return;
            GUI.skin = HighLogic.Skin;
            _windowRect = ClickThruBlocker.GUILayoutWindow(
                _windowId, _windowRect, DrawWindow, "4kSP Mod Window Scaler");
        }

        void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            // Only flag the Difficulty Settings toggle specifically - if
            // the checkbox right below is what's off, that's self-evident.
            if (ModWindowScalerConfig.Enabled && !ModWindowScalerConfig.EffectiveEnabled)
                GUILayout.Label("<i>Disabled in Difficulty Settings (\"Enable Mod Window Scaler\").</i>");

            _tmpEnabled = GUILayout.Toggle(_tmpEnabled, "Enabled");

            _tmpUseStock = GUILayout.Toggle(_tmpUseStock, "Use stock UI Scale");

            GUI.enabled = !_tmpUseStock;
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Scale: {0:F2}", _tmpScale), GUILayout.Width(150));
            _tmpScale = GUILayout.HorizontalSlider(_tmpScale,
                ModWindowScalerConfig.MinScale, ModWindowScalerConfig.MaxScale,
                GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            _tmpKeepOnScreen = GUILayout.Toggle(_tmpKeepOnScreen,
                "Keep windows on screen (auto-shrinks windows that don't fit even scaled)");

            _tmpLogWindows = GUILayout.Toggle(_tmpLogWindows,
                "Log every mod that opens a window (KSP.log) - use this to find the\n"
                + "assembly name for an OVERRIDE / exclude entry");

            GUILayout.Space(8);
            GUILayout.Label("<i>Per-mod overrides and excludes are set in\n"
                + "GameData/4kSP/PluginData/ModWindowScaler.cfg</i>");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                ApplyTmp();
            }

            if (GUILayout.Button("Save"))
            {
                ApplyTmp();
                ModWindowScalerConfig.Save();
            }

            if (GUILayout.Button("Default"))
            {
                ModWindowScalerConfig.ResetDefaults();
                _tmpEnabled = ModWindowScalerConfig.Enabled;
                _tmpUseStock = ModWindowScalerConfig.UseStockUIScale;
                _tmpScale = ModWindowScalerConfig.Scale;
                _tmpKeepOnScreen = ModWindowScalerConfig.KeepOnScreen;
                _tmpLogWindows = ModWindowScalerConfig.LogWindows;
            }

            if (GUILayout.Button("Close"))
            {
                _windowShown = false;
                if (_toolbar != null) _toolbar.SetFalse(false);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
