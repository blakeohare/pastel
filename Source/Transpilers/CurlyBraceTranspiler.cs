namespace Pastel.Transpilers
{
    internal abstract class CurlyBraceTranspiler : AbstractTranspiler
    {
        public CurlyBraceTranspiler(
            TranspilerContext transpilerCtx,
            AbstractExporter exporter,
            AbstractTypeTranspiler? typeTranslator,
            AbstractExpressionTranslator exprTranslator,
            AbstractStatementTranslator stmntTranslator)
            : base(
                  transpilerCtx,
                  exporter,
                  typeTranslator,
                  exprTranslator,
                  stmntTranslator)
        { }
    }
}
