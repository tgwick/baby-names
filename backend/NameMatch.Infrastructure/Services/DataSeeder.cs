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

    private class NameData
    {
        public required string NameText { get; set; }
        public int Gender { get; set; }
        public int PopularityScore { get; set; }
        public string? Origin { get; set; }
    }
}
