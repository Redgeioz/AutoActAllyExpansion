using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class Misc
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Progress_Custom), nameof(Progress_Custom.CancelWhenMoved), MethodType.Getter)]
    static bool CancelWhenMoved_Patch(Progress_Custom __instance, ref bool __result)
    {
        if (__instance.parent?.parent is AutoAct)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AIAct), nameof(AIAct.Cancel))]
    static IEnumerable<CodeInstruction> Cancel_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Brfalse)
            );

        var instruction = matcher.Instruction.Clone();

        return matcher
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Misc), nameof(CanPickHeld))),
                new CodeInstruction(instruction))
            .InstructionEnumeration();
    }

    static bool CanPickHeld(AIAct a)
    {
        if (a.owner.IsPCParty && (AutoAct.isSetting || a.owner.ai is AutoAct))
        {
            return false;
        }

        return true;
    }
}