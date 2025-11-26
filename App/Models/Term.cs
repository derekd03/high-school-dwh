namespace App.Models;

public class Term
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    public required string Year { get; set; }
    public required int TermNumber { get; set; }

    public ICollection<Class> Classes { get; set; } = new HashSet<Class>();
}

