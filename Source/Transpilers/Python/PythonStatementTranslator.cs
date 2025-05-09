﻿using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers.Python
{
    internal class PythonStatementTranslator : AbstractStatementTranslator
    {
        public PythonStatementTranslator(TranspilerContext ctx) : base(ctx) { }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Value));
            sb.Append('\n');
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(dictionary));
            sb.Append(".get(");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
            sb.Append(", ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
            sb.Append(")\n");
        }

        public override void TranslateStatements(TranspilerContext sb, Statement[] statements)
        {
            if (statements.Length == 0)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("pass\n");
            }
            else
            {
                base.TranslateStatements(sb, statements);
            }
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break\n");
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(expression));
            sb.Append("\n");
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateIfStatementNoIndent(sb, ifStatement);
        }

        private void TranslateIfStatementNoIndent(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append("if ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(ifStatement.Condition));
            sb.Append(":\n");
            sb.TabDepth++;
            if (ifStatement.IfCode.Length == 0)
            {
                // ideally this should be optimized out at compile-time. TODO: throw instead and do that
                sb.Append(sb.CurrentTab);
                sb.Append("pass\n");
            }
            else
            {
                this.TranslateStatements(sb, ifStatement.IfCode);
            }
            sb.TabDepth--;

            Statement[] elseCode = ifStatement.ElseCode;

            if (elseCode.Length == 0) return;

            if (elseCode.Length == 1 && elseCode[0] is IfStatement)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("el");
                this.TranslateIfStatementNoIndent(sb, (IfStatement)elseCode[0]);
            }
            else
            {
                sb.Append(sb.CurrentTab);
                sb.Append("else:\n");
                sb.TabDepth++;
                this.TranslateStatements(sb, elseCode);
                sb.TabDepth--;
            }
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(returnStatement.Expression));
            }
            sb.Append("\n");
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            string functionName = this.transpilerCtx.CurrentFunctionDefinition.NameToken.Value;
            int switchId = this.transpilerCtx.SwitchCounter++;
            PythonFakeSwitchStatement fakeSwitchStatement = PythonFakeSwitchStatement.Build(switchStatement, switchId, functionName);

            sb.Append(sb.CurrentTab);
            sb.Append(fakeSwitchStatement.ConditionVariableName);
            sb.Append(" = ");
            sb.Append(fakeSwitchStatement.DictionaryGlobalName);
            sb.Append(".get(");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(switchStatement.Condition));
            sb.Append(", ");
            sb.Append(fakeSwitchStatement.DefaultId);
            sb.Append(")\n");
            this.TranslateIfStatement(sb, fakeSwitchStatement.GenerateIfStatementBinarySearchTree());

            // This list of switch statements will be serialized at the end of the function definition as globals.
            sb.SwitchStatements.Add(fakeSwitchStatement);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.AppendVariableNameSafe(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            sb.Append("\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(whileLoop.Condition));
            sb.Append(":\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, whileLoop.Code);
            sb.TabDepth--;
        }
    }
}
