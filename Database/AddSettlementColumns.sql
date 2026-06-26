-- اجرا روی دیتابیس SQL Server پروژه (قبل از استفاده از قابلیت تسویه)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_Enabled')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_Enabled bit NOT NULL CONSTRAINT DF_tbUsers_Settlement_Enabled DEFAULT 0;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_Type')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_Type varchar(20) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_StartDate')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_StartDate datetime NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_DayOfWeek')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_DayOfWeek int NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_DayOfMonth')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_DayOfMonth int NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_LastPreWarning')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_LastPreWarning datetime NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_LastOverdueWarning')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_LastOverdueWarning datetime NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_IsBlocked')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_IsBlocked bit NOT NULL CONSTRAINT DF_tbUsers_Settlement_IsBlocked DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_BlockGraceDays')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_BlockGraceDays int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_LastDueDayWarning')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_LastDueDayWarning datetime NULL;
END
GO

-- Settlement_DayOfMonth = مهلت تسویه (روز پس از آخرین پرداخت)
-- Settlement_DayOfWeek = هشدار قبل از موعد (روز)
-- Settlement_BlockGraceDays = مهلت مسدودسازی پس از موعد (روز)
GO
