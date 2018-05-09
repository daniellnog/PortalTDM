﻿CREATE TABLE [dbo].[Execucao]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [IdScript_CondicaoScript_Ambiente] INT NULL,
    [IdTestData] INT NULL, 
	[IdStatusExecucao] INT NOT NULL,
	[IdTipoFaseTeste] INT NOT NULL,
	[Usuario] [varchar](500) NULL,
	[SituacaoAmbiente] [int] NULL, 
    [ToscaInput] VARCHAR(5000) NULL, 
    [ToscaOutput] VARCHAR(5000) NULL, 
    CONSTRAINT [FK_TipoFaseTeste_Execucao] FOREIGN KEY ([IdTipoFaseTeste]) REFERENCES [TipoFaseTeste]([Id]),
    CONSTRAINT [FK_TipoFaseTeste_StatusExecucao] FOREIGN KEY ([IdStatusExecucao]) REFERENCES [StatusExecucao]([Id]),
	CONSTRAINT [FK_Script_CondicaoScript_Ambiente_Execucao] FOREIGN KEY (IdScript_CondicaoScript_Ambiente) REFERENCES Script_CondicaoScript_Ambiente(Id)
)