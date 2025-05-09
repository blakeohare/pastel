﻿using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.Java
{
    internal class JavaTypeTranspiler : AbstractTypeTranspiler
    {
        internal bool UncheckedTypeWarning { get; set; }

        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "void": return "void";
                case "byte": return "byte";
                case "int": return "int";
                case "char": return "char";
                case "double": return "double";
                case "bool": return "boolean";
                case "object": return "Object";
                case "string": return "String";

                case "Array":
                    string innerType = this.TranslateType(type.Generics[0]);
                    return innerType + "[]";

                case "List":
                    return "ArrayList<" + TranslateJavaNestedType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "HashMap<" + TranslateJavaNestedType(type.Generics[0]) + ", " + TranslateJavaNestedType(type.Generics[1]) + ">";

                case "Func":
                    return "java.lang.reflect.Method";

                case "ClassValue":
                    return CrayonHacks.GetClassValueFullName();

                default:
                    if (type.IsStruct)
                    {
                        return type.TypeName;
                    }

                    throw new NotImplementedException();
            }
        }

        public string TranslateJavaNestedType(PType type)
        {
            switch (type.RootValue)
            {
                case "bool": return "Boolean";
                case "byte": return "Byte";
                case "char": return "Character";
                case "double": return "Double";
                case "int": return "Integer";
                default:
                    return this.TranslateType(type);
            }
        }

        public bool IsJavaPrimitiveTypeBoxed(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "double":
                case "bool":
                case "byte":
                case "object":
                case "char":
                    return true;
                default:
                    return false;
            }
        }
    }
}
