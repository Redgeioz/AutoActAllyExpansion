using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class Move
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Chara), nameof(Chara.CanReplace))]
    static IEnumerable<CodeInstruction> CanReplace_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Card), "get_IsPC")))
            .SetInstruction(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Move), nameof(IsPCOrAutoActChara))))
            .InstructionEnumeration();
    }

    static bool IsPCOrAutoActChara(Chara chara)
    {
        return chara.IsPC || chara.ai is AutoAct;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), nameof(Chara._Move))]
    static void Move_Patch(Chara __instance, ref Card.MoveType type)
    {
        if (__instance.IsPC || !__instance.IsPCParty || __instance.ai is not AutoAct)
        {
            return;
        }

        type = Card.MoveType.Force;
    }
}