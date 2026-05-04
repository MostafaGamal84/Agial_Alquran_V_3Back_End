/*
  Production database update script
  Append new database changes to this file in execution order.
  Run on production only after taking a backup and validating on staging.
*/

/* 2026-05-03
   Change: Add salary receive method for teachers on dbo.[User]
*/
IF COL_LENGTH('dbo.[User]', 'SalaryReceiveMethodId') IS NULL
BEGIN
    ALTER TABLE dbo.[User]
    ADD SalaryReceiveMethodId INT NULL;
END;
GO

/* 2026-05-03
   Change: Add user system membership to distinguish Quran school, academic school, or both
*/
IF COL_LENGTH('dbo.[User]', 'EducationSystemTypeId') IS NULL
BEGIN
    ALTER TABLE dbo.[User]
    ADD EducationSystemTypeId INT NOT NULL
        CONSTRAINT DF_User_EducationSystemTypeId DEFAULT (1) WITH VALUES;
END;
GO

/* 2026-05-03
   Change: Add academic subjects reports subsystem with separate circles, relationships, and reports
*/
IF OBJECT_ID('dbo.AcademicSubject', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicSubject
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicSubject PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        DisplayOrder INT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicSubject_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL
    );
END;
GO

IF OBJECT_ID('dbo.AcademicCircle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicCircle
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicCircle PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        TeacherId INT NOT NULL,
        BranchId INT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicCircle_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicCircle_Teacher FOREIGN KEY (TeacherId) REFERENCES dbo.[User](Id)
    );
END;
GO

IF OBJECT_ID('dbo.AcademicCircleStudent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicCircleStudent
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicCircleStudent PRIMARY KEY,
        AcademicCircleId INT NOT NULL,
        StudentId INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicCircleStudent_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicCircleStudent_Circle FOREIGN KEY (AcademicCircleId) REFERENCES dbo.AcademicCircle(Id),
        CONSTRAINT FK_AcademicCircleStudent_Student FOREIGN KEY (StudentId) REFERENCES dbo.[User](Id)
    );
END;
GO

IF OBJECT_ID('dbo.AcademicManagerCircle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicManagerCircle
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicManagerCircle PRIMARY KEY,
        ManagerId INT NOT NULL,
        AcademicCircleId INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicManagerCircle_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicManagerCircle_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_AcademicManagerCircle_Circle FOREIGN KEY (AcademicCircleId) REFERENCES dbo.AcademicCircle(Id)
    );
END;
GO

IF OBJECT_ID('dbo.AcademicManagerTeacher', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicManagerTeacher
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicManagerTeacher PRIMARY KEY,
        ManagerId INT NOT NULL,
        TeacherId INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicManagerTeacher_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicManagerTeacher_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_AcademicManagerTeacher_Teacher FOREIGN KEY (TeacherId) REFERENCES dbo.[User](Id)
    );
END;
GO

IF OBJECT_ID('dbo.AcademicManagerStudent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicManagerStudent
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicManagerStudent PRIMARY KEY,
        ManagerId INT NOT NULL,
        StudentId INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicManagerStudent_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicManagerStudent_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_AcademicManagerStudent_Student FOREIGN KEY (StudentId) REFERENCES dbo.[User](Id)
    );
END;
GO

