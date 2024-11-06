using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;

namespace Pastel.Parser
{
    internal class ExpressionParser
    {
        private PastelParser parser;
        public ExpressionParser(PastelParser parser)
        {
            this.parser = parser;
        }

        public StatementParser StatementParser { get { return this.parser.StatementParser; } }
        public EntityParser EntityParser { get { return this.parser.EntityParser; } }

        public Expression ParseExpression(TokenStream tokens)
        {
            return ParseBooleanCombination(tokens);
        }

        private Expression ParseOpChain(TokenStream tokens, HashSet<string> opsToLookFor, Func<TokenStream, Expression> fp)
        {
            Expression expression = fp(tokens);
            if (opsToLookFor.Contains(tokens.PeekValue()))
            {
                List<Expression> expressions = new List<Expression>() { expression };
                List<Token> ops = new List<Token>();
                while (opsToLookFor.Contains(tokens.PeekValue()))
                {
                    ops.Add(tokens.Pop());
                    expressions.Add(fp(tokens));
                }
                return new OpChain(expressions, ops);
            }
            return expression;
        }

        private static readonly HashSet<string> OPS_BOOLEAN_COMBINATION = new HashSet<string>(new string[] { "&&", "||" });
        private Expression ParseBooleanCombination(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_BOOLEAN_COMBINATION, ParseBitwise);
        }

