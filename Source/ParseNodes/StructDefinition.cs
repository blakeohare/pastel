using System.Collections.Generic;
using System.Linq;

namespace Pastel.Nodes
{
    internal class StructDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.STRUCT; } }

        public Token FirstToken { get; set; }
        public Token NameToken { get; set; }

        public PType[] FlatFieldTypes { get; set; }
        public Token[] FlatFieldNames { get; set; }
        public PType[] LocalFieldTypes { get; set; }
        public Token[] LocalFieldNames { get; set; }

        public Dictionary<string, int> LocalFieldIndexByName { get; set; }
        public Dictionary<string, int> FlatFieldIndexByName { get; set; }
        public PastelContext Context { get; private set; }
        public Token ParentName { get; set; }
        public StructDefinition Parent { get; set; }

        public StructDefinition(Token structToken, Token name, IList<PType> argTypes, IList<Token> argNames, Token parentName, PastelContext context)
        {
            this.Context = context;
            this.FirstToken = structToken;
            this.NameToken = name;
            this.ParentName = parentName;
            this.LocalFieldTypes = argTypes.ToArray();
            this.LocalFieldNames = argNames.ToArray();
            this.LocalFieldIndexByName = new Dictionary<string, int>();
            for (int i = this.LocalFieldNames.Length - 1; i >= 0; --i)
            {
                string argName = this.LocalFieldNames[i].Value;
                this.LocalFieldIndexByName[argName] = i;
            }
        }

        public void ResolveParentChain(
            Dictionary<string, StructDefinition> structDefinitions,
            Dictionary<StructDefinition, int> cycleCheck)
        {
            int resolutionStatus = cycleCheck[this];
            if (resolutionStatus == 2) return; // this has already been resolved
            if (resolutionStatus == 1) throw new ParserException(this.FirstToken, "The parent chain for this struct has a cycle.");
            cycleCheck[this] = 1;
            if (this.ParentName != null)
            {
                if (!structDefinitions.ContainsKey(this.ParentName.Value))
                {
                    throw new ParserException(this.ParentName, "There is no struct by the name of '" + this.ParentName.Value + "'");
                }
                this.Parent = structDefinitions[this.ParentName.Value];
                this.Parent.ResolveParentChain(structDefinitions, cycleCheck);
            }
            cycleCheck[this] = 2;
        }

        public void ResolveInheritedFields()
        {
            if (this.FlatFieldNames == null)
            {
                if (this.Parent != null)
                {
                    this.Parent.ResolveInheritedFields();
                    List<Token> flatFieldNames = new List<Token>(this.Parent.FlatFieldNames);
                    List<PType> flatFieldTypes = new List<PType>(this.Parent.FlatFieldTypes);
                    for (int i = 0; i < this.LocalFieldNames.Length; ++i)
                    {
                        flatFieldNames.Add(this.LocalFieldNames[i]);
                        flatFieldTypes.Add(this.LocalFieldTypes[i]);
                    }
                    this.FlatFieldNames = flatFieldNames.ToArray();
                    this.FlatFieldTypes = flatFieldTypes.ToArray();
                    this.FlatFieldIndexByName = new Dictionary<string, int>();
                    for (int i = 0; i < this.FlatFieldNames.Length; ++i)
                    {
                        string name = this.FlatFieldNames[i].Value;
                        if (this.FlatFieldIndexByName.ContainsKey(name))
                        {
                            throw new ParserException(this.FlatFieldNames[i], "This struct field hides an inhereited definition of '" + name + "'");
                        }
                        this.FlatFieldIndexByName[name] = i;
                    }
                }
                else
                {
                    this.FlatFieldNames = this.LocalFieldNames;
                    this.FlatFieldTypes = this.LocalFieldTypes;
                    this.FlatFieldIndexByName = this.LocalFieldIndexByName;
                }
            }
        }
    }
}
