-- =============================================================================
-- جدول کاربران موبایل (tbMobileUsers) + اتصال فاکتورها و سفارشات به دستگاه
--
-- این اسکریپت idempotent است و اجرای دوباره آن مشکلی ایجاد نمی کند.
-- روی دیتابیس SQL Server پروژه اجرا شود.
-- =============================================================================

-- ─────────────────────────────────────────────────────────────────────────────
-- ۱) روش پرداخت «اپلیکیشن» با شناسه ۵
--    tbDepositWallet_Log.FK_PayMethod_ID = 5 یعنی فاکتور از داخل اپلیکیشن ساخته شده
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.tbPaymentMethods WHERE tbpm_ID = 5)
BEGIN
    SET IDENTITY_INSERT dbo.tbPaymentMethods ON;

    INSERT INTO dbo.tbPaymentMethods (tbpm_ID, tbpm_MethodName, tbpm_Key)
    VALUES (5, N'اپلیکیشن', 'APP');

    SET IDENTITY_INSERT dbo.tbPaymentMethods OFF;
END
ELSE IF EXISTS (SELECT 1 FROM dbo.tbPaymentMethods WHERE tbpm_ID = 5 AND tbpm_Key <> 'APP')
BEGIN
    -- شناسه ۵ قبلا برای روش دیگری استفاده شده — بدون بررسی دستی چیزی تغییر داده نمی شود
    PRINT N'هشدار : tbPaymentMethods با شناسه 5 از قبل وجود دارد ولی tbpm_Key آن APP نیست. قبل از ادامه بررسی کنید.';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- ۲) جدول کاربران موبایل
--    هر رکورد یک نصب اپلیکیشن روی یک گوشی است و به یک نماینده تخصیص داده می شود.
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbMobileUsers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.tbMobileUsers
    (
        tbMu_ID                  int IDENTITY(1,1) NOT NULL,

        -- نماینده ای که این دستگاه زیرمجموعه اوست (از روی توکن نماینده در زمان ثبت)
        FK_User_ID               int NULL,

        -- شناسه یکتای دستگاه — کلید تشخیص تکراری بودن نصب
        tbMu_AndroidId           varchar(64) NOT NULL,

        -- اطلاعات لازم برای ارسال Push Notification
        tbMu_FirebaseToken       varchar(500) NULL,
        tbMu_NotificationEnabled bit NOT NULL CONSTRAINT DF_tbMobileUsers_NotifEnabled DEFAULT 0,

        -- مشخصات سخت افزاری
        tbMu_Manufacturer        nvarchar(100) NULL,
        tbMu_Model               nvarchar(100) NULL,
        tbMu_Device              nvarchar(100) NULL,
        tbMu_Product             nvarchar(100) NULL,
        tbMu_Hardware            nvarchar(100) NULL,

        -- مشخصات سیستم عامل و برنامه
        tbMu_AndroidVersion      varchar(20) NULL,
        tbMu_Sdk                 int NULL,
        tbMu_AppVersion          varchar(30) NULL,
        tbMu_VersionCode         int NULL,
        tbMu_PackageName         varchar(150) NULL,

        -- محلی سازی (برای ارسال نوتیفیکیشن در ساعت درست و به زبان درست)
        tbMu_Language            varchar(10) NULL,
        tbMu_Country             varchar(10) NULL,
        tbMu_Timezone            varchar(60) NULL,

        -- نمایشگر
        tbMu_ScreenWidth         int NULL,
        tbMu_ScreenHeight        int NULL,
        tbMu_Density             int NULL,

        tbMu_Rooted              bit NOT NULL CONSTRAINT DF_tbMobileUsers_Rooted DEFAULT 0,

        -- ردیابی
        tbMu_RegisterDate        datetime NULL,
        tbMu_LastSeenDate        datetime NULL,
        tbMu_LastIp              varchar(64) NULL,
        tbMu_IsActive            bit NOT NULL CONSTRAINT DF_tbMobileUsers_IsActive DEFAULT 1,

        CONSTRAINT PK_tbMobileUsers PRIMARY KEY CLUSTERED (tbMu_ID)
    );
END
GO

-- کلید خارجی به نماینده
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbMobileUsers_tbUsers')
BEGIN
    ALTER TABLE dbo.tbMobileUsers
        ADD CONSTRAINT FK_tbMobileUsers_tbUsers
        FOREIGN KEY (FK_User_ID) REFERENCES dbo.tbUsers (User_ID);
END
GO

-- هر androidId فقط یک رکورد — تشخیص نصب مجدد و جلوگیری از رکورد تکراری
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_tbMobileUsers_AndroidId' AND object_id = OBJECT_ID(N'dbo.tbMobileUsers'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_tbMobileUsers_AndroidId
        ON dbo.tbMobileUsers (tbMu_AndroidId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbMobileUsers_User' AND object_id = OBJECT_ID(N'dbo.tbMobileUsers'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbMobileUsers_User
        ON dbo.tbMobileUsers (FK_User_ID) INCLUDE (tbMu_LastSeenDate, tbMu_IsActive);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- ۳) اتصال سفارشات به دستگاه
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbOrders') AND name = 'FK_MobileUser_ID')
BEGIN
    ALTER TABLE dbo.tbOrders ADD FK_MobileUser_ID int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbOrders_tbMobileUsers')
BEGIN
    ALTER TABLE dbo.tbOrders
        ADD CONSTRAINT FK_tbOrders_tbMobileUsers
        FOREIGN KEY (FK_MobileUser_ID) REFERENCES dbo.tbMobileUsers (tbMu_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbOrders_MobileUser' AND object_id = OBJECT_ID(N'dbo.tbOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbOrders_MobileUser ON dbo.tbOrders (FK_MobileUser_ID);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- ۴) اتصال فاکتورها به دستگاه
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tbDepositWallet_Log') AND name = 'FK_MobileUser_ID')
BEGIN
    ALTER TABLE dbo.tbDepositWallet_Log ADD FK_MobileUser_ID int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tbDepositWallet_Log_tbMobileUsers')
BEGIN
    ALTER TABLE dbo.tbDepositWallet_Log
        ADD CONSTRAINT FK_tbDepositWallet_Log_tbMobileUsers
        FOREIGN KEY (FK_MobileUser_ID) REFERENCES dbo.tbMobileUsers (tbMu_ID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbDepositWallet_Log_MobileUser' AND object_id = OBJECT_ID(N'dbo.tbDepositWallet_Log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbDepositWallet_Log_MobileUser ON dbo.tbDepositWallet_Log (FK_MobileUser_ID);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- ۵) ایندکس های کمکی مسیر فاکتور اپلیکیشن
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbDepositWallet_Log_TaxId' AND object_id = OBJECT_ID(N'dbo.tbDepositWallet_Log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbDepositWallet_Log_TaxId ON dbo.tbDepositWallet_Log (dw_TaxId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tbDepositWallet_Log_Status_Price' AND object_id = OBJECT_ID(N'dbo.tbDepositWallet_Log'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbDepositWallet_Log_Status_Price ON dbo.tbDepositWallet_Log (dw_Status, dw_Price);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- ۶) فاکتورهای قدیمی API که با dw_PayMethod = 'ApiCard' ساخته شده بودند
--    قبلا FK_PayMethod_ID نداشتند و در پنل دیده نمی شدند.
-- ─────────────────────────────────────────────────────────────────────────────
UPDATE dbo.tbDepositWallet_Log
SET FK_PayMethod_ID = 5
WHERE dw_PayMethod = 'ApiCard' AND FK_PayMethod_ID IS NULL;
GO
