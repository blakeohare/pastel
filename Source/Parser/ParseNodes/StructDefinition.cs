using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class StructDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.STRUCT; } }

        public Token FirstToken { get; set; }
        public Token NameToken { get; set; }

        public PType[] FieldTypes { get; set; }
        public Token[] FieldNames { get; set; }

        public Dictionary<string, int> FieldIndexByName { get; set; }
        public PastelContext Context { get; private set; }

        public StructDefinition(Token structToken, Token name, IList<PType> argTypes, IList<Token> argNames, PastelContext context)
        {
            this.Context = context;
            this.FirstToken = structToken;
            this.NameToken = name;
            this.FieldTypes = [.. argTypes];
            this.FieldNames = [.. argNames];
            this.FieldIndexByName = [];

            for (int i = 0; i < this.FieldNames.Length; i++)
            {
                string argName = this.FieldNames[i].Value;
                this.FieldIndexByName[argName] = i;
            }
        }
    }
}
