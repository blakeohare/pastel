using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers
{
    internal abstract class AbstractTranspiler
    {
        public abstract string HelperCodeResourcePath { get; }

        protected TranspilerContext transpilerCtx;
        public AbstractTypeTranspiler TypeTranspiler { get; protected set; }
        public AbstractExporter Exporter { get; protected set; }
        public AbstractExpressionTranslator ExpressionTranslator { get; protected set; }

        public AbstractTranspiler(TranspilerContext transpilerCtx)
        {
            this.transpilerCtx = transpilerCtx;
        }

        public virtual void TranslateStatements(TranspilerContext sb, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; ++i)
            {
                this.TranslateStatement(sb, statements[i]);
            }
        }

        public void TranslateStatement(TranspilerContext sb, Statement stmnt)
        {
            string typeName = stmnt.GetType().Name;
            switch (typeName)
            {
                case "Assignment":
                    Assignment asgn = (Assignment)stmnt;
                    if (asgn.Value is CoreFunctionInvocation &&
                        asgn.Target is Variable &&
                        ((CoreFunctionInvocation)asgn.Value).Function == CoreFunction.DICTIONARY_TRY_GET)
                    {
                        Variable variableOut = (Variable)asgn.Target;
                        Expression[] tryGetArgs = ((CoreFunctionInvocation)asgn.Value).Args;
                        Expression dictionary = tryGetArgs[0];
                        Expression key = tryGetArgs[1];
                        Expression fallbackValue = tryGetArgs[2];
                        this.TranslateDictionaryTryGet(sb, dictionary, key, fallbackValue, variableOut);
                    }
                    else
                    {
                        this.TranslateAssignment(sb, asgn);
                    }
                    break;

                case "BreakStatement": this.TranslateBreak(sb); break;
                case "ExpressionAsStatement": this.TranslateExpressionAsStatement(sb, ((ExpressionAsStatement)stmnt).Expression); break;
                case "IfStatement": this.TranslateIfStatement(sb, (IfStatement)stmnt); break;
                case "ReturnStatement": this.TranslateReturnStatemnt(sb, (ReturnStatement)stmnt); break;
                case "SwitchStatement": this.TranslateSwitchStatement(sb, (SwitchStatement)stmnt); break;
                case "VariableDeclaration": this.TranslateVariableDeclaration(sb, (VariableDeclaration)stmnt); break;
                case "WhileLoop": this.TranslateWhileLoop(sb, (WhileLoop)stmnt); break;
                case "StatementBatch":
                    Statement[] stmnts = ((StatementBatch)stmnt).Statements;
                    for (int i = 0; i < stmnts.Length; ++i)
                    {
                        this.TranslateStatement(sb, stmnts[i]);
                    }
                    break;

                default:
                    throw new NotImplementedException(typeName);
            }
        }

        public abstract void TranslateAssignment(TranspilerContext sb, Assignment assignment);
        public abstract void TranslateBreak(TranspilerContext sb);
        public abstract void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut);
        public abstract void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression);
        public abstract void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement);
        public abstract void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement);
        public abstract void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement);
        public abstract void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl);
        public abstract void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop);

        public abstract void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef);
        public abstract void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic);

        public virtual void GenerateCodeForStructDeclaration(TranspilerContext sb, string structName)
        {
            throw new NotSupportedException();
        }

        // Overridden in languages that require a function to be declared separately in order for declaration order to not matter, such as C.
        public virtual void GenerateCodeForFunctionDeclaration(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            throw new NotSupportedException();
        }
    }
}
