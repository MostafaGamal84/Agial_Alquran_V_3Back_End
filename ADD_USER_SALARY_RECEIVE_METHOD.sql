IF COL_LENGTH('dbo.[User]', 'SalaryReceiveMethodId') IS NULL
BEGIN
    ALTER TABLE dbo.[User]
    ADD SalaryReceiveMethodId INT NULL;
END;
