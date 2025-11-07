using ubco.ovilab.uxf.extensions;
using UnityEngine;
using UXF;

public class Study2BaselineExperimentManager : ExperimentManager<BlockData>
{
    [SerializeField] private int practiceTrials = 5;
    [SerializeField] private int trials = 5;
    private int lap = 1;
    protected override void OnSessionBegin(Session session)
    {
        session.settingsToLog.Add("Lap");
        session.trackedObjects.AddRange(FindObjectsByType<Tracker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    protected override void ConfigureBlock(BlockData el, Block block, bool lastBlockCancelled)
    {
        lap = 1;
        block.settings.SetValue(StudyLogs.BlockName, el.name);
        if (el.name == "Baseline-Practice")
        {
            for (int i = 0; i < practiceTrials; i++)
            {
                Trial trial = block.CreateTrial();
            }
        }
        else if (el.name == "Baseline")
        {
            for (int i = 0; i < trials; i++)
            {
                Trial trial = block.CreateTrial();
            }
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Trigger ended");
            HandleTrial();
        }
    }

    private void HandleTrial()
    {
        Trial currentTrial = Session.instance.CurrentTrial;
        currentTrial.settings.SetValue("Lap", lap);
        NextTrial();
        lap++;
    }
    
    private void NextTrial(bool onlyStartNextTrial = false)
    {
        try
        {
            if (!onlyStartNextTrial && Session.instance.InTrial) Session.instance.EndCurrentTrial();
            Session.instance.BeginNextTrial();
        }
        catch (NoSuchTrialException)
        {
            Debug.Log("Block ended (i think)");
        }
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
