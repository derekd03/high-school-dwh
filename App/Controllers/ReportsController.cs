using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly string _oltpConnectionString;
        private readonly string _olapConnectionString;

        public ReportsController(IConfiguration configuration)
        {
            _oltpConnectionString = configuration.GetConnectionString("OLTPConnection")!;
            _olapConnectionString = configuration.GetConnectionString("OLAPConnection")!;
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string metric = "agpt")
        {
            var query = BuildAnalyticsSql(metric);
            var results = new List<Dictionary<string, object?>>();

            using var connection = new OracleConnection(_olapConnectionString);

            try
            {
                await connection.OpenAsync();

                using var cmd = new OracleCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);

                        try
                        {
                            if (reader.IsDBNull(i))
                            {
                                row[name] = null;
                            }
                            else
                            {
                                // Get everything as string to avoid type conversion issues
                                var stringValue = reader.GetString(i);
                                
                                // For numeric columns, try to parse and format
                                if (name.Equals("AVERAGEGRADE", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (decimal.TryParse(stringValue, out var decimalValue))
                                    {
                                        row[name] = Math.Round(decimalValue, 2, MidpointRounding.AwayFromZero);
                                    }
                                    else
                                    {
                                        row[name] = stringValue;
                                    }
                                }
                                else
                                {
                                    row[name] = stringValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // If GetString fails, try GetValue and ToString
                            try
                            {
                                var value = reader.GetValue(i);
                                row[name] = value?.ToString();
                            }
                            catch
                            {
                                row[name] = null;
                            }
                        }
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Database error: {ex.Message}", query = query });
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }

            return Ok(results);
        }

        private string BuildAnalyticsSql(string metric) => metric.ToLower() switch
        {
            // Average grades per teacher
            "agpt" => @"
                SELECT t.FirstName || ' ' || t.LastName AS TeacherName,
                    NVL(AVG(f.AvgGrade), 0) AS AverageGrade,
                    NVL(SUM(f.StudentCount), 0) AS TotalStudents
                FROM FactTeacherPerformance f
                JOIN DimTeacher t ON f.DimTeacherID = t.ID
                GROUP BY t.FirstName, t.LastName
                ORDER BY AverageGrade DESC",
            _ => throw new ArgumentException("Invalid metric specified. Valid options are: 'average grades per teacher'.")
        };
    }
}
