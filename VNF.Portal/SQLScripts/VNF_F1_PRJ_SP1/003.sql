
CREATE PROCEDURE sp_AtualizaStatusPortalServico
	@ChaveAcesso VARCHAR(255)
AS
BEGIN
	DECLARE @STATUS_INTEGRACAO_SAP VARCHAR(50) = '';

	IF(EXISTS(SELECT * 
			  FROM	 tbImportacaoNotaFiscal
			  WHERE	 ChaveAcesso = @ChaveAcesso))
	BEGIN
		SELECT	@STATUS_INTEGRACAO_SAP = STATUS_INTEGRACAO 
		FROM	vwStatusIntegracao
		WHERE	NFEID = @ChaveAcesso

		IF(@STATUS_INTEGRACAO_SAP = 'CONCLUÍDO')
		BEGIN
			DECLARE @IdNotaFiscal int;
			
			SELECT	@IdNotaFiscal = IdNotaFiscal FROM tbImportacaoNotaFiscal
			WHERE	ChaveAcesso = @ChaveAcesso
			
			UPDATE	OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO_TEST;password=QA_PORTAL_SERVICO').DTB_QA_BR_PORTAL_SERVICO.dbo.NotaFiscal
			SET		StatusNf = 'Registro Fiscal concluído'
			WHERE	id = @IdNotaFiscal
		END
	END
END


