namespace App.Services
{
    using App.Data;
    using App.Models.OLTP;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualBasic.FileIO;

    public class SeedDataService
    {
        private readonly OltpDbContext _context;

        public SeedDataService(OltpDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedTeachersAsync();
            await _context.SaveChangesAsync();

            await SeedDepartmentsAsync();
            await _context.SaveChangesAsync();

            await SeedCoursesFromCsvAsync();
            await _context.SaveChangesAsync();

            await SeedStudentsAsync();
            await _context.SaveChangesAsync();

            await SeedTermsAsync();
            await _context.SaveChangesAsync();

            await SeedClassInstancesAsync();
            await _context.SaveChangesAsync();

            await SeedEnrollmentsAsync();
            await _context.SaveChangesAsync();

            await SeedCoursePrerequisitesAsync();
            await _context.SaveChangesAsync();
        }

        private async Task SeedTeachersAsync()
        {
            if (await _context.Teachers.AnyAsync()) return;

            var teachers = new List<Teacher>
            {
            new() { Id = Guid.NewGuid(), FirstName="Alice", LastName="Smith", Email="alice.smith@example.com", Phone="555-1000" },
            new() { Id = Guid.NewGuid(), FirstName="Bob", LastName="Johnson", Email="bob.johnson@example.com", Phone="555-1001" },
            new() { Id = Guid.NewGuid(), FirstName="Carol", LastName="Williams", Email="carol.williams@example.com", Phone="555-1002" },
            new() { Id = Guid.NewGuid(), FirstName="David", LastName="Brown", Email="david.brown@example.com", Phone="555-1003" },
            new() { Id = Guid.NewGuid(), FirstName="Eva", LastName="Davis", Email="eva.davis@example.com", Phone="555-1004" },
            new() { Id = Guid.NewGuid(), FirstName="Frank", LastName="Miller", Email="frank.miller@example.com", Phone="555-1005" },
            new() { Id = Guid.NewGuid(), FirstName="Grace", LastName="Wilson", Email="grace.wilson@example.com", Phone="555-1006" },
            new() { Id = Guid.NewGuid(), FirstName="Henry", LastName="Moore", Email="henry.moore@example.com", Phone="555-1007" },
            new() { Id = Guid.NewGuid(), FirstName="Irene", LastName="Taylor", Email="irene.taylor@example.com", Phone="555-1008" },
            new() { Id = Guid.NewGuid(), FirstName="Jack", LastName="Anderson", Email="jack.anderson@example.com", Phone="555-1009" }
            };

            await _context.Teachers.AddRangeAsync(teachers);
        }

