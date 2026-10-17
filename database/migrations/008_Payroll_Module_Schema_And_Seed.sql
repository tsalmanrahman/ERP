-- ==============================================================================
-- 008_Payroll_Module_Schema_And_Seed.sql
-- Modern ERP Human Resources & Payroll Schema Migration
-- ==============================================================================

USE ERP;
GO

-- 1. Enhance hr.Employees with salary component breakdown and banking details
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'BasicSalary')
BEGIN
    ALTER TABLE hr.Employees ADD BasicSalary DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'HouseRent')
BEGIN
    ALTER TABLE hr.Employees ADD HouseRent DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'MedicalAllowance')
BEGIN
    ALTER TABLE hr.Employees ADD MedicalAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'ConveyanceAllowance')
BEGIN
    ALTER TABLE hr.Employees ADD ConveyanceAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'OtherAllowance')
BEGIN
    ALTER TABLE hr.Employees ADD OtherAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'ProvidentFundDeduction')
BEGIN
    ALTER TABLE hr.Employees ADD ProvidentFundDeduction DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'TaxDeduction')
BEGIN
    ALTER TABLE hr.Employees ADD TaxDeduction DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'BankName')
BEGIN
    ALTER TABLE hr.Employees ADD BankName NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'hr.Employees') AND name = 'BankAccountNumber')
BEGIN
    ALTER TABLE hr.Employees ADD BankAccountNumber NVARCHAR(50) NULL;
END;
GO

-- 2. Create hr.AttendanceLogs for raw device punch captures
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.AttendanceLogs'))
BEGIN
    CREATE TABLE hr.AttendanceLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_AttendanceLogs_Company REFERENCES sa.Companies(Id),
        EmployeeCode NVARCHAR(30) NOT NULL,
        PunchTime DATETIME2 NOT NULL,
        DeviceId NVARCHAR(50) NOT NULL DEFAULT 'TERMINAL-01',
        InOutMode NVARCHAR(10) NOT NULL DEFAULT 'PUNCH',
        IsProcessed BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE NONCLUSTERED INDEX IX_hr_AttendanceLogs_Company_Code_Time 
    ON hr.AttendanceLogs(CompanyId, EmployeeCode, PunchTime);
END;
GO

-- 3. Create hr.DailyAttendance for reconciled daily records
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.DailyAttendance'))
BEGIN
    CREATE TABLE hr.DailyAttendance (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_DailyAttendance_Company REFERENCES sa.Companies(Id),
        EmployeeId INT NOT NULL CONSTRAINT FK_hr_DailyAttendance_Employee REFERENCES hr.Employees(Id),
        AttendanceDate DATE NOT NULL,
        InTime TIME(0) NULL,
        OutTime TIME(0) NULL,
        WorkHours DECIMAL(5, 2) NOT NULL DEFAULT 0,
        OvertimeHours DECIMAL(5, 2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Present',
        Remarks NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_DailyAttendance_Date UNIQUE (CompanyId, EmployeeId, AttendanceDate)
    );
END;
GO

-- 4. Create hr.PayrollPeriods
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.PayrollPeriods'))
BEGIN
    CREATE TABLE hr.PayrollPeriods (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_PayrollPeriods_Company REFERENCES sa.Companies(Id),
        PeriodName NVARCHAR(50) NOT NULL,
        Year INT NOT NULL,
        Month INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        WorkingDays INT NOT NULL DEFAULT 26,
        IsClosed BIT NOT NULL DEFAULT 0,
        ClosedAtUtc DATETIME2 NULL,
        ClosedBy NVARCHAR(50) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_PayrollPeriods_Month UNIQUE (CompanyId, Year, Month)
    );
END;
GO

-- 5. Create hr.SalarySheets
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.SalarySheets'))
BEGIN
    CREATE TABLE hr.SalarySheets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_SalarySheets_Company REFERENCES sa.Companies(Id),
        PayrollPeriodId INT NOT NULL CONSTRAINT FK_hr_SalarySheets_Period REFERENCES hr.PayrollPeriods(Id),
        SheetNumber NVARCHAR(30) NOT NULL,
        ProcessingDate DATE NOT NULL,
        TotalGrossSalary DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalOvertimeAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalDeductions DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalNetSalary DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
        AccrualJournalVoucherId INT NULL CONSTRAINT FK_hr_SalarySheets_AccrualVoucher REFERENCES gl.JournalVouchers(Id),
        DisbursementJournalVoucherId INT NULL CONSTRAINT FK_hr_SalarySheets_DisbursementVoucher REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_SalarySheets_Period UNIQUE (CompanyId, PayrollPeriodId)
    );
