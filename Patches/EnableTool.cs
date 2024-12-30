
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableTool
{
    // To make allies' tool available
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Card), "Tool", MethodType.Getter)]
    static bool Tool_Patch(Card __instance, ref Thing __result)
    {
        if (__instance is not Chara chara)
        {
            return true;
        }

        if (!chara.IsPC && chara.ai is AutoAct)
        {
            __result = chara.held as Thing;
            return false;
        }

        return true;
    }

    // To show allies' tool
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TraitTool), "ShowAsTool", MethodType.Getter)]
    static bool ShowAsTool_Patch(TraitTool __instance, ref bool __result)
    {
        if (__instance.owner?.GetRootCard() is not Chara chara)
        {
            return true;
        }

        if (!chara.IsPC && chara.ai is AutoAct)
        {
            __result = false;
            return false;
        }

        return true;
    }
}