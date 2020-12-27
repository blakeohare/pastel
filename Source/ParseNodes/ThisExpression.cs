using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pastel.Nodes
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
            if (this.Owner is FunctionDefinition funcDef)
            {
                cd = funcDef.ClassDef;
            }
            else if (this.Owner is FieldDefinition fieldDef)
            {
                cd = fieldDef.ClassDef;
            }
            else if (this.Owner is ConstructorDefinition constructorDef)
            {
                cd = constructorDef.ClassDef;
            }
            else
            {
                throw new ParserException(this.FirstToken, "Cannot use the expression 'this' outside of classes.");
            }
            this.ResolvedType = PType.ForClass(this.FirstToken, cd);
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            throw new NotImplementedException();
        }
    }
}
