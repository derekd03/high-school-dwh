using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace App.Models;

public class Teacher : Person
{
    public ICollection<Department> Departments { get; set; } = new HashSet<Department>();
    public ICollection<Class> Classes { get; set; } = new HashSet<Class>();
}
