--liquibase formatted sql

--changeset gabrielian.abel:{new_version}.scah.pre_metadata.2025.202508061140  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Disabled APPFEATURE trigger
ALTER TABLE [SCAH].[APPFEATURE] DISABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]
GO

--changeset gabrielian.abel:{new_version}.scah.pre_metadata.2025.202508061149  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Disabled Data Lake triggers
DECLARE @SQL NVARCHAR(MAX) = '';

SELECT @SQL = @SQL + 'DISABLE TRIGGER [' + S.name + '].[' + T.name + '] ON [' + S.name + '].[' + O.name + '];' + CHAR(13)
FROM sys.triggers T
INNER JOIN sys.objects O ON T.parent_id = O.object_id
INNER JOIN sys.schemas S ON O.schema_id = S.schema_id
WHERE T.name LIKE '%DATALAKE_INGEST%'
    AND S.name = 'SCAH'
    AND T.is_disabled = 0;

EXEC sp_executesql @SQL;
GO