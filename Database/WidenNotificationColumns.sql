-- بزرگ‌کردن ستون‌های اعلان پنل تا متن پیام تلگرام کامل ذخیره شود
-- tbNoti_Title: nvarchar(30)  -> nvarchar(200)
-- tbNoti_Text:  nvarchar(200) -> nvarchar(max)
--
-- این اسکریپت را قبل از انتشار بیلدی که اعلان پنل را از روی پیام تلگرام می‌سازد اجرا کنید.

IF COL_LENGTH('dbo.tbNotifications', 'tbNoti_Title') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.tbNotifications')
          AND name = 'tbNoti_Title'
          AND max_length = 60 -- nvarchar(30) = 30 * 2 bytes
   )
BEGIN
    ALTER TABLE dbo.tbNotifications ALTER COLUMN tbNoti_Title nvarchar(200) NOT NULL;
END
GO

IF COL_LENGTH('dbo.tbNotifications', 'tbNoti_Text') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.tbNotifications')
          AND name = 'tbNoti_Text'
          AND max_length <> -1 -- -1 = nvarchar(max)
   )
BEGIN
    ALTER TABLE dbo.tbNotifications ALTER COLUMN tbNoti_Text nvarchar(max) NOT NULL;
END
GO
