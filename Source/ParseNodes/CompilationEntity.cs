namespace Pastel.ParseNodes
{
    internal enum CompilationEntityType
    {
        FUNCTION,
        ENUM,
        CONSTANT,
        GLOBAL,
        STRUCT,
    }

    internal interface ICompilationEntity
    {
        CompilationEntityType EntityType { get; }
    }
}
