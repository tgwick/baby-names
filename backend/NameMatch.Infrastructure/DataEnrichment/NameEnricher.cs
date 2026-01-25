using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.DataEnrichment;

/// <summary>
/// Service to enrich names with category mappings based on sound analysis and origin detection.
/// </summary>
public class NameEnricher : INameEnricher
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NameEnricher> _logger;

    // Origin keyword mappings
    private static readonly Dictionary<string, string[]> OriginKeywords = new()
    {
        ["HEBREW"] = new[] { "hebrew", "biblical", "jewish", "israel" },
        ["LATIN"] = new[] { "latin", "roman", "spanish", "italian", "portuguese", "french" },
        ["GREEK"] = new[] { "greek", "greece" },
        ["CELTIC"] = new[] { "celtic", "irish", "scottish", "welsh", "gaelic" },
        ["GERMANIC"] = new[] { "german", "germanic", "norse", "scandinavian", "nordic", "english", "anglo" },
        ["ARABIC"] = new[] { "arabic", "arab", "muslim", "islamic", "persian" },
        ["AFRICAN"] = new[] { "african", "swahili", "yoruba", "zulu", "nigerian" },
        ["ASIAN"] = new[] { "chinese", "japanese", "korean", "indian", "hindi", "sanskrit", "vietnamese", "thai" }
    };

    // Classic names (well-established traditional names)
    private static readonly HashSet<string> ClassicNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Male
        "James", "John", "William", "Robert", "Michael", "David", "Richard", "Joseph", "Thomas", "Charles",
        "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua", "Kenneth",
        "George", "Edward", "Brian", "Ronald", "Timothy", "Jason", "Jeffrey", "Ryan", "Jacob", "Gary",
        "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Samuel",
        "Henry", "Patrick", "Alexander", "Frank", "Raymond", "Gregory", "Peter", "Jack", "Dennis", "Jerry",
        // Female
        "Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Elizabeth", "Susan", "Jessica", "Sarah", "Karen",
        "Lisa", "Nancy", "Betty", "Margaret", "Sandra", "Ashley", "Kimberly", "Emily", "Donna", "Michelle",
        "Dorothy", "Carol", "Amanda", "Melissa", "Deborah", "Stephanie", "Rebecca", "Sharon", "Laura", "Cynthia",
        "Kathleen", "Amy", "Angela", "Shirley", "Anna", "Brenda", "Pamela", "Emma", "Nicole", "Helen",
        "Samantha", "Katherine", "Christine", "Debra", "Rachel", "Carolyn", "Janet", "Catherine", "Maria", "Heather",
        "Victoria", "Charlotte", "Grace", "Alice", "Eleanor", "Rose", "Claire", "Jane", "Anne", "Julia"
    };

    // Biblical names
    private static readonly HashSet<string> BiblicalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Old Testament
        "Adam", "Eve", "Noah", "Abraham", "Isaac", "Jacob", "Joseph", "Moses", "Aaron", "David",
        "Solomon", "Samuel", "Daniel", "Elijah", "Isaiah", "Jeremiah", "Ezekiel", "Joshua", "Caleb", "Gideon",
        "Ruth", "Esther", "Sarah", "Rebecca", "Rachel", "Leah", "Hannah", "Miriam", "Deborah", "Naomi",
        "Benjamin", "Judah", "Levi", "Reuben", "Simeon", "Nathan", "Jonah", "Joel", "Amos", "Micah",
        // New Testament
        "Matthew", "Mark", "Luke", "John", "Peter", "Paul", "James", "Andrew", "Philip", "Thomas",
        "Simon", "Timothy", "Titus", "Stephen", "Barnabas", "Silas",
        "Mary", "Martha", "Elizabeth", "Anna", "Lydia", "Priscilla", "Phoebe", "Tabitha", "Dorcas"
    };

    // Nature-inspired names
    private static readonly HashSet<string> NatureNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Flowers/Plants
        "Rose", "Lily", "Violet", "Daisy", "Jasmine", "Ivy", "Holly", "Hazel", "Willow", "Olive",
        "Sage", "Laurel", "Fern", "Iris", "Poppy", "Dahlia", "Magnolia", "Azalea", "Clover", "Heather",
        // Nature elements
        "Aurora", "Luna", "Stella", "Celeste", "Sky", "Skye", "Storm", "Rain", "River", "Brook",
        "Lake", "Ocean", "Summer", "Autumn", "Winter", "Dawn", "Eve", "Star",
        // Animals/Nature
        "Robin", "Wren", "Phoenix", "Wolf", "Bear", "Fox", "Hawk", "Falcon", "Leo", "Leon",
        // Earth/Stone
        "Clay", "Stone", "Flint", "Jasper", "Jade", "Ruby", "Pearl", "Amber", "Crystal", "Coral"
    };

    // Royal names
    private static readonly HashSet<string> RoyalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // British Royalty
        "William", "Henry", "George", "Edward", "Charles", "James", "Richard", "Arthur", "Albert", "Philip",
        "Elizabeth", "Victoria", "Charlotte", "Catherine", "Diana", "Anne", "Mary", "Margaret", "Alexandra", "Beatrice",
        // European Royalty
        "Louis", "Francis", "Frederick", "Leopold", "Ferdinand", "Maximilian",
        "Maria", "Sophia", "Isabella", "Eleanor", "Matilda", "Adelaide", "Eugenie"
    };

    // Vintage names (old-fashioned making a comeback)
    private static readonly HashSet<string> VintageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Male
        "Theodore", "Oliver", "Henry", "Arthur", "Felix", "Oscar", "Leo", "Jasper", "Atticus", "Silas",
        "Milo", "August", "Hugo", "Walter", "Clarence", "Harvey", "Cecil", "Percy", "Ernest", "Alfred",
        "Archie", "Stanley", "Chester", "Edgar", "Eugene", "Howard", "Leonard", "Milton", "Vernon", "Wilbur",
        // Female
        "Evelyn", "Eleanor", "Hazel", "Violet", "Clara", "Cora", "Ada", "Ivy", "Pearl", "Ruby",
        "Stella", "Beatrice", "Florence", "Harriet", "Edith", "Mabel", "Elsie", "Agnes", "Ethel", "Gertrude",
        "Josephine", "Matilda", "Nora", "Olive", "Rosalie", "Vera", "Winifred", "Adelaide", "Cordelia", "Genevieve"
    };

    public NameEnricher(ApplicationDbContext context, ILogger<NameEnricher> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Enriches all names in the database with sound analysis and category mappings.
    /// </summary>
    public async Task<Application.Interfaces.EnrichmentResult> EnrichAllNamesAsync(bool forceReenrich = false)
    {
        var result = new Application.Interfaces.EnrichmentResult();

        // Quick check: if not forcing re-enrich, see if enrichment was already done
        if (!forceReenrich)
        {
            var hasEnrichedNames = await _context.Names.AnyAsync(n => n.SyllableCount != null);
            if (hasEnrichedNames)
            {
                _logger.LogInformation("Names already enriched. Skipping enrichment.");
                return result;
            }
        }

        // Load all categories
        var categories = await _context.NameCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
        if (categories.Count == 0)
        {
            _logger.LogWarning("No categories found. Run category seeding first.");
            return result;
        }

        // Get names to process
        var namesQuery = _context.Names.Include(n => n.CategoryMappings).AsQueryable();

        if (!forceReenrich)
        {
            // Only process names without any category mappings
            namesQuery = namesQuery.Where(n => n.CategoryMappings.Count == 0);
        }

        var names = await namesQuery.ToListAsync();
        _logger.LogInformation("Processing {Count} names for enrichment...", names.Count);

        var batchSize = 100;
        var processed = 0;

        foreach (var batch in names.Chunk(batchSize))
        {
            foreach (var name in batch)
            {
                var mappingsAdded = EnrichName(name, categories, forceReenrich);
                result.MappingsCreated += mappingsAdded;
                result.NamesProcessed++;
            }

            await _context.SaveChangesAsync();
            processed += batch.Length;
            _logger.LogInformation("Enriched {Processed}/{Total} names...", processed, names.Count);
        }

        _logger.LogInformation("Enrichment complete. Processed {Names} names, created {Mappings} category mappings.",
            result.NamesProcessed, result.MappingsCreated);

        return result;
    }

    /// <summary>
    /// Enriches a single name with sound analysis and category mappings.
    /// </summary>
    private int EnrichName(Name name, Dictionary<string, int> categories, bool forceReenrich)
    {
        var mappingsAdded = 0;

        // Clear existing mappings if force re-enrich
        if (forceReenrich && name.CategoryMappings.Count > 0)
        {
            _context.NameCategoryMappings.RemoveRange(name.CategoryMappings);
            name.CategoryMappings.Clear();
        }

        // Sound analysis
        var analysis = SoundAnalyzer.Analyze(name.NameText);

        // Update name fields
        name.SyllableCount = analysis.SyllableCount;
        name.EndingSound = analysis.EndingSound;
        name.SoundType = analysis.SoundType;

        // Add length category (SHORT or LONG)
        if (categories.TryGetValue(analysis.LengthCategory, out var lengthCatId))
        {
            mappingsAdded += AddMapping(name, lengthCatId, 1.0f);
        }

        // Add sound category (SOFT or STRONG) if not neutral
        if (analysis.SoundCategory != null && categories.TryGetValue(analysis.SoundCategory, out var soundCatId))
        {
            mappingsAdded += AddMapping(name, soundCatId, 0.9f);
        }

        // Origin detection from existing Origin field
        if (!string.IsNullOrWhiteSpace(name.Origin))
        {
            var originLower = name.Origin.ToLowerInvariant();
            foreach (var (categoryCode, keywords) in OriginKeywords)
            {
                if (keywords.Any(k => originLower.Contains(k)))
                {
                    if (categories.TryGetValue(categoryCode, out var originCatId))
                    {
                        mappingsAdded += AddMapping(name, originCatId, 0.85f);
                    }
                    break; // Only assign one origin
                }
            }
        }

        // Style categories based on name lists
        if (ClassicNames.Contains(name.NameText) && categories.TryGetValue("CLASSIC", out var classicId))
        {
            mappingsAdded += AddMapping(name, classicId, 0.95f);
        }

        if (BiblicalNames.Contains(name.NameText) && categories.TryGetValue("BIBLICAL", out var biblicalId))
        {
            mappingsAdded += AddMapping(name, biblicalId, 1.0f);
        }

        if (NatureNames.Contains(name.NameText) && categories.TryGetValue("NATURE", out var natureId))
        {
            mappingsAdded += AddMapping(name, natureId, 0.95f);
        }

        if (RoyalNames.Contains(name.NameText) && categories.TryGetValue("ROYAL", out var royalId))
        {
            mappingsAdded += AddMapping(name, royalId, 0.9f);
        }

        if (VintageNames.Contains(name.NameText) && categories.TryGetValue("VINTAGE", out var vintageId))
        {
            mappingsAdded += AddMapping(name, vintageId, 0.85f);
        }

        // Trendy detection: high popularity + not classic
        if (name.PopularityScore >= 80 && !ClassicNames.Contains(name.NameText))
        {
            if (categories.TryGetValue("TRENDY", out var trendyId))
            {
                mappingsAdded += AddMapping(name, trendyId, 0.7f);
            }
        }

        // Unique detection: low popularity
        if (name.PopularityScore <= 20)
        {
            if (categories.TryGetValue("UNIQUE", out var uniqueId))
            {
                mappingsAdded += AddMapping(name, uniqueId, 0.8f);
            }
        }

        return mappingsAdded;
    }

    private int AddMapping(Name name, int categoryId, float confidence)
    {
        // Check if mapping already exists
        if (name.CategoryMappings.Any(m => m.CategoryId == categoryId))
            return 0;

        name.CategoryMappings.Add(new NameCategoryMapping
        {
            NameId = name.Id,
            CategoryId = categoryId,
            Confidence = confidence
        });

        return 1;
    }
}
