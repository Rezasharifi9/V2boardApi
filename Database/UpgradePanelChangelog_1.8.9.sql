-- =============================================================================
-- ارتقا به 1.8.9 — تنظیمات نماینده، هشدار سقف، حذف نماینده، بهبود رزرو/تمدید
-- پیش‌نیاز: 1.8.8 (یا هر نسخه قبلی از tbPanelChangelogVersions)
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.8')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.9')
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.8.9', '1405/04/12', 189, 1, 1);

    DECLARE @v189 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.8.9');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        -- ── Admin ──
        (@v189, N'[New] تنظیمات پیشرفته ربات نماینده', N'تنظیم درصد دعوت (InvitePercent) و امکان غیرفعال‌سازی فروش از طریق ربات در پروفایل نماینده.', 1, 10, 1),
        (@v189, N'[New] کنترل فروش پنل برای ادمین', N'امکان غیرفعال‌سازی فروش کل پنل از پروفایل ادمین (IsNotActiveSell).', 1, 20, 1),
        (@v189, N'[New] هشدار ۸۰٪ سقف اعتبار نماینده', N'ارسال خودکار پیام تلگرام به نماینده هنگام مصرف بیش از ۸۰٪ سقف بدهی/اعتبار اشتراک.', 1, 30, 1),
        (@v189, N'[New] مرتب‌سازی لیست نمایندگان', N'نمایش نمایندگانی که سقف را رد کرده‌اند در ابتدای لیست و نمایندگانی که بالای ۸۰٪ سقف هستند در ردیف بعد.', 1, 40, 1),
        (@v189, N'[New] حذف نماینده (فقط ادمین)', N'امکان حذف کامل نماینده پس از تأیید؛ فاکتورها، لاگ‌ها، کاربران تلگرام و داده‌های وابسته پنل پاک می‌شوند. اشتراک‌های سرور (Subscriptions) حفظ می‌شوند.', 1, 50, 1),
        (@v189, N'[Updated] لاگ و بازگشت وجه بسته رزرو', N'ثبت لاگ «رزرو بسته» فقط هنگام رزرو؛ حذف لاگ‌های مرتبط هنگام لغو رزرو و ثبت بازگشت وجه در تاریخچه نماینده.', 1, 60, 1),

        -- ── Agent ──
        (@v189, N'[New] تنظیمات درصد دعوت و فروش ربات', N'مدیریت InvitePercent و غیرفعال‌سازی فروش ربات از بخش تنظیمات ربات در پروفایل.', 2, 10, 1),
        (@v189, N'[New] هشدار ۸۰٪ سقف اعتبار', N'دریافت پیام تلگرام هنگام نزدیک شدن به سقف بدهی/اعتبار اشتراک (بالای ۸۰٪).', 2, 20, 1),
        (@v189, N'[Updated] تأیید قبل از تمدید بدون بسته فعال', N'اگر بسته فعالی روی اشتراک نباشد، قبل از فعال‌سازی مستقیم بسته تمدیدی از کاربر تأیید گرفته می‌شود.', 2, 30, 1),
        (@v189, N'[Updated] محدودیت حذف اشتراک', N'نماینده تا پایان بسته فعال امکان حذف اشتراک را ندارد.', 2, 40, 1),
        (@v189, N'[Updated] لاگ و بازگشت وجه بسته رزرو', N'بهبود ثبت لاگ رزرو بسته و نمایش بازگشت وجه هنگام لغو یا حذف بسته رزرو.', 2, 50, 1),
        (@v189, N'[Fixed] ارسال هشدار تلگرام', N'در صورت بلاک ربات یا خطای ارسال پیام هشدار، خطا در لاگ سیستم ثبت نمی‌شود و هشدار در ارسال بعدی تکرار می‌شود.', 2, 60, 1);
END
GO

-- فقط آخرین نسخه active باید current باشد
IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_IsActive = 1)
BEGIN
    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsActive = 1;

    UPDATE v SET tbPclv_IsCurrent = 1
    FROM dbo.tbPanelChangelogVersions v
    INNER JOIN (
        SELECT TOP 1 tbPclv_ID AS Id
        FROM dbo.tbPanelChangelogVersions
        WHERE tbPclv_IsActive = 1
        ORDER BY tbPclv_SortOrder DESC, tbPclv_ID DESC
    ) latest ON v.tbPclv_ID = latest.Id;
END
GO

-- پس از اجرا: App Pool را recycle کنید تا کش نسخه JS به‌روز شود.
