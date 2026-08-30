-- =============================================================================
-- اتصال اشتراک‌ها (tbLinks) به نماینده، دستگاه موبایل و کاربر ربات
--
-- FK_User_ID          نماینده صاحب اشتراک (از بخش اشتراک‌های پنل پر می‌شود)
-- FK_MobileUser_ID    دستگاه اپ — وقتی اشتراک به اپ اضافه شد
-- FK_TelegramUserID   کاربر ربات — وقتی اشتراک به ربات اضافه شد
--
-- این اسکریپت idempotent است.
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbLinks') AND name = 'FK_User_ID')
BEGIN
    ALTER TABLE dbo.tbLinks ADD FK_User_ID int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbLinks_tbUsers')
BEGIN
    ALTER TABLE dbo.tbLinks
        ADD CONSTRAINT FK_tbLinks_tbUsers
        FOREIGN KEY (FK_User_ID) REFERENCES dbo.tbUsers (User_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbLinks_User' AND object_id = OBJECT_ID(N'dbo.tbLinks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbLinks_User ON dbo.tbLinks (FK_User_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbLinks') AND name = 'FK_MobileUser_ID')
BEGIN
    ALTER TABLE dbo.tbLinks ADD FK_MobileUser_ID int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbLinks_tbMobileUsers')
BEGIN
    ALTER TABLE dbo.tbLinks
        ADD CONSTRAINT FK_tbLinks_tbMobileUsers
        FOREIGN KEY (FK_MobileUser_ID) REFERENCES dbo.tbMobileUsers (tbMu_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbLinks_MobileUser' AND object_id = OBJECT_ID(N'dbo.tbLinks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbLinks_MobileUser ON dbo.tbLinks (FK_MobileUser_ID);
END
GO
