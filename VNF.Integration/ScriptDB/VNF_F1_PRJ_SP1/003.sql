/*
   Thursday, October 20, 20169:26:08 AM
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
CREATE TABLE VNF.tbImportacaoTipoDocumento
	(
	IdImportacaoTipoDocumento int NOT NULL IDENTITY (1, 1),
	DescTipoDocumento varchar(255) NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE VNF.tbImportacaoTipoDocumento ADD CONSTRAINT
	PK_tbImportacaoTipoDocumento PRIMARY KEY CLUSTERED 
	(
	IdImportacaoTipoDocumento
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE VNF.tbImportacaoTipoDocumento SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
select Has_Perms_By_Name(N'VNF.tbImportacaoTipoDocumento', 'Object', 'ALTER') as ALT_Per, Has_Perms_By_Name(N'VNF.tbImportacaoTipoDocumento', 'Object', 'VIEW DEFINITION') as View_def_Per, Has_Perms_By_Name(N'VNF.tbImportacaoTipoDocumento', 'Object', 'CONTROL') as Contr_Per 