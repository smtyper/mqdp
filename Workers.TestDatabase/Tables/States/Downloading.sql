CREATE TABLE [States].[Downloading]
(
    [RegistryId] NVARCHAR(40) NOT NULL,
    [FileName] NVARCHAR(40) NOT NULL,
    CONSTRAINT [PK_Downloading] PRIMARY KEY ([RegistryId], [FileName]),
    INDEX [IX_Key] ([RegistryId], [FileName])
)
