using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.Screens;
using ToolbarControl_NS;
using ClickThroughFix;

namespace _4kSP_RnD
{
    // Scales the R&D Complex UI for high-DPI monitors:
    //   1) raises the tech tree zoom cap (UIGridArea.zoomMax)
    //   2) scales the parts list tiles (RDPartList GridLayoutGroup)
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RDSceneScaler : MonoBehaviour
    {
        private const string TAG = "[4kSP-RD]";

        private const string TB_ICON_BIG   = "4kSP/PluginData/4kSP-38";
        private const string TB_ICON_SMALL = "4kSP/PluginData/4kSP-24";
        private const string TB_BTN_ID     = "4kSPRDBtn";
        private const string TB_TOOLTIP    = "4kSP R&D Scaler";

        private static readonly ApplicationLauncher.AppScenes TB_SCENES =
            ApplicationLauncher.AppScenes.SPACECENTER;

        private ToolbarControl _toolbar;
        private GameObject     _toolbarGO;
        private bool   _windowShown;
        private int    _windowId;
        private Rect   _windowRect = new Rect(200, 200, 360, 220);

        private float _tmpMaxZoom;
        private float _tmpPartsScale;
        private bool  _inRnD;

        void Start()
        {
            RDScalerConfig.Load();

            _tmpMaxZoom    = RDScalerConfig.MaxZoom;
            _tmpPartsScale = RDScalerConfig.PartsScale;

            _windowId = Random.Range(10000, 2000000) + GetType().Name.GetHashCode();

            GameEvents.onGUIRnDComplexSpawn.Add(OnRnDSpawn);
            GameEvents.onGUIRnDComplexDespawn.Add(OnRnDDespawn);

            AddToolbarButton();
        }

        void OnDestroy()
        {
            GameEvents.onGUIRnDComplexSpawn.Remove(OnRnDSpawn);
            GameEvents.onGUIRnDComplexDespawn.Remove(OnRnDDespawn);

            if (_toolbarGO != null)
            {
                Destroy(_toolbarGO);
                _toolbarGO = null;
                _toolbar   = null;
            }
        }

        void OnRnDSpawn()
        {
            _inRnD = true;
            StartCoroutine(ApplyWhenReady());
        }

        void OnRnDDespawn()
        {
            _inRnD = false;
        }

        IEnumerator ApplyWhenReady()
        {
            int guard = 0;
            while (RDController.Instance == null && guard++ < 300) yield return null;
            yield return null;

            if (RDController.Instance == null) yield break;

            ApplyAll();
        }

        void ApplyAll()
        {
            PatchZoomLimit();
            ScalePartsGrid();
        }

        void PatchZoomLimit()
        {
            var rd = RDController.Instance;
            var grid = rd != null ? rd.gridArea : null;
            if (grid == null) return;

            grid.zoomMax = RDScalerConfig.MaxZoom;
        }

        // Tile hierarchy under RDPartList.partTransformMask:
        //
        //   [PartList] GridLayoutGroup              <- glg
        //     [PartListItem(Clone)] UIListItem      <- wrapper 50x72 (real GLG item)
        //       [StateButton] RDPartListItem        <- listItems[i] (50x50 icon)
        //       [Text] TextMeshProUGUI "poss."      <- sibling of the button
        //
        // rd.partList.listItems[i] returns the RDPartListItem (MonoBehaviour
        // sitting on the StateButton), not the wrapper. Scaling the button
        // alone scales only the icon and leaves the "poss." label at base
        // size; scaling the parent (UIListItem wrapper) scales icon and
        // label together, preserving the stock prefab geometry.
        //
        // cellSize is kept at its base value so GLG keeps laying out with
        // the original cell footprint, while the wrapper localScale = p
        // inflates rendering. Spacing, padding and constraint count are
        // compensated to avoid overlap and keep everything inside the
        // container:
        //
        //   spacing    += baseCell * (p - 1)     // visible gap = baseSpacing
        //   padding    += baseCell * (p - 1)/2   // pivot (0.5, 0.5) -> symmetric
        //   constraint =  floor(baseCount / p)   // fewer columns
        //
        void ScalePartsGrid()
        {
            var rd = RDController.Instance;
            if (rd == null || rd.partList == null) return;

            var mask = rd.partList.partTransformMask;
            if (mask == null) return;

            var glg = mask.GetComponent<GridLayoutGroup>()
                      ?? mask.GetComponentInChildren<GridLayoutGroup>(true);
            if (glg == null) return;

            CacheGridBase(glg);

            float p = RDScalerConfig.PartsScale;

            glg.cellSize = _baseCell;

            glg.spacing = new Vector2(
                _baseSpacing.x + _baseCell.x * (p - 1f),
                _baseSpacing.y + _baseCell.y * (p - 1f));

            int extraXhalf = Mathf.RoundToInt(_baseCell.x * (p - 1f) * 0.5f);
            int extraYhalf = Mathf.RoundToInt(_baseCell.y * (p - 1f) * 0.5f);
            glg.padding = new RectOffset(
                _basePadding.left   + extraXhalf,
                _basePadding.right  + extraXhalf,
                _basePadding.top    + extraYhalf,
                _basePadding.bottom + extraYhalf);

            if (_baseConstraintCount > 0)
            {
                glg.constraint      = _baseConstraint;
                int cc = Mathf.FloorToInt(_baseConstraintCount / Mathf.Max(0.01f, p));
                glg.constraintCount = Mathf.Max(1, cc);
            }

            // Scale existing items. New items created during
            // RDPartList.Refresh() are caught by the Harmony postfix.
            var items = rd.partList.listItems;
            if (items != null)
            {
                var sv = new Vector3(p, p, 1f);
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it == null || it.transform == null) continue;
                    var wrapper = it.transform.parent;
                    if (wrapper != null) wrapper.localScale = sv;
                }
            }

