﻿using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class StatementBatch : Statement
    {
        public Statement[] Statements { get; set; }
        public StatementBatch(Token firstToken, IList<Statement> statements) : base(firstToken)
        {
            List<Statement> items = new List<Statement>();
            AddAllItems(items, statements);
            this.Statements = items.ToArray();
        }

        private void AddAllItems(List<Statement> items, IList<Statement> statements)
        {
            Statement item;
            int length = statements.Count;
            for (int i = 0; i < length; ++i)
            {
                item = statements[i];
                if (item is StatementBatch)
                {
                    AddAllItems(items, ((StatementBatch)item).Statements);
                }
                else
                {
                    items.Add(item);
                }
            }
        }

        public override Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            List<Statement> statements = new List<Statement>();
            for (int i = 0; i < this.Statements.Length; ++i)
            {
                Statement stmnt = this.Statements[i].ResolveNamesAndCullUnusedCode(compiler);
                if (stmnt is StatementBatch)
                {
                    statements.AddRange(((StatementBatch)stmnt).Statements);
                }
                else
                {
                    statements.Add(stmnt);
                }
            }

            if (statements.Count == 1)
            {
                return statements[0];
            }

            this.Statements = statements.ToArray();
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            for (int i = 0; i < this.Statements.Length; ++i)
            {
                this.Statements[i].ResolveTypes(varScope, compiler);
            }
        }

        internal override Statement ResolveWithTypeContext(PastelCompiler compiler)
        {
            ResolveWithTypeContext(compiler, this.Statements);
            return this;
        }
    }
}