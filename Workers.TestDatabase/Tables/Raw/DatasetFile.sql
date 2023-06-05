CREATE TABLE [Raw].[DatasetFile]
(
    [RegistryId] NVARCHAR(40) NOT NULL,
    [FileName] NVARCHAR(40) NOT NULL,
    [ChangeDate] DATETIME2 NOT NULL,
    CONSTRAINT [PK_File] PRIMARY KEY ([RegistryId], [FileName]),
    INDEX [IX_RegistryId_FileName] ([RegistryId], [FileName]),
    INDEX [IX_ChangeDate] ([ChangeDate])
)
