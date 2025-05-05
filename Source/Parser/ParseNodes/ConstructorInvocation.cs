using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ConstructorInvocation : Expression
    {
        public PType Type { get; set; }
        public Expression[] Args { get; set; }
        public StructDefinition StructDefinition { get; set; }

        public ConstructorInvocation(Token firstToken, PType type, IList<Expression> args, ICompilationEntity owner) 
            : base(ExpressionType.CONSTRUCTOR_INVOCATION, firstToken, owner)
        {
            this.Type = type;
            this.Args = args.ToArray();
            this.ResolvedType = type;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            for (int i = 0; i < this.Args.Length; ++i)
            {
                this.Args[i] = this.Args[i].ResolveWithTypeContext(resolver);
            }

            string type = this.Type.RootValue;
            switch (type)
            {
                case "Array":
                case "List":
                case "Dictionary":
                case "StringBuilder":
                    break;

                default:
                    PType[] resolvedArgTypes;

                    if (this.Type.IsStruct)
                    {
                        StructDefinition sd = this.Type.StructDef;
                        this.StructDefinition = sd;
                        resolvedArgTypes = sd.FieldTypes;
                    }
                    else
                    {
                        throw new ParserException(this.FirstToken, "Cannot instantiate this item.");
                    }

                    int fieldCount = resolvedArgTypes.Length;
                    if (fieldCount != this.Args.Length)
                    {
                        throw new ParserException(this.FirstToken, "Incorrect number of args in constructor. Expected " + fieldCount + ", found " + Args.Length);
                    }

                    for (int i = 0; i < fieldCount; ++i)
                    {
                        PType actualType = this.Args[i].ResolvedType;
                        PType expectedType = resolvedArgTypes[i];
                        if (!PType.CheckAssignment(resolver, expectedType, actualType))
                        {
                            throw new ParserException(
                                this.Args[i].FirstToken,
                                "Cannot use an arg of this type for this struct field. Expected " +
                                expectedType.ToString() +
                                " but found " +
                                actualType.ToString());
                        }
                    }
                    break;
            }

            return this;
        }
    }
}
