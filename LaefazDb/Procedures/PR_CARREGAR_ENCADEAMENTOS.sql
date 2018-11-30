CREATE PROCEDURE PR_CARREGAR_ENCADEAMENTOS 

	@IDSTATUSCADASTRADA NVARCHAR(1) = 1, 
	@IDSTATUSEMGERACAO  NVARCHAR(1) = 2, 
	@IDSTATUSDISPONIVEL NVARCHAR(1) = 3, 
	@IDSTATUSERRO       NVARCHAR(1) = 4, 
	@IDSTATUSRESERVADA  NVARCHAR(1) = 5, 
	@IDSTATUSUTILIZADA  NVARCHAR(1) = 6 
AS 
  BEGIN 
      SELECT DISTINCT en.id 
             IdEncadeamento, 
             en.descricao 
             Descricao, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  encadeamento_testdata.idencadeamento = en.id) qtdTds, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSCADASTRADA 
                     AND encadeamento_testdata.idencadeamento = en.id) 
             QtdCadastrada, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSEMGERACAO 
                     AND encadeamento_testdata.idencadeamento = en.id) 
             QtdEmGeracao, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSDISPONIVEL 
                     AND encadeamento_testdata.idencadeamento = en.id) 
             QtdDisponivel, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSERRO 
                     AND encadeamento_testdata.idencadeamento = en.id) 
			 QtdErro, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSRESERVADA 
                     AND encadeamento_testdata.idencadeamento = en.id) 
             QtdReservada, 
             (SELECT Count(encadeamento_testdata.id) 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.idstatus = @IDSTATUSUTILIZADA 
                     AND encadeamento_testdata.idencadeamento = en.id) 
             QtdUtilizada, 
             (SELECT testdata.geradopor 
              FROM   encadeamento_testdata 
                     INNER JOIN testdata 
                             ON encadeamento_testdata.idtestdata = testdata.id 
              WHERE  testdata.geradopor IS NOT NULL 
                     AND encadeamento_testdata.idencadeamento = en.id) GeradoPor 
      FROM   encadeamento en 
             INNER JOIN encadeamento_testdata entd 
                     ON en.id = entd.idencadeamento 
             INNER JOIN testdata td 
                     ON entd.idtestdata = td.id 
             LEFT JOIN execucao ex 
                    ON td.idexecucao = ex.id 
  END