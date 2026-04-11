using System;
using HarmonyLib;
using KSP.UI.Screens;
using UnityEngine;

namespace FourkSP
{
    // Harmony patch bootstrap. Runs once at MainMenu.
    //
    // A postfix is used (instead of a polling loop in RDSceneScaler)
    // because each click on a tech tree node calls RDPartList.Refresh(),
    // which destroys and recreates all tiles via AddPartListItem. The
    // new GameObjects are instantiated with localScale = Vector3.one,
    // and an external poller would always be one frame behind. The
    // Harmony postfix runs in the same stack as AddPartListItem so the
    // tile shows up already scaled on its very first frame.
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RDScalerHarmonyBootstrap : MonoBehaviour
    {
        private const string HARMONY_ID = "com.fourksp.rdscaler";
        private const string TAG = "[4kSP-RD/Harmony]";
        private static bool _patched;

        void Start()
        {
            // Loaded here (not in RDSceneScaler.Start) so the values are
            // ready before the user enters any scene.
            try { RDScalerConfig.Load(); }
            catch (Exception e) { Debug.LogError(TAG + " Load err: " + e); }

            if (_patched) return;
            try
            {
                var h = new Harmony(HARMONY_ID);
                h.PatchAll(typeof(RDScalerHarmonyBootstrap).Assembly);
                _patched = true;
            }
            catch (Exception e)
            {
                Debug.LogError(TAG + " PatchAll err: " + e);
            }
        }
    }

    // Postfix on RDPartList.AddPartListItem: scales the freshly-created
    // UIListItem wrapper so the tile is rendered at the right size on
    // the first frame without flicker.
    //
    // listItems[i] exposes the RDPartListItem (MonoBehaviour on the
    // inner StateButton), not the wrapper. The parent of the button is
    // the real GridLayoutGroup item and holds both the icon and the
    // "poss." label as siblings: scaling the parent carries both along
    // while preserving the stock prefab geometry.
    //
    // The newly added item is always the LAST element of
    // __instance.listItems (the patched method is void and does not
    // return the reference).
    [HarmonyPatch(typeof(RDPartList), "AddPartListItem")]
    public static class Patch_RDPartList_AddPartListItem
    {
        [HarmonyPostfix]
        public static void Postfix(RDPartList __instance)
        {
            try
            {
                if (__instance == null) return;
                var items = __instance.listItems;
                if (items == null || items.Count == 0) return;

                var last = items[items.Count - 1];
                if (last == null || last.transform == null) return;
                var wrapper = last.transform.parent;
                if (wrapper == null) return;

                float s = RDScalerConfig.PartsScale;
                wrapper.localScale = new Vector3(s, s, 1f);
            }
            catch (Exception e)
            {
                Debug.LogError("[4kSP-RD/Harmony] postfix AddPartListItem: " + e);
            }
        }
    }
}
