using Pastel.Parser.ParseNodes;
using System.Collections.Generic;

namespace Pastel.Parser
{
    internal class StatementParser
    {
        private PastelParser parser;
        public StatementParser(PastelParser parser)
        {
            this.parser = parser;
        }

        public ExpressionParser ExpressionParser { get { return this.parser.ExpressionParser; } }
        public EntityParser EntityParser { get { return this.parser.EntityParser; } }

        public List<Executable> ParseCodeBlock(TokenStream tokens, bool curlyBracesRequired)
        {
            bool hasCurlyBrace = false;
            if (curlyBracesRequired)
            {
                hasCurlyBrace = true;
                tokens.PopExpected("{");
            }
            else
            {
                hasCurlyBrace = tokens.PopIfPresent("{");
            }

            List<Executable> code = new List<Executable>();
            if (hasCurlyBrace)
            {
                while (!tokens.PopIfPresent("}"))
                {
                    ParseCodeLine(code, tokens);
                }
            }
            else
            {
                ParseCodeLine(code, tokens);
            }

            return code;
        }

        private void ParseCodeLine(List<Executable> codeOut, TokenStream tokens)
        {
            Executable line = ParseExecutable(tokens, false);
            Executable[] lines = null;
            if (line is ExpressionAsExecutable)
            {
                lines = ((ExpressionAsExecutable)line).ImmediateResolveMaybe(this.parser);
            }

            if (lines == null)
            {
                codeOut.Add(line);
            }
            else
            {
                codeOut.AddRange(lines);
            }
        }

        public Executable ParseExecutable(TokenStream tokens, bool isForLoop)
        {
            if (!isForLoop)
            {
                switch (tokens.PeekValue())
                {
                    case "if": return ParseIfStatement(tokens);
                    case "for": return ParseForLoop(tokens);
                    case "while": return ParseWhileLoop(tokens);
                    case "switch": return ParseSwitchStatement(tokens);
                    case "break": return ParseBreak(tokens);
                    case "return": return ParseReturn(tokens);
                }
            }

            int currentState = tokens.SnapshotState();
            PType assignmentType = PType.TryParse(tokens);
            if (assignmentType != null && tokens.HasMore && ExpressionParser.IsValidName(tokens.PeekValue()))
            {
                Token variableName = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid variable name");

                if (tokens.PopIfPresent(";"))
                {
                    return new VariableDeclaration(assignmentType, variableName, null, null, this.parser.Context);
                }

                Token equalsToken = tokens.PopExpected("=");
                Expression assignmentValue = this.ExpressionParser.ParseExpression(tokens);
                if (!isForLoop)
                {
                    tokens.PopExpected(";");
                }
                return new VariableDeclaration(assignmentType, variableName, equalsToken, assignmentValue, this.parser.Context);
            }
            tokens.RevertState(currentState);

            Expression expression = this.ExpressionParser.ParseExpression(tokens);

            if (!isForLoop && tokens.PopIfPresent(";"))
            {
                return new ExpressionAsExecutable(expression);
            }

            if (isForLoop && (tokens.IsNext(";") || tokens.IsNext(",") || tokens.IsNext(")")))
            {
                return new ExpressionAsExecutable(expression);
            }

            if (PastelParser.OP_TOKENS.Contains(tokens.PeekValue()))
            {
                Token opToken = tokens.Pop();
                Expression assignmentValue = this.ExpressionParser.ParseExpression(tokens);

                if (!isForLoop && tokens.PopIfPresent(";"))
                {
                    return new Assignment(expression, opToken, assignmentValue);
                }

                if (isForLoop && (tokens.IsNext(";") || tokens.IsNext(",") || tokens.IsNext(")")))
                {
                    return new Assignment(expression, opToken, assignmentValue);
                }
            }

            tokens.PopExpected(";"); // Exhausted possibilities. This will crash intentionally.
            return null; // unreachable code
        }

        public Executable[] ParseImportedCode(Token importToken, string path)
        {
            string code = this.parser.LoadCode(importToken, path);
            TokenStream tokens = new TokenStream(Tokenizer.Tokenize(path, code));
            List<Executable> output = new List<Executable>();
            while (tokens.HasMore)
            {
                ParseCodeLine(output, tokens);
            }
            return output.ToArray();
        }

