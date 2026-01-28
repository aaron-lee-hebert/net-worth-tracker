namespace NetWorthTracker.Core.Entities;

public class AlertConfiguration : BaseEntity
{
    public virtual Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Enable/disable all alerts for this user
    /// </summary>
    public virtual bool AlertsEnabled { get; set; } = true;

    /// <summary>
    /// Alert when net worth changes by this percentage (e.g., 5 = 5%)
    /// Set to 0 to disable
    /// </summary>
    public virtual decimal NetWorthChangeThreshold { get; set; } = 5m;

    /// <summary>
    /// Alert when cash runway falls below this many months
    /// Set to 0 to disable
    /// </summary>
    public virtual int CashRunwayMonths { get; set; } = 3;

    /// <summary>
    /// Send monthly snapshot email
    /// </summary>
    public virtual bool MonthlySnapshotEnabled { get; set; } = true;

    /// <summary>
    /// Last time a net worth change alert was sent
    /// </summary>
    public virtual DateTime? LastNetWorthAlertSentAt { get; set; }

    /// <summary>
    /// Last time a cash runway alert was sent
    /// </summary>
    public virtual DateTime? LastCashRunwayAlertSentAt { get; set; }

    /// <summary>
    /// Last time a monthly snapshot was sent
    /// </summary>
    public virtual DateTime? LastMonthlySnapshotSentAt { get; set; }

    /// <summary>
    /// Net worth value when last alert was sent (to calculate change)
    /// </summary>
    public virtual decimal? LastAlertedNetWorth { get; set; }
}
