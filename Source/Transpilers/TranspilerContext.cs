using Pastel.Parser.ParseNodes;
using Pastel.Transpilers.Python;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    public class TranspilerContext
    {
        private System.Text.StringBuilder buffer = new System.Text.StringBuilder();

        internal List<PythonFakeSwitchStatement> SwitchStatements { get; private set; }

        public string UniquePrefixForNonCollisions { get; set; }

        // This is a hack for conveying extra information to the top-level function serializer for switch statement stuff.
        // This reference is updated in TranslateFunctionDefinition.
        internal FunctionDefinition PY_HACK_CurrentFunctionDef { get; set; }
        public int SwitchCounter { get; set; }
        private int currentIndentDepth = 0;
        public string CurrentTab { get; private set; }
        internal AbstractTranspiler Transpiler { get; set; }
        private HashSet<string> featureUsage = new HashSet<string>();
        public PastelContext PastelContext { get; private set; }

        internal TranspilerContext(PastelContext ctx)
        {
            this.PastelContext = ctx;
            if (ctx.Language == Language.PYTHON)
            {
                this.SwitchCounter = 0;
                this.SwitchStatements = new List<PythonFakeSwitchStatement>();
            }
            this.TabDepth = 0;
        }

        public void MarkFeatureAsBeingUsed(string value)
        {
            this.featureUsage.Add(value);
        }

        public string[] GetFeatures()
        {
            return this.featureUsage.ToArray();
        }

        private List<string> tabCache = ["", "\t"];
        public int TabDepth
        {
            get
            {
                return this.currentIndentDepth;
            }
            set
            {
                this.currentIndentDepth = value;
                while (this.currentIndentDepth >= this.tabCache.Count)
                {
                    this.tabCache.Add(this.tabCache.Last() + '\t');
                }
                this.CurrentTab = this.tabCache[this.currentIndentDepth];
            }
        }

        public TranspilerContext Append(char c)
        {
            this.buffer.Append(c);
            return this;
        }

        public TranspilerContext Append(string s)
        {
            this.buffer.Append(s);
            return this;
        }

        public TranspilerContext Append(int v)
        {
            this.buffer.Append(v);
            return this;
        }

        public string FlushAndClearBuffer()
        {
            string value = this.buffer.ToString();
            this.buffer.Clear();
            return value;
        }

        internal FunctionDefinition CurrentFunctionDefinition
        {
            get { return this.PY_HACK_CurrentFunctionDef; }
            set
            {
                this.PY_HACK_CurrentFunctionDef = value;
                this.SwitchCounter = 0;
            }
        }
    }
}
