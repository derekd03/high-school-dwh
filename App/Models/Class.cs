using Microsoft.AspNetCore.Components.Routing;

namespace App.Models;
public class Class
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string Location { get; set; }

    public int Day { get; set; } // 1/2
    public int Period { get; set; } // 1-4

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public Guid CourseId { get; set; }
    public required Course Course { get; set; }

    public Guid TeacherId { get; set; }
    public required Teacher Teacher { get; set; }

    public Guid TermId { get; set; }
    public required Term Term { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
}
