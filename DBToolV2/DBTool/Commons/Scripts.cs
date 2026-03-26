using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBTool.Commons
{
    public class Scripts
    {
        public static string ErrorDeletingUsers = "Error deleting users";
        public static string ErrorDroppingSchema = "Error dropping schema";
        public static string ErrorDeletingSchemaInTable = "Error deleting schema in table";

        public static string UpdateAppFeature(bool enabled, bool visible, string version, string expiryDate, string key, string displayName, string description)
        {
            if (expiryDate != null)
            {
                expiryDate = $"'{DateTime.Parse(expiryDate).ToString("MM/dd/yyyy")}'";
            }
            else
                expiryDate = "null";

            string updateScript = string.Format($"ALTER TABLE [SCAH].[APPFEATURE] DISABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]\r\n\r\nUPDATE SCAH.APPFEATURE\r\nSET\r\n\tENABLED = {{0}},\r\n\tVISIBLE = {{1}},\r\n\tVERSION = '{{2}}',\r\n\tEXPIRYDATE = {{3}},DISPLAYNAME='{{5}}',DESCRIPTION='{{6}}'\r\nWHERE\r\n\t[KEY] = '{{4}}'\r\n\t\r\nALTER TABLE [SCAH].[APPFEATURE] ENABLE TRIGGER [APPFEATURE_AUDIT_UPDATE]", Convert.ToInt16(enabled), Convert.ToInt16(visible), version, expiryDate, key,displayName,description);

            return updateScript;
        }

        public static string DeleteUser(string fshName)
        {
            return $"DELETE FROM SCAH.USERPREF WHERE VALUE = '{fshName}' AND NAME = 'ACTIVE_SCHEMANAME'";
        }

        public static string DropSchema(string fshName)
        {
            return $"EXEC [SCAH].[P_DROP_SCHEMA] '{fshName}'";
        }

        public static string DeleteDataSchema(string dataSchemaId)
        {
            return string.Format(@"DELETE 
FROM 
	SCAH.DATASCHEMAS
WHERE 
	DATASCHEMASID = {0}
	
INSERT INTO SCAH.DATASCHEMAHISTORY
	(
	DATASCHEMASID, 
	LOG,
	MODIFYID, 
	MODIFYDATE
	)
VALUES (
	{0},
	'Deleted',
	1,
	GETUTCDATE()
	)", dataSchemaId);
        }

        public static string GetSchemaTable(string schema)
        {
            return string.Format(@"SELECT TABLE_NAME as 'TABLE', TABLE_SCHEMA AS 'SCHEMA', '' as 'TOTALCOUNT', '' as 'STATUS',
  STUFF((SELECT  ', ' + COLUMN_NAME
           FROM INFORMATION_SCHEMA.COLUMNS t2
           where t2.TABLE_NAME = t1.TABLE_NAME AND T2.TABLE_SCHEMA = T1.TABLE_SCHEMA AND T2.COLUMN_NAME NOT IN ('ROWVERSION')
           ORDER BY T2.ORDINAL_POSITION
		   FOR XML PATH('')),1,1,'') AS COLUMNS
from INFORMATION_SCHEMA.TABLES t1
 WHERE TABLE_SCHEMA IN ('{0}') AND TABLE_TYPE = 'BASE TABLE' 
GROUP BY TABLE_SCHEMA,TABLE_NAME
ORDER BY TABLE_SCHEMA,TABLE_NAME", string.Join(",", schema));
        }

        public static string ExtractTableScript(string columnNames, string schemaName, string tableName, long offSet, int runPerRecord)
        {
            return $"SELECT {columnNames} FROM {schemaName}.{tableName} WITH (NOLOCK)  ORDER BY 1 OFFSET {offSet} ROWS FETCH NEXT {runPerRecord} ROWS ONLY";
        }

        public static string GenerateSCAHLocaleScript(List<Language> languages, string filter,
            bool removeMnemonic = false, bool newOrUpdatedOnly = false, bool includeLength = false)
        {

            string str = string.Empty;

            int ctr = 0;
            string query = string.Empty;

            string filterStatement = " {0}.[KEY] NOT IN (SELECT VALUE FROM STRING_SPLIT('{1}',',')) ";

            if (!includeLength)
            {
                if (!removeMnemonic)
                    str = $"SELECT DISTINCT({languages[0].Name}.[KEY]) AS [KEY],   {string.Join(",", languages.Select(x => $"{x.Name}.[{x.Culture}]"))} FROM ";
                else
                {
                    List<string> columns = languages.Select(x => $"{x.Name}.[{x.Culture}]").ToList();
                    columns.Insert(1, $"{languages[0].Name}.[{languages[0].Culture}] as '{languages[0].Culture}_noMnemonic'");
                    str = $"SELECT DISTINCT({languages[0].Name}.[KEY]),{string.Join(",", columns.ToList())} FROM ";
                }
                foreach (Language language in languages)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = string.Format("(SELECT [KEY], [VALUE] AS '{1}' FROM SCAH.LOCALE WHERE [LANGUAGE] = '{1}') {0}", language.Name, language.Culture);
                    }
                    else
                    {
                        query += string.Format(" LEFT JOIN (SELECT [KEY], [VALUE] AS '{1}' FROM SCAH.LOCALE WHERE [LANGUAGE] = '{1}') {0} ON {2}.[KEY] = {0}.[KEY] ", language.Name, language.Culture, languages[ctr].Name);
                        ctr++;
                    }
                }
            }
            else
            {
                if (!removeMnemonic)
                    str = $"SELECT DISTINCT({languages[0].Name}.[KEY]),   {string.Join(",", languages.Select(x => $"{x.Name}.[{x.Culture}],'' AS [Len_{x.Culture}]"))} FROM ";
                else
                {
                    List<string> columns = languages.Select(x => $"{x.Name}.[{x.Culture}],'' AS [Len_{x.Culture}]").ToList();
                    columns.Insert(1, $"{languages[0].Name}.[{languages[0].Culture}] AS '{languages[0].Culture}_noMnemonic', '' AS [Len_{languages[0].Culture}_noMnemonic]");
                    str = $"SELECT DISTINCT({languages[0].Name}.[KEY]),{string.Join(",", columns.ToList())} FROM ";
                }
                foreach (Language language in languages)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = string.Format("(SELECT [KEY], [VALUE] AS '{1}' FROM SCAH.LOCALE WHERE [LANGUAGE] = '{1}') {0}", language.Name, language.Culture);
                    }
                    else
                    {
                        query += string.Format(" LEFT JOIN (SELECT [KEY], [VALUE] AS '{1}' FROM SCAH.LOCALE WHERE [LANGUAGE] = '{1}') {0} ON {2}.[KEY] = {0}.[KEY] ", language.Name, language.Culture, languages[ctr].Name);
                        ctr++;
                    }
                }
            }

            List<string> filters = new List<string>();

            if (!string.IsNullOrEmpty(filter))
            {
                /// format filter
                /// 
                string[] filtersplitted = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);

                List<string> lstFilter = new List<string>();

                foreach (string s in filtersplitted)
                {
                    string newValue = s.TrimStart().TrimEnd().Trim().Replace(Environment.NewLine, "");
                    lstFilter.Add(newValue);
                }




                filterStatement = string.Format(filterStatement, languages[0].Name, string.Join(',', lstFilter));
                filters.Add(filterStatement);
            }

            if (newOrUpdatedOnly)
            {
                foreach (Language language in languages)
                {
                    filters.Add($"{languages[0].Name}.[{languages[0].Culture}] = {language.Name}.[{language.Culture}]");
                }
            }

            string whereStatement = filters.Count() > 0 ? $" WHERE {string.Join(" and ", filters)}" : "";

            return str = str + query + whereStatement + $" ORDER BY {languages[0].Name}.[KEY]";

        }

        public static string GenerateCultureInfoLocaleScript(List<Language> languages, string schema, bool includeLength = false)
        {
            string header = string.Empty;

            string query = string.Empty;
            int ctr = 0;
            if (!includeLength)
            {
                header = $"SELECT {languages[0].Name}.TABREF, {languages[0].Name}.REFID,{languages[0].Name}.REFCODE,   {string.Join(",", languages.Select(x => $"{x.Name}.[{x.Culture}]"))} FROM ";

                foreach (Language language in languages)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = string.Format(" (SELECT TABREF, REFID, REFCODE,NAME  AS '{1}' FROM {2}.CULTUREINFO WHERE [CULTURE] = '{1}' AND ISDELETED = 0) {0}", language.Name, language.Culture, schema);
                    }
                    else
                    {
                        query += string.Format(" LEFT JOIN (SELECT TABREF, REFID, REFCODE,NAME AS '{1}' FROM {3}.CULTUREINFO WHERE [CULTURE] = '{1}' AND ISDELETED = 0) {0} ON {0}.[TABREF] = {2}.[TABREF] AND {0}.[REFID] = {2}.[REFID] AND ISNULL({0}.[REFCODE],'') = ISNULL({2}.[REFCODE],'')", language.Name, language.Culture, languages[ctr].Name, schema);
                        ctr++;
                    }


                }
            }
            else
            {
                header = $"SELECT {languages[0].Name}.TABREF, {languages[0].Name}.REFID,{languages[0].Name}.REFCODE,   {string.Join(",", languages.Select(x => $"{x.Name}.[{x.Culture}], '' AS [Len_{x.Culture}]"))} FROM ";


                foreach (Language language in languages)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = string.Format(" (SELECT TABREF, REFID, REFCODE,NAME  AS '{1}' FROM {2}.CULTUREINFO WHERE [CULTURE] = '{1}' AND ISDELETED = 0) {0}", language.Name, language.Culture, schema);
                    }
                    else
                    {
                        query += string.Format(" LEFT JOIN (SELECT TABREF, REFID, REFCODE,NAME AS '{1}' FROM {3}.CULTUREINFO WHERE [CULTURE] = '{1}' AND ISDELETED = 0) {0} ON {0}.[TABREF] = {2}.[TABREF] AND {0}.[REFID] = {2}.[REFID] AND ISNULL({0}.[REFCODE],'') = ISNULL({2}.[REFCODE],'')", language.Name, language.Culture, languages[ctr].Name, schema);
                        ctr++;
                    }


                }
            }

            return header + query + $" ORDER BY {languages[0].Name}.[TABREF], {languages[0].Name}.[REFID]"; ;
        }

        public static string CreateUpdateScriptForSCAH(string value,string localeKey, Language language)
        {
            return $"UPDATE SCAH.LOCALE SET [VALUE]=N'{value.Replace("'", "''")}' WHERE [KEY] = N'{localeKey}' AND [LANGUAGE] = N'{language.Culture}' {Environment.NewLine}GO";
        }

        public static string CreateUpdateScriptForFSH(string value, string tabRef, string refId, Language language)
        {
            return $"UPDATE <%SCHEMA_NAME%>.CULTUREINFO SET [NAME] =N'{value.Replace("'", "''")}' WHERE [TABREF] = N'{tabRef}' AND REFID = {refId} AND [CULTURE] = N'{language.Culture}' {Environment.NewLine}GO";
        }

    }
}
