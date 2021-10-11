CREATE PROCEDURE SP_INSERT_HISTORICO_PORTALSERVICO
	 @Data			DATETIME
	,@idNotaFiscal	INT
	,@UsuarioCodigo VARCHAR(50)
	,@UsuarioNome	VARCHAR(50)
	,@NovoStatus	VARCHAR(50)
	,@Descricao		VARCHAR(50)
AS

	-- INSERE REGISTRO NO HISTORICO INFORMANDO QUE O STATUS MUDO
	INSERT	INTO OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO_TEST;password=QA_PORTAL_SERVICO').DTB_QA_BR_PORTAL_SERVICO.dbo.HistoricoNF
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
		 @Data			
		,@idNotaFiscal	
		,@UsuarioCodigo 
		,@UsuarioNome	
		,@NovoStatus	
		,@Descricao		
	)

	SELECT 'HISTORICO' AS STATUS