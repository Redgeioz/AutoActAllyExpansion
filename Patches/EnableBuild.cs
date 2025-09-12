using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class EnableBuild
{
    static Chara Builder;

    [HarmonyTranspiler, HarmonyPatch(typeof(TaskBuild), nameof(TaskBuild.OnProgressComplete))]
    static IEnumerable<CodeInstruction> OnProgressComplete_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((TaskBuild thiz) => { Builder = thiz.owner; }))
            // EClass.pc.held == null
            .MatchStartForward(
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldfld))
            .RemoveInstructions(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((TaskBuild thiz) => EClass.pc.held is Thing t && (t.trait is not TraitBlock || !thiz.pos.HasBlock)))
            // EClass.pc.held.GetRootCard() != EClass.pc
            .MatchStartForward(
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt))
            .SetInstruction(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnableBuild), nameof(Builder))))
            // this.pos.Distance(EClass.pc.pos) > 1
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Call))
            .SetInstruction(
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnableBuild), nameof(Builder))))
            // this.target = (EClass.pc.held.category.installOne ? EClass.pc.held.Split(1) : EClass.pc.held);
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.pc))))
            .RemoveInstructions(12)
            .InsertAndAdvance(Transpilers.EmitDelegate(
                () => Builder.held.category.installOne ? Builder.held.Split(1) : Builder.held))
            .MatchStartForward(
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(EClass), nameof(EClass.pc))))
            .Repeat(matcher => matcher
                .SetInstruction(
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EnableBuild), nameof(Builder)))
                )
            )
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