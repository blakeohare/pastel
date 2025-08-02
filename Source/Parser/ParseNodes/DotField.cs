using System;
using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class DotField : Expression
    {
        public Expression Root { get; set; }
        public Token DotToken { get; set; }
        public Token FieldName { get; set; }

        public CoreFunction CoreFunctionId { get; set; }
        public StructDefinition StructType { get; set; }

        public DotField(Expression root, Token dotToken, Token fieldName)
            : base(ExpressionType.DOT_FIELD, root.FirstToken, root.Owner)
        {
            this.Root = root;
            this.DotToken = dotToken;
            this.FieldName = fieldName;
            this.CoreFunctionId = CoreFunction.NONE;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (this.Root.Type == ExpressionType.VARIABLE)
            {
                string rootNamespace = ((Variable)this.Root).Name;
                if (CoreFunctionLookup.IsAnyRootNamespace(rootNamespace))
                {

                    CoreFunction coreFunction = CoreFunctionLookup.GetCoreFunction(
                        rootNamespace,
                        this.FieldName.Value);
                    if (coreFunction == CoreFunction.NONE)
                    {
                        throw new UNTESTED_ParserException(
                            this.FieldName,
                            rootNamespace + " does not have a function named ." + this.FieldName.Value);
                    }

                    return new CoreFunctionReference(this.FirstToken, coreFunction, this.Owner);
                }
            }
            
            this.Root = this.Root.ResolveNamesAndCullUnusedCode(resolver);

            if (this.Root is EnumReference enumRef)
            {
                EnumDefinition enumDef = enumRef.EnumDef;
                InlineConstant enumValue = enumDef.GetValue(this.FieldName);
                return enumValue.CloneWithNewToken(this.FirstToken);
            }

            return this;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            if (this.Root.Type != ExpressionType.VARIABLE) {
                throw new UNTESTED_ParserException(
                    this.FirstToken,
                    "Not able to resolve this constant.");
            }
         
            Variable varRoot = (Variable)this.Root;   
            string enumName = varRoot.Name;
            EnumDefinition enumDef = resolver.GetEnumDefinition(enumName);
            if (enumDef == null)
            {
                throw new UNTESTED_ParserException(
                    this.FirstToken,
                    "Not able to resolve this constant.");
            }

            return enumDef.GetValue(this.FieldName);
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Root = this.Root.ResolveType(varScope, resolver);

            PType rootType = this.Root.ResolvedType;
            if (rootType.IsStruct)
            {
                string fieldName = this.FieldName.Value;
                rootType.FinalizeType(resolver);
                this.StructType = rootType.StructDef;
                int fieldIndex;
                if (!this.StructType.FieldIndexByName.TryGetValue(fieldName, out fieldIndex))
                {
                    throw new UNTESTED_ParserException(
                        this.FieldName, 
                        "The struct '" + this.StructType.NameToken.Value + "' does not have a field called '" + fieldName + "'.");
                }
                this.ResolvedType = this.StructType.FieldTypes[fieldIndex];

                return this;
            }

            this.CoreFunctionId = CoreFunctionLookup.DetermineCoreFunctionId(this.Root.ResolvedType, this.FieldName.Value);
            if (this.CoreFunctionId != CoreFunction.NONE)
            {
                CoreFunctionReference cfr = new CoreFunctionReference(this.FirstToken, this.CoreFunctionId, this.Root, this.Owner);
                cfr.ResolvedType = new PType(this.Root.FirstToken, null, "@CoreFunc");
                return cfr;
            }

            throw new UNTESTED_ParserException(this.FieldName, "No field named '." + this.FieldName.Value + "'.");
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Root = this.Root.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
