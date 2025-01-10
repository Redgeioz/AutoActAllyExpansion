using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AutoActMod;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableSteal
{
    static IEnumerable<MethodInfo> TargetMethods() => [
    AccessTools.Method(
            AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass9_0")),
            "<Run>b__2"),
                AccessTools.Method(
            AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass9_0")),
            "<Run>b__3"),
        ];

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.pc))))
            .Repeat(matcher => matcher
                .RemoveInstruction()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(
                        OpCodes.Ldfld,
                        AccessTools.Field(
                            AccessTools.FirstInner(typeof(AI_Steal), t => t.Name.Contains("DisplayClass9_0")), "<>4__this")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(AIAct), "owner"))
                )
            )
            .InstructionEnumeration();
    }

}