﻿using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using Pastel.Transpilers;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    public class PastelContext
    {
        private PastelCompiler lazyInitCompiler = null;
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

        // TODO: refactor this all into a platform capabilities object.
        public bool ClassDefinitionsInSeparateFiles { get { return this.Transpiler.ClassDefinitionsInSeparateFiles; } }
        public bool UsesStructDefinitions { get { return this.Transpiler.UsesStructDefinitions; } }
        public bool UsesClassDefinitions { get { return this.Transpiler.UsesClassDefinitions; } }
        public bool UsesFunctionDeclarations { get { return this.Transpiler.UsesFunctionDeclarations; } }
        public bool UsesStructDeclarations { get { return this.Transpiler.UsesStructDeclarations; } }
        public bool HasStructsInSeparateFiles { get { return this.Transpiler.HasStructsInSeparateFiles; } }

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
                compiler.StructDefinitions,
                compiler.ClassDefinitions
            ).Resolve();
            return this;
        }

        public Dictionary<string, string> GetCodeForClasses()
        {
            TranspilerContext ctx = this.TranspilerContext;
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (ClassDefinition cd in this.GetCompiler().GetClassDefinitions())
            {
                this.Transpiler.GenerateCodeForClass(ctx, cd);
                output[cd.NameToken.Value] = ctx.FlushAndClearBuffer();
            }
            return output;
        }

        public Dictionary<string, string> GetCodeForStructs()
        {
            TranspilerContext ctx = this.TranspilerContext;
            Dictionary<string, string> output = new Dictionary<string, string>();
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
            TranspilerContext ctx = this.TranspilerContext;
            Dictionary<string, string> output = this.GetCodeForFunctionsLookup();

            System.Text.StringBuilder userCodeBuilder = new System.Text.StringBuilder();

            foreach (string fnName in output.Keys.OrderBy(s => s))
            {
                userCodeBuilder.Append("\n\n");
                userCodeBuilder.Append(output[fnName]);
            }

            string userCode = userCodeBuilder.ToString().Trim();

            Dictionary<string, string> codeChunks = this.GetCodeChunks(ctx.Transpiler, userCode);
            string[] chunkOrder = this.GetChunkOrder(codeChunks, userCode);

            List<string> finalCodeBuilder = new List<string>();
            foreach (string chunkId in chunkOrder)
            {
                finalCodeBuilder.Add(codeChunks[chunkId]);
            }

            string finalCode = string.Join("\n\n", finalCodeBuilder);
            return finalCode;
        }

        private Dictionary<string, string> GetCodeChunks(AbstractTranspiler transpiler, string userCode)
        {
            string resourcePath = transpiler.HelperCodeResourcePath;
            Dictionary<string, string> output = new Dictionary<string, string>();
            if (resourcePath == null) return output;

            string helperCode = ResourceReader.ReadTextFile(resourcePath);

            string currentId = null;
            List<string> currentChunk = new List<string>();
            foreach (string lineRaw in helperCode.Split('\n'))
            {
                string line = lineRaw.TrimEnd();
                if (line.Contains("PASTEL_ENTITY_ID"))
                {
                    if (currentId != null)
                    {
                        output[currentId] = string.Join("\n", currentChunk).Trim();
                    }
                    currentId = line.Split(':')[1].Trim();
                    currentChunk.Clear();
                }
                else
                {
                    currentChunk.Add(lineRaw);
                }
            }

            if (currentId != null)
            {
                output[currentId] = string.Join("\n", currentChunk).Trim();
            }

            output[""] = userCode;

            return output;
        }


        private string[] GetChunkOrder(Dictionary<string, string> chunksByMarker, string userCode)
        {
            HashSet<string> allMarkers = new HashSet<string>(chunksByMarker.Keys);
            List<string> nonPstPrefixThings = new List<string>();
            List<string> pstPrefixedThings = new List<string>();
            foreach (string marker in chunksByMarker.Keys.OrderBy(m => m).Where(m => m.Length > 0))
            {
                if (marker.StartsWith("PST"))
                {
                    pstPrefixedThings.Add(marker);
                }
                else
                {
                    nonPstPrefixThings.Add(marker);
                }
            }
            Dictionary<string, string[]> dependencies = new Dictionary<string, string[]>();
            foreach (string marker in chunksByMarker.Keys.OrderBy(m => m))
            {
                string code = chunksByMarker[marker];
                dependencies[marker] = FindUsedMarkers(code, nonPstPrefixThings, pstPrefixedThings, allMarkers, marker).ToArray();
            }

            dependencies["PST_RegisterExtensibleCallback"] = new string[] { "PST_ExtCallbacks" };
            dependencies[""] = dependencies[""].Concat(new string[] { "PST_RegisterExtensibleCallback" }).ToArray();

            List<string> orderedKeys = new List<string>();

            this.PopulateOrderedChunkKeys("", orderedKeys, dependencies, new Dictionary<string, int>());

            return orderedKeys.ToArray();
        }

        private void PopulateOrderedChunkKeys(
            string currentItem,
            List<string> orderedKeys,
            Dictionary<string, string[]> dependencies,
            Dictionary<string, int> traversalState) // { missing/0 - not used | 1 - seen but dependencies not added yet | 2 - added along with all dependencies }
        {
            if (!traversalState.ContainsKey(currentItem))
            {
                traversalState[currentItem] = 1;
            }
            else if (traversalState[currentItem] == 1)
            {
                throw new System.Exception("dependency loop: " + currentItem + " depends on itself indirectly.");
            }
            else if (traversalState[currentItem] == 2)
            {
                return; // already added
            }

            foreach (string dep in dependencies[currentItem])
            {
                PopulateOrderedChunkKeys(dep, orderedKeys, dependencies, traversalState);
            }

            traversalState[currentItem] = 2;
            orderedKeys.Add(currentItem);
        }

        private IList<string> FindUsedMarkers(string code, IList<string> nonPstMarkers, IList<string> pstMarkers, HashSet<string> allMarkers, string exclusion)
        {
            List<string> usedMarkers = new List<string>();
            foreach (string nonPstMarker in nonPstMarkers)
            {
                if (exclusion != nonPstMarker && code.Contains(nonPstMarker))
                {
                    usedMarkers.Add(nonPstMarker);
                }
            }

            string[] pstParts = code.Split(new string[] { "PST" }, System.StringSplitOptions.None);
            for (int i = 1; i < pstParts.Length; ++i)
            {
                string markerName = GetMarkerNameHacky(pstParts[i]);
                if (allMarkers.Contains(markerName) && exclusion != markerName)
                {
                    usedMarkers.Add(markerName);
                }
            }

            return usedMarkers;
        }

        private string GetMarkerNameHacky(string potentialMarkerNameWithoutPst)
        {
            char c;
            for (int i = 0; i < potentialMarkerNameWithoutPst.Length; ++i)
            {
                c = potentialMarkerNameWithoutPst[i];
                if (!((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' ||
                    c == '$'))
                {
                    return "PST" + potentialMarkerNameWithoutPst.Substring(0, i);
                }
            }
            return "PST" + potentialMarkerNameWithoutPst;
        }
    }
}
