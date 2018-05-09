	/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

-- Cadastro de Ambiente Virtual 'VDI01'
IF NOT EXISTS (SELECT * FROM AmbienteVirtual WHERE Descricao = 'VDI01')
BEGIN
	INSERT INTO AmbienteVirtual (Descricao, IP) VALUES ('VDI01', '10.43.6.141');
END

-- Cadastro de Ambiente Virtual 'VDI02'
IF NOT EXISTS (SELECT * FROM AmbienteVirtual WHERE Descricao = 'VDI02')
BEGIN
	INSERT INTO AmbienteVirtual (Descricao, IP) VALUES ('VDI02', '10.43.6.219');
END