            // Unity would rebuild the layout next frame; force it now
            // so live slider changes show up without lag.
            LayoutRebuilder.ForceRebuildLayoutImmediate(mask as RectTransform);
        }

        // Initial snapshot of the GridLayoutGroup so ApplyAll always
        // recomputes from a clean base instead of compounding on the
        // current state (which would drift as the slider moves).
        private static Vector2 _baseCell;
        private static Vector2 _baseSpacing;
        private static GridLayoutGroup.Constraint _baseConstraint;
        private static int _baseConstraintCount;
        private static RectOffset _basePadding;
        private static bool _baseGridCached;

        private static void CacheGridBase(GridLayoutGroup glg)
        {
            if (_baseGridCached) return;
            _baseCell            = glg.cellSize;
            _baseSpacing         = glg.spacing;
            _baseConstraint      = glg.constraint;
            _baseConstraintCount = glg.constraintCount;
            // Deep copy the RectOffset: glg.padding is a live reference
            // and would be mutated by subsequent writes.
            var pad = glg.padding;
            _basePadding = new RectOffset(
                pad != null ? pad.left   : 0,
                pad != null ? pad.right  : 0,
                pad != null ? pad.top    : 0,
                pad != null ? pad.bottom : 0);
            _baseGridCached = true;
        }

        void AddToolbarButton()
        {
            if (_toolbar != null) return;

            _toolbarGO = new GameObject("4kSPRDScalerToolbar");
            _toolbar = _toolbarGO.AddComponent<ToolbarControl>();
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
            _tmpMaxZoom    = RDScalerConfig.MaxZoom;
            _tmpPartsScale = RDScalerConfig.PartsScale;
            _windowShown   = true;
        }

        void OnToolbarFalse()
        {
            _windowShown = false;
        }

        void OnGUI()
        {
            if (!_windowShown) return;
            GUI.skin = HighLogic.Skin;
            _windowRect = ClickThruBlocker.GUILayoutWindow(
                _windowId, _windowRect, DrawWindow, "4kSP R&D Scaler");
        }

        void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Max Zoom: {0:F2}", _tmpMaxZoom),
                GUILayout.Width(150));
            float newZoom = GUILayout.HorizontalSlider(_tmpMaxZoom,
                RDScalerConfig.MinZoomLimit, RDScalerConfig.MaxZoomLimit,
                GUILayout.Width(180));
            if (!Mathf.Approximately(newZoom, _tmpMaxZoom))
            {
                _tmpMaxZoom = newZoom;
                RDScalerConfig.MaxZoom = newZoom;
                if (_inRnD) ApplyAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Parts Scale: {0:F2}", _tmpPartsScale),
                GUILayout.Width(150));
            float newParts = GUILayout.HorizontalSlider(_tmpPartsScale,
                RDScalerConfig.MinPartsScale, RDScalerConfig.MaxPartsScale,
                GUILayout.Width(180));
            if (!Mathf.Approximately(newParts, _tmpPartsScale))
            {
                _tmpPartsScale = newParts;
                RDScalerConfig.PartsScale = newParts;
                if (_inRnD) ApplyAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (!_inRnD)
                GUILayout.Label("<i>Enter the R&amp;D Complex to see live changes.</i>");
            else
                GUILayout.Label("<b>Live changes active</b>");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                RDScalerConfig.MaxZoom    = _tmpMaxZoom;
                RDScalerConfig.PartsScale = _tmpPartsScale;
                if (_inRnD) ApplyAll();
            }

            if (GUILayout.Button("Save"))
            {
                RDScalerConfig.MaxZoom    = _tmpMaxZoom;
                RDScalerConfig.PartsScale = _tmpPartsScale;
                if (_inRnD) ApplyAll();
                RDScalerConfig.Save();
            }

            if (GUILayout.Button("Default"))
            {
                _tmpMaxZoom    = RDScalerConfig.DefaultMaxZoom;
                _tmpPartsScale = RDScalerConfig.DefaultPartsScale;
                RDScalerConfig.ResetDefaults();
                if (_inRnD) ApplyAll();
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
