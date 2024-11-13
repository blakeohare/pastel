namespace Pastel.Parser.ParseNodes
{
    internal enum CompilationEntityType
    {
        FUNCTION,
        ENUM,
        CONSTANT,
        STRUCT,
    }

    internal interface ICompilationEntity
    {
        CompilationEntityType EntityType { get; }
    }
}
