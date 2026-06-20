-- اجرا روی دیتابیس SQL Server پروژه (قبل از استفاده از تغییرات نسخه پنل)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbPanelChangelogVersions' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbPanelChangelogVersions
    (
        tbPclv_ID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        tbPclv_Version     VARCHAR(20) NOT NULL,
        tbPclv_ReleaseDate VARCHAR(20) NOT NULL,
        tbPclv_SortOrder   INT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_SortOrder DEFAULT (0),
        tbPclv_IsCurrent   BIT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_IsCurrent DEFAULT (0),
        tbPclv_IsActive    BIT NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_IsActive DEFAULT (1),
        tbPclv_CreatedAt   DATETIME NOT NULL CONSTRAINT DF_tbPanelChangelogVersions_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tbPanelChangelogItems' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.tbPanelChangelogItems
    (
        tbPcli_ID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FK_Version_ID      INT NOT NULL,
        tbPcli_Title       NVARCHAR(200) NOT NULL,
        tbPcli_Description NVARCHAR(1000) NOT NULL,
        tbPcli_Audience    TINYINT NOT NULL, -- 1=Admin, 2=Agent
        tbPcli_SortOrder   INT NOT NULL CONSTRAINT DF_tbPanelChangelogItems_SortOrder DEFAULT (0),
        tbPcli_IsActive    BIT NOT NULL CONSTRAINT DF_tbPanelChangelogItems_IsActive DEFAULT (1),
        CONSTRAINT FK_tbPanelChangelogItems_tbPanelChangelogVersions
            FOREIGN KEY (FK_Version_ID) REFERENCES dbo.tbPanelChangelogVersions(tbPclv_ID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tbPanelChangelogItems_FK_Version_ID')
BEGIN
    CREATE INDEX IX_tbPanelChangelogItems_FK_Version_ID
        ON dbo.tbPanelChangelogItems(FK_Version_ID, tbPcli_SortOrder);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions)
BEGIN
    SET IDENTITY_INSERT dbo.tbPanelChangelogVersions ON;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_ID, tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        (1, '1.7.0', '1404/03/30', 170, 1, 1),
        (2, '1.6.0', '1404/03/15', 160, 0, 1);

    SET IDENTITY_INSERT dbo.tbPanelChangelogVersions OFF;

    SET IDENTITY_INSERT dbo.tbPanelChangelogItems ON;

    INSERT INTO dbo.tbPanelChangelogItems
        (tbPcli_ID, FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        (1, 1, N'تنظیمات تسویه بدهی نمایندگان', N'تب تسویه در جزئیات نماینده برای فعال‌سازی تسویه هفتگی/ماهانه، هشدار تلگرام و مسدودسازی خودکار در صورت عدم پرداخت.', 1, 10, 1),
        (2, 1, N'مدیریت فاکتورها', N'بخش پرداخت‌ها به فاکتورها تغییر نام داد. ثبت فاکتور با نمایش بدهی نماینده، پر شدن خودکار مبلغ و تاریخ فعلی.', 1, 20, 1),
        (3, 1, N'تائید فاکتور از لیست', N'امکان تائید فاکتورهای در انتظار پرداخت مستقیماً از جدول فاکتورها.', 1, 30, 1),
        (4, 1, N'محدودیت ویرایش اشتراک', N'ویرایش اشتراک و آیکون مداد فقط برای ادمین (نقش ۱) در دسترس است.', 1, 40, 1),
        (5, 1, N'فاکتورهای من', N'صفحه فاکتورها برای پیگیری و مشاهده وضعیت فاکتورهای ثبت‌شده توسط نماینده.', 2, 50, 1),
        (6, 1, N'تمدید و رزرو بسته اشتراک', N'امکان رزرو بسته جدید از منوی تمدید اشتراک؛ بسته در زمان مناسب به‌صورت خودکار فعال می‌شود.', 2, 60, 1),
        (7, 1, N'تاریخچه مصرف پیشرفته', N'نمایش تاریخچه مصرف به‌صورت جدول با جستجوی بازه تاریخ، نمایش مجموع مصرف و خروجی PDF.', 2, 70, 1),
        (8, 1, N'ادامه ورود به پنل', N'امکان ادامه ورود به پنل تا ۷ روز بدون وارد کردن مجدد نام کاربری و رمز.', 2, 80, 1),
        (9, 2, N'اعلان تلگرام هنگام ثبت فاکتور', N'ارسال پیام تلگرام به نماینده هنگام ثبت فاکتور با کسر از بدهی یا مبلغ بالاتر از بدهی.', 1, 10, 1),
        (10, 2, N'بهبود تجربه لیست اشتراک‌ها', N'بهبود منوی عملیات اشتراک‌ها شامل QR Code، کپی لینک و تغییر نام اشتراک.', 2, 20, 1);

    SET IDENTITY_INSERT dbo.tbPanelChangelogItems OFF;
END
GO
