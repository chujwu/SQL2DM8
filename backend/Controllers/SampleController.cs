using Microsoft.AspNetCore.Mvc;
using SQLServerToDM8.Models;
using SQLServerToDM8.Services;

namespace SQLServerToDM8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    private readonly ISampleDataService _sampleDataService;
    private readonly IConversionEngine _conversionEngine;
    private readonly ILogger<SampleController> _logger;

    public SampleController(
        ISampleDataService sampleDataService,
        IConversionEngine conversionEngine,
        ILogger<SampleController> logger)
    {
        _sampleDataService = sampleDataService;
        _conversionEngine = conversionEngine;
        _logger = logger;
    }

    /// <summary>
    /// 获取示例对象列表
    /// </summary>
    [HttpGet("objects")]
    public ActionResult<List<SampleSqlObject>> GetSampleObjects()
    {
        return Ok(_sampleDataService.GetSampleObjects());
    }

    /// <summary>
    /// 转换示例对象
    /// </summary>
    [HttpPost("convert")]
    public ActionResult<List<ConvertResult>> ConvertSampleObjects()
    {
        var samples = _sampleDataService.GetSampleObjects();
        var results = new List<ConvertResult>();

        foreach (var sample in samples)
        {
            var result = _conversionEngine.Convert(
                sample.SqlServerSql,
                sample.Type,
                sample.Name,
                sample.Schema);

            results.Add(result);
        }

        return Ok(results);
    }

    /// <summary>
    /// 转换单个示例对象
    /// </summary>
    [HttpPost("convert/{index}")]
    public ActionResult<ConvertResult> ConvertSampleObject(int index)
    {
        var samples = _sampleDataService.GetSampleObjects();

        if (index < 0 || index >= samples.Count)
        {
            return BadRequest(new { message = "无效的索引" });
        }

        var sample = samples[index];
        var result = _conversionEngine.Convert(
            sample.SqlServerSql,
            sample.Type,
            sample.Name,
            sample.Schema);

        return Ok(result);
    }
}
