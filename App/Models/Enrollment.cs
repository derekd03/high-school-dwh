namespace App.Models
{
    public class Enrollment
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = default!;

        public Guid ClassId { get; set; }
        public Class Class { get; set; } = default!;

        public int Grade { get; set; }
        public int AttendanceRate { get; set; }
        public char WorkHabits { get; set; }
        public bool Final { get; set; }
    }
}
