namespace App.Models.OLTP;

public class Term
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Year { get; set; }
    public required int TermNumber { get; set; }

    public ICollection<ClassInstance> Classes { get; set; } = new HashSet<ClassInstance>();
}

