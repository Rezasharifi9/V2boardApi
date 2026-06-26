-- هشدارهای بسته رزرو در ربات (حجم ۲/۱ گیگ و زمان ۲/۱ روز)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'tbLinks') AND name = 'tbL_ReserveWarnMask')
BEGIN
    ALTER TABLE tbLinks ADD tbL_ReserveWarnMask int NOT NULL CONSTRAINT DF_tbLinks_ReserveWarnMask DEFAULT 0;
END
GO
