/*
   Thursday, October 20, 20169:30:07 AM
   User: VNF
   Server: sors0845b.mdir.co
   Database: DTB_VNF
   Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.tbImportacaoTipoDocumento SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.tbImportacaoTipoDocumento', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.tbImportacaoTipoDocumento', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.tbImportacaoTipoDocumento', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
EXECUTE sp_rename N'dbo.tbImportacaoNotaFiscal.IdTipoDocumento', N'Tmp_IdImportacaoTipoDocumento', 'COLUMN' 
GO
EXECUTE sp_rename N'dbo.tbImportacaoNotaFiscal.Tmp_IdImportacaoTipoDocumento', N'IdImportacaoTipoDocumento', 'COLUMN' 
GO
ALTER TABLE dbo.tbImportacaoNotaFiscal ADD CONSTRAINT
	FK_tbImportacaoNotaFiscal_tbImportacaoTipoDocumento FOREIGN KEY
	(
	IdImportacaoTipoDocumento
	) REFERENCES dbo.tbImportacaoTipoDocumento
	(
	IdImportacaoTipoDocumento
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE dbo.tbImportacaoNotaFiscal SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'CONTROL') as Contr_Per 