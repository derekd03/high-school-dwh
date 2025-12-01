using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace App.Services
{
    public class ETLService
    {
        private readonly string _oltpConnectionString;
        public readonly string _olapConnectionString;

        public ETLService(string oltpConnectionString, string olapConnectionString)
        {
            _oltpConnectionString = oltpConnectionString;
            _olapConnectionString = olapConnectionString;
        }

        public async Task RunETLAsync(List<string> logList)
        {
            logList.Add("ETL process started.");

            await using var oltpConn = new OracleConnection(_oltpConnectionString);
            await using var olapConn = new OracleConnection(_olapConnectionString);

            await oltpConn.OpenAsync();
            await olapConn.OpenAsync();

            using var tx = olapConn.BeginTransaction();

            try
            {
                await ClearOlapTablesAsync(olapConn, logList);

                // Dimensions
                await LoadDimTermAsync(oltpConn, olapConn, logList);
                await LoadDimStudentAsync(oltpConn, olapConn, logList);
                await LoadDimTeacherAsync(oltpConn, olapConn, logList);
                await LoadDimDepartmentAsync(oltpConn, olapConn, logList);
                await LoadDimDepartmentTeacherBridgeAsync(oltpConn, olapConn, logList);
                await LoadDimCourseAsync(oltpConn, olapConn, logList);

                // Bridge: prerequisites
                await LoadFactPrerequisiteAsync(oltpConn, olapConn, logList);

                // Facts
                await LoadFactClassAsync(oltpConn, olapConn, logList);
                await LoadFactEnrollmentAsync(oltpConn, olapConn, logList);
                await LoadFactTeacherPerformanceAsync(oltpConn, olapConn, logList);
                await LoadFactDepartmentSummaryAsync(oltpConn, olapConn, logList);

                tx.Commit();
                logList.Add("ETL transaction committed.");
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* swallow */ }
                logList.Add($"ETL failed: {ex.Message}");
                throw;
            }
            finally
            {
                logList.Add("ETL process finished.");
            }
        }

        public async Task ClearOlapTablesAsync(OracleConnection olapConn, List<string> log)
        {
            // delete in dependency order: facts -> bridges -> dims
            var tables = new[]
            {
                "FACTDEPARTMENTSUMMARY",
                "FACTTEACHERPERFORMANCE",
                "FACTENROLLMENT",
                "FACTCLASS",
                "FACTPREREQUISITE",

                "DIMDEPARTMENTTEACHER", // bridge
                // dims
                "DIMCOURSE",
                "DIMDEPARTMENT",
                "DIMTEACHER",
                "DIMSTUDENT",
                "DIMTERM"
            };

            foreach (var table in tables)
            {
                try
                {
                    log.Add($"Clearing table {table}...");
                    using var cmd = new OracleCommand($"DELETE FROM {table}", olapConn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    log.Add($"Error clearing {table}: {ex.Message}");
                    throw;
                }
            }

            log.Add("All OLAP tables cleared.");
        }

        #region Helpers
        private OracleParameter CreateRawParam(string name, byte[] value)
            => new(name, OracleDbType.Raw, 16) { Value = value };

        private OracleParameter CreateNullableRawParam(string name, byte[]? value)
            => new(name, OracleDbType.Raw, 16) { Value = value ?? (object)DBNull.Value };

        private OracleParameter CreateStringParam(string name, string? value, int size)
            => new(name, OracleDbType.NVarchar2, size) { Value = value ?? (object)DBNull.Value };

        private OracleParameter CreateIntParam(string name, int value)
            => new(name, OracleDbType.Int32) { Value = value };

        private OracleParameter CreateNullableIntParam(string name, int? value)
            => new(name, OracleDbType.Int32) { Value = value ?? (object)DBNull.Value };

        private OracleParameter CreateDecimalParam(string name, decimal value)
            => new(name, OracleDbType.Decimal) { Value = value };

        private OracleParameter CreateNullableDecimalParam(string name, decimal? value)
            => new(name, OracleDbType.Decimal) { Value = value ?? (object)DBNull.Value };

        private OracleParameter CreateDateParam(string name, DateTime value)
            => new(name, OracleDbType.Date) { Value = value };

        private OracleParameter CreateNullableDateParam(string name, DateTime? value)
            => new(name, OracleDbType.Date) { Value = value ?? (object)DBNull.Value };
        #endregion

        #region Dim Loaders
        public async Task LoadDimTermAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimTerm...");

            const string sql = @"SELECT ""Id"", ""Year"", ""TermNumber"" FROM ""HIGHSCHOOL_OLTP"".""Terms""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);

                string? year = null;
                if (!reader.IsDBNull(1))
                {
                    year = reader.GetValue(1)?.ToString(); // read as object, convert to string
                }

                int termNumber = 0;
                if (!reader.IsDBNull(2))
                {
                    // read as object, convert safely to int
                    var termObj = reader.GetValue(2);
                    termNumber = Convert.ToInt32(termObj);
                }

                const string insert = @"INSERT INTO DIMTERM (ID, YEAR, TERMNUMBER) VALUES (:id, :year, :termNumber)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("id", id));
                cmd.Parameters.Add(CreateStringParam("year", year, 4));
                cmd.Parameters.Add(CreateIntParam("termNumber", termNumber));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimTerm loaded successfully.");
        }

        public async Task LoadDimStudentAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimStudent...");
            const string sql = @"SELECT ""Id"", ""FirstName"", ""LastName"", ""GradeLevel"", ""Phone"", ""Email"" FROM ""HIGHSCHOOL_OLTP"".""Students""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);
                var first = reader.IsDBNull(1) ? null : reader.GetString(1);
                var last = reader.IsDBNull(2) ? null : reader.GetString(2);
                var grade = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var phone = reader.IsDBNull(4) ? null : reader.GetString(4);
                var email = reader.IsDBNull(5) ? null : reader.GetString(5);

                const string insert = @"INSERT INTO DIMSTUDENT (ID, FIRSTNAME, LASTNAME, PHONE, EMAIL, GRADELEVEL) VALUES (:id, :first, :last, :phone, :email, :grade)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("id", id));
                cmd.Parameters.Add(CreateStringParam("first", first, 255));
                cmd.Parameters.Add(CreateStringParam("last", last, 255));
                cmd.Parameters.Add(CreateStringParam("phone", phone, 100));
                cmd.Parameters.Add(CreateStringParam("email", email, 255));
                cmd.Parameters.Add(CreateNullableIntParam("grade", grade));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimStudent loaded successfully.");
        }

        public async Task LoadDimTeacherAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimTeacher...");
            const string sql = @"SELECT ""Id"", ""FirstName"", ""LastName"", ""Phone"", ""Email"" FROM ""HIGHSCHOOL_OLTP"".""Teachers""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);
                var first = reader.IsDBNull(1) ? null : reader.GetString(1);
                var last = reader.IsDBNull(2) ? null : reader.GetString(2);
                var phone = reader.IsDBNull(3) ? null : reader.GetString(3);
                var email = reader.IsDBNull(4) ? null : reader.GetString(4);

                const string insert = @"INSERT INTO DIMTEACHER (ID, FIRSTNAME, LASTNAME, PHONE, EMAIL) VALUES (:id, :first, :last, :phone, :email)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("id", id));
                cmd.Parameters.Add(CreateStringParam("first", first, 255));
                cmd.Parameters.Add(CreateStringParam("last", last, 255));
                cmd.Parameters.Add(CreateStringParam("phone", phone, 100));
                cmd.Parameters.Add(CreateStringParam("email", email, 255));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimTeacher loaded successfully.");
        }

        public async Task LoadDimDepartmentAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimDepartment...");
            const string sql = @"SELECT ""Id"", ""Name"", ""Location"", ""HeadId"" FROM ""HIGHSCHOOL_OLTP"".""Departments""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);
                var name = reader.IsDBNull(1) ? null : reader.GetString(1);
                var location = reader.IsDBNull(2) ? null : reader.GetString(2);
                var headId = reader.IsDBNull(3) ? null : reader.GetFieldValue<byte[]>(3);

                const string insert = @"INSERT INTO DIMDEPARTMENT (ID, NAME, LOCATION, TEACHERID) VALUES (:id, :name, :loc, :head)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("id", id));
                cmd.Parameters.Add(CreateStringParam("name", name, 255));
                cmd.Parameters.Add(CreateStringParam("loc", location, 255));
                cmd.Parameters.Add(CreateNullableRawParam("head", headId));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimDepartment loaded successfully.");
        }

        public async Task LoadDimDepartmentTeacherBridgeAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimDepartmentTeacher bridge...");
            const string sql = @"
                SELECT d.""Id"" AS DepartmentId, t.""Id"" AS TeacherId
                FROM ""HIGHSCHOOL_OLTP"".""Departments"" d
                JOIN ""HIGHSCHOOL_OLTP"".""DepartmentTeacher"" dt ON d.""Id"" = dt.""DepartmentsId""
                JOIN ""HIGHSCHOOL_OLTP"".""Teachers"" t ON dt.""TeachersId"" = t.""Id""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var deptId = reader.GetFieldValue<byte[]>(0);
                var teacherId = reader.GetFieldValue<byte[]>(1);

                const string insert = @"INSERT INTO DIMDEPARTMENTTEACHER (DEPARTMENTID, TEACHERID) VALUES (:dept, :teacher)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("dept", deptId));
                cmd.Parameters.Add(CreateRawParam("teacher", teacherId));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimDepartmentTeacher bridge loaded successfully.");
        }

        public async Task LoadDimCourseAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading DimCourse...");
            const string sql = @"SELECT ""Id"", ""Code"", ""Title"", ""Credits"", ""DepartmentId"" FROM ""HIGHSCHOOL_OLTP"".""Courses""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);
                var code = reader.IsDBNull(1) ? null : reader.GetString(1);
                var title = reader.IsDBNull(2) ? null : reader.GetString(2);
                var credits = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var deptId = reader.IsDBNull(4) ? null : reader.GetFieldValue<byte[]>(4);

                const string insert = @"INSERT INTO DIMCOURSE (ID, CODE, TITLE, CREDITS, DEPARTMENTID) VALUES (:id, :code, :title, :credits, :dept)";
                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateRawParam("id", id));
                cmd.Parameters.Add(CreateStringParam("code", code, 50));
                cmd.Parameters.Add(CreateStringParam("title", title, 255));
                cmd.Parameters.Add(CreateNullableIntParam("credits", credits));
                cmd.Parameters.Add(CreateNullableRawParam("dept", deptId));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("DimCourse loaded successfully.");
        }

        #endregion

        #region FactLoaders

        public async Task LoadFactPrerequisiteAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading FACTPREREQUISITE...");

            const string checkTableSql = @"
                SELECT COUNT(*) 
                FROM ALL_TABLES 
                WHERE OWNER = 'HIGHSCHOOL_OLTP' AND TABLE_NAME = 'CourseCourse'";

            using (var checkCmd = new OracleCommand(checkTableSql, oltpConn))
            {
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (exists == 0)
                {
                    log.Add("ETL skipped FACTPREREQUISITE: OLTP table CourseCourse does not exist.");
                    return;
                }
            }

            const string sql = @"
                SELECT ""IsPrerequisiteForId"" AS CourseId, ""PrerequisitesId"" AS PrerequisiteId
                FROM ""HIGHSCHOOL_OLTP"".""CourseCourse""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var courseId = reader.IsDBNull(0) ? null : reader.GetFieldValue<byte[]>(0);
                var prereqId = reader.IsDBNull(1) ? null : reader.GetFieldValue<byte[]>(1);

                const string insert = @"
                    INSERT INTO FACTPREREQUISITE (PREREQUISITEID, COURSEID)
                    VALUES (:prereq, :course)";

                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateNullableRawParam("prereq", prereqId));
                cmd.Parameters.Add(CreateNullableRawParam("course", courseId));
                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("FACTPREREQUISITE loaded successfully.");
        }

        public async Task LoadFactClassAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading FACTCLASS...");

            const string sql = @"
                SELECT ""Id"", ""Location"", ""Day"", ""Period"", ""StartDate"", ""EndDate"", ""CourseId"", ""TeacherId"", ""TermId""
                FROM ""HIGHSCHOOL_OLTP"".""Classes""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetFieldValue<byte[]>(0);
                var location = reader.IsDBNull(1) ? null : reader.GetString(1);
                var day = reader.GetInt32(2);
                var period = reader.GetInt32(3);
                var startDate = reader.GetString(4);
                var endDate = reader.GetString(5);
                var courseId = reader.GetFieldValue<byte[]>(6);
                var teacherId = reader.GetFieldValue<byte[]>(7);
                var termId = reader.GetFieldValue<byte[]>(8);

                const string insert = @"
                    INSERT INTO FACTCLASS
                    (ID, LOCATION, DAY, PERIOD, STARTDATE, ENDDATE, COURSEID, TEACHERID, DIMTERMID)
                    VALUES (:p_id, :p_loc, :p_day, :p_period, :p_startdate, :p_enddate, :p_courseid, :p_teacherid, :p_termid)";

                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateNullableRawParam("p_id", id));
                cmd.Parameters.Add(CreateStringParam("p_loc", location, 255));
                cmd.Parameters.Add(CreateNullableIntParam("p_day", day));
                cmd.Parameters.Add(CreateNullableIntParam("p_period", period));
                cmd.Parameters.Add(CreateStringParam("p_startdate", startDate, 20));
                cmd.Parameters.Add(CreateStringParam("p_enddate", endDate, 20));
                cmd.Parameters.Add(CreateNullableRawParam("p_courseid", courseId));
                cmd.Parameters.Add(CreateNullableRawParam("p_teacherid", teacherId));
                cmd.Parameters.Add(CreateNullableRawParam("p_termid", termId));

                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("FACTCLASS loaded successfully.");
        }

        public async Task LoadFactEnrollmentAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading FACTENROLLMENT...");

            const string sql = @"
                SELECT e.""StudentId"",
                       e.""ClassId"",
                       e.""Grade"",
                       c.""Credits"",
                       e.""AttendanceRate"",
                       e.""WorkHabits"",
                       e.""Final"",
                       cl.""TermId""
                FROM ""HIGHSCHOOL_OLTP"".""Enrollments"" e
                JOIN ""HIGHSCHOOL_OLTP"".""Classes"" cl ON e.""ClassId"" = cl.""Id""
                JOIN ""HIGHSCHOOL_OLTP"".""Courses"" c ON cl.""CourseId"" = c.""Id""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var studentId = reader.IsDBNull(0) ? null : reader.GetFieldValue<byte[]>(0);
                var classId = reader.IsDBNull(1) ? null : reader.GetFieldValue<byte[]>(1);
                var grade = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                var credits = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var attendance = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                var workHabits = reader.IsDBNull(5) ? null : reader.GetString(5);
                var finalVal = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6);
                var termId = reader.IsDBNull(7) ? null : reader.GetFieldValue<byte[]>(7);

                const string insert = @"
                    INSERT INTO FACTENROLLMENT
                    (STUDENTID, CLASSID, GRADE, CREDITS, ATTENDANCERATE, WORKHABITS, FINAL, DIMTERMID)
                    VALUES (:student, :class, :grade, :credits, :attendance, :work, :final, :term)";

                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateNullableRawParam("student", studentId));
                cmd.Parameters.Add(CreateNullableRawParam("class", classId));
                cmd.Parameters.Add(CreateNullableIntParam("grade", grade));
                cmd.Parameters.Add(CreateNullableIntParam("credits", credits));
                cmd.Parameters.Add(CreateNullableIntParam("attendance", attendance));
                cmd.Parameters.Add(CreateStringParam("work", workHabits, 10));
                cmd.Parameters.Add(new OracleParameter("final", OracleDbType.Int16) { Value = finalVal.HasValue ? (object)(finalVal.Value == 0 ? 0 : 1) : DBNull.Value });
                cmd.Parameters.Add(CreateNullableRawParam("term", termId));

                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("FACTENROLLMENT loaded successfully.");
        }

        public async Task LoadFactTeacherPerformanceAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading FACTTEACHERPERFORMANCE...");

            const string sql = @"
                SELECT c.""Id"" AS ClassId,
                    c.""TeacherId"" AS TeacherId,
                    c.""TermId"" AS TermId,
                    AVG(e.""Grade"") AS AvgGrade,
                    COUNT(e.""StudentId"") AS StudentCount
                FROM ""HIGHSCHOOL_OLTP"".""Classes"" c
                LEFT JOIN ""HIGHSCHOOL_OLTP"".""Enrollments"" e ON c.""Id"" = e.""ClassId""
                GROUP BY c.""Id"", c.""TeacherId"", c.""TermId""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var classId = reader.IsDBNull(0) ? null : reader.GetFieldValue<byte[]>(0);
                var teacherId = reader.IsDBNull(1) ? null : reader.GetFieldValue<byte[]>(1);
                var termId = reader.IsDBNull(2) ? null : reader.GetFieldValue<byte[]>(2);

                int? avgGrade = null;
                if (!reader.IsDBNull(3))
                {
                    // Try multiple conversion approaches
                    try
                    {
                        // First try direct decimal conversion
                        avgGrade = Convert.ToInt32(Math.Round(reader.GetDecimal(3)));
                    }
                    catch (InvalidCastException)
                    {
                        // Fallback to double conversion if decimal fails
                        avgGrade = Convert.ToInt32(Math.Round(reader.GetDouble(3)));
                    }
                }

                int? studentCount = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4));

                const string insert = @"
                    INSERT INTO FACTTEACHERPERFORMANCE
                    (AVGGRADE, STUDENTCOUNT, DIMTEACHERID, FACTCLASSID, DIMTERMID)
                    VALUES (:avg, :count, :teacher, :class, :term)";

                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateNullableDecimalParam("avg", avgGrade));
                cmd.Parameters.Add(CreateNullableIntParam("count", studentCount));
                cmd.Parameters.Add(CreateNullableRawParam("teacher", teacherId));
                cmd.Parameters.Add(CreateNullableRawParam("class", classId));
                cmd.Parameters.Add(CreateNullableRawParam("term", termId));

                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("FACTTEACHERPERFORMANCE loaded successfully.");
        }

        public async Task LoadFactDepartmentSummaryAsync(OracleConnection oltpConn, OracleConnection olapConn, List<string> log)
        {
            log.Add("Loading FACTDEPARTMENTSUMMARY...");

            const string sql = @"
                SELECT d.""Id"" AS DepartmentId,
                       cl.""TermId"" AS TermId,
                       AVG(e.""Grade"") AS AvgGrade,
                       COUNT(DISTINCT e.""StudentId"") AS StudentCount
                FROM ""HIGHSCHOOL_OLTP"".""Enrollments"" e
                JOIN ""HIGHSCHOOL_OLTP"".""Classes"" cl ON e.""ClassId"" = cl.""Id""
                JOIN ""HIGHSCHOOL_OLTP"".""Courses"" co ON cl.""CourseId"" = co.""Id""
                JOIN ""HIGHSCHOOL_OLTP"".""Departments"" d ON co.""DepartmentId"" = d.""Id""
                GROUP BY d.""Id"", cl.""TermId""";

            using var selectCmd = new OracleCommand(sql, oltpConn);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var deptId = reader.IsDBNull(0) ? null : reader.GetFieldValue<byte[]>(0);
                var termId = reader.IsDBNull(1) ? null : reader.GetFieldValue<byte[]>(1);

                int? avgGrade = null;
                if (!reader.IsDBNull(2))
                {
                    // Try multiple conversion approaches
                    try
                    {
                        // First try direct decimal conversion
                        avgGrade = Convert.ToInt32(Math.Round(reader.GetDecimal(2)));
                    }
                    catch (InvalidCastException)
                    {
                        // Fallback to double conversion if decimal fails
                        avgGrade = Convert.ToInt32(Math.Round(reader.GetDouble(2)));
                    }
                }

                // COUNT(StudentId) safely
                int? studentCount = null;
                if (!reader.IsDBNull(3))
                {
                    studentCount = Convert.ToInt32(reader.GetValue(3));
                }

                const string insert = @"
                    INSERT INTO FACTDEPARTMENTSUMMARY
                    (AVGGRADE, STUDENTCOUNT, DIMDEPARTMENTID, DIMTERMID)
                    VALUES (:avg, :count, :dept, :term)";

                using var cmd = new OracleCommand(insert, olapConn);
                cmd.Parameters.Add(CreateNullableIntParam("avg", avgGrade));
                cmd.Parameters.Add(CreateNullableIntParam("count", studentCount));
                cmd.Parameters.Add(CreateNullableRawParam("dept", deptId));
                cmd.Parameters.Add(CreateNullableRawParam("term", termId));

                await cmd.ExecuteNonQueryAsync();
            }

            log.Add("FACTDEPARTMENTSUMMARY loaded successfully.");
        }

        #endregion
    }
}
