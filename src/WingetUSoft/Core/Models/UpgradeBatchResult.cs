namespace WingetUSoft;

public sealed class UpgradeBatchItemResult
{
    public string PackageId { get; init; } = "";
    public UpgradeResult Result { get; init; } = new();
}

public sealed class UpgradeBatchResult
{
    public bool UserCancelled { get; init; }
    public bool CancelledAfterCurrentPackage { get; init; }
    public int ExitCode { get; init; }
    public string ErrorOutput { get; init; } = "";
    public List<UpgradeBatchItemResult> Items { get; init; } = [];
}

public sealed class UpgradeBatchStatusInfo
{
    public string Phase { get; init; } = "";
    public string PackageId { get; init; } = "";
    public int CurrentIndex { get; init; }
    public int TotalCount { get; init; }
}
