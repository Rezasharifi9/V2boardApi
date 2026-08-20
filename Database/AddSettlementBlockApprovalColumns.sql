-- =============================================================================
-- ستون‌های تاییدیه مسدودسازی تسویه نماینده (اجرا روی SQL Server پروژه)
-- پیش‌نیاز: Database/AddSettlementColumns.sql
--
-- Settlement_BlockApprovalPending   = درخواست تایید مسدودسازی برای ادمین ارسال شده و منتظر تصمیم است
-- Settlement_LastBlockApprovalSent  = آخرین باری که درخواست تایید به ادمین ارسال شد (برای یادآوری هر ۲ روز)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_BlockApprovalPending')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_BlockApprovalPending bit NOT NULL CONSTRAINT DF_tbUsers_Settlement_BlockApprovalPending DEFAULT 0;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbUsers') AND name = 'Settlement_LastBlockApprovalSent')
BEGIN
    ALTER TABLE tbUsers ADD Settlement_LastBlockApprovalSent datetime NULL;
END
GO
