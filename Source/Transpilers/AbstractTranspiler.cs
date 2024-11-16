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
        public AbstractStatementTranslator StatementTranslator { get; protected set; }

        public AbstractTranspiler(TranspilerContext transpilerCtx)
        {
            this.transpilerCtx = transpilerCtx;
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
