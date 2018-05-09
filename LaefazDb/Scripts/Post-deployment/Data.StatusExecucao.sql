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

IF NOT EXISTS (SELECT * FROM StatusExecucao)
BEGIN
	INSERT INTO StatusExecucao (Id, Descricao) VALUES (1, 'Aguardando Processamento');
	INSERT INTO StatusExecucao (Id, Descricao) VALUES (2, 'Em Processamento');
	INSERT INTO StatusExecucao (Id, Descricao) VALUES (3, 'Processando Log Tosca');
	INSERT INTO StatusExecucao (Id, Descricao) VALUES (4, 'Sucesso');
	INSERT INTO StatusExecucao (Id, Descricao) VALUES (5, 'Falha');
END