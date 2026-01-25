namespace NameMatch.Domain.Enums;

/// <summary>
/// Name style categories for filtering based on trend analysis.
/// </summary>
public enum NameStyle
{
    /// <summary>
    /// No preference - show all names.
    /// </summary>
    None = 0,

    /// <summary>
    /// Trendy names - rising in popularity with recent peak.
    /// Filter criteria: TrendScore > 0.3 AND PeakDecade >= 2000
    /// </summary>
    Trendy = 1,

    /// <summary>
    /// Classic names - consistently popular over many decades.
    /// Filter criteria: StabilityScore >= 0.5 AND DecadesPresent >= 4
    /// </summary>
    Classic = 2,

    /// <summary>
    /// Unique names - uncommon, stand out from the crowd.
    /// Filter criteria: PopularityScore <= 30
    /// </summary>
    Unique = 3
}
