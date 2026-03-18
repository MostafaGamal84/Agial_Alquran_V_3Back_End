BEGIN TRY
    BEGIN TRAN;

    ALTER TABLE [dbo].[StudentPayment]
    ALTER COLUMN [Amount] DECIMAL(18,2) NULL;

    ALTER TABLE [dbo].[StudentSubscribeHistory]
    ALTER COLUMN [OldAmount] DECIMAL(18,2) NULL;

    ALTER TABLE [dbo].[StudentSubscribeHistory]
    ALTER COLUMN [NewAmount] DECIMAL(18,2) NULL;

    ALTER TABLE [dbo].[StudentSubscribeHistory]
    ALTER COLUMN [AmountPaidBeforeChange] DECIMAL(18,2) NULL;

    ALTER TABLE [dbo].[StudentSubscribeHistory]
    ALTER COLUMN [RemainingAmountAfterChange] DECIMAL(18,2) NULL;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK;

    THROW;
END CATCH;
