using System.Collections.Generic;
using AASettings = AutoActMod.Settings;
using AutoActMod.Actions;

namespace AutoActAllyExpansion.Actions;

public class AutoActTrain(AI_PracticeDummy source) : AutoAct(source)
{
    public AI_PracticeDummy practice = source;
    public AI_PracticeDummy Child => child as AI_PracticeDummy;
    public Card Target
    {
        get => Child.target;
        set => Child.target = value;
    }
    public override Point Pos => Target.pos;

    public static AutoActTrain TryCreate(AIAct source)
    {
        if (!Settings.Enable || source is not AI_PracticeDummy a) { return null; }
        return new AutoActTrain(a);
    }

    public override IEnumerable<Status> Run()
    {
        Target = FindThing(t => t.trait is TraitTrainingDummy, AASettings.DetRangeSq);
        if (Target.IsNull())
        {
            yield return Fail();
        }

        yield return DoGoto(Pos, 1, true);
        yield return SetNextTask(practice);
    }
}