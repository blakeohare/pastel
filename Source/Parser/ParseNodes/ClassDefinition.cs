using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ClassDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.CLASS; } }

        public PastelContext Context { get; private set; }
        public Token FirstToken { get; set; }
        public Token NameToken { get; set; }
        public Token[] InheritTokens { get; set; }
        public ClassDefinition ParentClass { get; set; }
        public FunctionDefinition[] Methods { get; set; }
        public FieldDefinition[] Fields { get; set; }
        public Dictionary<string, ICompilationEntity> Members { get; set; }
        public ConstructorDefinition Constructor { get; set; }

        public ClassDefinition(PastelContext context, Token classToken, Token nameToken)
        {
            Context = context;
            FirstToken = classToken;
            NameToken = nameToken;
            ParentClass = null;
        }

        internal void AddMembers(Dictionary<string, ICompilationEntity> members)
        {
            Members = new Dictionary<string, ICompilationEntity>(members);
            Fields = members.Keys.OrderBy(k => k).Select(k => members[k]).OfType<FieldDefinition>().ToArray();
            Methods = members.Keys.OrderBy(k => k).Select(k => members[k]).OfType<FunctionDefinition>().ToArray();
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Constructor.ResolveNamesAndCullUnusedCode(compiler);
            foreach (FieldDefinition fd in Fields)
            {
                fd.ResolveNamesAndCullUnusedCode(compiler);
            }

            foreach (FunctionDefinition fd in Methods)
            {
                fd.ResolveNamesAndCullUnusedCode(compiler);
            }
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            Constructor.ResolveTypes(compiler);
            foreach (FieldDefinition fd in Fields)
            {
                fd.ResolveTypes(compiler);
            }

            foreach (FunctionDefinition fd in Methods)
            {
                fd.ResolveTypes(compiler);
            }
        }
    }
}
