namespace BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Models;

public class BatchTestCaseResult
{
    public int InputAge { get; set; }
    public string EquivalenceClass { get; set; } = string.Empty;
    public string ExpectedCategory { get; set; } = string.Empty;
    public string ActualCategory { get; set; } = string.Empty;
    public bool Passed { get; set; }
}
