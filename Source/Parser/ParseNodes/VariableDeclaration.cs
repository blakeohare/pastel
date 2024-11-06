using System;
using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class VariableDeclaration : Statement, ICompilationEntity
    {
        public CompilationEntityType EntityType
        {
            get
            {
                if (IsConstant) return CompilationEntityType.CONSTANT;
                throw new Exception(); // this shouldn't have been a top-level thing.
            }
        }

        public PType Type { get; set; }
        public Token VariableNameToken { get; set; }
        public Token EqualsToken { get; set; }
        public Expression Value { get; set; }
        public PastelContext Context { get; private set; }

        public bool IsConstant { get; set; }

        public VariableDeclaration(
            PType type,
            Token variableNameToken,
            Token equalsToken,
            Expression assignmentValue,
            PastelContext context) : base(type.FirstToken)
        {
            Context = context;
            Type = type;
            VariableNameToken = variableNameToken;
            EqualsToken = equalsToken;
            Value = assignmentValue;
        }

        public override Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            if (Value == null)
            {
                throw new ParserException(FirstToken, "Cannot have variable declaration without a value.");
            }
            Value = Value.ResolveNamesAndCullUnusedCode(compiler);

            return this;
        }

        public void DoConstantResolutions(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            Value = Value.DoConstantResolution(cycleDetection, compiler);
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            Value = Value.ResolveType(varScope, compiler);

            if (!PType.CheckAssignment(compiler, Type, Value.ResolvedType))
            {
                throw new ParserException(Value.FirstToken, "Cannot assign this type to a " + Type);
            }

            varScope.DeclareVariables(VariableNameToken, Type);
        }

        internal override Statement ResolveWithTypeContext(PastelCompiler compiler)
        {
            if (Value != null)
            {
                Value = Value.ResolveWithTypeContext(compiler);
            }
            return this;
        }
    }
}
