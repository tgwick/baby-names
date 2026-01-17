namespace NameMatch.Application.Interfaces;

public interface INameEnricher
{
    Task<EnrichmentResult> EnrichAllNamesAsync(bool forceReenrich = false);
}

public class EnrichmentResult
{
    public int NamesProcessed { get; set; }
    public int MappingsCreated { get; set; }
}
