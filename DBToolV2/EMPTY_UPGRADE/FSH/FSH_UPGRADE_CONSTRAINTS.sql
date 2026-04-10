--liquibase formatted sql

--changeset <username>:2026.07.00.${schemaName}.constraint.0001  splitStatements:true endDelimiter:GO
--param schemaName:string
--comment: