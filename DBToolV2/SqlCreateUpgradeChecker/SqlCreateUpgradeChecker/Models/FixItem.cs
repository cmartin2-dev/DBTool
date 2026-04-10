namespace SqlCreateUpgradeChecker.Models;

public class FixItem
{
    public bool IsSelected { get; set; } = true;
    public string Source { get; set; } = string.Empty;       // "Previous CREATE" or "UPGRADE"
    public string SourceFile { get; set; } = string.Empty;   // e.g., FSH_CREATE_TABLES.sql or FSH_UPGRADE_TABLES.sql
    public string TargetFile { get; set; } = string.Empty;   // e.g., FSH_CREATE_TABLES.sql
    public string Label { get; set; } = string.Empty;        // First meaningful SQL line
    public string SqlContent { get; set; } = string.Empty;   // The full GO-delimited block to append
    public int SourceLine { get; set; }
}
