using System;
using System.Collections;
using System.Transactions;

namespace App.Models.OLTP;

public class Student : Person
{
	public int GradeLevel { get; set; }
	public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
}