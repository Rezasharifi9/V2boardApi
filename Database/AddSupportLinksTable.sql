-- =============================================================================
-- جدول لینک های ارتباطی پشتیبانی هر نماینده (tbSupportLinks)
--
-- این اسکریپت idempotent است و اجرای دوباره آن مشکلی ایجاد نمی کند.
-- روی دیتابیس SQL Server پروژه اجرا شود.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbSupportLinks' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.tbSupportLinks
    (
        tbSl_ID          int IDENTITY(1,1) NOT NULL,
        FK_User_ID       int NOT NULL,
        tbSl_Title       nvarchar(100) NOT NULL,
        tbSl_Link        nvarchar(500) NULL,
        tbSl_Phone       nvarchar(30) NULL,

        CONSTRAINT PK_tbSupportLinks PRIMARY KEY CLUSTERED (tbSl_ID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbSupportLinks_tbUsers')
BEGIN
    ALTER TABLE dbo.tbSupportLinks
        ADD CONSTRAINT FK_tbSupportLinks_tbUsers
        FOREIGN KEY (FK_User_ID) REFERENCES dbo.tbUsers (User_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbSupportLinks_User' AND object_id = OBJECT_ID(N'dbo.tbSupportLinks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbSupportLinks_User
        ON dbo.tbSupportLinks (FK_User_ID);
END
GO
