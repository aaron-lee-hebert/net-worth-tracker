namespace NetWorthTracker.Core.Entities;

public class MonthlySnapshot : BaseEntity
{
    public virtual Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// The month this snapshot is for (first day of month)
    /// </summary>
    public virtual DateTime Month { get; set; }

    /// <summary>
    /// Net worth at end of month
    /// </summary>
    public virtual decimal NetWorth { get; set; }

    /// <summary>
    /// Total assets at end of month
    /// </summary>
    public virtual decimal TotalAssets { get; set; }

    /// <summary>
    /// Total liabilities at end of month
    /// </summary>
    public virtual decimal TotalLiabilities { get; set; }

    /// <summary>
    /// Net worth change from previous month (absolute)
    /// </summary>
    public virtual decimal NetWorthDelta { get; set; }

    /// <summary>
    /// Net worth change from previous month (percentage)
    /// </summary>
    public virtual decimal NetWorthDeltaPercent { get; set; }

    /// <summary>
    /// Account that contributed most to the change
    /// </summary>
    public virtual string? BiggestContributorName { get; set; }

    /// <summary>
    /// Amount of change from biggest contributor
    /// </summary>
    public virtual decimal BiggestContributorDelta { get; set; }

    /// <summary>
    /// Whether biggest contributor was positive or negative
    /// </summary>
    public virtual bool BiggestContributorPositive { get; set; }

    /// <summary>
    /// One-sentence interpretation of the month
    /// </summary>
    public virtual string? Interpretation { get; set; }

    /// <summary>
    /// Whether the email was sent for this snapshot
    /// </summary>
    public virtual bool EmailSent { get; set; }

    /// <summary>
    /// When the email was sent
    /// </summary>
    public virtual DateTime? EmailSentAt { get; set; }
}
