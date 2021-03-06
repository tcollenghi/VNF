/*
   Thursday, October 20, 20169:43:46 AM
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
ALTER TABLE dbo.tbImportacaoNotaFiscal SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'dbo.tbImportacaoNotaFiscal', 'Object', 'CONTROL') as Contr_Per BEGIN TRANSACTION
GO
CREATE TABLE VNF.tbImportacaoItemNF
	(
	IdImportacaoItemNF int NOT NULL IDENTITY (1, 1),
	IdImportacaoNotaFiscal int NOT NULL,
	IdItemNotaFiscal int NOT NULL,
	ChaveAcesso varchar(255) NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE VNF.tbImportacaoItemNF ADD CONSTRAINT
	PK_tbImportacaoItemNF PRIMARY KEY CLUSTERED 
	(
	IdImportacaoItemNF
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE VNF.tbImportacaoItemNF ADD CONSTRAINT
	FK_tbImportacaoItemNF_tbImportacaoNotaFiscal FOREIGN KEY
	(
	IdImportacaoNotaFiscal
	) REFERENCES dbo.tbImportacaoNotaFiscal
	(
	IdImportacaoNotaFiscal
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO
ALTER TABLE VNF.tbImportacaoItemNF SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'VNF.tbImportacaoItemNF', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'VNF.tbImportacaoItemNF', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'VNF.tbImportacaoItemNF', 'Object', 'CONTROL') as Contr_Per 