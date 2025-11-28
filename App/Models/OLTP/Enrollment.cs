namespace App.Models.OLTP
{
    public class Enrollment
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = default!;

        public Guid ClassId { get; set; }
        public ClassInstance Class { get; set; } = default!;

        public int Grade { get; set; }
        public int AttendanceRate { get; set; }
        public char WorkHabits { get; set; }
        public bool Final { get; set; }
    }
}
