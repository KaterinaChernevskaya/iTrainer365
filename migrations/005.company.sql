CREATE TABLE [dbo].[Companies]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ItemListID] INT NULL DEFAULT NULL,
    [ItemListTitle] NVARCHAR(50) NULL DEFAULT NULL,
    [ItemListTenantId] NVARCHAR(50) NULL DEFAULT NULL,
    [CreatedAt] DATETIME NULL DEFAULT(getdate())
)

ALTER TABLE [dbo].[Users] ADD [Company] INT NULL DEFAULT NULL CONSTRAINT [FK_Users_ToCompanies] FOREIGN KEY ([Company]) REFERENCES [Companies]([Id]);
