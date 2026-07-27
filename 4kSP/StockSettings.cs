
using System.Collections;
using System.Reflection;

namespace FourkSP
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    // HighLogic.CurrentGame.Parameters.CustomParams<_4kSP>().
    public class _4kSP : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "4kSP"; } }
        public override string DisplaySection { get { return "4kSP"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }


        [GameParameters.CustomParameterUI("Use Stock UI Scale",
            toolTip = "Use the stock UI Scale settings for the map")]
        public bool useStockUIScale = true;


        [GameParameters.CustomFloatParameterUI("UI Scale", minValue = FourkSP.MinScale, maxValue = FourkSP.MaxScale, stepCount = 151, displayFormat = "F2",
            toolTip = "Custom UI Scaling")]
        public float UI_Scale = 1f;

        [GameParameters.CustomFloatParameterUI("Icon Size", minValue = FourkSP.MinIconSize, maxValue = FourkSP.MaxIconSize, stepCount = 151, displayFormat = "F2",
            toolTip = "Icon size on the map & tracking station views")]
        public float UI_IconSize = 1f;


        [GameParameters.CustomFloatParameterUI("Font size (points)", minValue = FourkSP.MinFontSize, maxValue = FourkSP.MaxFontSize, stepCount = 161, displayFormat = "F2")]
        public float UI_FontSize = 12f;

        // Independent of the map-icon settings above: lets someone who
        // doesn't want the R&D Complex scaler (4kSP-RnD.dll) turn it off
        // without uninstalling it. RDSceneScaler reads this directly and
        // reverts everything it changed (zoom cap, parts grid, tile scale)
        // when it goes false; see RDSceneScaler.ResetToStock().
        [GameParameters.CustomParameterUI("Enable R&D Scaler",
            toolTip = "Scale the R&D Complex tech tree parts list for high-DPI displays")]
        public bool rdScalerEnabled = true;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            useStockUIScale = true;
            UI_Scale = 1f;
            //UI_LineSpacing = 1f;
            UI_FontSize = 12;
            rdScalerEnabled = true;
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "useStockUIScale" || member.Name == "rdScalerEnabled")
                return true;
            return !useStockUIScale;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters) 
        {
            return true; 
        }

        public override IList ValidValues(MemberInfo member) { return null; }
    }

}
