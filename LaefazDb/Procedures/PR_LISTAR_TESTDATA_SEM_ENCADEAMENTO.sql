CREATE PROCEDURE PR_LISTAR_TESTDATA_SEM_ENCADEAMENTO
	@IDDATAPOOL NVARCHAR(255) = NULL,
	@IDTESTDATA NVARCHAR(255) = NULL
AS
BEGIN
	SELECT DISTINCT
		td.Id IdTestData,
		td.Descricao,
		td.CasoTesteRelativo,
		td.Observacao,
		td.DataGeracao,
		td.GeradoPor,
		td.IdStatus,
		td.IdDataPool,
		td.GerarMigracao,
		td.CaminhoEvidencia,
		td.TempoEstimadoExecucao,
		td.ClassificacaoMassa,
		st.Descricao DescricaoStatus,
		dtp.Descricao DescricaoDataPool,
		dtp.IdScript_CondicaoScript,
		dtp.IdAut,
		AUT.Descricao DescricaoAut
	FROM 
		TestData td
			INNER JOIN Status st on td.IdStatus = st.Id
			INNER JOIN DataPool dtp on td.IdDataPool = dtp.Id
			INNER JOIN AUT on dtp.IdAut = AUT.Id
	WHERE 
			(dtp.Id = @IDDATAPOOL OR @IDDATAPOOL IS NULL)
			AND(td.Id = @IDTESTDATA OR @IDTESTDATA IS NULL)
			AND
				(
						SELECT 
							COUNT(Id)
						FROM 
							Encadeamento_TestData
						WHERE
							Encadeamento_TestData.IdTestData = td.Id
				 )=0
				
	ORDER BY td.Id
END