using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableBuild
{
    static HashSet<Point> Field = [];
    static bool IsFieldValid = false;
    static Chara Builder;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoActBuild), nameof(AutoActBuild.OnStart))]
    static void OnStart_Patch(AutoAct __instance)
    {
        if (__instance.owner.IsPC)
        {
            IsFieldValid = false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatch(typeof(AutoActBuild), nameof(AutoActBuild.Init))]
    static class Init_Patch
    {
        static bool Prefix(AutoActBuild __instance)
        {
            if (__instance.Child.IsNull())
            {
                return false;
            }
            return true;
        }

        static void Postfix(AutoActBuild __instance)
        {
            if (__instance.owner.IsNull() || !__instance.owner.IsPCParty)
            {
                return;
            }

            if (IsFieldValid)
            {
                __instance.field = Field;
            }
            else
            {
                IsFieldValid = true;
                Field = __instance.field;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaskBuild), nameof(TaskBuild.OnProgressComplete))]
    static IEnumerable<CodeInstruction> OnProgressComplete_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldfld))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((TaskBuild thiz) =>
                {
                    Builder = thiz.owner;
                    return thiz.held == EClass.pc.held && EClass.pc.held.HasValue();
                }))
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnableBuild), nameof(Builder))))
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld))
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(EClass), "get_pc")))
            .Repeat(matcher =>
            {
                matcher
                    .RemoveInstruction()
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnableBuild), nameof(Builder)))
                    );
            })
            .InstructionEnumeration();
    }
}