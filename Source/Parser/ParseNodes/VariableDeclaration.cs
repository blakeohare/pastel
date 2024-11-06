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

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (Value == null)
            {
                throw new ParserException(FirstToken, "Cannot have variable declaration without a value.");
            }
            Value = Value.ResolveNamesAndCullUnusedCode(resolver);

            return this;
        }

        public void DoConstantResolutions(HashSet<string> cycleDetection, Resolver resolver)
        {
            Value = Value.DoConstantResolution(cycleDetection, resolver);
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            Value = Value.ResolveType(varScope, resolver);

            if (!PType.CheckAssignment(resolver, Type, Value.ResolvedType))
            {
                throw new ParserException(Value.FirstToken, "Cannot assign this type to a " + Type);
            }

            varScope.DeclareVariables(VariableNameToken, Type);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (Value != null)
            {
                Value = Value.ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
