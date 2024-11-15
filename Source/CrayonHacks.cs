using Pastel.Parser.ParseNodes;
using Pastel.Parser;

namespace Pastel
{
    // Historical note: Pastel exists as a standalone utility that came from refactoring
    // an intermediate compilation step of the Crayon interpreter. In other words, this was
    // all Crayon compiler code at one point. There are certain hacks that were in use that
    // cannot be changed without breaking Crayon. These all need to be addressed in the Crayon
    // codebase first before they can be removed. By adding a reference to all such hacks here
    // they can be tracked a bit more methodically than the occasional "// TODO" sprinkled
    // about the codebase.
    internal static class CrayonHacks
    {
        // TODO(pastel-split): This is just a quick and dirty short-circuit logic for && and ||
        // Do full logic later. Currently this is causing problems in specific snippets in Crayon libraries.
        internal static Expression BoolLogicResolver(Expression binaryOp, string opValue, InlineConstant? left)
        {
            if (left != null)
            {
                if (opValue == "&&" && left.Value is bool)
                {
                    return (bool)left.Value ? binaryOp : left;
                }
                if (opValue == "||" && left.Value is bool)
                {
                    return (bool)left.Value ? left : binaryOp;
                }
            }

            return binaryOp;
        }

        // TODO: This is a Crayon-ism that needs to be removed
        // TODO: also this is dangerously likely to affect other projects. At least add a
        // hacky `if (structDef.NameToken.FileName == blah)` that'll be at least somewhat more
        // likely to not create false positives in the mean time.
        internal static bool IsJavaValueStruct(StructDefinition? structDef) // nullable to allow more non-invasive inline usage
        {
            if (structDef == null || structDef.NameToken.Value != "Value") return false;
            foreach (Token t in structDef.FieldNames)
            {
                if (t.FileName == "internalValue") return true;
            }
            return false;
        }

        // TODO: oh no.
        internal static string GetClassValueFullName()
        {
            // java.lang.ClassValue collision
            return "org.crayonlang.interpreter.structs.ClassValue";
        }

        internal static string SwapJavaStructNameForFullyQualifiedIfNecessaryToAvoidConflict(string originalName)
        {
            if (originalName == "ClassValue")
            {
                return "org.crayonlang.interpreter.structs.ClassValue";
            }
            return originalName;
        }
    }
}
