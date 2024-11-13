﻿using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    public class PType
    {
        private enum TypeCategory
        {
            PRIMITIVE,
            STRUCT,
            LIST,
            ARRAY,
            DICTIONARY,
            NULL,
            VOID,
            OBJECT,
            TEMPLATE,
            FUNCTION,
            CORE_FUNCTION,

            UNKNOWN,
        }

        // THIS MUST GO FIRST
        private static readonly PType[] EMPTY_GENERICS = new PType[0];

        public static readonly PType INT = new PType(null, null, "int");
        public static readonly PType CHAR = new PType(null, null, "char");
        public static readonly PType BOOL = new PType(null, null, "bool");
        public static readonly PType STRING = new PType(null, null, "string");
        public static readonly PType DOUBLE = new PType(null, null, "double");
        public static readonly PType VOID = new PType(null, null, "void");
        public static readonly PType NULL = new PType(null, null, "null");

        public Token FirstToken { get; set; }
        public string RootValue { get; set; }
        public string Namespace { get; set; }
        public string TypeName { get; set; }
        public Token[] RootChain { get; set; }
        public PType[] Generics { get; set; }

        public bool HasTemplates { get; set; }

        public bool IsNullable { get; set; }

        private TypeCategory Category { get; set; }
        public bool IsStruct { get { return Category == TypeCategory.STRUCT; } }

        internal StructDefinition StructDef
        {
            get
            {
                return structReference;
            }
        }

        public static PType FunctionOf(Token tokenOfFunctionRefOccurrence, PType returnType, IList<PType> argumentTypes)
        {
            List<PType> generics = new List<PType>();
            generics.Add(returnType);
            generics.AddRange(argumentTypes);

            return new PType(tokenOfFunctionRefOccurrence, null, "Func", generics.ToArray());
        }

        public PType(Token firstToken, string namespaceName, string typeName, params PType[] generics) : this(firstToken, namespaceName, typeName, new List<PType>(generics)) { }
        public PType(Token firstToken, string namespaceName, string typeName, List<PType> generics)
        {
            FirstToken = firstToken;
            RootValue = namespaceName == null ? typeName : namespaceName + "." + typeName;
            Namespace = namespaceName;
            TypeName = typeName;
            Generics = generics == null ? EMPTY_GENERICS : generics.ToArray();

            // Uses an invalid character to prevent the possibility of creating this type directly in code.
            if (RootValue == "@CoreFunc")
            {
                Category = TypeCategory.CORE_FUNCTION;
            }

            if (Generics.Length == 1)
            {
                if (RootValue == "List") Category = TypeCategory.LIST;
                else if (RootValue == "Array") Category = TypeCategory.ARRAY;
                else if (RootValue == "Func") Category = TypeCategory.FUNCTION;
                else throw new ParserException(firstToken, "A generic cannot be applied to this type.");
            }
            else if (Generics.Length == 2)
            {
                if (RootValue == "Dictionary") Category = TypeCategory.DICTIONARY;
                else if (RootValue == "Func") Category = TypeCategory.FUNCTION;
                else throw new ParserException(firstToken, "Two generics cannot be applied to this type.");
            }
            else if (Generics.Length > 2)
            {
                if (RootValue == "Func") Category = TypeCategory.FUNCTION;
                else throw new ParserException(firstToken, "Invalid number of generics.");
            }
            else
            {
                switch (RootValue)
                {
                    case "null":
                        Category = TypeCategory.NULL;
                        break;

                    case "int":
                    case "char":
                    case "double":
                    case "bool":
                    case "string":
                    case "number":
                    case "byte":
                    case "StringBuilder":
                        Category = TypeCategory.PRIMITIVE;
                        break;

                    case "object":
                        Category = TypeCategory.OBJECT;
                        break;

                    case "void":
                        Category = TypeCategory.VOID;
                        break;

                    case "List":
                    case "Array":
                    case "Dictionary":
                        throw new ParserException(FirstToken, "This type requires generics");

                    case "@CoreFunc":
                        Category = TypeCategory.CORE_FUNCTION;
                        break;

                    default:
                        if (RootValue.Length == 1)
                        {
                            Category = TypeCategory.TEMPLATE;
                        }
                        else
                        {
                            Category = TypeCategory.STRUCT;
                        }
                        break;
                }
            }

            switch (Category)
            {
                case TypeCategory.STRUCT:
                case TypeCategory.ARRAY:
                case TypeCategory.LIST:
                case TypeCategory.DICTIONARY:
                case TypeCategory.FUNCTION:
                case TypeCategory.OBJECT:
                case TypeCategory.NULL:
                    IsNullable = true;
                    break;
                case TypeCategory.PRIMITIVE:
                    IsNullable = RootValue == "string" || RootValue == "StringBuilder";
                    break;
                default:
                    IsNullable = false;
                    break;
            }

            HasTemplates = Category == TypeCategory.TEMPLATE;
            if (!HasTemplates && Generics.Length > 0)
            {
                for (int i = 0; i < Generics.Length; ++i)
                {
                    if (Generics[i].HasTemplates)
                    {
                        HasTemplates = true;
                        break;
                    }
                }
            }
        }

        private bool isTypeFinalized = false;
        private StructDefinition structReference = null;

        internal void FinalizeType(Resolver resolver)
        {
            this.FinalizeType(resolver.CompilerContext);
        }

        internal void FinalizeType(PastelCompiler compilerContext)
        {
            if (this.isTypeFinalized) return;
            this.isTypeFinalized = true;

            if (this.Category == TypeCategory.STRUCT)
            {
                PastelCompiler targetContext = compilerContext;

                if (targetContext != null)
                {
                    this.structReference = targetContext.GetStructDefinition(this.TypeName);
                }

                if (this.structReference == null)
                {
                    throw new ParserException(this.FirstToken, "Could not find a class or struct by the name of '" + this.RootValue + "'");
                }
            }

            for (int i = 0; i < this.Generics.Length; ++i)
            {
                this.Generics[i].FinalizeType(compilerContext);
            }
        }

        public PType ResolveTemplates(Dictionary<string, PType> templateLookup)
        {
            if (!this.HasTemplates)
            {
                return this;
            }

            if (this.RootValue.Length == 1)
            {
                if (templateLookup.TryGetValue(this.RootValue, out PType newType))
                {
                    return newType;
                }
                return this;
            }

            List<PType> generics = [];
            for (int i = 0; i < this.Generics.Length; ++i)
            {
                generics.Add(this.Generics[i].ResolveTemplates(templateLookup));
            }
            return new PType(this.FirstToken, this.Namespace, this.TypeName, generics.ToArray());
        }

        // when a templated type coincides with an actual value, add that template key to the lookup output param.
        internal static bool CheckAssignmentWithTemplateOutput(Resolver resolver, PType templatedType, PType actualValue, Dictionary<string, PType> output)
        {
            if (templatedType.Category == TypeCategory.OBJECT) return true;

            // Most cases, nothing to do
            if (templatedType.IsIdenticalOrChildOf(resolver, actualValue))
            {
                return true;
            }

            if (templatedType.RootValue.Length == 1)
            {
                if (output.ContainsKey(templatedType.RootValue))
                {
                    PType requiredType = output[templatedType.RootValue];
                    // if it's already encountered it better match the existing value
                    if (actualValue.IsIdenticalOrChildOf(resolver, requiredType))
                    {
                        return true;
                    }

                    // It's also possible that this is null, in which case the type must be nullable.
                    if (actualValue.Category == TypeCategory.NULL && requiredType.IsNullable)
                    {
                        return true;
                    }

                    return false;
                }
                output[templatedType.RootValue] = actualValue;
                return true;
            }

            if (templatedType.Generics.Length != actualValue.Generics.Length)
            {
                // completely different. don't even try to match templates
                return false;
            }

            if (templatedType.RootValue != actualValue.RootValue)
            {
                if (templatedType.RootValue.Length == 1)
                {
                    if (output.ContainsKey(templatedType.RootValue))
                    {
                        // if it's already encountered it better match the existing value
                        if (actualValue.IsIdentical(resolver, output[templatedType.RootValue]))
                        {
                            // yup, that's okay
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // first time this type was encountered.
                        output[templatedType.RootValue] = actualValue;
                    }
                }
                else
                {
                    // different type
                    return false;
                }
            }

            for (int i = 0; i < templatedType.Generics.Length; ++i)
            {
                if (!CheckAssignmentWithTemplateOutput(resolver, templatedType.Generics[i], actualValue.Generics[i], output))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool CheckAssignment(Resolver resolver, PType targetType, PType value)
        {
            if (targetType.Category == TypeCategory.VOID) return false;
            return CheckReturnType(resolver, targetType, value);
        }

        internal static bool CheckReturnType(Resolver resolver, PType returnType, PType value)
        {
            // This is an assert, not a user-facing exception. Null should never appear here.
            if (value == null) throw new ParserException(returnType.FirstToken, "This should not happen.");

            if (value.IsIdenticalOrChildOf(resolver, returnType)) return true;
            if (returnType.Category == TypeCategory.OBJECT) return true;
            if (returnType.Category == TypeCategory.VOID) return false;
            if (value.Category == TypeCategory.NULL)
            {
                if (returnType.Category == TypeCategory.PRIMITIVE && returnType.TypeName == "string") return true;
                if (returnType.Generics.Length > 0) return true;
                if (returnType.IsStruct) return true;
            }
            return false;
        }

        private bool IsParentOf(Resolver resolver, PType moreSpecificTypeOrSame)
        {
            if (moreSpecificTypeOrSame == this) return true;
            if (Category == TypeCategory.OBJECT) return true;
            if (Generics.Length == 0)
            {
                // why no treatment of int as a subtype of double? because there needs to be an explicit type conversion
                // for languages that aren't strongly typed and won't auto-convert.
                return RootValue == moreSpecificTypeOrSame.RootValue;
            }

            // All that's left are Arrays, Lists, and Dictionaries, which must match exactly.
            return IsIdentical(resolver, moreSpecificTypeOrSame);
        }

        internal bool IsIdenticalOrChildOf(Resolver resolver, PType other)
        {
            if (IsIdentical(resolver, other)) return true;

            // only structs should be here if this is to return true. If not, then it's a no.
            if (!IsStruct || !other.IsStruct) return false;

            if (IsStruct != other.IsStruct) return false;
            if (IsStruct)
            {
                if (StructDef == null || other.StructDef == null) throw new System.Exception("This check cannot occur without resolving struct information for PTypes.");
                StructDefinition walker = StructDef;
                StructDefinition target = other.StructDef;
                while (walker != null)
                {
                    if (walker == target) return true;
                    walker = walker.Parent;
                }
            }

            return false;
        }

        internal bool IsIdentical(Resolver resolver, PType other)
        {
            if (!this.isTypeFinalized) this.FinalizeType(resolver);
            if (!other.isTypeFinalized) other.FinalizeType(resolver);

            if (this.Category != other.Category) return false;

            if (this.Generics.Length != other.Generics.Length) return false;

            if (this.IsStruct)
            {
                return this.structReference == other.structReference;
            }

            if (this.RootValue != other.RootValue)
            {
                string thisRoot = this.RootValue;
                string thatRoot = other.RootValue;
                if (thisRoot == "number" && (thatRoot == "double" || thatRoot == "int")) return true;
                if (thatRoot == "number" && (thisRoot == "double" || thisRoot == "int")) return true;
                return false;
            }

            for (int i = this.Generics.Length - 1; i >= 0; --i)
            {
                if (!this.Generics[i].IsIdentical(resolver, other.Generics[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static PType Parse(TokenStream tokens)
        {
            PType type = TryParse(tokens);
            if (type != null) return type;
            throw new ParserException(tokens.Peek(), "Expected a type here.");
        }

        internal static PType TryParse(TokenStream tokens)
        {
            int index = tokens.SnapshotState();
            PType type = ParseImpl(tokens);
            if (type == null)
            {
                tokens.RevertState(index);
                return null;
            }

            if (tokens.IsNext("[") && tokens.PeekAhead(1) == "]")
            {
                throw new ParserException(tokens.Peek(), "Array types are defined with the Array class, not square brackets.");
            }

            return type;
        }

        private static Token[] reusableRootNameParserOut = new Token[2];

        // Attempts to pop a Namespaced.TypeName or a TypeName from the token stream.
        // The values are applied to reusableRootNameParserOut and the number of values
        // parsed are returned as an integer. Possible values are 0, 1, and 2.
        // This method will update the token stream through the valid tokens.
        private static int ParseRootNameImpl(TokenStream tokens)
        {
            if (!tokens.HasMore) return 0;
            int zeroIndex = tokens.SnapshotState();
            Token firstToken = tokens.Pop();
            if (!ExpressionParser.IsValidName(firstToken.Value))
            {
                tokens.RevertState(zeroIndex);
                return 0;
            }
            reusableRootNameParserOut[0] = firstToken;
            int oneIndex = tokens.SnapshotState();
            if (!tokens.PopIfPresent(".")) return 1;

            if (!tokens.HasMore)
            {
                tokens.RevertState(oneIndex);
                return 1;
            }
            Token secondToken = tokens.Pop();
            if (!ExpressionParser.IsValidName(secondToken.Value))
            {
                tokens.RevertState(oneIndex);
                return 1;
            }

            reusableRootNameParserOut[1] = secondToken;

            return 2;
        }

        private static PType ParseImpl(TokenStream tokens)
        {
            int consecutiveTokenCount = ParseRootNameImpl(tokens);
            if (consecutiveTokenCount == 0) return null;
            Token namespaceToken = null;
            string namespaceTokenValue = null;
            Token typeToken = null;
            Token firstToken = reusableRootNameParserOut[0];
            if (consecutiveTokenCount == 1)
            {
                typeToken = reusableRootNameParserOut[0];
            }
            else
            {
                namespaceToken = reusableRootNameParserOut[0];
                namespaceTokenValue = namespaceToken.Value;
                typeToken = reusableRootNameParserOut[1];
            }

            if (namespaceToken == null)
            {
                switch (typeToken.Value)
                {
                    case "int":
                    case "char":
                    case "double":
                    case "bool":
                    case "void":
                    case "string":
                    case "object":
                        return new PType(firstToken, null, typeToken.Value);
                }
            }

            int tokenIndex = tokens.SnapshotState();

            bool isError = false;
            if (tokens.PopIfPresent("<"))
            {
                List<PType> generics = new List<PType>();
                while (!tokens.PopIfPresent(">"))
                {
                    if (generics.Count > 0)
                    {
                        if (!tokens.PopIfPresent(","))
                        {
                            isError = true;
                            break;
                        }
                    }

                    PType generic = ParseImpl(tokens);
                    if (generic == null) return null;

                    generics.Add(generic);
                }
                if (!isError)
                {
                    return new PType(firstToken, namespaceTokenValue, typeToken.Value, generics);
                }

                // If there was an error while parsing generics, then this may still be a valid type.
                tokens.RevertState(tokenIndex);
                return new PType(firstToken, namespaceTokenValue, typeToken.Value);
            }
            else
            {
                return new PType(firstToken, namespaceTokenValue, typeToken.Value);
            }
        }

        public override string ToString()
        {
            // only used for debugging and errors, so string concatenation is fine.
            string output = RootValue;
            if (Generics.Length > 0)
            {
                output += "<";
                for (int i = 0; i < Generics.Length; ++i)
                {
                    if (i > 0) output += ", ";
                    output += Generics[i].ToString();
                }
                output += ">";
            }
            return output;
        }
    }
}
