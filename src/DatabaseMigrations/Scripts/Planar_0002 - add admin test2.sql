SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Tsahi Atias
-- Create date: 2022-12-02
-- Description:	Check the permission of read/write/execute
-- =============================================
CREATE PROCEDURE [Admin].[TestPermission] 
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRANSACTION

	-- TEST Read
	SELECT TOP 1 [Key] FROM [GlobalConfig]

	-- TEST Write
	INSERT INTO [GlobalConfig] ([Key], [Value], [Type]) VALUES ('TestWrite', 'TestWrite', 'string')

	-- TEST Delete
	DELETE FROM [GlobalConfig] WHERE [Key] = 'TestWrite'

	-- TEST Exec
	EXEC [Admin].[TestExecPermission] 

	ROLLBACK
END
GO
