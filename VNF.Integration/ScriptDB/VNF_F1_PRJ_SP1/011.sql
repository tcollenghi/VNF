ALTER PROCEDURE SP_DELETE_NFS_BY_PORTALSERVICO
	@IdNotaFiscal int,
	@ConfirmaConcluido varchar(1)
As
	Declare @ChaveAcessoNFEID varchar(255) = '';
	Declare @StatusIntegracao varchar(255) = '';

	select @ChaveAcessoNFEID = ChaveAcesso from tbImportacaoNotaFiscal
	where IdNotaFiscal = @IdNotaFiscal

	if(@ChaveAcessoNFEID != '') begin
		
		select @StatusIntegracao = STATUS_INTEGRACAO from vwStatusIntegracao
		where NFEID = @ChaveAcessoNFEID
		
		If (@StatusIntegracao = 'CONCLUÍDO') Begin

			If(@ConfirmaConcluido = 'S') Begin

				delete from tbNfe					where NFEID = @ChaveAcessoNFEID
				delete from tbDoc_Cab				where NFEID = @ChaveAcessoNFEID
				delete from tbDoc_Cab_Nfe			where NFEID = @ChaveAcessoNFEID
				delete from TbDoc_Item				where NFEID = @ChaveAcessoNFEID
				delete from TbDoc_Item_Nfe			where NFEID = @ChaveAcessoNFEID
				delete from TbJun					where NFEID = @ChaveAcessoNFEID
				delete from TbLog					where NFEID = @ChaveAcessoNFEID
				exec		SP_DELETE_DOC_INFORMATION			  @ChaveAcessoNFEID
				delete from TbMEN					where nfeid = @ChaveAcessoNFEID
				delete from TbNFE_CAB				where nfeid = @ChaveAcessoNFEID
				delete from TbNFE_ITEM				where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_NFE_REF		where nfeid = @ChaveAcessoNFEID
				delete from TbIPLOG					where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_NFE_DUP		where nfeid = @ChaveAcessoNFEID
				delete from TbPORT_EF				where nfeid = @ChaveAcessoNFEID
				delete from TbBO					where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_CTE			where nfeid = @ChaveAcessoNFEID
				delete from TbIntegracao			where nfeid = @ChaveAcessoNFEID
				delete from TbIntegracaoMensagens	where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_SAP			where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_ITEM				where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_ITEM_NFE			where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_ITEM_SAP			where nfeid = @ChaveAcessoNFEID
				delete from TbLOG					where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_ANEXOS		where nfeid = @ChaveAcessoNFEID
				delete from TbDANFE					where nfeid = @ChaveAcessoNFEID
				delete from TbNFE					where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB_NFE			where nfeid = @ChaveAcessoNFEID
				delete from TbDANFE_ITEM			where nfeid = @ChaveAcessoNFEID
				delete from TbVER					where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_CAB				where nfeid = @ChaveAcessoNFEID
				delete from TbDOC_ITEM_CTE			where nfeid = @ChaveAcessoNFEID
				delete from TbIntegracaoPostagens	where ipo_nfeid =	@ChaveAcessoNFEID
				delete from tbImportacaoItemNF		where ChaveAcesso = @ChaveAcessoNFEID
				delete from tbImportacaoNotaFiscal	where ChaveAcesso = @ChaveAcessoNFEID

				Select 'EXCLUIDO_VNF' as RETORNO

			End Else Begin

				Select 'STATUS_SAP_CONCLUIDO' as RETORNO

			End
				
		End Else Begin

			delete from tbNfe					where NFEID = @ChaveAcessoNFEID
			delete from tbDoc_Cab				where NFEID = @ChaveAcessoNFEID
			delete from tbDoc_Cab_Nfe			where NFEID = @ChaveAcessoNFEID
			delete from TbDoc_Item				where NFEID = @ChaveAcessoNFEID
			delete from TbDoc_Item_Nfe			where NFEID = @ChaveAcessoNFEID
			delete from TbJun					where NFEID = @ChaveAcessoNFEID
			delete from TbLog					where NFEID = @ChaveAcessoNFEID
			exec		SP_DELETE_DOC_INFORMATION			  @ChaveAcessoNFEID
			delete from TbMEN					where nfeid = @ChaveAcessoNFEID
			delete from TbNFE_CAB				where nfeid = @ChaveAcessoNFEID
			delete from TbNFE_ITEM				where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_NFE_REF		where nfeid = @ChaveAcessoNFEID
			delete from TbIPLOG					where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_NFE_DUP		where nfeid = @ChaveAcessoNFEID
			delete from TbPORT_EF				where nfeid = @ChaveAcessoNFEID
			delete from TbBO					where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_CTE			where nfeid = @ChaveAcessoNFEID
			delete from TbIntegracao			where nfeid = @ChaveAcessoNFEID
			delete from TbIntegracaoMensagens	where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_SAP			where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_ITEM				where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_ITEM_NFE			where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_ITEM_SAP			where nfeid = @ChaveAcessoNFEID
			delete from TbLOG					where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_ANEXOS		where nfeid = @ChaveAcessoNFEID
			delete from TbDANFE					where nfeid = @ChaveAcessoNFEID
			delete from TbNFE					where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB_NFE			where nfeid = @ChaveAcessoNFEID		
			delete from TbDANFE_ITEM			where nfeid = @ChaveAcessoNFEID
			delete from TbVER					where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_CAB				where nfeid = @ChaveAcessoNFEID
			delete from TbDOC_ITEM_CTE			where nfeid = @ChaveAcessoNFEID
			delete from TbIntegracaoPostagens	where ipo_nfeid =	@ChaveAcessoNFEID
			delete from tbImportacaoItemNF		where ChaveAcesso = @ChaveAcessoNFEID
			delete from tbImportacaoNotaFiscal	where ChaveAcesso = @ChaveAcessoNFEID

			Select 'EXCLUIDO_VNF' as RETORNO

		End
	End Else Begin

		Select 'SEM_CHAVE_VNF' as RETORNO

	End

