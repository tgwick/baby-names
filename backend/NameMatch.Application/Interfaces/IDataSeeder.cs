namespace NameMatch.Application.Interfaces;

public interface IDataSeeder
{
    /// <summary>
    /// Seeds baby names into the database from the processed JSON file.
    /// </summary>
    Task SeedNamesAsync();
}
