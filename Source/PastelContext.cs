using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using Pastel.Transpilers;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    public class PastelContext
    {
        private PastelCompiler? lazyInitCompiler = null;
        public Language Language { get; private set; }
        private Dictionary<string, object> constants = new Dictionary<string, object>();
        internal ExtensionSet ExtensionSet { get; private set; }
        internal AbstractTranspiler Transpiler { get; private set; }
        public TranspilerContext TranspilerContext { get; private set; }
        public IInlineImportCodeLoader CodeLoader { get; private set; }

        private string dir;

        public PastelContext(string dir, Language language, IInlineImportCodeLoader codeLoader)
        {
            this.dir = dir;
            this.CodeLoader = codeLoader;
            this.Language = language;
            this.ExtensionSet = new ExtensionSet();

            // TODO: do something about this weird cycle.
            this.TranspilerContext = new TranspilerContext(this);
            this.Transpiler = LanguageUtil.CreateTranspiler(this.Language, this.TranspilerContext);
            this.TranspilerContext.Transpiler = this.Transpiler;
        }

        public PastelContext(string dir, string languageId, IInlineImportCodeLoader codeLoader)
            : this(dir, LanguageUtil.ParseLanguage(languageId), codeLoader)
        { }

        public override string ToString()
        {
            return "Pastel Context: " + dir;
        }

        public PastelContext SetConstant(string key, object value)
        {
            this.constants[key] = value;
            return this;
        }

        internal PastelCompiler GetCompiler()
        {
            if (this.lazyInitCompiler == null)
            {
                this.lazyInitCompiler = new PastelCompiler(
                    this,
                    this.Language,
                    this.constants,
                    this.CodeLoader,
                    this.ExtensionSet);
            }
            return this.lazyInitCompiler;
        }

        public PastelContext CompileCode(string filename, string code)
        {
            this.GetCompiler().CompileBlobOfCode(filename, code);
            return this;
        }

        public PastelContext CompileFile(Token throwLocation, string filename)
        {
            return this.CompileCode(filename, this.CodeLoader.LoadCode(throwLocation, filename));
        }

        public PastelContext FinalizeCompilation()
        {
            PastelCompiler compiler = this.GetCompiler();
            new Resolver(
                compiler, 
                compiler.EnumDefinitions,
                compiler.ConstantDefinitions,
                compiler.FunctionDefinitions,
                compiler.StructDefinitions
            ).Resolve();
            return this;
        }

        public Dictionary<string, string> GetCodeForStructs()
        {
            TranspilerContext ctx = this.TranspilerContext;
            Dictionary<string, string> output = [];
            foreach (StructDefinition sd in this.GetCompiler().GetStructDefinitions())
            {
                this.Transpiler.GenerateCodeForStruct(ctx, sd);
                output[sd.NameToken.Value] = ctx.FlushAndClearBuffer();
            }
            return output;
        }

        public string GetCodeForStructDeclaration(string structName)
        {
            TranspilerContext ctx = this.TranspilerContext;
            this.Transpiler.GenerateCodeForStructDeclaration(ctx, structName);
            return ctx.FlushAndClearBuffer();
        }

        public Dictionary<string, string> GetCodeForFunctionsLookup()
        {
            TranspilerContext ctx = this.TranspilerContext;
            return this.GetCompiler().GetFunctionCodeAsLookupTEMP(ctx, "");
        }

        public string GetCodeForFunctionDeclarations()
        {
            TranspilerContext ctx = this.TranspilerContext;
            return this.GetCompiler().GetFunctionDeclarationsTEMP(ctx, "");
        }

        public string GetCodeForFunctions()
        {
            Dictionary<string, string> output = this.GetCodeForFunctionsLookup();

            string userCode = string.Join("\n\n", output.Keys.OrderBy(name => name).Select(name => output[name]));
            string userCodeWithPastelHelpers = TranspilationHelperCodeUtil.InjectTranspilationHelpers(
                this.TranspilerContext.Transpiler, userCode);

            return userCodeWithPastelHelpers;
        }
    }
}
