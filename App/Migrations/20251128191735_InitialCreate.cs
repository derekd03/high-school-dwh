using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "HIGHSCHOOL_OLTP");

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    GradeLevel = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FirstName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    LastName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Phone = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    FirstName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    LastName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Phone = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Year = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TermNumber = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Location = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    HeadId = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Teachers_HeadId",
                        column: x => x.HeadId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Code = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Title = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Credits = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentTeacher",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    DepartmentsId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TeachersId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentTeacher", x => new { x.DepartmentsId, x.TeachersId });
                    table.ForeignKey(
                        name: "FK_DepartmentTeacher_Departments_DepartmentsId",
                        column: x => x.DepartmentsId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentTeacher_Teachers_TeachersId",
                        column: x => x.TeachersId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Location = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Day = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Period = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    StartDate = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    EndDate = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    CourseId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TeacherId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TermId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Classes_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseCourse",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    IsPrerequisiteForId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    PrerequisitesId = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCourse", x => new { x.IsPrerequisiteForId, x.PrerequisitesId });
                    table.ForeignKey(
                        name: "FK_CourseCourse_Courses_IsPrerequisiteForId",
                        column: x => x.IsPrerequisiteForId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseCourse_Courses_PrerequisitesId",
                        column: x => x.PrerequisitesId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "HIGHSCHOOL_OLTP",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ClassId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Grade = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AttendanceRate = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    WorkHabits = table.Column<string>(type: "NVARCHAR2(1)", nullable: false),
                    Final = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => new { x.StudentId, x.ClassId });
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "HIGHSCHOOL_OLTP",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CourseId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Classes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TeacherId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Classes",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_TermId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Classes",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCourse_PrerequisitesId",
                schema: "HIGHSCHOOL_OLTP",
                table: "CourseCourse",
                column: "PrerequisitesId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Departments",
                column: "HeadId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTeacher_TeachersId",
                schema: "HIGHSCHOOL_OLTP",
                table: "DepartmentTeacher",
                column: "TeachersId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassId",
                schema: "HIGHSCHOOL_OLTP",
                table: "Enrollments",
                column: "ClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCourse",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "DepartmentTeacher",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Classes",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Courses",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Terms",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "HIGHSCHOOL_OLTP");

            migrationBuilder.DropTable(
                name: "Teachers",
                schema: "HIGHSCHOOL_OLTP");
        }
    }
}
