﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ConstructorInvocation : Expression
    {
        public PType Type { get; set; }
        public Expression[] Args { get; set; }
        public StructDefinition StructDefinition { get; set; }
        public ClassDefinition ClassDefinition { get; set; }

        public ConstructorInvocation(Token firstToken, PType type, IList<Expression> args, ICompilationEntity owner)
            : base(firstToken, owner)
        {
            Type = type;
            Args = args.ToArray();
            ResolvedType = type;
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
            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveWithTypeContext(resolver);
            }

            string type = Type.RootValue;
            switch (type)
            {
                case "Array":
                case "List":
                case "Dictionary":
                case "StringBuilder":
                    break;

                default:
                    PType[] resolvedArgTypes;

                    if (Type.IsStruct)
                    {
                        StructDefinition sd = Type.StructDef;
                        StructDefinition = sd;
                        resolvedArgTypes = sd.FlatFieldTypes;
                    }
                    else if (Type.IsClass)
                    {
                        ClassDefinition cd = Type.ClassDef;
                        ClassDefinition = cd;
                        resolvedArgTypes = cd.Constructor.ArgTypes;
                    }
                    else
                    {
                        throw new ParserException(FirstToken, "Cannot instantiate this item.");
                    }
                    int fieldCount = resolvedArgTypes.Length;
                    if (fieldCount != Args.Length)
                    {
                        throw new ParserException(FirstToken, "Incorrect number of args in constructor. Expected " + fieldCount + ", found " + Args.Length);
                    }

                    for (int i = 0; i < fieldCount; ++i)
                    {
                        PType actualType = Args[i].ResolvedType;
                        PType expectedType = resolvedArgTypes[i];
                        if (!PType.CheckAssignment(resolver, expectedType, actualType))
                        {
                            throw new ParserException(Args[i].FirstToken, "Cannot use an arg of this type for this " + (Type.IsClass ? "constructor argument" : "struct field") + ". Expected " + expectedType.ToString() + " but found " + actualType.ToString());
                        }
                    }
                    break;
            }

            return this;
        }
    }
}