        public Executable ParseIfStatement(TokenStream tokens)
        {
            Token ifToken = tokens.PopExpected("if");
            tokens.PopExpected("(");
            Expression condition = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(")");
            IList<Executable> ifCode = ParseCodeBlock(tokens, false);
            Token elseToken = null;
            IList<Executable> elseCode = [];
            if (tokens.IsNext("else"))
            {
                elseToken = tokens.Pop();
                elseCode = ParseCodeBlock(tokens, false);
            }

            if (condition is InlineConstant)
            {
                InlineConstant ic = (InlineConstant)condition;
                if (ic.Value is bool)
                {
                    return new ExecutableBatch(ifToken, (bool)ic.Value ? ifCode : elseCode);
                }
            }

            return new IfStatement(ifToken, condition, ifCode, elseToken, elseCode);
        }

        public ForLoop ParseForLoop(TokenStream tokens)
        {
            Token forToken = tokens.PopExpected("for");
            List<Executable> initCode = new List<Executable>();
            Expression condition = null;
            List<Executable> stepCode = new List<Executable>();
            tokens.PopExpected("(");
            if (!tokens.PopIfPresent(";"))
            {
                initCode.Add(ParseExecutable(tokens, true));
                while (tokens.PopIfPresent(","))
                {
                    initCode.Add(ParseExecutable(tokens, true));
                }
                tokens.PopExpected(";");
            }

            if (!tokens.PopIfPresent(";"))
            {
                condition = this.ExpressionParser.ParseExpression(tokens);
                tokens.PopExpected(";");
            }

            if (!tokens.PopIfPresent(")"))
            {
                stepCode.Add(ParseExecutable(tokens, true));
                while (tokens.PopIfPresent(","))
                {
                    stepCode.Add(ParseExecutable(tokens, true));
                }
                tokens.PopExpected(")");
            }

            List<Executable> code = ParseCodeBlock(tokens, false);
            return new ForLoop(forToken, initCode, condition, stepCode, code);
        }

        public WhileLoop ParseWhileLoop(TokenStream tokens)
        {
            Token whileToken = tokens.PopExpected("while");
            tokens.PopExpected("(");
            Expression condition = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(")");
            List<Executable> code = ParseCodeBlock(tokens, false);
            return new WhileLoop(whileToken, condition, code);
        }

        public SwitchStatement ParseSwitchStatement(TokenStream tokens)
        {
            Token switchToken = tokens.PopExpected("switch");
            tokens.PopExpected("(");
            Expression condition = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(")");
            tokens.PopExpected("{");

            List<SwitchStatement.SwitchChunk> chunks = new List<SwitchStatement.SwitchChunk>();
            while (!tokens.PopIfPresent("}"))
            {
                List<Expression> caseExpressions = new List<Expression>();
                List<Token> caseAndDefaultTokens = new List<Token>();
                bool thereAreCases = true;
                while (thereAreCases)
                {
                    switch (tokens.PeekValue())
                    {
                        case "case":
                            caseAndDefaultTokens.Add(tokens.Pop());
                            Expression caseExpression = this.ExpressionParser.ParseExpression(tokens);
                            tokens.PopExpected(":");
                            caseExpressions.Add(caseExpression);
                            break;

                        case "default":
                            caseAndDefaultTokens.Add(tokens.Pop());
                            tokens.PopExpected(":");
                            caseExpressions.Add(null);
                            break;

                        default:
                            thereAreCases = false;
                            break;
                    }
                }

                List<Executable> chunkCode = new List<Executable>();
                string next = tokens.PeekValue();
                while (next != "}" && next != "default" && next != "case")
                {
                    ParseCodeLine(chunkCode, tokens);
                    next = tokens.PeekValue();
                }

                chunks.Add(new SwitchStatement.SwitchChunk(caseAndDefaultTokens, caseExpressions, chunkCode));
            }

            return new SwitchStatement(switchToken, condition, chunks);
        }

        public BreakStatement ParseBreak(TokenStream tokens)
        {
            Token breakToken = tokens.PopExpected("break");
            tokens.PopExpected(";");
            return new BreakStatement(breakToken);
        }

        public ReturnStatement ParseReturn(TokenStream tokens)
        {
            Token returnToken = tokens.PopExpected("return");
            if (tokens.PopIfPresent(";"))
            {
                return new ReturnStatement(returnToken, null);
            }

            Expression expression = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(";");
            return new ReturnStatement(returnToken, expression);
        }

        public VariableDeclaration ParseAssignmentWithNewFirstToken(Token newToken, TokenStream tokens)
        {
            Executable executable = ParseExecutable(tokens, false);
            VariableDeclaration assignment = executable as VariableDeclaration;
            if (assignment == null)
            {
                throw new ParserException(newToken, "Expected an assignment here.");
            }
            assignment.FirstToken = newToken;
            return assignment;
        }

    }
}
