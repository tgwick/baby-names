using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedNamesAsync()
    {
        // Check if names are already seeded
        var existingCount = await _context.Names.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("Names already seeded ({Count} names in database). Skipping.", existingCount);
            return;
        }

        _logger.LogInformation("Starting name seeding...");

        // Look for the processed-names.json file
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(basePath, "data", "processed-names.json"),
            Path.Combine(basePath, "..", "..", "..", "..", "data", "ssa-names", "processed-names.json"),
            Path.Combine(basePath, "..", "..", "..", "..", "..", "data", "ssa-names", "processed-names.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "data", "ssa-names", "processed-names.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "ssa-names", "processed-names.json")
        };

        string? jsonPath = null;
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                jsonPath = fullPath;
                break;
            }
        }

        if (jsonPath == null)
        {
            _logger.LogWarning("Could not find processed-names.json. Tried paths: {Paths}",
                string.Join(", ", possiblePaths.Select(Path.GetFullPath)));
            return;
        }

        _logger.LogInformation("Found names file at: {Path}", jsonPath);

        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var nameData = JsonSerializer.Deserialize<List<NameData>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (nameData == null || nameData.Count == 0)
            {
                _logger.LogWarning("No names found in the JSON file.");
                return;
            }

            _logger.LogInformation("Loading {Count} names into database...", nameData.Count);

            var names = nameData.Select(n => new Name
            {
                NameText = n.NameText,
                Gender = (Gender)n.Gender,
                PopularityScore = n.PopularityScore,
                Origin = n.Origin
            }).ToList();

            // Add in batches for performance
            const int batchSize = 500;
            for (int i = 0; i < names.Count; i += batchSize)
            {
                var batch = names.Skip(i).Take(batchSize);
                await _context.Names.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Current}/{Total} names...", Math.Min(i + batchSize, names.Count), names.Count);
            }

            _logger.LogInformation("Successfully seeded {Count} names!", names.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding names");
            throw;
        }
    }

    public async Task SeedCategoriesAsync()
    {
        // Check if categories are already seeded
        var existingCount = await _context.NameCategories.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("Categories already seeded ({Count} categories in database). Skipping.", existingCount);
            return;
        }

        _logger.LogInformation("Starting category seeding...");

        var categories = new List<NameCategory>
        {
            // Origin categories
            new() { Code = "HEBREW", DisplayName = "Hebrew", CategoryType = "ORIGIN", Description = "Names with Hebrew origins, often biblical", DisplayOrder = 1 },
            new() { Code = "LATIN", DisplayName = "Latin", CategoryType = "ORIGIN", Description = "Names derived from Latin or Romance languages", DisplayOrder = 2 },
            new() { Code = "GREEK", DisplayName = "Greek", CategoryType = "ORIGIN", Description = "Names with Greek origins or mythology", DisplayOrder = 3 },
            new() { Code = "CELTIC", DisplayName = "Celtic", CategoryType = "ORIGIN", Description = "Names from Irish, Scottish, Welsh traditions", DisplayOrder = 4 },
            new() { Code = "GERMANIC", DisplayName = "Germanic", CategoryType = "ORIGIN", Description = "Names from German, Norse, or Scandinavian origins", DisplayOrder = 5 },
            new() { Code = "ARABIC", DisplayName = "Arabic", CategoryType = "ORIGIN", Description = "Names with Arabic or Middle Eastern origins", DisplayOrder = 6 },
            new() { Code = "AFRICAN", DisplayName = "African", CategoryType = "ORIGIN", Description = "Names from various African cultures", DisplayOrder = 7 },
            new() { Code = "ASIAN", DisplayName = "Asian", CategoryType = "ORIGIN", Description = "Names from East Asian, South Asian, or Southeast Asian cultures", DisplayOrder = 8 },

            // Style categories
            new() { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", Description = "Timeless, traditional names that never go out of style", DisplayOrder = 10 },
            new() { Code = "MODERN", DisplayName = "Modern", CategoryType = "STYLE", Description = "Contemporary names with a fresh feel", DisplayOrder = 11 },
            new() { Code = "BIBLICAL", DisplayName = "Biblical", CategoryType = "STYLE", Description = "Names from the Bible or religious texts", DisplayOrder = 12 },
            new() { Code = "NATURE", DisplayName = "Nature", CategoryType = "STYLE", Description = "Names inspired by nature, flowers, seasons, or elements", DisplayOrder = 13 },
            new() { Code = "TRENDY", DisplayName = "Trendy", CategoryType = "STYLE", Description = "Currently popular or rising in popularity", DisplayOrder = 14 },
            new() { Code = "VINTAGE", DisplayName = "Vintage", CategoryType = "STYLE", Description = "Old-fashioned names making a comeback", DisplayOrder = 15 },
            new() { Code = "UNIQUE", DisplayName = "Unique", CategoryType = "STYLE", Description = "Rare or uncommon names that stand out", DisplayOrder = 16 },
            new() { Code = "ROYAL", DisplayName = "Royal", CategoryType = "STYLE", Description = "Names associated with royalty or nobility", DisplayOrder = 17 },

            // Sound categories
            new() { Code = "SOFT", DisplayName = "Soft Sounds", CategoryType = "SOUND", Description = "Names with gentle, flowing sounds", DisplayOrder = 20 },
            new() { Code = "STRONG", DisplayName = "Strong Sounds", CategoryType = "SOUND", Description = "Names with bold, powerful sounds", DisplayOrder = 21 },
            new() { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", Description = "Names with 1-2 syllables", DisplayOrder = 22 },
            new() { Code = "LONG", DisplayName = "Long", CategoryType = "SOUND", Description = "Names with 3 or more syllables", DisplayOrder = 23 },
        };

        await _context.NameCategories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} categories!", categories.Count);
    }

    private class NameData
    {
        public required string NameText { get; set; }
        public int Gender { get; set; }
        public int PopularityScore { get; set; }
        public string? Origin { get; set; }
    }
}
