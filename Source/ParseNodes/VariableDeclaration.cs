﻿using System;
using System.Collections.Generic;

namespace Pastel.ParseNodes
{
    internal class VariableDeclaration : Executable, ICompilationEntity
    {
        public CompilationEntityType EntityType
        {
            get
            {
                if (this.IsConstant) return CompilationEntityType.CONSTANT;
                if (this.IsGlobal) return CompilationEntityType.GLOBAL;
                throw new Exception(); // this shouldn't have been a top-level thing.
            }
        }

        public PType Type { get; set; }
        public Token VariableNameToken { get; set; }
        public Token EqualsToken { get; set; }
        public Expression Value { get; set; }

        public bool IsConstant { get; set; }
        public bool IsGlobal { get; set; }

        public VariableDeclaration(
            PType type,
            Token variableNameToken,
            Token equalsToken,
            Expression assignmentValue) : base(type.FirstToken)
        {
            this.Type = type;
            this.VariableNameToken = variableNameToken;
            this.EqualsToken = equalsToken;
            this.Value = assignmentValue;
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            if (this.Value == null)
            {
                throw new ParserException(this.FirstToken, "Cannot have variable declaration without a value.");
            }
            this.Value = this.Value.ResolveNamesAndCullUnusedCode(compiler);

            return this;
        }

        public void DoConstantResolutions(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            this.Value = this.Value.DoConstantResolution(cycleDetection, compiler);
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            this.Value = this.Value.ResolveType(varScope, compiler);

            if (!PType.CheckAssignment(this.Type, this.Value.ResolvedType))
            {
                if (this.Type == PType.DOUBLE && this.Value.ResolvedType == PType.INT)
                {
                    throw new ParserException(this.Value.FirstToken, "Must explicitly convert integer to a double/float.");
                }
                throw new ParserException(this.Value.FirstToken, "Cannot assign this type to a " + this.Type);
            }

            varScope.DeclareVariables(this.VariableNameToken, this.Type);
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            if (this.Value != null)
            {
                this.Value = this.Value.ResolveWithTypeContext(compiler);
            }
            return this;
        }
    }
}