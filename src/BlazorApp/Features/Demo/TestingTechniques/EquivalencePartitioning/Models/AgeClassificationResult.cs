namespace BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Models;

public class AgeClassificationResult
{
    public int? Age { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EquivalenceClass { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}
