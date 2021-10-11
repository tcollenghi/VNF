/*ESTA PROCEDURE ATUALIZA O STATUS DO DOCUMENTO
E TAMBEM INSERE REGISTRO NO HISTORICO INFORMANDO 
QUE FOI ATUALIZADO O STATUS DO DOCUMENTO */

ALTER PROCEDURE sp_AtualizaStatusPortalServico
	@ChaveAcesso VARCHAR(255),
	@DataAtual datetime
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
			
			-- ATUALIZA O STATUS NO PORTAL DE SERVÇO
			UPDATE	OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO_TEST;password=QA_PORTAL_SERVICO').DTB_PRJ_BR_PORTAL_SERVICO.dbo.NotaFiscal
			SET		StatusNf = 'Registro Fiscal concluído'
			WHERE	id = @IdNotaFiscal

			-- INSERE REGISTRO NO HISTORICO INFORMANDO QUE O STATUS MUDO
			insert	into OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO_TEST;password=QA_PORTAL_SERVICO').DTB_PRJ_BR_PORTAL_SERVICO.dbo.HistoricoNF
			(
				 Data
				,idNotaFiscal
				,UsuarioCodigo
				,UsuarioNome
				,NovoStatus
				,Descricao
			)
			VALUES
			(
				 @DataAtual
				,@IdNotaFiscal
				,'Sistema'
				,'Sistema'
				,'Registro Fiscal concluído'
				,'Atualização de status automatico (VNF)'
			)
			
		END
	END

	Select 'VERIFICACAOSTATUS' as status
END