namespace DBTool.Commons;

public class ScaffoldItem
{
    public string Action { get; set; } = string.Empty;    // "Copy" or "Template"
    public string Source { get; set; } = string.Empty;     // Source path
    public string Destination { get; set; } = string.Empty; // Destination path
    public string Status { get; set; } = string.Empty;     // Pending, Created, Error
}
