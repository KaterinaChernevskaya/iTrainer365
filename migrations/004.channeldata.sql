CREATE TABLE [dbo].[ChannelData]
(
    [User] INT NOT NULL DEFAULT NULL, 
    [ChannelDataKey] VARCHAR(50) NOT NULL DEFAULT NULL, 
    [ChannelDataValue] NVARCHAR(255) NULL DEFAULT NULL, 
    CONSTRAINT [PK_ChannelData] PRIMARY KEY ([User], [ChannelDataKey]),
    CONSTRAINT [FK_ChannelData_ToUsers] FOREIGN KEY ([User]) REFERENCES [Users]([Id])
)