using System.Collections.Generic;

namespace Pastel.ParseNodes
{
    public class PType
    {
        // THIS MUST GO FIRST
        private static readonly PType[] EMPTY_GENERICS = new PType[0];

        public static readonly PType INT = new PType(null, "int");
        public static readonly PType CHAR = new PType(null, "char");
        public static readonly PType BOOL = new PType(null, "bool");
        public static readonly PType STRING = new PType(null, "string");
        public static readonly PType DOUBLE = new PType(null, "double");
        public static readonly PType VOID = new PType(null, "void");
        public static readonly PType NULL = new PType(null, "null");

        public Token FirstToken { get; set; }
        public string RootValue { get; set; }
        public PType[] Generics { get; set; }

        public bool HasTemplates { get; set; }

        public bool IsNullable { get; set; }

        public PType(Token firstToken, string value, params PType[] generics) : this(firstToken, value, new List<PType>(generics)) { }
        public PType(Token firstToken, string value, List<PType> generics)
        {
            this.FirstToken = firstToken;
            this.RootValue = value;
            this.Generics = generics == null ? EMPTY_GENERICS : generics.ToArray();
            this.IsNullable = value == "string" || value == "object" || (value[0] >= 'A' && value[0] <= 'Z');

            if (value.Length == 1)
            {
                this.HasTemplates = true;
            }
            else if (this.Generics.Length > 0)
            {
                for (int i = 0; i < this.Generics.Length; ++i)
                {
                    if (this.Generics[i].HasTemplates)
                    {
                        this.HasTemplates = true;
                        break;
                    }
                }
            }

            if (this.Generics.Length > 0 && this.RootValue.Length == 1)
            {
                throw new ParserException(this.FirstToken, "Cannot have a templated root type with generics.");
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
                PType newType;
                if (templateLookup.TryGetValue(this.RootValue, out newType))
                {
                    return newType;
                }
                return this;
            }

            List<PType> generics = new List<PType>();
            for (int i = 0; i < this.Generics.Length; ++i)
            {
                generics.Add(this.Generics[i].ResolveTemplates(templateLookup));
            }
            return new PType(this.FirstToken, this.RootValue, generics.ToArray());
        }

        // when a templated type coincides with an actual value, add that template key to the lookup output param.
        public static bool CheckAssignmentWithTemplateOutput(PType templatedType, PType actualValue, Dictionary<string, PType> output)
        {
            if (templatedType.RootValue == "object") return true;

            // Most cases, nothing to do
            if (templatedType.IsIdentical(actualValue))
            {
                return true;
            }

            if (templatedType.RootValue.Length == 1)
            {
                if (output.ContainsKey(templatedType.RootValue))
                {
                    PType requiredType = output[templatedType.RootValue];
                    // if it's already encountered it better match the existing value
                    if (actualValue.IsIdentical(requiredType))
                    {
                        return true;
                    }

                    // It's also possible that this is null, in which case the type must be nullable.
                    if (actualValue.RootValue == "null" && requiredType.IsNullable)
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
                        if (actualValue.IsIdentical(output[templatedType.RootValue]))
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
                if (!CheckAssignmentWithTemplateOutput(templatedType.Generics[i], actualValue.Generics[i], output))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckAssignment(PType targetType, PType value)
        {
            if (targetType.RootValue == "void") return false;
            return CheckReturnType(targetType, value);
        }

        public static bool CheckReturnType(PType returnType, PType value)
        {
            if (returnType.IsIdentical(value)) return true;
            if (returnType.RootValue == "object") return true;
            if (returnType.RootValue == "void") return false;
            if (value.RootValue == "null")
            {
                if (returnType.RootValue == "string") return true;
                if (returnType.Generics.Length > 0) return true;

                // is struct?
                char c = returnType.RootValue[0];
                if (c >= 'A' && c <= 'Z') return true;
            }
            return false;
        }

        private bool IsParentOf(PType moreSpecificTypeOrSame)
        {
            if (moreSpecificTypeOrSame == this) return true;
            if (this.RootValue == "object") return true;
            if (this.Generics.Length == 0)
            {
                // why no treatment of int as a subtype of double? because there needs to be an explicit type conversion
                // for languages that aren't strongly typed and won't auto-convert.
                return this.RootValue == moreSpecificTypeOrSame.RootValue;
            }

            // All that's left are Arrays, Lists, and Dictionaries, which must match exactly.
            return this.IsIdentical(moreSpecificTypeOrSame);
        }

        public bool IsIdentical(PType other)
        {
            if (this.Generics.Length != other.Generics.Length) return false;
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
                if (!this.Generics[i].IsIdentical(other.Generics[i]))
                {
                    return false;
                }
            }
            return true;
        }
        
        public override string ToString()
        {
            // only used for debugging and errors, so string concatenation is fine.
            string output = this.RootValue;
            if (this.Generics.Length > 0)
            {
                output += "<";
                for (int i = 0; i < this.Generics.Length; ++i)
                {
                    if (i > 0) output += ", ";
                    output += this.Generics[i].ToString();
                }
                output += ">";
            }
            return output;
        }
    }
}
