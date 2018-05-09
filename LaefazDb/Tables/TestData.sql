CREATE TABLE [dbo].[TestData]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [IdExecucao] INT NULL, 
    [IdDataPool] INT NOT NULL, 
    [IdStatus] INT NOT NULL, 
    [IdUsuario] INT NULL, 
	[IdScript_CondicaoScript] INT NOT NULL,
    [Descricao] VARCHAR(500) NOT NULL,     
    [GerarMigracao] BIT NOT NULL DEFAULT 0, 
    [CasoTesteRelativo] VARCHAR(500) NULL, 
    [Observacao] VARCHAR(500) NULL,     [InicioExecucao] DATETIME NULL, 
    [TerminoExecucao] DATETIME NULL,
	[GeradoPor] VARCHAR(500) NULL, 
    [DataGeracao] DATETIME NULL, 
    [CaminhoEvidencia] VARCHAR(500) NULL, 
    CONSTRAINT [FK_TestData_Execucao] FOREIGN KEY ([IdExecucao]) REFERENCES [Execucao]([Id]),
    CONSTRAINT [FK_TestData_DataPool] FOREIGN KEY ([IdDataPool]) REFERENCES [DataPool]([Id]),
    CONSTRAINT [FK_TestData_Status] FOREIGN KEY ([IdStatus]) REFERENCES [Status]([Id]),
    CONSTRAINT [FK_TestData_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuario]([Id]),
	CONSTRAINT [FK_TestData_Script_CondicaoScript] FOREIGN KEY ([IdScript_CondicaoScript]) REFERENCES [Script_CondicaoScript]([Id])
)
