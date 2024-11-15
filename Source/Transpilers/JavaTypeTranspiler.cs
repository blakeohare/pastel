using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers
{
    internal class JavaTypeTranspiler : AbstractTypeTranspiler
    {
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
                    return "ArrayList<" + this.TranslateJavaNestedType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "HashMap<" + this.TranslateJavaNestedType(type.Generics[0]) + ", " + this.TranslateJavaNestedType(type.Generics[1]) + ">";

                case "Func":
                    return "java.lang.reflect.Method";

                // TODO: oh no.
                case "ClassValue":
                    // java.lang.ClassValue collision
                    return "org.crayonlang.interpreter.structs.ClassValue";

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
