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

        private static HashSet<string> primitiveTypes = new HashSet<string>()
        {
            "int",
            "char",
            "double",
            "bool",
            "void",
            "string",
            "object",
        };

        private static PType TryParseImpl(TokenStream tokens)
        {
            Token token = tokens.Pop();
            if (token.Type != TokenType.ALPHANUMS) return null;
            if (primitiveTypes.Contains(token.Value))
            {
                return new PType(token, token.Value);
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
