using AASettings = AutoActMod.Settings;
using HarmonyLib;
using AutoActMod.Actions;
using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod;
using System;
using System.Reflection;

namespace AutoActAllyExpansion.Patches;

[HarmonyPatch]
static class AutoAct_Patch
{
    static Point LastStartPos;
    static int LastStartDir = 0;
    static bool CanWait() => !EClass.pc.party.members.TrueForAll(chara => chara.IsPC || chara.ai is not AutoAct || !chara.ai.IsRunning);

    static void PCWait()
    {
        AutoAct.SetAutoAct(EClass.pc, new AutoActWait
        {
            canContinue = CanWait,
        });
    }

    [HarmonyPatch]
    static class SetupSettings_Patch
    {
        static MethodInfo TargetMethod() => AccessTools.Method(
            AccessTools.FirstInner(typeof(AASettings), t => t.Name.Contains("<>c")),
            "<SetupSettings>b__56_0"
        );
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_1),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(UIContextMenu), nameof(UIContextMenu.Show), [])))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Settings), nameof(Settings.SetupSettings))))
                .InstructionEnumeration();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnStart))]
    static void OnStart_Patch(AutoAct __instance)
    {
        if (__instance.owner.IsNull() || !__instance.owner.IsPCParty || EClass._zone.IsRegion || !Settings.Enable)
        {
            return;
        }

        var ai = EClass.pc.ai as AutoAct;
        if (!__instance.owner.IsPC || ai is AutoActWait)
        {
            return;
        }

        LastStartPos = ai.startPos;
        LastStartDir = ai.startDir;
        EClass.pc.party.members.ForEach(chara =>
        {
            if (chara.IsPC || chara.ride.HasValue() || chara.host.HasValue())
            {
                return;
            }

            switch (ai)
            {
                case AutoActHarvestMine _:
                    TrySetAutoActHarvestMine(chara);
                    break;
                case AutoActDig _:
                    TrySetAutoActDig(chara);
                    break;
                case AutoActPlow _:
                    TrySetAutoActPlow(chara);
                    break;
                case AutoActBuild _:
                    TrySetAutoActBuild(chara);
                    break;
                case AutoActShear _:
                    TrySetAutoActShear(chara);
                    break;
                case AutoActWater _:
                    TrySetAutoActWater(chara);
                    break;
                case AutoActSteal _:
                    TrySetAutoActSteal(chara);
                    break;
            }
        });

        if (Settings.PCWait && CanWait())
        {
            PCWait();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AutoAct), nameof(AutoAct.OnCancelOrSuccess))]
    static void OnCancelOrSuccess_Patch(AutoAct __instance)
    {
        if (__instance.owner.IsNull() || AutoAct.IsSetting)
        {
            return;
        }

        if (__instance.owner.IsPC)
        {
            EClass.pc.party.members.ForEach(chara =>
            {
                if (chara.IsPC || !chara.ai.IsRunning)
                {
                    return;
                }

                if (chara.ai is AutoAct a && (__instance.status == AIAct.Status.Fail || !a.CanProgress()))
                {
                    a.Fail();
                }
            });

            if (__instance is not AutoActWait && CanWait())
            {
                PCWait();
            }
        }
        else
        {
            var owner = __instance.owner;
            if (owner.held is Chara || owner.things.Find(t => t == owner.held).HasValue())
            {
                owner.PickHeld();
            }
            else
            {
                owner.held = null;
            }
        }
    }

    internal static AutoAct TrySetAutoAct(Chara chara, AIAct a)
    {
        var autoAct = AutoAct.TrySetAutoAct(chara, a);

        autoAct.onStart = a =>
        {
            a.startPos = LastStartPos;
            a.startDir = LastStartDir;
        };

        return autoAct;
    }

    internal static void TrySetAutoActHarvestMine(Chara chara)
    {
        var pc = EClass.pc;
        var ai = pc.ai as AutoActHarvestMine;

        Thing tool = null;
        var axe = chara.things.Find(t => t.trait is TraitTool && t.HasElement(225, 1));
        var pickaxe = chara.things.Find(t => t.trait is TraitTool && t.HasElement(220, 1));
        if (pickaxe.HasValue() || axe.HasValue())
        {
            tool = pickaxe ?? axe;
        }

        if (pc.held?.trait is TraitToolSickle)
        {
            tool = chara.things.Find(t => t.trait is TraitToolSickle);
            if (tool.IsNull())
            {
                return;
            }
        }
        else if (ai.Pos.HasObj)
        {
            var str = ai.Pos.sourceObj.reqHarvest[0];
            if (ai.Pos.sourceObj.HasGrowth)
            {
                tool = axe ?? pickaxe;
                if (!(ai.Pos.cell.CanHarvest() || EClass.sources.elements.alias[str].id == 250) && tool.IsNull())
                {
                    return;
                }
            }
            else if (str == "digging")
            {
                tool = chara.things.Find(t => t.trait is TraitTool && t.HasElement(230, 1));
                if (tool.IsNull())
                {
                    return;
                }
            }
            else if (EClass.sources.elements.alias[str].id == 250)
            {
                tool = pickaxe ?? axe;
            }
            else if (tool.IsNull())
            {
                return;
            }
        }
        else if (tool.IsNull())
        {
            return;
        }

        if (!ai.Pos.HasObj && tool == axe)
        {
            tool = pickaxe;
            if (pickaxe.IsNull())
            {
                return;
            }
        }

        if (tool.HasValue())
        {
            chara.HoldCard(tool);
        }

        if (ai.Child is TaskHarvest th)
        {
            var source = new TaskHarvest
            {
                pos = th.pos.Copy(),
                mode = th.mode,
                target = th.target,
            };
            TrySetAutoAct(chara, source);
        }
        else
        {
            var source = new TaskMine
            {
                pos = ai.Pos.Copy(),
            };
            TrySetAutoAct(chara, source);
        }
    }

    internal static void TrySetAutoActDig(Chara chara)
    {
        var diggingTool = chara.things.Find(t => t.trait is TraitTool && t.HasElement(230, 1));
        if (diggingTool.IsNull())
        {
            return;
        }

        chara.HoldCard(diggingTool);

        var refTask = EClass.pc.ai as AutoActDig;
        var source = new TaskDig
        {
            pos = refTask.Pos.Copy(),
            mode = refTask.Child.mode,
        };

        var autoAct = TrySetAutoAct(chara, source) as AutoActDig;

        autoAct.w = refTask.w;
        autoAct.h = refTask.h;
    }

    internal static void TrySetAutoActPlow(Chara chara)
    {
        var tool = chara.things.Find(t => t.trait is TraitTool && t.HasElement(286, 1));
        if (tool.IsNull())
        {
            return;
        }

        chara.HoldCard(tool);

        var refTask = EClass.pc.ai as AutoActPlow;
        var source = new TaskPlow
        {
            pos = refTask.Pos.Copy(),
        };

        var autoAct = TrySetAutoAct(chara, source) as AutoActPlow;

        autoAct.w = refTask.w;
        autoAct.h = refTask.h;
    }

    internal static void TrySetAutoActPlayMusic(Chara chara)
    {
        var tool = chara.things.Find<TraitToolMusic>();
        if (tool.IsNull())
        {
            return;
        }

        TrySetAutoAct(chara, new AI_PlayMusic { tool = tool });

        chara.HoldCard(tool);
    }

    internal static void TrySetAutoActBuild(Chara chara)
    {
        var ai = EClass.pc.ai as AutoAct;
        var held = EClass.pc.held as Thing;
        var refTask = EClass.pc.ai as AutoActBuild;

        chara.held = held;
        var autoAct = TrySetAutoAct(chara, new TaskBuild
        {
            recipe = held.trait.GetRecipe(),
            held = held,
            pos = ai.Pos.Copy()
        }) as AutoActBuild;

        autoAct.w = refTask.w;
        autoAct.h = refTask.h;
    }

    internal static void TrySetAutoActShear(Chara chara)
    {
        var tool = chara.things.Find<TraitToolShears>();
        if (tool.IsNull())
        {
            return;
        }

        chara.HoldCard(tool);

        var refTask = EClass.pc.ai.child as AI_Shear;
        TrySetAutoAct(chara, new AI_Shear { target = refTask.target });
    }

    internal static void TrySetAutoActWater(Chara chara)
    {
        var tool = chara.things.Find<TraitToolWaterCan>();
        if (tool.IsNull())
        {
            return;
        }

        chara.HoldCard(tool);

        var refTask = EClass.pc.ai as AutoActWater;
        AutoAct.SetAutoAct(EClass.pc, new AutoActWater
        {
            waterFirst = refTask.waterFirst
        });
    }

    internal static void TrySetAutoActSteal(Chara chara)
    {
        var refTask = EClass.pc.ai.child as AI_Steal;
        TrySetAutoAct(chara, new AI_Steal
        {
            target = refTask.target
        });
    }
}