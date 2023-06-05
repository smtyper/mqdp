CREATE TABLE [States].[Prediction]
(
    [HashId] UNIQUEIDENTIFIER NOT NULL,
    [ChangeDate] DATETIME2 NOT NULL,
    [IsInProcessing] BIT NOT NULL,
    CONSTRAINT [PK_Prediction] PRIMARY KEY ([HashId]),
    INDEX [IX_Key] ([HashId]),
    INDEX [IX_Status] ([IsInProcessing])
)
