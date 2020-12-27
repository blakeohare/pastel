using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel.Nodes
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
            this.Context = context;
            this.FirstToken = classToken;
            this.NameToken = nameToken;
            this.ParentClass = null;
        }

        internal void AddMembers(Dictionary<string, ICompilationEntity> members)
        {
            this.Members = new Dictionary<string, ICompilationEntity>(members);
            this.Fields = members.Keys.OrderBy(k => k).Select(k => members[k]).OfType<FieldDefinition>().ToArray();
            this.Methods = members.Keys.OrderBy(k => k).Select(k => members[k]).OfType<FunctionDefinition>().ToArray();
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Constructor.ResolveNamesAndCullUnusedCode(compiler);
            foreach (FieldDefinition fd in this.Fields)
            {
                fd.ResolveNamesAndCullUnusedCode(compiler);
            }

            foreach (FunctionDefinition fd in this.Methods)
            {
                fd.ResolveNamesAndCullUnusedCode(compiler);
            }
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            this.Constructor.ResolveTypes(compiler);
            foreach (FieldDefinition fd in this.Fields)
            {
                fd.ResolveTypes(compiler);
            }

            foreach (FunctionDefinition fd in this.Methods)
            {
                fd.ResolveTypes(compiler);
            }
        }
    }
}
