-- =============================================================================
-- ارتقا به 1.9.2 (1004) — ذخیره فاکتور نمایندگان به ریال و نمایش به تومان
-- پیش‌نیاز: 1.9.1 (1003)
--
-- قالب نسخه‌بندی: MAJOR.MINOR.PATCH (BUILD)
--   1.9.2 = نسخه معنایی (SemVer)   |   (1004) = شماره بیلد یکنواخت افزایشی
--
-- این اسکریپت را فقط یک‌بار اجرا کنید (ترجیحاً هم‌زمان با انتشار بیلد جدید،
-- قبل از ثبت فاکتور جدید از پنل). فاکتورهای تسویه/پرداخت خودکار از قبل ریالی
-- بودند و دست نخورده می‌مانند.
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.1 (1003)')
   AND NOT EXISTS (SELECT 1 FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.2 (1004)')
BEGIN
    -- تبدیل فاکتورهای قدیمی تومانی به ریال (×۱۰)
    -- رد می‌شود:
    --   status = 1           فاکتور در انتظار پرداخت خودکار (از قبل ریال)
    --   توضیح تسویه خودکار   فاکتور تسویه (از قبل ریال)
    --   status = 3 و سه رقم آخر ≠ ۰  پرداخت‌شدهٔ خودکار CreateFactoryForPay (از قبل ریال)
    UPDATE dbo.tbUserFactors
    SET tbUf_Value = tbUf_Value * 10
    WHERE tbUf_Value IS NOT NULL
      AND tbUf_Value <> 0
      AND ISNULL(tbUf_Status, 0) <> 1
      AND (tbUf_Description IS NULL OR tbUf_Description <> N'فاکتور خودکار تسویه بدهی')
      AND NOT (
            tbUf_Status = 3
            AND CAST(ROUND(tbUf_Value, 0) AS BIGINT) % 1000 <> 0
          );

    UPDATE dbo.tbPanelChangelogVersions SET tbPclv_IsCurrent = 0 WHERE tbPclv_IsCurrent = 1;

    INSERT INTO dbo.tbPanelChangelogVersions
        (tbPclv_Version, tbPclv_ReleaseDate, tbPclv_SortOrder, tbPclv_IsCurrent, tbPclv_IsActive)
    VALUES
        ('1.9.2 (1004)', '1405/05/29', 194, 1, 1);

    DECLARE @v1004 INT = (SELECT tbPclv_ID FROM dbo.tbPanelChangelogVersions WHERE tbPclv_Version = '1.9.2 (1004)');

    INSERT INTO dbo.tbPanelChangelogItems
        (FK_Version_ID, tbPcli_Title, tbPcli_Description, tbPcli_Audience, tbPcli_SortOrder, tbPcli_IsActive)
    VALUES
        -- ── ادمین ──
        (@v1004, N'[Changed] ذخیره فاکتور نمایندگان به ریال', N'از این نسخه مبلغ فاکتور نماینده در پایگاه داده به ریال ثبت می‌شود. در فرم افزودن فاکتور همچنان مبلغ را به تومان وارد کنید.', 1, 10, 1),
        (@v1004, N'[Changed] نمایش مبالغ فاکتور به تومان', N'جدول فاکتورها، جمع فاکتورها، تاریخچه نماینده و داشبورد مبالغ ذخیره‌شده را به تومان نشان می‌دهند.', 1, 20, 1),
        -- ── نماینده ──
        (@v1004, N'[Changed] نمایش مبالغ فاکتور به تومان', N'مبالغ فاکتور در لیست و صفحات پنل فقط به تومان نمایش داده می‌شوند.', 2, 10, 1);
END
GO

-- تعمیر tbPclv_IsCurrent — فقط آخرین نسخه فعال باید current باشد
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

-- پس از اجرا: App Pool را recycle کنید تا کش نسخه و کش JS به‌روز شود.
