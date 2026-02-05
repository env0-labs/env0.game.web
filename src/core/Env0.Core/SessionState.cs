namespace Env0.Core;

public sealed class SessionState
{
    public bool IsComplete { get; set; }
    public ContextRoute NextContext { get; set; }
    public ContextRoute TerminalReturnContext { get; set; } = ContextRoute.None;
    public string? TerminalStartFilesystem { get; set; }
    public string? RecordsReturnSceneId { get; set; }
    public bool ResumeRecords { get; set; }
    public bool ShowNumericOptions { get; set; }
    public string? MaintenanceMachineId { get; set; }
    public string? MaintenanceFilesystem { get; set; }
    public MaintenanceVariant MaintenanceVariant { get; set; } = MaintenanceVariant.Processing;
    public int InputTicks { get; set; }
    public bool AutomationEnabled { get; set; }
    public int AutomationStartTick { get; set; }
    public int AutomationCompleted { get; set; }
    public int ManualCompletions { get; set; }
    public int BatchesCompleted { get; set; }
    public bool MaintenanceExitUnlocked { get; set; }
    public int AutomationBatchesCreated { get; set; }
    public int NextBatchId { get; set; }
    public List<MaintenanceBatch> MaintenanceBatches { get; } = new List<MaintenanceBatch>();

    // Cross-context UX: avoid reprinting the same objective line every input.
    public string? LastObjectiveKey { get; set; }
    public int LastObjectiveAtTick { get; set; }
}

