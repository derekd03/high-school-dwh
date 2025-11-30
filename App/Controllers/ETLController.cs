using App.Services;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class ETLController : ControllerBase
{
    private readonly ETLService _etlService;

    public ETLController(ETLService etlService)
    {
        _etlService = etlService ?? throw new ArgumentNullException(nameof(etlService));
    }

    [HttpGet("run")]
    public async Task<IActionResult> RunETL()
    {
        var logList = new List<string>();
        logList.Add("Starting ETL...");

        try
        {
            await _etlService.RunETLAsync(logList);
            logList.Add("ETL job completed successfully!");
        }
        catch (Exception ex)
        {
            logList.Add($"ETL failed: {ex.Message}");
        }

        return Ok(new { logs = logList });
    }

    [HttpGet("purge")]
    public async Task<IActionResult> RunETLClear()
    {
        var logList = new List<string>();
        logList.Add("Clearing OLAP tables...");

        try
        {
            await using var olapConn = new OracleConnection(_etlService._olapConnectionString);
            await olapConn.OpenAsync();
            await _etlService.ClearOlapTablesAsync(olapConn, logList);
            logList.Add("Clear completed successfully!");
        }
        catch (Exception ex)
        {
            logList.Add($"Clear failed: {ex.Message}");
        }

        return Ok(new { logs = logList });
    }
}
