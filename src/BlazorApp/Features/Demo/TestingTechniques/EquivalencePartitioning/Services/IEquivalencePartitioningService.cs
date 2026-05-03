using BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Models;

namespace BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Services;

public interface IEquivalencePartitioningService
{
    AgeClassificationResult ClassifyAge(int? age);
    List<BatchTestCaseResult> RunBatchTest();
}
