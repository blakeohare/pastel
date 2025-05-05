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
                if (this.IsConstant) return CompilationEntityType.CONSTANT;
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
            this.Context = context;
            this.Type = type;
            this.VariableNameToken = variableNameToken;
            this.EqualsToken = equalsToken;
            this.Value = assignmentValue;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            string name = this.VariableNameToken.Value;
            if (resolver.GetFunctionDefinition(name) != null)
            {
                throw new ParserException(
                    this.VariableNameToken,
                    "Name conflict: '" + name + "' is the name of a function and cannot be used as a variable name.");
            }

            if (resolver.GetEnumDefinition(name) != null)
            {
                throw new ParserException(
                    this.VariableNameToken,
                    "Name conflict: '" + name + "' is the name of an enum and cannot be used as a variable name.");
            }
            
            if (resolver.CompilerContext.StructDefinitions.ContainsKey(name))
            {
                throw new ParserException(
                    this.VariableNameToken,
                    "Name conflict: '" + name + "' is the name of a struct and cannot be used as a variable name.");
            }

            if (resolver.CompilerContext.ConstantDefinitions.ContainsKey(name))
            {
                throw new ParserException(
                    this.VariableNameToken,
                    "Name conflict: '" + name + "' is the name of a constant and cannot be used as a variable name.");
            }

            if (this.Value == null)
            {
                throw new ParserException(this.FirstToken, "Cannot have variable declaration without a value.");
            }
            this.Value = this.Value.ResolveNamesAndCullUnusedCode(resolver);

            return this;
        }

        public void DoConstantResolutions(HashSet<string> cycleDetection, Resolver resolver)
        {
            this.Value = this.Value.DoConstantResolution(cycleDetection, resolver);
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Value = this.Value.ResolveType(varScope, resolver);

            if (!PType.CheckAssignment(resolver, this.Type, this.Value.ResolvedType))
            {
                throw new ParserException(this.Value.FirstToken, "Cannot assign this type to a " + this.Type);
            }

            varScope.DeclareVariables(this.VariableNameToken, this.Type);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (this.Value != null)
            {
                this.Value = this.Value.ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
