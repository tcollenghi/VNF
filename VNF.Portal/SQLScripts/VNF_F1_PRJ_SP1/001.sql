
CREATE VIEW vwIntegracaoPSGetPdf
as
	--///////////////// TESTE ////////////////////
	select ID, LINK_DOWNLOAD, ANEXO_NF, BINARY_ARQUIVO
	from OPENDATASOURCE('SQLNCLI', 'Data Source=SORS0845B.mdir.co;user id=PORTAL_SERVICO;password=PORTAL_SERVICO').DTB_PORTAL_SERVICO.dbo.NotaFiscal

	--///////////////// QA ////////////////////
	--select ID, LINK_DOWNLOAD, ANEXO_NF, BINARY_ARQUIVO
	--from OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO_TEST;password=QA_PORTAL_SERVICO').DTB_QA_BR_PORTAL_SERVICO.dbo.NotaFiscal

	--///////////////// PRODUCAO ////////////////////
	--select ID, LINK_DOWNLOAD, ANEXO_NF, BINARY_ARQUIVO
	--from OPENDATASOURCE('SQLNCLI', 'Data Source=172.18.49.37,1902;user id=PORTAL_SERVICO;password=PORTAL_SERVICO').DTB_BR_PORTAL_SERVICO.dbo.NotaFiscal
	