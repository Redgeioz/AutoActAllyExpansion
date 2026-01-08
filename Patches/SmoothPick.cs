using AutoActMod;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class SmoothPick
{
    static Chara SmoothPickChara = null;

    [HarmonyPatch, HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressComplete))]
    static class Progress_Custom_Patch
    {
        static void Prefix(Progress_Custom __instance)
        {
            if (__instance.owner?.IsPCParty is true && !__instance.owner.IsPC)
            {
                SmoothPickChara = __instance.owner;
            }
        }

        static void Postfix(Progress_Custom __instance)
        {
            if (__instance.owner?.IsPCParty is true && !__instance.owner.IsPC)
            {
                SmoothPickChara = null;
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.TrySmoothPick), [typeof(Point), typeof(Thing), typeof(Chara)])]
    static bool TrySmoothPick_Patch(Map __instance, Point p, Thing t, Chara c)
    {
        if (SmoothPickChara.IsNull() || (c?.IsPC is false && c != SmoothPickChara))
        {
            return true;
        }

        if (Settings.PickForPC)
        {
            EClass.pc.PickOrDrop(p, t, true);
        }
        else
        {
            SmoothPickChara.PickOrDrop(p, t, true);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.Pick))]
    static bool Pick_Patch(Chara __instance, Thing t, bool msg, bool tryStack)
    {
        if (__instance != SmoothPickChara)
        {
            return true;
        }

        if (Settings.PickForPC)
        {
            EClass.pc.Pick(t, msg, tryStack);
            return false;
        }

        return true;
    }
}