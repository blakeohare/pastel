using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class EnumDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.ENUM; } }

        public PastelContext Context { get; private set; }
        public Token FirstToken { get; set; }
        public Token NameToken { get; set; }
        public Token[] ValueTokens { get; set; }
        public Dictionary<string, Expression> ValuesByName { get; set; }

        public HashSet<string> UnresolvedValues = new HashSet<string>();

        public EnumDefinition(Token enumToken, Token nameToken, PastelContext context)
        {
            FirstToken = enumToken;
            NameToken = nameToken;
            Context = context;
        }

        internal void InitializeValues(IList<Token> valueTokens, IList<Expression> valueExpressions)
        {
            ValueTokens = valueTokens.ToArray();
            ValuesByName = new Dictionary<string, Expression>();
            int length = ValueTokens.Length;
            int highestValue = 0;
            bool highestSet = false;
            List<string> autoAssignMe = new List<string>();
            for (int i = 0; i < length; ++i)
            {
                string name = ValueTokens[i].Value;
                if (ValuesByName.ContainsKey(name))
                {
                    throw new ParserException(FirstToken, "The enum '" + NameToken.Value + "' has multiple definitions of '" + name + "'");
                }
                Expression expression = valueExpressions[i];
                if (expression == null)
                {
                    autoAssignMe.Add(name);
                }
                else
                {
                    ValuesByName[name] = expression;

                    if (expression is InlineConstant)
                    {
                        InlineConstant ic = (InlineConstant)expression;
                        if (ic.Value is int)
                        {
                            if (!highestSet || (int)ic.Value > highestValue)
                            {
                                highestValue = (int)ic.Value;
                                highestSet = true;
                            }
                        }
                        else
                        {
                            throw new ParserException(expression.FirstToken, "Only integers are allowed as enum values.");
                        }
                    }
                    else
                    {
                        UnresolvedValues.Add(name);
                    }
                }
            }

            // anything that doesn't have a value assigned to it, auto-assign incrementally from the highest value provided.
            foreach (string name in autoAssignMe)
            {
                ValuesByName[name] = new InlineConstant(PType.INT, FirstToken, highestValue++, this);
            }
        }

        public InlineConstant GetValue(Token name)
        {
            Expression value;
            if (ValuesByName.TryGetValue(name.Value, out value))
            {
                return (InlineConstant)value;
            }
            throw new ParserException(name, "The enum value '" + name.Value + "' does not exist in the definition of '" + NameToken.Value + "'.");
        }

        internal void DoConstantResolutions(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            string prefix = NameToken.Value + ".";
            foreach (string name in UnresolvedValues)
            {
                string cycleKey = prefix + name;
                if (cycleDetection.Contains(cycleKey))
                {
                    throw new ParserException(FirstToken, "This enum has a cycle in its value declarations in '" + name + "'");
                }
                cycleDetection.Add(cycleKey);

                InlineConstant ic = ValuesByName[cycleKey].DoConstantResolution(cycleDetection, compiler);
                if (!(ic.Value is int))
                {
                    throw new ParserException(ic.FirstToken, "Enum values must resolve into integers. This does not.");
                }

                ValuesByName[cycleKey] = ic;
                cycleDetection.Remove(cycleKey);
            }
        }
    }
}
