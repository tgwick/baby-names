namespace NameMatch.Application.Interfaces;

public interface IDataSeeder
{
    /// <summary>
    /// Seeds baby names into the database from the processed JSON file.
    /// </summary>
    Task SeedNamesAsync();

    /// <summary>
    /// Seeds name categories (origins, styles, sounds) into the database.
    /// </summary>
    Task SeedCategoriesAsync();
}
