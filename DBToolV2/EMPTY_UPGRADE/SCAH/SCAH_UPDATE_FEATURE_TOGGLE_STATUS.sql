--liquibase formatted sql

--changeset system:{new_version}.scah.feature_toggle_status.001  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Update feature toggle
ALTER TABLE [SCAH].[APPFEATURE] DISABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]
GO

--changeset system:{new_version}.scah.feature_toggle_status.002  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Update feature toggle
UPDATE SCAH.APPFEATURE
SET [ENABLED] = 1
WHERE RELEASEEXPIRY IS NOT NULL 
	AND RELEASEEXPIRY <= SUBSTRING((SELECT [VERSION] FROM SCAH.SCVERINFO), 1, 7)
	AND VISIBLE = 1
	AND [ENABLED] = 0
GO

--changeset system:{new_version}.scah.feature_toggle_status.003  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Update feature toggle
ALTER TABLE [SCAH].[APPFEATURE] ENABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]
GO
