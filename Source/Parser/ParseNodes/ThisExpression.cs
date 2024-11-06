using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ThisExpression : Expression
    {
        public ThisExpression(Token token, ICompilationEntity owner)
            : base(token, owner)
        {
            if (owner == null)
            {
                throw new Exception();
            }
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            ClassDefinition cd;
            if (Owner is FunctionDefinition funcDef)
            {
                cd = funcDef.ClassDef;
            }
            else if (Owner is FieldDefinition fieldDef)
            {
                cd = fieldDef.ClassDef;
            }
            else if (Owner is ConstructorDefinition constructorDef)
            {
                cd = constructorDef.ClassDef;
            }
            else
            {
                throw new ParserException(FirstToken, "Cannot use the expression 'this' outside of classes.");
            }
            ResolvedType = PType.ForClass(FirstToken, cd);
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            throw new NotImplementedException();
        }
    }
}
