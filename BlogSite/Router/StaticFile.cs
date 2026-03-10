namespace BlogSite;

public partial class Router
{
    public class StaticFileResult(string filePath, string mimeType) : RouterResult
    {
        public readonly string FilePath = Path.GetFullPath(filePath);
        public readonly string MimeType = mimeType;
        
        public FileInfo FileInfo => new FileInfo(FilePath);
        public override string ToString() => $"{MimeType} '{FilePath}'";
    }
}
