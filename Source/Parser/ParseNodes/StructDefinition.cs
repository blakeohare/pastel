using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
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
            Context = context;
            FirstToken = structToken;
            NameToken = name;
            ParentName = parentName;
            LocalFieldTypes = argTypes.ToArray();
            LocalFieldNames = argNames.ToArray();
            LocalFieldIndexByName = new Dictionary<string, int>();
            for (int i = LocalFieldNames.Length - 1; i >= 0; --i)
            {
                string argName = LocalFieldNames[i].Value;
                LocalFieldIndexByName[argName] = i;
            }
        }

        public void ResolveParentChain(
            Dictionary<string, StructDefinition> structDefinitions,
            Dictionary<StructDefinition, int> cycleCheck)
        {
            int resolutionStatus = cycleCheck[this];
            if (resolutionStatus == 2) return; // this has already been resolved
            if (resolutionStatus == 1) throw new ParserException(FirstToken, "The parent chain for this struct has a cycle.");
            cycleCheck[this] = 1;
            if (ParentName != null)
            {
                if (!structDefinitions.ContainsKey(ParentName.Value))
                {
                    throw new ParserException(ParentName, "There is no struct by the name of '" + ParentName.Value + "'");
                }
                Parent = structDefinitions[ParentName.Value];
                Parent.ResolveParentChain(structDefinitions, cycleCheck);
            }
            cycleCheck[this] = 2;
        }

        public void ResolveInheritedFields()
        {
            if (FlatFieldNames == null)
            {
                if (Parent != null)
                {
                    Parent.ResolveInheritedFields();
                    List<Token> flatFieldNames = new List<Token>(Parent.FlatFieldNames);
                    List<PType> flatFieldTypes = new List<PType>(Parent.FlatFieldTypes);
                    for (int i = 0; i < LocalFieldNames.Length; ++i)
                    {
                        flatFieldNames.Add(LocalFieldNames[i]);
                        flatFieldTypes.Add(LocalFieldTypes[i]);
                    }
                    FlatFieldNames = flatFieldNames.ToArray();
                    FlatFieldTypes = flatFieldTypes.ToArray();
                    FlatFieldIndexByName = new Dictionary<string, int>();
                    for (int i = 0; i < FlatFieldNames.Length; ++i)
                    {
                        string name = FlatFieldNames[i].Value;
                        if (FlatFieldIndexByName.ContainsKey(name))
                        {
                            throw new ParserException(FlatFieldNames[i], "This struct field hides an inhereited definition of '" + name + "'");
                        }
                        FlatFieldIndexByName[name] = i;
                    }
                }
                else
                {
                    FlatFieldNames = LocalFieldNames;
                    FlatFieldTypes = LocalFieldTypes;
                    FlatFieldIndexByName = LocalFieldIndexByName;
                }
            }
        }
    }
}
