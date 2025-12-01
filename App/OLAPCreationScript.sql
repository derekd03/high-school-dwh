-- ================================
-- Drop existing OLAP tables
-- ================================

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE FACTDEPARTMENTSUMMARY CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE FACTTEACHERPERFORMANCE CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE FACTENROLLMENT CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE FACTCLASS CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE FACTPREREQUISITE CASCADE CONSTRAINTS';

    EXECUTE IMMEDIATE 'DROP TABLE DIMDEPARTMENTTEACHER CASCADE CONSTRAINTS';

    EXECUTE IMMEDIATE 'DROP TABLE DIMCOURSE CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE DIMDEPARTMENT CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE DIMTEACHER CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE DIMSTUDENT CASCADE CONSTRAINTS';
    EXECUTE IMMEDIATE 'DROP TABLE DIMTERM CASCADE CONSTRAINTS';
EXCEPTION
    WHEN OTHERS THEN
        NULL; -- ignore errors if tables do not exist
END;
/

-- ================================
-- Create OLAP tables (fixed)
-- ================================

-- Dim Tables
CREATE TABLE DimTerm (
    ID          RAW(16)        NOT NULL,
    Year        NUMBER(4),
    TermNumber  NUMBER(2)      NOT NULL,
    CONSTRAINT PK_DimTerm PRIMARY KEY (ID)
);

CREATE TABLE DimStudent (
    ID          RAW(16)         NOT NULL,
    FirstName   NVARCHAR2(255),
    LastName    NVARCHAR2(255),
    Phone       NVARCHAR2(100),
    Email       NVARCHAR2(255),
    GradeLevel  NUMBER(2),
    CONSTRAINT PK_DimStudent PRIMARY KEY (ID)
);

CREATE TABLE DimTeacher (
    ID          RAW(16)         NOT NULL,
    FirstName   NVARCHAR2(255),
    LastName    NVARCHAR2(255),
    Phone       NVARCHAR2(100),
    Email       NVARCHAR2(255),
    CONSTRAINT PK_DimTeacher PRIMARY KEY (ID)
);

CREATE TABLE DimDepartment (
    ID          RAW(16)         NOT NULL,
    Name        NVARCHAR2(255),
    Location    NVARCHAR2(255),
    TeacherID   RAW(16),
    CONSTRAINT PK_DimDepartment PRIMARY KEY (ID),
    CONSTRAINT FK_Department_Teacher FOREIGN KEY (TeacherID) REFERENCES DimTeacher(ID)
);

CREATE TABLE DimDepartmentTeacher (
    DepartmentID RAW(16) NOT NULL,
    TeacherID    RAW(16) NOT NULL,
    CONSTRAINT FK_DDT_Department FOREIGN KEY (DepartmentID) REFERENCES DimDepartment(ID),
    CONSTRAINT FK_DDT_Teacher FOREIGN KEY (TeacherID) REFERENCES DimTeacher(ID)
);

CREATE TABLE DimCourse (
    ID           RAW(16)         NOT NULL,
    Code         NVARCHAR2(50),
    Title        NVARCHAR2(255),
    Credits      NUMBER(2),
    DepartmentID RAW(16),
    CONSTRAINT PK_DimCourse PRIMARY KEY (ID),
    CONSTRAINT FK_Course_Department FOREIGN KEY (DepartmentID) REFERENCES DimDepartment(ID)
);

-- Fact Tables
CREATE TABLE FactPrerequisite (
    PrerequisiteID RAW(16),
    CourseID       RAW(16),
    CONSTRAINT FK_Prereq_Course FOREIGN KEY (CourseID) REFERENCES DimCourse(ID)
);

CREATE TABLE FactClass (
    ID         RAW(16) NOT NULL,
    Location   NVARCHAR2(255),
    Day        NUMBER(2),
    Period     NUMBER(2),
    StartDate  NVARCHAR2(20),
    EndDate    NVARCHAR2(20),
    CourseID   RAW(16),
    TeacherID  RAW(16),
    DimTermID  RAW(16),
    CONSTRAINT PK_FactClass PRIMARY KEY (ID),
    CONSTRAINT FK_Class_Course FOREIGN KEY (CourseID) REFERENCES DimCourse(ID),
    CONSTRAINT FK_Class_Teacher FOREIGN KEY (TeacherID) REFERENCES DimTeacher(ID),
    CONSTRAINT FK_Class_Term FOREIGN KEY (DimTermID) REFERENCES DimTerm(ID)
);

CREATE TABLE FactEnrollment (
    StudentID      RAW(16),
    ClassID        RAW(16),
    Grade          NUMBER(3),
    Credits        NUMBER(2),
    AttendanceRate NUMBER(3),
    WorkHabits     NVARCHAR2(10),
    Final          NUMBER(1),
    DimTermID      RAW(16),
    CONSTRAINT FK_Enrollment_Student FOREIGN KEY (StudentID) REFERENCES DimStudent(ID),
    CONSTRAINT FK_Enrollment_Class FOREIGN KEY (ClassID) REFERENCES FactClass(ID),
    CONSTRAINT FK_Enrollment_Term FOREIGN KEY (DimTermID) REFERENCES DimTerm(ID)
);

CREATE TABLE FactTeacherPerformance (
    AvgGrade      NUMBER(3),
    StudentCount  NUMBER(5),
    DimTeacherID  RAW(16),
    FactClassID   RAW(16),
    DimTermID     RAW(16),
    CONSTRAINT FK_TeacherPerf_Teacher FOREIGN KEY (DimTeacherID) REFERENCES DimTeacher(ID),
    CONSTRAINT FK_TeacherPerf_Class FOREIGN KEY (FactClassID) REFERENCES FactClass(ID),
    CONSTRAINT FK_TeacherPerf_Term FOREIGN KEY (DimTermID) REFERENCES DimTerm(ID)
);

CREATE TABLE FactDepartmentSummary (
    AvgGrade      NUMBER(3),
    StudentCount  NUMBER(6),
    DimDepartmentID RAW(16),
    DimTermID       RAW(16),
    CONSTRAINT FK_DeptSummary_Department FOREIGN KEY (DimDepartmentID) REFERENCES DimDepartment(ID),
    CONSTRAINT FK_DeptSummary_Term FOREIGN KEY (DimTermID) REFERENCES DimTerm(ID)
);