END;
GO

-- 6. Create hr.SalarySheetLines
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.SalarySheetLines'))
BEGIN
    CREATE TABLE hr.SalarySheetLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SalarySheetId INT NOT NULL CONSTRAINT FK_hr_SalarySheetLines_Sheet REFERENCES hr.SalarySheets(Id) ON DELETE CASCADE,
        EmployeeId INT NOT NULL CONSTRAINT FK_hr_SalarySheetLines_Employee REFERENCES hr.Employees(Id),
        BasicSalary DECIMAL(18, 4) NOT NULL DEFAULT 0,
        HouseRent DECIMAL(18, 4) NOT NULL DEFAULT 0,
        MedicalAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        ConveyanceAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        OtherAllowance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        GrossSalary DECIMAL(18, 4) NOT NULL DEFAULT 0,
        WorkingDays INT NOT NULL DEFAULT 26,
        PresentDays INT NOT NULL DEFAULT 0,
        LateDays INT NOT NULL DEFAULT 0,
        AbsentDays INT NOT NULL DEFAULT 0,
        OvertimeHours DECIMAL(6, 2) NOT NULL DEFAULT 0,
        OvertimeRate DECIMAL(18, 4) NOT NULL DEFAULT 0,
        OvertimeAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        AbsentDeduction DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TaxDeduction DECIMAL(18, 4) NOT NULL DEFAULT 0,
        ProvidentFundDeduction DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalDeductions DECIMAL(18, 4) NOT NULL DEFAULT 0,
        NetSalaryPayable DECIMAL(18, 4) NOT NULL DEFAULT 0,
        PaymentMethod NVARCHAR(20) NOT NULL DEFAULT 'BankTransfer',
        BankName NVARCHAR(100) NULL,
        BankAccountNumber NVARCHAR(50) NULL,
        Remarks NVARCHAR(250) NULL
    );

    CREATE NONCLUSTERED INDEX IX_hr_SalarySheetLines_Sheet 
    ON hr.SalarySheetLines(SalarySheetId);
END;
GO

-- 7. Seed Initial Departments, Designations & Employees for Company 1
DECLARE @CompanyId INT = 1;

IF NOT EXISTS (SELECT 1 FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'HR')
BEGIN
    INSERT INTO hr.Departments (CompanyId, DepartmentCode, DepartmentName, IsActive, CreatedBy)
    VALUES (@CompanyId, 'HR', 'Human Resources', 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'ENG')
BEGIN
    INSERT INTO hr.Departments (CompanyId, DepartmentCode, DepartmentName, IsActive, CreatedBy)
    VALUES (@CompanyId, 'ENG', 'Software Engineering', 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'OPS')
BEGIN
    INSERT INTO hr.Departments (CompanyId, DepartmentCode, DepartmentName, IsActive, CreatedBy)
    VALUES (@CompanyId, 'OPS', 'Operations & Logistics', 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'HR-MGR')
BEGIN
    INSERT INTO hr.Designations (CompanyId, DesignationCode, DesignationTitle, IsActive, CreatedBy)
    VALUES (@CompanyId, 'HR-MGR', 'HR Manager', 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'SR-ENG')
BEGIN
    INSERT INTO hr.Designations (CompanyId, DesignationCode, DesignationTitle, IsActive, CreatedBy)
    VALUES (@CompanyId, 'SR-ENG', 'Senior Software Engineer', 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'WH-SUP')
