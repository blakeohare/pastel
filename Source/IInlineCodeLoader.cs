namespace Pastel
{
    public interface IInlineImportCodeLoader
    {
        string LoadCode(string path);
    }

    public class InlineImportCodeLoader : IInlineImportCodeLoader
    {
        public string LoadCode(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}
