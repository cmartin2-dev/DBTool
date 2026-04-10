--liquibase formatted sql

--changeset gabrielian.abel:{new_version}.scah.post_metadata.2025.202508061140  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Enabled APPFEATURE trigger
ALTER TABLE [SCAH].[APPFEATURE] ENABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]
GO

--changeset gabrielian.abel:{new_version}.scah.post_metadata.2025.202508061153  splitStatements:true endDelimiter:GO runAlways:true
--param schemaName:string
--comment: Enabled Data Lake triggers
IF EXISTS (
    SELECT TOP 1 1 FROM [SCAH].[APPSETTINGCE] 
    WHERE [KEY] = 'datalakesettings'
        AND [VALUE] IS NOT NULL 
        AND JSON_VALUE([VALUE], '$.isAllowPublish') = 'true'
)
BEGIN
    DECLARE @SQL NVARCHAR(MAX) = '';
    
    SELECT @SQL = @SQL + 'ENABLE TRIGGER [' + S.name + '].[' + T.name + '] ON [' + S.name + '].[' + O.name + '];' + CHAR(13)
    FROM sys.triggers T
    INNER JOIN sys.objects O ON T.parent_id = O.object_id
    INNER JOIN sys.schemas S ON O.schema_id = S.schema_id
    WHERE T.name LIKE '%DATALAKE_INGEST%'
        AND S.name = 'SCAH'
        AND T.is_disabled = 1;

    EXEC sp_executesql @SQL;
END
GO