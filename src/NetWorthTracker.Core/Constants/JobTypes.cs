namespace NetWorthTracker.Core.Constants;

public static class JobTypes
{
    public const string AlertProcessing = "Alert.Processing";
    public const string MonthlySnapshot = "Alert.MonthlySnapshot";
    public const string SnapshotEmail = "Alert.SnapshotEmail";
    public const string EmailQueue = "Email.QueueProcessing";
    public const string SessionCleanup = "Session.Cleanup";
    public const string DataRetention = "Data.Retention";
}
