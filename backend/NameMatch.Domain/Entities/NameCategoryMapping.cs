namespace NameMatch.Domain.Entities;

public class NameCategoryMapping
{
    public int Id { get; set; }
    public int NameId { get; set; }
    public int CategoryId { get; set; }
    public float Confidence { get; set; } = 1.0f;

    public Name Name { get; set; } = null!;
    public NameCategory Category { get; set; } = null!;
}
