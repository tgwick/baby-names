using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly INameEnricher _nameEnricher;
    private readonly ILogger<AdminController> _logger;
    private readonly IConfiguration _configuration;

    public AdminController(
        INameEnricher nameEnricher,
        ILogger<AdminController> logger,
        IConfiguration configuration)
    {
        _nameEnricher = nameEnricher;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Enriches all names with category mappings based on sound analysis and pattern detection.
    /// Requires admin key for authorization.
    /// </summary>
    [HttpPost("enrich-names")]
    [AllowAnonymous]
    public async Task<IActionResult> EnrichNames(
        [FromQuery] bool force = false,
        [FromHeader(Name = "X-Admin-Key")] string? adminKey = null)
    {
        // Simple admin key validation (for development/admin use)
        var expectedKey = _configuration["Admin:EnrichmentKey"] ?? "dev-enrich-key";
        if (adminKey != expectedKey)
        {
            _logger.LogWarning("Unauthorized enrichment attempt");
            return Unauthorized(new { success = false, errors = new[] { "Invalid admin key" } });
        }

        _logger.LogInformation("Starting name enrichment (force={Force})", force);

        try
        {
            var result = await _nameEnricher.EnrichAllNamesAsync(force);

            return Ok(new
            {
                success = true,
                data = new
                {
                    result.NamesProcessed,
                    result.MappingsCreated
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during name enrichment");
            return StatusCode(500, new { success = false, errors = new[] { "Enrichment failed" } });
        }
    }

    /// <summary>
    /// Gets enrichment statistics.
    /// </summary>
    [HttpGet("enrichment-stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEnrichmentStats(
        [FromServices] Infrastructure.Data.ApplicationDbContext context)
    {
        var totalNames = await context.Names.CountAsync();
        var enrichedNames = await context.Names.CountAsync(n => n.CategoryMappings.Any());
        var totalMappings = await context.NameCategoryMappings.CountAsync();
        var categories = await context.NameCategories.CountAsync();

        // Get breakdown by category type
        var mappingsByCategory = await context.NameCategoryMappings
            .Include(m => m.Category)
            .GroupBy(m => m.Category!.CategoryType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalNames,
                enrichedNames,
                unenrichedNames = totalNames - enrichedNames,
                totalMappings,
                categories,
                averageMappingsPerName = enrichedNames > 0 ? (double)totalMappings / enrichedNames : 0,
                mappingsByCategory
            }
        });
    }
}
