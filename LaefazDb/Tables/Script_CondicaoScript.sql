CREATE TABLE [dbo].[Script_CondicaoScript]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
	[IdScript] INT NOT NULL, 
	[IdCondicaoScript] INT NULL, 
	[QueryTosca] VARCHAR(MAX) NULL, 
	[ListaExecucaoTosca] VARCHAR(MAX) NULL, 
	[CaminhoArquivoTCS] VARCHAR(MAX) NULL, 
	[DiretorioRelatorio] VARCHAR(MAX) NULL, 
	[TempoEstimadoExecucao] DATETIME NOT NULL, 
	[NomeTecnicoScript] VARCHAR(500) UNIQUE NOT NULL, 
	CONSTRAINT [FK_Script_CondicaoScript_Script] FOREIGN KEY (IdScript) REFERENCES Script(Id),
	CONSTRAINT [FK_Script_CondicaoScript_CondicaoScript] FOREIGN KEY (IdCondicaoScript) REFERENCES CondicaoScript(Id)
)
GO

CREATE UNIQUE INDEX [IX_Script_CondicaoScript] ON [dbo].[Script_CondicaoScript] (IdScript, IdCondicaoScript)
