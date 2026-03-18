using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableBuild
{
    [HarmonyTranspiler, HarmonyPatch(typeof(TaskBuild), nameof(TaskBuild.OnProgressComplete))]
    static IEnumerable<CodeInstruction> OnProgressComplete_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            // EClass.pc.held == null
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldfld))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                Transpilers.EmitDelegate((TaskBuild thiz) =>
                {
                    return thiz.owner.held is not null
                        && (thiz.owner.ai is not AutoAct
                            || (EClass.pc.held is Thing t && t.trait is TraitBlock && !thiz.pos.HasBlock))
                        && thiz.owner.held.GetRootCard() == (thiz.owner.ai is AutoAct ? EClass.pc : thiz.owner);
                }))
            // EClass.pc.held.GetRootCard() != EClass.pc
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld))
            .RemoveInstructions(7)
            .InstructionEnumeration();
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(TaskPoint), nameof(TaskPoint.Run), MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> TaskPoint_Run_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(AIAct), nameof(AIAct.CanProgress)))
            )
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(AIAct), nameof(AIAct.CanProgress)))
            )
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate((TaskPoint thiz) =>
                {
                    if (thiz is TaskBuild && thiz.parent is AutoActBuild autoAct)
                    {
                        autoAct.CheckHeld();
                    }
                })
            )
            .InstructionEnumeration();
    }
}