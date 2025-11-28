using System;
using System.Collections;
using System.Collections.Generic;

namespace App.Models.OLTP;

public class Course
{
	public Guid Id { get; set; } = Guid.NewGuid();

    public required string Code { get; set; }
	public required string Title { get; set; }
	public int Credits { get; set; }
	
	public Guid DepartmentId { get; set; }
    public required Department Department { get; set; }

    public required ICollection<Course> Prerequisites { get; set; } = new HashSet<Course>();
    public required ICollection<Course> IsPrerequisiteFor { get; set; } = new HashSet<Course>();
}
