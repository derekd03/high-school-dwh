using System;

namespace App.Models;

public abstract class Person
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
}
