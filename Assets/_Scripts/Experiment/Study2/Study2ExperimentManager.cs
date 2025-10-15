using ubco.ovilab.uxf.extensions;
using UXF;

public class Study2ExperimentManager : ExperimentManager<ScenarioBlockData>
{
    protected override void OnSessionBegin(Session session)
    {
        
    }

    protected override void ConfigureBlock(ScenarioBlockData el, Block block, bool lastBlockCancelled)
    {
        
    }

    protected override void OnBlockBegin(Block block)
    {
        
    }

    protected override void OnTrialBegin(Trial trial)
    {
        
    }

    protected override void OnTrialEnd(Trial trial)
    {
        
    }

    protected override void OnBlockEnd(Block block)
    {
        
    }

    protected override void OnSessionEnd(Session session)
    {
        
    }
}

public class ScenarioBlockData: BlockData
{
    public string TakeOverScenario;
    public string UserInterface;
}