using System;
using AutoActMod;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class SmoothPick
{
    static Chara SmoothPickChara = null;

    [HarmonyPatch]
    [HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.OnProgressComplete))]
    static class Progress_Custom_Patch
    {
        static void Prefix(Progress_Custom __instance)
        {
            if (__instance.owner.HasValue() && __instance.owner.IsPCParty && !__instance.owner.IsPC)
            {
                SmoothPickChara = __instance.owner;
            }
        }

        static void Postfix(Progress_Custom __instance)
        {
            if (__instance.owner.HasValue() && __instance.owner.IsPCParty && !__instance.owner.IsPC)
            {
                SmoothPickChara = null;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Map), nameof(Map.TrySmoothPick), new Type[] { typeof(Point), typeof(Thing), typeof(Chara) })]
    static bool TrySmoothPick_Patch(Map __instance, Point p, Thing t, Chara c)
    {
        if (!(SmoothPickChara.HasValue() || (c.HasValue() && !c.IsAgent && c.IsPCParty && !c.IsPC)))
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
}