IF OBJECT_ID('dbo.AcademicReport', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademicReport
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicReport PRIMARY KEY,
        AcademicCircleId INT NOT NULL,
        StudentId INT NOT NULL,
        TeacherId INT NOT NULL,
        SubjectId INT NOT NULL,
        ReportDate DATETIME NOT NULL,
        StageId INT NOT NULL,
        LessonTitle NVARCHAR(500) NOT NULL,
        StudentPerformanceId INT NOT NULL,
        PreviousHomeworkStatusId INT NOT NULL,
        HomeworkScore INT NOT NULL,
        NextHomework NVARCHAR(1000) NULL,
        TeacherNotes NVARCHAR(2000) NULL,
        SessionDurationMinutes INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_AcademicReport_IsDeleted DEFAULT (0),
        CreatedAt DATETIME NULL,
        CreatedBy INT NULL,
        ModefiedAt DATETIME NULL,
        ModefiedBy INT NULL,
        CONSTRAINT FK_AcademicReport_Circle FOREIGN KEY (AcademicCircleId) REFERENCES dbo.AcademicCircle(Id),
        CONSTRAINT FK_AcademicReport_Student FOREIGN KEY (StudentId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_AcademicReport_Teacher FOREIGN KEY (TeacherId) REFERENCES dbo.[User](Id),
        CONSTRAINT FK_AcademicReport_Subject FOREIGN KEY (SubjectId) REFERENCES dbo.AcademicSubject(Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcademicCircle_TeacherId' AND object_id = OBJECT_ID('dbo.AcademicCircle'))
BEGIN
    CREATE INDEX IX_AcademicCircle_TeacherId ON dbo.AcademicCircle(TeacherId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AcademicCircleStudent_CircleStudent' AND object_id = OBJECT_ID('dbo.AcademicCircleStudent'))
BEGIN
    CREATE UNIQUE INDEX UX_AcademicCircleStudent_CircleStudent
        ON dbo.AcademicCircleStudent(AcademicCircleId, StudentId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AcademicManagerCircle_ManagerCircle' AND object_id = OBJECT_ID('dbo.AcademicManagerCircle'))
BEGIN
    CREATE UNIQUE INDEX UX_AcademicManagerCircle_ManagerCircle
        ON dbo.AcademicManagerCircle(ManagerId, AcademicCircleId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AcademicManagerTeacher_ManagerTeacher' AND object_id = OBJECT_ID('dbo.AcademicManagerTeacher'))
BEGIN
    CREATE UNIQUE INDEX UX_AcademicManagerTeacher_ManagerTeacher
        ON dbo.AcademicManagerTeacher(ManagerId, TeacherId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AcademicManagerStudent_ManagerStudent' AND object_id = OBJECT_ID('dbo.AcademicManagerStudent'))
BEGIN
    CREATE UNIQUE INDEX UX_AcademicManagerStudent_ManagerStudent
        ON dbo.AcademicManagerStudent(ManagerId, StudentId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcademicReport_ReportDate' AND object_id = OBJECT_ID('dbo.AcademicReport'))
BEGIN
    CREATE INDEX IX_AcademicReport_ReportDate ON dbo.AcademicReport(ReportDate);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcademicReport_TeacherId' AND object_id = OBJECT_ID('dbo.AcademicReport'))
BEGIN
    CREATE INDEX IX_AcademicReport_TeacherId ON dbo.AcademicReport(TeacherId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AcademicReport_StudentId' AND object_id = OBJECT_ID('dbo.AcademicReport'))
BEGIN
    CREATE INDEX IX_AcademicReport_StudentId ON dbo.AcademicReport(StudentId);
END;
GO

UPDATE dbo.AcademicSubject
SET Name = N'اللغة العربية'
WHERE Name = N'Ø§Ù„Ù„ØºØ© Ø§Ù„Ø¹Ø±Ø¨ÙŠØ©';
GO

UPDATE dbo.AcademicSubject
SET Name = N'اللغة الإنجليزية'
WHERE Name = N'Ø§Ù„Ù„ØºØ© Ø§Ù„Ø¥Ù†Ø¬Ù„ÙŠØ²ÙŠØ©';
GO

UPDATE dbo.AcademicSubject
SET Name = N'الرياضيات' خلي
WHERE Name = N'Ø§Ù„Ø±ÙŠØ§Ø¶ÙŠØ§Øª';
GO

UPDATE dbo.AcademicSubject
SET Name = N'العلوم'
WHERE Name = N'Ø§Ù„Ø¹Ù„ÙˆÙ…';
GO

UPDATE dbo.AcademicSubject
SET Name = N'الدراسات الاجتماعية'
WHERE Name = N'Ø§Ù„Ø¯Ø±Ø§Ø³Ø§Øª Ø§Ù„Ø§Ø¬ØªÙ…Ø§Ø¹ÙŠØ©';
GO

UPDATE dbo.AcademicSubject
SET Name = N'الحاسب الآلي'
WHERE Name = N'Ø§Ù„Ø­Ø§Ø³Ø¨ Ø§Ù„Ø¢Ù„ÙŠ';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'اللغة العربية')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'اللغة العربية', 1, 0, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'اللغة الإنجليزية')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'اللغة الإنجليزية', 2, 0, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'الرياضيات')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'الرياضيات', 3, 0, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'العلوم')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'العلوم', 4, 0, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'الدراسات الاجتماعية')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'الدراسات الاجتماعية', 5, 0, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AcademicSubject WHERE Name = N'الحاسب الآلي')
BEGIN
    INSERT INTO dbo.AcademicSubject (Name, DisplayOrder, IsDeleted, CreatedAt)
    VALUES (N'الحاسب الآلي', 6, 0, GETDATE());
END;
GO
