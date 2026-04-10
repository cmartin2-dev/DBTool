namespace DBTool.Commons;

public class FdbResult
{
    public string Folder { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string S3Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string S3Status { get; set; } = string.Empty;
    public string LocalFullPath { get; set; } = string.Empty;
}
