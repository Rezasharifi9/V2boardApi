-- =============================================================================
-- جدول نسخه و لینک دانلود اپلیکیشن (tbAppRelease)
--
-- یک ردیف برای کل پنل: لینک دانلود، نسخه نمایشی، شماره بیلد، متن تغییرات، نصب اجباری.
-- این اسکریپت idempotent است و اجرای دوباره آن مشکلی ایجاد نمی کند.
-- روی دیتابیس SQL Server پروژه اجرا شود.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbAppRelease' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbAppRelease
    (
        tbAr_ID            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        tbAr_DownloadUrl   NVARCHAR(500) NULL,
        tbAr_Version       NVARCHAR(30) NULL,
        tbAr_VersionCode   INT NULL,
        tbAr_Changelog     NVARCHAR(MAX) NULL,   -- JSON آرایه رشته‌ها: ["تغییر ۱","تغییر ۲"]
        tbAr_ForceInstall  BIT NOT NULL CONSTRAINT DF_tbAppRelease_ForceInstall DEFAULT (0),
        tbAr_UpdatedAt     DATETIME NOT NULL CONSTRAINT DF_tbAppRelease_UpdatedAt DEFAULT (GETDATE())
    );
END
GO
