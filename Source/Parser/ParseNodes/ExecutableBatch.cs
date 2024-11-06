using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class ExecutableBatch : Executable
    {
        public Executable[] Executables { get; set; }
        public ExecutableBatch(Token firstToken, IList<Executable> executables) : base(firstToken)
        {
            List<Executable> items = new List<Executable>();
            AddAllItems(items, executables);
            Executables = items.ToArray();
        }

        private void AddAllItems(List<Executable> items, IList<Executable> executables)
        {
            Executable item;
            int length = executables.Count;
            for (int i = 0; i < length; ++i)
            {
                item = executables[i];
                if (item is ExecutableBatch)
                {
                    AddAllItems(items, ((ExecutableBatch)item).Executables);
                }
                else
                {
                    items.Add(item);
                }
            }
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            List<Executable> executables = new List<Executable>();
            for (int i = 0; i < Executables.Length; ++i)
            {
                Executable exec = Executables[i].ResolveNamesAndCullUnusedCode(compiler);
                if (exec is ExecutableBatch)
                {
                    executables.AddRange(((ExecutableBatch)exec).Executables);
                }
                else
                {
                    executables.Add(exec);
                }
            }

            if (executables.Count == 1)
            {
                return executables[0];
            }

            Executables = executables.ToArray();
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            for (int i = 0; i < Executables.Length; ++i)
            {
                Executables[i].ResolveTypes(varScope, compiler);
            }
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            ResolveWithTypeContext(compiler, Executables);
            return this;
        }
    }
}
