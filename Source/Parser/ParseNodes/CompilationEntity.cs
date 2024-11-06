namespace Pastel.Parser.ParseNodes
{
    internal enum CompilationEntityType
    {
        FUNCTION,
        ENUM,
        CONSTANT,
        STRUCT,
        CLASS,
        CONSTRUCTOR,
        FIELD,
    }

    internal interface ICompilationEntity
    {
        CompilationEntityType EntityType { get; }
    }
}
