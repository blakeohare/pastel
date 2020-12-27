namespace Pastel.Nodes
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
        PastelContext Context { get; }
    }
}
