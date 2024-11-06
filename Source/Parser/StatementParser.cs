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

        public List<Statement> ParseCodeBlock(TokenStream tokens, bool curlyBracesRequired)
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

            List<Statement> code = new List<Statement>();
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

        private void ParseCodeLine(List<Statement> codeOut, TokenStream tokens)
        {
            Statement line = ParseStatement(tokens, false);
            Statement[] lines = null;
            if (line is ExpressionAsStatement)
            {
                lines = ((ExpressionAsStatement)line).ImmediateResolveMaybe(this.parser);
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

        public Statement ParseStatement(TokenStream tokens, bool isForLoop)
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
                return new ExpressionAsStatement(expression);
            }

            if (isForLoop && (tokens.IsNext(";") || tokens.IsNext(",") || tokens.IsNext(")")))
            {
                return new ExpressionAsStatement(expression);
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

        public Statement[] ParseImportedCode(Token importToken, string path)
        {
            string code = this.parser.LoadCode(importToken, path);
            TokenStream tokens = new TokenStream(Tokenizer.Tokenize(path, code));
            List<Statement> output = new List<Statement>();
            while (tokens.HasMore)
            {
                ParseCodeLine(output, tokens);
            }
            return output.ToArray();
        }

        public Statement ParseIfStatement(TokenStream tokens)
        {
            Token ifToken = tokens.PopExpected("if");
            tokens.PopExpected("(");
            Expression condition = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(")");
            IList<Statement> ifCode = ParseCodeBlock(tokens, false);
            Token elseToken = null;
            IList<Statement> elseCode = [];
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
                    return new StatementBatch(ifToken, (bool)ic.Value ? ifCode : elseCode);
                }
            }

            return new IfStatement(ifToken, condition, ifCode, elseToken, elseCode);
        }

        public ForLoop ParseForLoop(TokenStream tokens)
        {
            Token forToken = tokens.PopExpected("for");
            List<Statement> initCode = new List<Statement>();
            Expression condition = null;
            List<Statement> stepCode = new List<Statement>();
            tokens.PopExpected("(");
            if (!tokens.PopIfPresent(";"))
            {
                initCode.Add(ParseStatement(tokens, true));
                while (tokens.PopIfPresent(","))
                {
                    initCode.Add(ParseStatement(tokens, true));
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
                stepCode.Add(ParseStatement(tokens, true));
                while (tokens.PopIfPresent(","))
                {
                    stepCode.Add(ParseStatement(tokens, true));
                }
                tokens.PopExpected(")");
            }

            List<Statement> code = ParseCodeBlock(tokens, false);
            return new ForLoop(forToken, initCode, condition, stepCode, code);
        }

        public WhileLoop ParseWhileLoop(TokenStream tokens)
        {
            Token whileToken = tokens.PopExpected("while");
            tokens.PopExpected("(");
            Expression condition = this.ExpressionParser.ParseExpression(tokens);
            tokens.PopExpected(")");
            List<Statement> code = ParseCodeBlock(tokens, false);
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

                List<Statement> chunkCode = new List<Statement>();
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
            Statement stmnt = ParseStatement(tokens, false);
            VariableDeclaration assignment = stmnt as VariableDeclaration;
            if (assignment == null)
            {
                throw new ParserException(newToken, "Expected an assignment here.");
            }
            assignment.FirstToken = newToken;
            return assignment;
        }

    }
}
