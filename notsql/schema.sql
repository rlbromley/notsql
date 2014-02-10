CREATE TABLE [dbo].[docs] (
    [_id]       UNIQUEIDENTIFIER NOT NULL,
    [_rev]      UNIQUEIDENTIFIER NOT NULL,
    [tablename] NVARCHAR (100)   NOT NULL,
    [doc]       NVARCHAR (MAX)   NOT NULL
);
GO

CREATE TABLE [dbo].[keys] (
    [_id]       INT              IDENTITY (1, 1) NOT NULL,
    [_docid]    UNIQUEIDENTIFIER NOT NULL,
    [key]       NVARCHAR (MAX)   NOT NULL,
    [val]       NVARCHAR (MAX)   NOT NULL,
    [tablename] NVARCHAR (100)   NOT NULL,
    CONSTRAINT [PK_keys] PRIMARY KEY CLUSTERED ([_id] ASC)
);
GO

CREATE PROCEDURE [dbo].[sp_storedoc]
	@id uniqueidentifier,
	@rev uniqueidentifier,
	@tablename nvarchar(100),
	@doc nvarchar(MAX)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @exists uniqueidentifier
	SELECT @exists = [_rev] FROM [docs] WHERE [_id] = @id
	IF (@exists = @rev)
		BEGIN
			DELETE FROM [docs] WHERE [_id] = @id
		END
	ELSE
		IF @exists IS NOT NULL
			BEGIN
				RAISERROR (N'Revision ID mismatch', 10, 1)
			END
	INSERT INTO [docs] ([_id], [_rev], [tablename], [doc]) VALUES (@id, @rev, @tablename, @doc)
	DELETE FROM [keys] WHERE [_docid] = @id
END
GO

CREATE PROCEDURE [dbo].[sp_retdoc]
	@id uniqueidentifier,
	@tablename nvarchar(100)
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [docs] SET [_rev] = NEWID() WHERE [_id] = @id AND [tablename] = @tablename
	SELECT [_id], [_rev], [tablename], [doc] FROM [docs] WHERE [_id] = @id AND [tablename] = @tablename
END
GO