        private static readonly HashSet<string> OPS_BITWISE = new HashSet<string>(new string[] { "&", "|", "^" });
        private Expression ParseBitwise(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_BITWISE, ParseEquality);
        }

        private static readonly HashSet<string> OPS_EQUALITY = new HashSet<string>(new string[] { "==", "!=" });
        private Expression ParseEquality(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_EQUALITY, ParseInequality);
        }

        private static readonly HashSet<string> OPS_INEQUALITY = new HashSet<string>(new string[] { "<", ">", "<=", ">=" });
        private Expression ParseInequality(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_INEQUALITY, ParseBitShift);
        }

        private Expression ParseBitShift(TokenStream tokens)
        {
            Expression left = ParseAddition(tokens);
            Token bitShift = tokens.PopBitShiftHackIfPresent();
            if (bitShift != null)
            {
                Expression right = ParseAddition(tokens);
                return new OpChain(new Expression[] { left, right }, new Token[] { bitShift });
            }
            return left;
        }

        private static readonly HashSet<string> OPS_ADDITION = new HashSet<string>(new string[] { "+", "-" });
        private Expression ParseAddition(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_ADDITION, ParseMultiplication);
        }

        private static readonly HashSet<string> OPS_MULTIPLICATION = new HashSet<string>(new string[] { "*", "/", "%" });
        private Expression ParseMultiplication(TokenStream tokens)
        {
            return ParseOpChain(tokens, OPS_MULTIPLICATION, ParsePrefixes);
        }

        private Expression ParsePrefixes(TokenStream tokens)
        {
            string next = tokens.PeekValue();
            if (next == "-" || next == "!")
            {
                Token op = tokens.Pop();
                Expression root = ParsePrefixes(tokens);
                return new UnaryOp(op, root);
            }

            return ParseIncrementOrCast(tokens);
        }

        private Expression ParseIncrementOrCast(TokenStream tokens)
        {
            Token prefix = null;
            if (tokens.IsNext("++") || tokens.IsNext("--"))
            {
                prefix = tokens.Pop();
            }

            Expression expression;
            int tokenIndex = tokens.SnapshotState();
            if (prefix == null &&
                tokens.PeekValue() == "(" &&
                IsValidName(tokens.PeekAhead(1)))
            {
                Token parenthesis = tokens.Pop();
                PType castType = PType.TryParse(tokens);
                if (castType != null && tokens.PopIfPresent(")"))
                {
                    expression = ParseIncrementOrCast(tokens);
                    return new CastExpression(parenthesis, castType, expression);
                }
                tokens.RevertState(tokenIndex);
            }

            expression = ParseEntity(tokens);

            if (prefix != null)
            {
                expression = new InlineIncrement(prefix, prefix, expression, true);
            }

            if (tokens.IsNext("++") || tokens.IsNext("--"))
            {
                expression = new InlineIncrement(expression.FirstToken, tokens.Pop(), expression, false);
            }

            return expression;
        }

        private Expression ParseEntity(TokenStream tokens)
        {
            if (tokens.IsNext("new"))
            {
                Token newToken = tokens.Pop();
                PType typeToConstruct = PType.Parse(tokens);
                if (!tokens.IsNext("(")) tokens.PopExpected("("); // intentional error if not present.
                Expression constructorReference = new ConstructorReference(newToken, typeToConstruct, this.parser.ActiveEntity);
                return ParseEntityChain(constructorReference, tokens);
            }

            if (tokens.PopIfPresent("("))
            {
                Expression expression = ParseExpression(tokens);
                tokens.PopExpected(")");
                return ParseOutEntitySuffixes(tokens, expression);
            }

            Expression root = ParseEntityRoot(tokens);
            return ParseOutEntitySuffixes(tokens, root);
        }

        private Expression ParseOutEntitySuffixes(TokenStream tokens, Expression root)
        {
            while (true)
            {
                switch (tokens.PeekValue())
                {
                    case ".":
                    case "[":
                    case "(":
                        root = ParseEntityChain(root, tokens);
                        break;
                    default:
                        return root;
                }
            }
        }

        private Expression ParseEntityRoot(TokenStream tokens)
        {
            string next = tokens.PeekValue();
            switch (next)
            {
                case "true":
                case "false":
                    return new InlineConstant(PType.BOOL, tokens.Pop(), next == "true", this.parser.ActiveEntity);
                case "null":
                    return new InlineConstant(PType.NULL, tokens.Pop(), null, this.parser.ActiveEntity);
                case ".":
                    Token dotToken = tokens.Pop();
                    Token numToken = tokens.Pop();
                    EnsureInteger(tokens.Pop(), false, false);
                    string strValue = "0." + numToken.Value;
                    double dblValue;
                    if (!numToken.HasWhitespacePrefix && double.TryParse(strValue, out dblValue))
                    {
                        return new InlineConstant(PType.DOUBLE, dotToken, dblValue, this.parser.ActiveEntity);
                    }
                    throw new ParserException(dotToken, "Unexpected '.'");

                default: break;
            }
            char firstChar = next[0];
            switch (firstChar)
            {
                case '\'':
                    return new InlineConstant(PType.CHAR, tokens.Pop(), CodeUtil.ConvertStringTokenToValue(next), this.parser.ActiveEntity);
                case '"':
                    return new InlineConstant(PType.STRING, tokens.Pop(), CodeUtil.ConvertStringTokenToValue(next), this.parser.ActiveEntity);
                case '@':
                    Token atToken = tokens.PopExpected("@");
                    Token compileTimeFunction = EnsureTokenIsValidName(tokens.Pop(), "Expected compile time function name.");
                    if (!tokens.IsNext("(")) tokens.PopExpected("(");
                    return new CompileTimeFunctionReference(atToken, compileTimeFunction, this.parser.ActiveEntity);
            }

            if (firstChar >= '0' && firstChar <= '9')
            {
                Token numToken = tokens.Pop();
                if (tokens.IsNext("."))
                {
                    EnsureInteger(numToken, false, false);
                    Token dotToken = tokens.Pop();
                    if (dotToken.HasWhitespacePrefix) throw new ParserException(dotToken, "Unexpected '.'");
                    Token decimalToken = tokens.Pop();
                    EnsureInteger(decimalToken, false, false);
                    if (decimalToken.HasWhitespacePrefix) throw new ParserException(decimalToken, "Unexpected '" + decimalToken.Value + "'");
                    double dblValue;
                    if (double.TryParse(numToken.Value + "." + decimalToken.Value, out dblValue))
                    {
                        return new InlineConstant(PType.DOUBLE, numToken, dblValue, this.parser.ActiveEntity);
                    }
                    throw new ParserException(decimalToken, "Unexpected token.");
                }
                else
                {
                    int numValue = EnsureInteger(numToken, true, true);
                    return new InlineConstant(PType.INT, numToken, numValue, this.parser.ActiveEntity);
                }
            }

            if (tokens.IsNext("this"))
            {
                return new ThisExpression(tokens.Pop(), this.parser.ActiveEntity);
            }

            if (IsValidName(tokens.PeekValue()))
            {
                return new Variable(tokens.Pop(), this.parser.ActiveEntity);
            }

            throw new ParserException(tokens.Peek(), "Unrecognized expression.");
        }

        private Expression ParseEntityChain(Expression root, TokenStream tokens)
        {
            switch (tokens.PeekValue())
            {
                case ".":
                    Token dotToken = tokens.Pop();
                    Token field = EnsureTokenIsValidName(tokens.Pop(), "Invalid field name");
                    return new DotField(root, dotToken, field);
                case "[":
                    Token openBracket = tokens.Pop();
                    Expression index = ParseExpression(tokens);
                    tokens.PopExpected("]");
                    return new BracketIndex(root, openBracket, index);
                case "(":
                    Token openParen = tokens.Pop();
                    List<Expression> args = new List<Expression>();
                    while (!tokens.PopIfPresent(")"))
                    {
                        if (args.Count > 0) tokens.PopExpected(",");
                        args.Add(ParseExpression(tokens));
                    }
                    return new FunctionInvocation(root, openParen, args).MaybeImmediatelyResolve(this.parser);
                default:
                    throw new Exception();
            }
        }

        // This function became really weird for legitimate reasons, but deseparately needs to be written (or rather,
        // the places where this is called need to be rewritten).
        private int EnsureInteger(Token token, bool allowHex, bool calculateValue)
        {
            string value = token.Value;
            char c;
            if (allowHex && value.StartsWith("0x"))
            {
                value = value.Substring(2).ToLower();
                int num = 0;
                for (int i = 0; i < value.Length; ++i)
                {
                    num *= 16;
                    c = value[i];
                    if (c >= '0' && c <= '9')
                    {
                        num += c - '0';
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        num += c - 'a' + 10;
                    }
                }
                return calculateValue ? num : 0;
            }
            for (int i = value.Length - 1; i >= 0; --i)
            {
                c = value[i];
                if (c < '0' || c > '9')
                {
                    throw new ParserException(token, "Expected number");
                }
            }
            if (!calculateValue) return 0;
            int output;
            if (!int.TryParse(value, out output))
            {
                throw new ParserException(token, "Integer is too big.");
            }
            return output;
        }

        // TODO: Token Types to answer this question
        public static Token EnsureTokenIsValidName(Token token, string errorMessage)
        {
            if (IsValidName(token.Value))
            {
                return token;
            }
            throw new ParserException(token, errorMessage);
        }

        public static bool IsValidName(string value)
        {
            char c;
            for (int i = value.Length - 1; i >= 0; --i)
            {
                c = value[i];
                if (!(
                    c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9' && i > 0 ||
                    c == '_'))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
