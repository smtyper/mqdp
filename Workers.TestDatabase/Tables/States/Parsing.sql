CREATE TABLE [States].[Parsing]
(
    [RegistryId] NVARCHAR(40) NOT NULL,
    [FileName] NVARCHAR(40) NOT NULL,
    [ChangeDate] DATETIME2 NOT NULL,
    [IsInProcessing] BIT NOT NULL,
    CONSTRAINT [PK_Parsing] PRIMARY KEY ([RegistryId], [FileName]),
    INDEX [IX_Key] ([RegistryId], [FileName]),
    INDEX [IX_Status] ([IsInProcessing])
)
