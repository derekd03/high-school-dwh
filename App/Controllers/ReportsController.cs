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
                SELECT 
                    t.FirstName || ' ' || t.LastName AS TeacherName,
                    NVL(SUM(f.StudentCount), 0) AS TotalStudents,
                    NVL(AVG(f.AvgGrade), 0) AS AverageGrade
                FROM FactTeacherPerformance f
                JOIN DimTeacher t ON f.DimTeacherID = t.ID
                GROUP BY t.FirstName, t.LastName
                ORDER BY AverageGrade DESC",
            
            // ROLLUP: Enrollment Summary by Department -> Course
            "esbd" => @"
                SELECT dept.Name AS DepartmentName,
                    c.Title AS CourseTitle,
                    dt.Year AS TermYear,
                    COUNT(e.StudentID) AS EnrollmentCount
                FROM FactEnrollment e
                JOIN FactClass cls      ON e.ClassID = cls.ID
                JOIN DimCourse c        ON cls.CourseID = c.ID
                JOIN DimDepartment dept ON c.DepartmentID = dept.ID
                JOIN DimTerm dt         ON e.DimTermID = dt.ID
                GROUP BY ROLLUP (dept.Name, c.Title, dt.Year)
                HAVING GROUPING(dt.Year) = 0
                ORDER BY DepartmentName, CourseTitle, TermYear",

            // CUBE: Attendance Summary by Student and Year
            "asst" => @"
                SELECT 
                    s.FirstName || ' ' || s.LastName AS StudentName,
                    dt.Year AS TermYear,
                    TRUNC(AVG(e.AttendanceRate), 1) AS AvgAttendanceRate
                FROM FactEnrollment e
                JOIN DimStudent s ON e.StudentID = s.ID
                JOIN DimTerm dt   ON e.DimTermID = dt.ID
                GROUP BY CUBE (s.FirstName, s.LastName, dt.Year)
                HAVING 
                    GROUPING(s.FirstName) = 0 
                    AND GROUPING(s.LastName) = 0
                    AND GROUPING(dt.Year) = 0 
                ORDER BY StudentName, TermYear",

            // GROUPING SETS: Class Count by Teacher, with yearly summaries
            "ccbt" => @"
                SELECT 
                    t.FirstName || ' ' || t.LastName AS TeacherName,
                    dt.Year AS TermYear,
                    COUNT(
                    CASE WHEN fc.ID IS NOT NULL 
                    AND fc.TeacherId IS NOT NULL 
                    AND fc.DimTermId IS NOT NULL THEN 1 END
                    ) AS ClassesHandled
                FROM DimTeacher t
                LEFT JOIN FactClass fc 
                    ON fc.TeacherId = t.ID
                LEFT JOIN DimTerm dt 
                    ON fc.DimTermId = dt.ID
                GROUP BY GROUPING SETS (
                    (t.FirstName, t.LastName, dt.Year),  -- teacher + year
                    (t.FirstName, t.LastName),           -- teacher total
                    (dt.Year),                           -- year total
                    ()                                   -- grand total
                )
                HAVING dt.Year IS NOT NULL  -- keep only rows with a year or subtotals
                ORDER BY TeacherName NULLS LAST, TermYear NULLS LAST",

            // GROUP BY: Enrollment Count per Class
            "ecpc" => @"
                SELECT c.Title AS CourseTitle,
                       cls.Location,
                       dt.Year AS TermYear,
                       COUNT(e.StudentID) AS EnrollmentCount
                FROM FactEnrollment e
                JOIN FactClass cls ON e.ClassID = cls.ID
                JOIN DimCourse c   ON cls.CourseID = c.ID
                JOIN DimTerm dt    ON e.DimTermID = dt.ID
                GROUP BY c.Title, cls.Location, dt.Year
                ORDER BY EnrollmentCount DESC",

            _ => throw new ArgumentException("Invalid metric specified. Valid options are: 'average grades per teacher'.")
        };
    }
}
