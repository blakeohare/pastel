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
            this.FirstToken = enumToken;
            this.NameToken = nameToken;
            this.Context = context;
        }

        internal void InitializeValues(IList<Token> memberNameTokens, IList<Expression> valueExpressions)
        {
            this.ValueTokens = memberNameTokens.ToArray();

            HashSet<string> dupCheck = [];
            foreach (Token memToken in memberNameTokens)
            {
                string name = memToken.Value;
                if (dupCheck.Contains(name))
                {
                    throw new TestedParserException(
                        memToken,
                        "The enum '" + this.NameToken.Value + "' has multiple definitions of '" + name + "'");
                }
                dupCheck.Add(name);
            }

            this.ValuesByName = new Dictionary<string, Expression>();
            int length = this.ValueTokens.Length;
            int highestValue = 0;
            bool highestSet = false;
            List<string> autoAssignMe = new List<string>();
            for (int i = 0; i < length; ++i)
            {
                string name = this.ValueTokens[i].Value;
                Expression expression = valueExpressions[i];
                if (expression == null)
                {
                    autoAssignMe.Add(name);
                }
                else
                {
                    this.ValuesByName[name] = expression;

                    if (expression is InlineConstant ic)
                    {
                        if (ic.Value is int icVal)
                        {
                            if (!highestSet || icVal > highestValue)
                            {
                                highestValue = icVal;
                                highestSet = true;
                            }
                        }
                        else
                        {
                            throw new UNTESTED_ParserException(expression.FirstToken, "Only integers are allowed as enum values.");
                        }
                    }
                    else
                    {
                        this.UnresolvedValues.Add(name);
                    }
                }
            }

            // anything that doesn't have a value assigned to it, auto-assign incrementally from the highest value provided.
            foreach (string name in autoAssignMe)
            {
                this.ValuesByName[name] = InlineConstant.OfInteger(highestValue++, this.FirstToken, this);
            }
        }

        public InlineConstant GetValue(Token name)
        {
            Expression value;
            if (this.ValuesByName.TryGetValue(name.Value, out value))
            {
                return (InlineConstant)value;
            }
            throw new UNTESTED_ParserException(
                name, 
                "The enum value '" + name.Value + "' does not exist in the definition of '" + this.NameToken.Value + "'.");
        }

        internal void DoConstantResolutions(HashSet<string> cycleDetection, Resolver resolver)
        {
            string prefix = this.NameToken.Value + ".";
            foreach (string name in this.UnresolvedValues)
            {
                string cycleKey = prefix + name;
                if (cycleDetection.Contains(cycleKey))
                {
                    throw new UNTESTED_ParserException(
                        this.FirstToken, 
                        "This enum has a cycle in its value declarations in '" + name + "'");
                }
                cycleDetection.Add(cycleKey);

                InlineConstant ic = this.ValuesByName[cycleKey].DoConstantResolution(cycleDetection, resolver);
                if (!(ic.Value is int))
                {
                    throw new UNTESTED_ParserException(
                        ic.FirstToken,
                        "Enum values must resolve into integers.");
                }

                this.ValuesByName[cycleKey] = ic;
                cycleDetection.Remove(cycleKey);
            }
        }
    }
}
