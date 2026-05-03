using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Services;

namespace BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning;

[Route("Demo/TestingTechniques")]
public class EquivalencePartitioningController : Controller
{
    private readonly IEquivalencePartitioningService _service;
    private readonly ILogger<EquivalencePartitioningController> _logger;

    public EquivalencePartitioningController(
        IEquivalencePartitioningService service,
        ILogger<EquivalencePartitioningController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Route("EquivalencePartitioning")]
    public IActionResult EquivalencePartitioning()
    {
        return View("~/Features/Demo/TestingTechniques/EquivalencePartitioning/Views/EquivalencePartitioning.cshtml");
    }

    [HttpGet("/api/demo/testing/equivalence")]
    public IActionResult Classify([FromQuery] int? age)
    {
        try
        {
            var result = _service.ClassifyAge(age);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Equivalence partitioning classify error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("/api/demo/testing/equivalence/batch")]
    public IActionResult BatchTest()
    {
        try
        {
            var results = _service.RunBatchTest();
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Equivalence partitioning batch error");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
