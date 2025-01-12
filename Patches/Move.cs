using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class Move
{
    [HarmonyTranspiler, HarmonyPatch(typeof(Chara), nameof(Chara.CanReplace))]
    static IEnumerable<CodeInstruction> CanReplace_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Card), nameof(Card.IsPC))))
            .SetInstruction(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Misc), nameof(Misc.IsPCOrAutoActChara))))
            .InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(AI_Goto), nameof(AI_Goto.TryGoTo))]
    static IEnumerable<CodeInstruction> TryGoTo_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Card), nameof(Card.IsPC))),
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ldarg_0))
            .SetInstruction(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Misc), nameof(Misc.IsPCOrAutoActChara))))
            .InstructionEnumeration();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.MoveByForce))]
    static bool MoveByForce_Patch(Chara __instance)
    {
        if (__instance.IsPCParty && !__instance.IsPC && __instance.ai is AutoAct && !__instance.pos.HasBlock)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara._Move))]
    static void Move_Patch(Chara __instance, ref Card.MoveType type)
    {
        if (__instance.IsPC || !__instance.IsPCParty || __instance.ai is not AutoAct)
        {
            return;
        }

        type = Card.MoveType.Force;
    }
}