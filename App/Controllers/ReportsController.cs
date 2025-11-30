using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

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
        public async Task<IActionResult> GetAnalytics([FromQuery] string metric = "profit")
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
                                // Convert everything to string to avoid type conversion issues
                                var value = reader.GetValue(i);
                                row[name] = value?.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            row[name] = $"Error: {ex.Message}";
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
            "profit" => @"
                SELECT 
                    p.product_name AS ProductName,
                    SUM(s.quantity_sold * (s.sale_price - p.cost_price)) AS TotalProfit
                FROM 
                    SalesFact s
                JOIN 
                    ProductDim p ON s.product_id = p.product_id
                GROUP BY 
                    p.product_name
                ORDER BY 
                    TotalProfit DESC",
            "sales" => @"
                SELECT 
                    p.product_name AS ProductName,
                    SUM(s.quantity_sold * s.sale_price) AS TotalSales
                FROM 
                    SalesFact s
                JOIN 
                    ProductDim p ON s.product_id = p.product_id
                GROUP BY 
                    p.product_name
                ORDER BY 
                    TotalSales DESC",
            "customers" => @"
                SELECT 
                    c.customer_name AS CustomerName,
                    COUNT(s.sale_id) AS TotalPurchases
                FROM 
                    SalesFact s
                JOIN 
                    CustomerDim c ON s.customer_id = c.customer_id
                GROUP BY 
                    c.customer_name
                ORDER BY 
                    TotalPurchases DESC",
            _ => throw new ArgumentException("Invalid metric specified. Valid options are: profit, sales, customers.")
        };
    }
}
