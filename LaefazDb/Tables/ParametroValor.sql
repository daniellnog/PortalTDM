CREATE TABLE [dbo].[ParametroValor]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [IdTestData] INT NOT NULL, 
    [IdParametroScript] INT NOT NULL, 
    [Valor] VARCHAR(5000) NOT NULL, 
    CONSTRAINT [FK_ParametroValor_TestData] FOREIGN KEY ([IdTestData]) REFERENCES [TestData]([Id]),
    CONSTRAINT [FK_ParametroValor_ParametroScript] FOREIGN KEY ([IdParametroScript]) REFERENCES [ParametroScript]([Id])
)
