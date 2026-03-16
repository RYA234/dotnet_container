using Microsoft.AspNetCore.Mvc;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo;

public partial class DemoController : Controller
{
    private readonly INPlusOneService _nPlusOneService;
    private readonly IFullScanService _fullScanService;
    private readonly ISelectStarService _selectStarService;
    private readonly ILikeSearchService _likeSearchService;
    private readonly IValidationDemoService _validationDemoService;
    private readonly ILoggingDemoService _loggingDemoService;
    private readonly IDatabaseConnectionDemoService _dbConnectionDemoService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(
        INPlusOneService nPlusOneService,
        IFullScanService fullScanService,
        ISelectStarService selectStarService,
        ILikeSearchService likeSearchService,
        IValidationDemoService validationDemoService,
        ILoggingDemoService loggingDemoService,
        IDatabaseConnectionDemoService dbConnectionDemoService,
        ILogger<DemoController> logger)
    {
        _nPlusOneService = nPlusOneService;
        _fullScanService = fullScanService;
        _selectStarService = selectStarService;
        _likeSearchService = likeSearchService;
        _validationDemoService = validationDemoService;
        _loggingDemoService = loggingDemoService;
        _dbConnectionDemoService = dbConnectionDemoService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("~/Features/Demo/Views/Index.cshtml");
    }
}