BEGIN
    INSERT INTO hr.Designations (CompanyId, DesignationCode, DesignationTitle, IsActive, CreatedBy)
    VALUES (@CompanyId, 'WH-SUP', 'Warehouse Supervisor', 1, 'System');
END;

-- Seed Employees
DECLARE @EngDeptId INT = (SELECT Id FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'ENG');
DECLARE @HrDeptId INT = (SELECT Id FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'HR');
DECLARE @OpsDeptId INT = (SELECT Id FROM hr.Departments WHERE CompanyId = @CompanyId AND DepartmentCode = 'OPS');

DECLARE @EngDesigId INT = (SELECT Id FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'SR-ENG');
DECLARE @HrDesigId INT = (SELECT Id FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'HR-MGR');
DECLARE @OpsDesigId INT = (SELECT Id FROM hr.Designations WHERE CompanyId = @CompanyId AND DesignationCode = 'WH-SUP');

IF NOT EXISTS (SELECT 1 FROM hr.Employees WHERE CompanyId = @CompanyId AND EmployeeCode = 'EMP-001')
BEGIN
    INSERT INTO hr.Employees (
        CompanyId, EmployeeCode, FirstName, LastName, DepartmentId, DesignationId,
        DateOfBirth, JoiningDate, Email, Phone, GrossSalary, BasicSalary, HouseRent,
        MedicalAllowance, ConveyanceAllowance, OtherAllowance, ProvidentFundDeduction,
        TaxDeduction, BankName, BankAccountNumber, IsActive, CreatedBy
    )
    VALUES (
        @CompanyId, 'EMP-001', 'Arif', 'Rahman', @EngDeptId, @EngDesigId,
        '1990-05-15', '2022-01-01', 'arif.rahman@erp.local', '+8801711000001',
        120000.00, 60000.00, 30000.00, 15000.00, 15000.00, 0.00, 3000.00,
        5000.00, 'Prime Bank Ltd.', '110203001-EMP001', 1, 'System'
    );
END;

IF NOT EXISTS (SELECT 1 FROM hr.Employees WHERE CompanyId = @CompanyId AND EmployeeCode = 'EMP-002')
BEGIN
    INSERT INTO hr.Employees (
        CompanyId, EmployeeCode, FirstName, LastName, DepartmentId, DesignationId,
        DateOfBirth, JoiningDate, Email, Phone, GrossSalary, BasicSalary, HouseRent,
        MedicalAllowance, ConveyanceAllowance, OtherAllowance, ProvidentFundDeduction,
        TaxDeduction, BankName, BankAccountNumber, IsActive, CreatedBy
    )
    VALUES (
        @CompanyId, 'EMP-002', 'Farzana', 'Akter', @HrDeptId, @HrDesigId,
        '1993-08-20', '2022-06-01', 'farzana.akter@erp.local', '+8801711000002',
        80000.00, 40000.00, 20000.00, 10000.00, 10000.00, 0.00, 2000.00,
        2500.00, 'Prime Bank Ltd.', '110203001-EMP002', 1, 'System'
    );
END;

IF NOT EXISTS (SELECT 1 FROM hr.Employees WHERE CompanyId = @CompanyId AND EmployeeCode = 'EMP-003')
BEGIN
    INSERT INTO hr.Employees (
        CompanyId, EmployeeCode, FirstName, LastName, DepartmentId, DesignationId,
        DateOfBirth, JoiningDate, Email, Phone, GrossSalary, BasicSalary, HouseRent,
        MedicalAllowance, ConveyanceAllowance, OtherAllowance, ProvidentFundDeduction,
        TaxDeduction, BankName, BankAccountNumber, IsActive, CreatedBy
    )
    VALUES (
        @CompanyId, 'EMP-003', 'Tanvir', 'Hossain', @OpsDeptId, @OpsDesigId,
        '1995-12-10', '2023-03-15', 'tanvir.hossain@erp.local', '+8801711000003',
        45000.00, 25000.00, 12000.00, 5000.00, 3000.00, 0.00, 1500.00,
        0.00, 'Prime Bank Ltd.', '110203001-EMP003', 1, 'System'
    );
END;
GO
