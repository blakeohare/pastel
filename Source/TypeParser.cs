using Pastel.ParseNodes;
using System.Collections.Generic;

namespace Pastel
{
    internal static class TypeParser
    {
        public static PType TryParse(TokenStream tokens)
        {
            TokenStreamState state = tokens.SnapshotState();
            tokens.DisableMultiCharTokens();
            PType type = TryParseImpl(tokens);
            tokens.EnableMultiCharTokens();
            if (type == null)
            {
                tokens.RestoreState(state);
                return null;
            }
            return type;
        }

        public static PType Parse(TokenStream tokens)
        {
            PType type = TryParse(tokens);
            if (type == null)
            {
                throw new ParserException(tokens.Peek(), "Expected a type name.");
            }
            return type;
        }

        private static Dictionary<string, PType> primitiveTypes = new Dictionary<string, PType>()
        {
            { "int", PType.INT },
            { "char", PType.CHAR },
            { "double", PType.DOUBLE },
            { "bool", PType.BOOL },
            { "void", PType.VOID },
            { "string", PType.STRING },
            { "object", PType.OBJECT },
        };

        private static PType TryParseImpl(TokenStream tokens)
        {
            Token token = tokens.Pop();
            if (token.Type != TokenType.ALPHANUMS) return null;
            if (primitiveTypes.ContainsKey(token.Value))
            {
                return primitiveTypes[token.Value];
            }

            TokenStreamState state = tokens.SnapshotState();
            if (!tokens.PopIfPresent("<"))
            {
                return new PType(token, token.Value);
            }

            PType firstGeneric = TryParseImpl(tokens);
            List<PType> generics = new List<PType>() { firstGeneric };
            while (tokens.PopIfPresent(","))
            {
                PType nextGeneric = TryParseImpl(tokens);
                if (nextGeneric == null)
                {
                    tokens.RestoreState(state);
                    return new PType(token, token.Value);
                }
                generics.Add(nextGeneric);
            }
            if (!tokens.PopIfPresent(">"))
            {
                tokens.RestoreState(state);
                return new PType(token, token.Value);
            }

            return new PType(token, token.Value, generics);
        }
    }
}
