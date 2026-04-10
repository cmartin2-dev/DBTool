namespace DBTool.Commons;

public class AnalysisResult
{
    public string FileType { get; set; } = string.Empty;   // Tables, Views, Functions, etc.
    public string ChangeType { get; set; } = string.Empty;  // New Table, New Column, Alter Column, New Index, etc.
    public string ObjectName { get; set; } = string.Empty;  // Table/View/Function name
    public string Detail { get; set; } = string.Empty;      // Column name, index name, specifics
    public string Status { get; set; } = string.Empty;      // Missing, Present, Appended as ALTER
    public string ChangesetId { get; set; } = string.Empty;  // Liquibase changeset ID
    public string UpgradeScript { get; set; } = string.Empty;  // Full changeset body from UPGRADE
    public string UpgradeFileName { get; set; } = string.Empty; // Source UPGRADE file name
    public string CreateFileName { get; set; } = string.Empty;  // Corresponding CREATE file name
    public string CreateSnippet { get; set; } = string.Empty;   // Relevant snippet from CREATE
}
