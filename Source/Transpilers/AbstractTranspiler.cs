using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers
{
    internal abstract class AbstractTranspiler
    {
        public abstract string HelperCodeResourcePath { get; }

        protected TranspilerContext transpilerCtx;
        public AbstractTypeTranspiler TypeTranspiler { get; private set; }
        public AbstractExporter Exporter { get; private set; }
        public AbstractExpressionTranslator ExpressionTranslator { get; private set; }
        public AbstractStatementTranslator StatementTranslator { get; private set; }

        public AbstractTranspiler(
            TranspilerContext transpilerCtx,
            AbstractExporter exporter,
            AbstractTypeTranspiler? typeTranslator,
            AbstractExpressionTranslator exprTranslator,
            AbstractStatementTranslator stmntTranslator)
        {
            this.transpilerCtx = transpilerCtx;
            this.Exporter = exporter;
            this.TypeTranspiler = typeTranslator;
            this.ExpressionTranslator = exprTranslator;
            this.StatementTranslator = stmntTranslator;
        }

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
