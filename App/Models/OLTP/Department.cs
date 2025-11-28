namespace App.Models.OLTP;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }
    public required string Location { get; set; }
    
    public Guid? HeadId { get; set; }
    public Teacher? Head { get; set; }

    public required ICollection<Course> Courses { get; set; }
    public required ICollection<Teacher> Teachers { get; set; }
}
