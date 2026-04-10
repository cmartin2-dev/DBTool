--liquibase formatted sql

--changeset system:{new_version}.scah.metadata.0001  splitStatements:true endDelimiter:GO
--param schemaName:string
--comment: SCVERINFO
UPDATE SCAH.SCVERINFO 
SET version = '{new_version}';