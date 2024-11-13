using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;

namespace Pastel.Parser
{
    internal class EntityParser
    {
        private PastelParser parser;
        public EntityParser(PastelParser parser)
        {
            this.parser = parser;
        }

        public ExpressionParser ExpressionParser { get { return this.parser.ExpressionParser; } }
        public StatementParser StatementParser { get { return this.parser.StatementParser; } }

        public ICompilationEntity[] ParseText(string filename, string text)
        {
            TokenStream tokens = new TokenStream(Tokenizer.Tokenize(filename, text));
            List<ICompilationEntity> output = new List<ICompilationEntity>();
            while (tokens.HasMore)
            {
                switch (tokens.PeekValue())
                {
                    case "enum":
                        output.Add(ParseEnumDefinition(tokens));
                        break;

                    case "const":
                        output.Add(ParseConstDefinition(tokens));
                        break;

                    case "struct":
                        output.Add(ParseStructDefinition(tokens));
                        break;

                    case "@":
                        Token atToken = tokens.Pop();
                        ICompilationEntity[] inlinedEntities = this.ParseTopLevelInlineImport(atToken, tokens);
                        output.AddRange(inlinedEntities);
                        break;

                    default:
                        output.Add(ParseFunctionDefinition(tokens));
                        break;
                }
            }
            return output.ToArray();
        }

        public VariableDeclaration ParseConstDefinition(TokenStream tokens)
        {
            Token constToken = tokens.PopExpected("const");
            VariableDeclaration assignment = this.StatementParser.ParseAssignmentWithNewFirstToken(constToken, tokens);
            assignment.IsConstant = true;
            return assignment;
        }

        public EnumDefinition ParseEnumDefinition(TokenStream tokens)
        {
            Token enumToken = tokens.PopExpected("enum");
            Token nameToken = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid name for an enum.");
            EnumDefinition enumDef = new EnumDefinition(enumToken, nameToken, this.parser.Context);
            this.parser.ActiveEntity = enumDef;
            List<Token> valueTokens = new List<Token>();
            List<Expression> valueExpressions = new List<Expression>();
            tokens.PopExpected("{");
            bool first = true;
            while (!tokens.PopIfPresent("}"))
            {
                if (!first)
                {
                    tokens.PopExpected(",");
                }
                else
                {
                    first = false;
                }

                if (tokens.PopIfPresent("}"))
                {
                    break;
                }

                Token valueToken = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid name for a enum value.");
                valueTokens.Add(valueToken);
                if (tokens.PopIfPresent("="))
                {
                    Expression value = this.ExpressionParser.ParseExpression(tokens);
                    valueExpressions.Add(value);
                }
                else
                {
                    valueExpressions.Add(null);
                }
            }

            enumDef.InitializeValues(valueTokens, valueExpressions);
            this.parser.ActiveEntity = null;
            return enumDef;
        }

        public StructDefinition ParseStructDefinition(TokenStream tokens)
        {
            Token structToken = tokens.PopExpected("struct");
            Token nameToken = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid struct name");

            Token parentName = null;
            if (tokens.PopIfPresent("extends"))
            {
                parentName = tokens.PopIdentifier();
            }

            List<PType> structFieldTypes = new List<PType>();
            List<Token> structFieldNames = new List<Token>();
            tokens.PopExpected("{");
            while (!tokens.PopIfPresent("}"))
            {
                PType fieldType = PType.Parse(tokens);
                Token fieldName = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid struct field name");
                structFieldTypes.Add(fieldType);
                structFieldNames.Add(fieldName);
                tokens.PopExpected(";");
            }
            return new StructDefinition(structToken, nameToken, structFieldTypes, structFieldNames, parentName, this.parser.Context);
        }

        public FunctionDefinition ParseFunctionDefinition(TokenStream tokens)
        {
            PType returnType = PType.Parse(tokens);
            Token nameToken = ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Expected function name");
            tokens.PopExpected("(");
            List<PType> argTypes = new List<PType>();
            List<Token> argNames = new List<Token>();
            while (!tokens.PopIfPresent(")"))
            {
                if (argTypes.Count > 0) tokens.PopExpected(",");
                argTypes.Add(PType.Parse(tokens));
                argNames.Add(ExpressionParser.EnsureTokenIsValidName(tokens.Pop(), "Invalid function arg name"));
            }
            FunctionDefinition funcDef = new FunctionDefinition(nameToken, returnType, argTypes, argNames);
            this.parser.ActiveEntity = funcDef;
            List<Statement> code = this.StatementParser.ParseCodeBlock(tokens, true);
            this.parser.ActiveEntity = null;
            funcDef.Code = code.ToArray();
            return funcDef;
        }

        private ICompilationEntity[] ParseTopLevelInlineImport(Token atToken, TokenStream tokens)
        {
            string sourceFile = null;
            string functionName = tokens.PeekValue();
            switch (functionName)
            {
                case "import":
                    tokens.PopExpected("import");
                    tokens.PopExpected("(");
                    Token stringToken = tokens.Pop();
                    tokens.PopExpected(")");
                    tokens.PopExpected(";");
                    sourceFile = CodeUtil.ConvertStringTokenToValue(stringToken.Value);
                    break;

                case "importIfTrue":
                case "importIfFalse":
                    tokens.Pop();
                    tokens.PopExpected("(");
                    Token constantExpression = tokens.Pop();
                    string constantValue = CodeUtil.ConvertStringTokenToValue(constantExpression.Value);
                    tokens.PopExpected(",");
                    Token pathToken = tokens.Pop();
                    tokens.PopExpected(")");
                    tokens.PopExpected(";");
                    object value = this.parser.GetConstant(constantValue, false);
                    if (!(value is bool)) value = false;
                    bool valueBool = (bool)value;
                    if (functionName == "importIfFalse")
                    {
                        valueBool = !valueBool;
                    }

                    if (valueBool)
                    {
                        sourceFile = CodeUtil.ConvertStringTokenToValue(pathToken.Value);
                    }
                    break;

                default:
                    // intentional crash...
                    tokens.PopExpected("import");
                    break;
            }

            if (sourceFile != null)
            {
                string code = this.parser.LoadCode(atToken, sourceFile);
                return ParseText(sourceFile, code);
            }

            return [];
        }
    }
}