        private async Task SeedDepartmentsAsync()
        {
            if (await _context.Departments.AnyAsync()) return;

            var teachers = await _context.Teachers.ToListAsync();

            if (!teachers.Any())
            {
                Console.WriteLine("No teachers found. Departments cannot be seeded yet.");
                return;
            }

            Teacher T(string email)
            {
                var teacher = teachers.FirstOrDefault(t => t.Email == email);
                if (teacher == null)
                    throw new InvalidOperationException($"Teacher with email '{email}' not found. Make sure SeedTeachersAsync ran first.");
                return teacher;
            }

            var departments = new List<Department>
            {
                new()
                {
                    Name = "English",
                    Location = "012A",
                    HeadId = T("alice.smith@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("alice.smith@example.com") }
                },
                new()
                {
                    Name = "Mathematics",
                    Location = "145",
                    HeadId = T("bob.johnson@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("bob.johnson@example.com") }
                },
                new()
                {
                    Name = "Science",
                    Location = "233A",
                    HeadId = T("carol.williams@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("carol.williams@example.com") }
                },
                new()
                {
                    Name = "Social Studies",
                    Location = "278",
                    HeadId = T("david.brown@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("david.brown@example.com") }
                },
                new()
                {
                    Name = "Physical Education",
                    Location = "300A",
                    HeadId = T("eva.davis@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("eva.davis@example.com") }
                },
                new()
                {
                    Name = "French / Modern Languages",
                    Location = "199",
                    HeadId = T("frank.miller@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("frank.miller@example.com") }
                },
                new()
                {
                    Name = "Applied Skills / Business / Home Economics",
                    Location = "277A",
                    HeadId = T("grace.wilson@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("grace.wilson@example.com") }
                },
                new()
                {
                    Name = "Performing Arts – Music / Theatre",
                    Location = "310",
                    HeadId = T("henry.moore@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("henry.moore@example.com") }
                },
                new()
                {
                    Name = "Tech Education / Visual Arts",
                    Location = "322A",
                    HeadId = T("irene.taylor@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("irene.taylor@example.com") }
                },
                new()
                {
                    Name = "Other / General",
                    Location = "099",
                    HeadId = T("jack.anderson@example.com").Id,
                    Courses = new HashSet<Course>(),
                    Teachers = new HashSet<Teacher> { T("jack.anderson@example.com") }
                }
            };

            await _context.Departments.AddRangeAsync(departments);
            await _context.SaveChangesAsync();
        }

        private async Task SeedCoursesFromCsvAsync()
        {
            if (await _context.Courses.AnyAsync())
                return;

            string basePath = Directory.GetCurrentDirectory();
            string path = Path.Combine(basePath, "courses.csv");

            if (!File.Exists(path))
                throw new FileNotFoundException("courses.csv not found in project root", path);

            var departments = await _context.Departments.ToListAsync();
            var courses = new List<Course>();

            using (var reader = new StreamReader(path))
            {
                string? header = await reader.ReadLineAsync(); // skip header

                while (!reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Split CSV using simple comma parsing, but handle quoted fields
                    // e.g., "ART101","Podcasting and Audio Storytelling 11",3,"Languages"
                    string[] parts = line.Split(',');
                    if (parts.Length < 4)
                        continue;

                    string code = parts[0].Trim();
                    string title = parts[1].Trim().Trim('"'); // remove potential quotes
                    string creditsString = parts[2].Trim();
                    string departmentName = parts[3].Trim().Trim('"'); // remove potential quotes

                    // Try parse credits safely
                    if (!int.TryParse(creditsString, out int credits))
                    {
                        Console.WriteLine($"WARNING: Skipping course '{title}' because credits is invalid: '{creditsString}'");
                        continue;
                    }

                    var department = departments.FirstOrDefault(d =>
                        d.Name.Equals(departmentName, StringComparison.OrdinalIgnoreCase));

                    if (department == null)
                    {
                        Console.WriteLine($"WARNING: Department not found for course `{code}` — '{departmentName}'");
                        continue;
                    }

                    var course = new Course
                    {
                        Code = code,
                        Title = title,
                        Credits = credits,
                        Department = department,
                        DepartmentId = department.Id,
                        Prerequisites = new HashSet<Course>(),
                        IsPrerequisiteFor = new HashSet<Course>()
                    };

                    courses.Add(course);
                }
            }

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Seeded {courses.Count} courses from CSV.");
        }

        private async Task SeedStudentsAsync()
        {
            if (await _context.Students.AnyAsync()) return;

            var firstNames = new[]
            {
                "Liam","Olivia","Noah","Emma","Ava","Sophia","Jackson","Mia","Lucas","Isabella",
                "Ethan","Amelia","Mason","Harper","Logan","Evelyn","James","Abigail","Elijah","Emily",
                "Benjamin","Ella","Henry","Elizabeth","Sebastian","Sofia","Jack","Avery","Owen","Scarlett",
                "Daniel","Grace","Jacob","Chloe","Wyatt","Victoria","Michael","Riley","Alexander","Aria",
                "Matthew","Lily","Samuel","Hannah","David","Luna","Joseph","Zoe","Carter","Nora"
            };

            var lastNames = new[]
            {
                "Smith","Johnson","Williams","Brown","Jones","Garcia","Miller","Davis","Rodriguez","Martinez",
                "Hernandez","Lopez","Gonzalez","Wilson","Anderson","Thomas","Taylor","Moore","Jackson","Martin",
                "Lee","Perez","Thompson","White","Harris","Sanchez","Clark","Ramirez","Lewis","Robinson",
                "Walker","Young","Allen","King","Wright","Scott","Torres","Nguyen","Hill","Flores",
                "Green","Adams","Nelson","Baker","Hall","Rivera","Campbell","Mitchell","Carter","Roberts"
            };

            var random = new Random();
            var students = new List<Student>();

            for (int i = 0; i < 100; i++)
            {
                string first = firstNames[random.Next(firstNames.Length)];
                string last = lastNames[random.Next(lastNames.Length)];

                students.Add(new Student
                {
                    Id = Guid.NewGuid(),
                    FirstName = first,
                    LastName = last,
                    GradeLevel = random.Next(8, 13),  // Grades 8–12
                    Email = $"{first.ToLower()}.{last.ToLower()}{random.Next(1000, 9999)}@school.example.com",
                    Phone = $"555-{random.Next(2000, 9999)}"
                });
            }

            await _context.Students.AddRangeAsync(students);
        }

        private async Task SeedTermsAsync()
        {
            if (await _context.Terms.AnyAsync()) return;

            var terms = new List<Term>
            {
                new() { Id = Guid.NewGuid(), Year = "2024", TermNumber = 1 },
                new() { Id = Guid.NewGuid(), Year = "2024", TermNumber = 2 }
            };

            await _context.Terms.AddRangeAsync(terms);
        }

        private async Task SeedClassInstancesAsync()
        {
            if (await _context.Classes.AnyAsync()) return;

            var courses = await _context.Courses.ToListAsync();
            var teachers = await _context.Teachers.ToListAsync();
            var terms = await _context.Terms.ToListAsync();

            if (teachers.Count == 0 || courses.Count == 0 || terms.Count == 0)
                throw new InvalidOperationException("Courses, Teachers, and Terms must be seeded before ClassInstances.");

            var random = new Random();
            var classInstances = new List<ClassInstance>();

            foreach (var course in courses)
            {
                var teacher = teachers[random.Next(teachers.Count)];
                var term = terms[random.Next(terms.Count)];

                classInstances.Add(new ClassInstance
                {
                    Id = Guid.NewGuid(),
                    Location = $"Room {random.Next(100, 500)}",
                    Day = random.Next(1, 3), // Day 1 to 2
                    Period = random.Next(1, 5), // Periods 1 to 4
                    StartDate = new DateOnly(2024, 9, 5),
                    EndDate = new DateOnly(2024, 6, 20),
                    CourseId = course.Id,
                    Course = course,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    TermId = term.Id,
                    Term = term,
                    Enrollments = new List<Enrollment>()
                });
            }

            await _context.Classes.AddRangeAsync(classInstances);
        }

        private async Task SeedEnrollmentsAsync()
        {
            if (await _context.Enrollments.AnyAsync()) return;

            var students = await _context.Students.ToListAsync();
            var classInstances = await _context.Classes.ToListAsync();

            if (students.Count == 0 || classInstances.Count == 0)
                throw new InvalidOperationException("Students and ClassInstances must be seeded before Enrollments.");

            var random = new Random();
            var enrollments = new List<Enrollment>();

            foreach (var classInstance in classInstances)
            {
                // Enroll 10 to 30 students in each class
                int numberOfEnrollments = random.Next(10, 31);
                var selectedStudents = students.OrderBy(x => random.Next()).Take(numberOfEnrollments).ToList();
                foreach (var student in selectedStudents)
                {
                    int grade = random.Next(50, 101); // 50–100

                    char workHabit;

                    if (grade < 65)
                        workHabit = 'N';
                    else if (grade < 80)
                        workHabit = 'S';
                    else if (grade < 90)
                        workHabit = 'G';
                    else
                        workHabit = 'E';

                    enrollments.Add(new Enrollment
                    {
                        StudentId = student.Id,
                        ClassId = classInstance.Id,
                        Grade = grade,
                        AttendanceRate = random.Next(75, 101),
                        WorkHabits = workHabit,
                        Final = true
                    });
                }
            }

            await _context.Enrollments.AddRangeAsync(enrollments);
        }

        private async Task SeedCoursePrerequisitesAsync()
        {
            // Load all courses with prerequisites and IsPrerequisiteFor
            var courses = await _context.Courses
                .Include(c => c.Prerequisites)
                .Include(c => c.IsPrerequisiteFor)
                .ToListAsync();

            // Exit early if any course already has prerequisites
            if (courses.Any(c => c.Prerequisites.Any()))
                return;

            // Map keywords → standardized subject group
            Dictionary<string, string> subjectMap = new()
            {
                ["Math"] = "Math",
                ["Calculus"] = "Math",
                ["Precalculus"] = "Math",
                ["Foundations"] = "Math",
                ["English"] = "English",
                ["Literary"] = "English",
                ["Creative"] = "English",
                ["New Media"] = "English",
                ["Science"] = "Science",
                ["Biology"] = "Science",
                ["Chemistry"] = "Science",
                ["Physics"] = "Science",
                ["Earth"] = "Science",
                ["Social"] = "Social",
                ["History"] = "Social",
                ["Geography"] = "Social",
                ["Civics"] = "Social",
                ["Law"] = "Social",
                ["French"] = "Languages",
                ["Spanish"] = "Languages",
                ["Japanese"] = "Languages"
            };

            string GetGroup(string title)
            {
                foreach (var kv in subjectMap)
                    if (title.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                return "";
            }

            int ExtractGrade(string title)
            {
                foreach (int g in new[] { 12, 11, 10, 9, 8 })
                    if (title.Contains($" {g}", StringComparison.OrdinalIgnoreCase))
                        return g;
                return 0;
            }

            // Preload existing relationships in memory to avoid duplicate inserts
            var existingRelations = courses
                .SelectMany(c => c.Prerequisites.Select(p => (CourseId: c.Id, PrereqId: p.Id)))
                .ToHashSet();

            foreach (var course in courses)
            {
                string group = GetGroup(course.Title);
                if (string.IsNullOrEmpty(group))
                    continue; // Not a core course

                int grade = ExtractGrade(course.Title);
                if (grade <= 8)
                    continue; // No prereq for grade 8

                // Find one-year-earlier candidates
                var candidates = courses
                    .Where(c => GetGroup(c.Title) == group && ExtractGrade(c.Title) == grade - 1)
                    .OrderBy(c => c.Title)
                    .ToList();

                if (!candidates.Any())
                    continue;

                var prerequisite = candidates.First();

                // Only add if the relationship doesn't exist
                if (!existingRelations.Contains((course.Id, prerequisite.Id)))
                {
                    course.Prerequisites.Add(prerequisite);
                    // optional in-memory navigation
                    prerequisite.IsPrerequisiteFor.Add(course);

                    // Add to hash set to prevent duplicates in this run
                    existingRelations.Add((course.Id, prerequisite.Id));
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
