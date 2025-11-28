using System;
using System.Collections.Generic;
using App.Models.OLTP;
using Microsoft.EntityFrameworkCore;

namespace App.Data;

public partial class OltpDbContext : DbContext
{
    public OltpDbContext()
    {
    }

    public OltpDbContext(DbContextOptions<OltpDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ClassInstance> Classes => Set<ClassInstance>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Term> Terms => Set<Term>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("HIGHSCHOOL_OLTP")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Ignore<Person>();

        modelBuilder.Entity<Student>()
        .ToTable("Students");

        modelBuilder.Entity<Teacher>()
            .ToTable("Teachers");

        modelBuilder.Entity<Department>()
            .HasOne(d => d.Head)
            .WithMany()
            .HasForeignKey(d => d.HeadId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Teachers)
            .WithMany(t => t.Departments);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Courses)
            .WithOne(c => c.Department)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Teacher>()
            .HasMany(t => t.Classes)
            .WithOne(cls => cls.Teacher)
            .HasForeignKey(cls => cls.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasKey(e => new { e.StudentId, e.ClassId });

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassId);

        modelBuilder.Entity<Term>()
            .HasMany(t => t.Classes)
            .WithOne(c => c.Term)
            .HasForeignKey(c => c.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
