using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System.Collections.Generic;
using System.Text;

// I'm so sorry.

namespace Pastel.Transpilers.Python
{
    /*
        A switch statement has some cases and possibly a default.
        These cases may be strings or integers.
        If there is no default, then we pretend there's one with an empty code chunk.

        Each case/default code chunk is given an ID number from 0 to n - 1

        For every single switch statement that is translated in Python, there is a dictionary
        that is serialized after the function definition. This dictionary has the following name format:
        "swlookup__{function name}__{number that is allocated starting from 1 on a per function basis}"

        In the actual code, the switch statement is serialized as a lookup in that dictionary with a .get
        The value that is looked up is the switch condition value and the default fallback value is the code chunk ID of the default.

        This is assigned into a value called sc_{ID}

        The switch code itself is a binary search tree of if statements.
     */
    internal class PythonFakeSwitchStatement
    {
        private string functionName;
        private int switchId;
        private ICompilationEntity owner;
        private Dictionary<InlineConstant, int> expressionsToChunkIds;
        private Dictionary<int, Statement[]> chunkIdsToCode;

        public int DefaultId { get; set; }

        private string conditionVariableName = null;
        public string ConditionVariableName
        {
            get
            {
                if (conditionVariableName == null)
                {
                    conditionVariableName = "sc_" + switchId;
                }
                return conditionVariableName;
            }
        }

        private string dictionaryGlobalName = null;
        public string DictionaryGlobalName
        {
            get
            {
                if (dictionaryGlobalName == null)
                {
                    dictionaryGlobalName = "swlookup__" + functionName + "__" + switchId;
                }
                return dictionaryGlobalName;
            }
        }

        public static PythonFakeSwitchStatement Build(SwitchStatement switchStatement, int switchId, string functionName)
        {
            ICompilationEntity owner = switchStatement.Condition.Owner;
            Dictionary<InlineConstant, int> expressionToId = new Dictionary<InlineConstant, int>();
            Dictionary<int, Statement[]> codeById = new Dictionary<int, Statement[]>();
            int? nullableDefaultId = null;
            Statement[] defaultCode = null;
            for (int i = 0; i < switchStatement.Chunks.Length; ++i)
            {
                SwitchStatement.SwitchChunk chunk = switchStatement.Chunks[i];
                int currentId = i;
                Expression[] cases = chunk.Cases;
                for (int j = 0; j < cases.Length; ++j)
                {
                    InlineConstant caze = (InlineConstant)cases[j];
                    if (caze == null)
                    {
                        nullableDefaultId = currentId;
                        defaultCode = chunk.Code;
                    }
                    else
                    {
                        expressionToId[caze] = currentId;
                    }
                }

                codeById[currentId] = chunk.Code;
            }

            int defaultId;
            if (nullableDefaultId != null)
            {
                defaultId = nullableDefaultId.Value;
                if (!codeById.ContainsKey(defaultId))
                {
                    codeById[defaultId] = defaultCode;
                }
            }
            else
            {
                defaultId = codeById.Count;
                codeById[defaultId] = new Statement[0];
            }

            return new PythonFakeSwitchStatement(functionName, switchId, defaultId, expressionToId, codeById, owner);
        }

        private PythonFakeSwitchStatement(
            string functionName,
            int switchId,
            int defaultChunkId,
            Dictionary<InlineConstant, int> expressionsToChunkIds,
            Dictionary<int, Statement[]> chunkIdsToCode,
            ICompilationEntity owner)
        {
            this.owner = owner;
            this.functionName = functionName;
            this.switchId = switchId;
            DefaultId = defaultChunkId;
            this.expressionsToChunkIds = expressionsToChunkIds;
            this.chunkIdsToCode = chunkIdsToCode;
        }

        public string GenerateGlobalDictionaryLookup()
        {
            StringBuilder dictionaryBuilder = new StringBuilder();
            dictionaryBuilder.Append(DictionaryGlobalName);
            dictionaryBuilder.Append(" = { ");

            bool isInteger = false;
            bool first = true;
            foreach (InlineConstant ic in expressionsToChunkIds.Keys)
            {
                if (first)
                {
                    isInteger = ic.ResolvedType.RootValue == "int";
                    first = false;
                }
                else
                {
                    dictionaryBuilder.Append(", ");
                }

                int id = expressionsToChunkIds[ic];
                if (isInteger)
                {
                    dictionaryBuilder.Append((int)ic.Value);
                }
                else
                {
                    dictionaryBuilder.Append(CodeUtil.ConvertStringValueToCode((string)ic.Value));
                }
                dictionaryBuilder.Append(": ");
                dictionaryBuilder.Append(expressionsToChunkIds[ic]);
            }
            dictionaryBuilder.Append(" }");
            return dictionaryBuilder.ToString();
        }

        public IfStatement GenerateIfStatementBinarySearchTree()
        {
            return GenerateIfStatementBinarySearchTree(0, chunkIdsToCode.Count - 1, chunkIdsToCode);
        }

        private IfStatement GenerateIfStatementBinarySearchTree(int lowId, int highId, Dictionary<int, Statement[]> codeById)
        {
            if (lowId + 2 == highId)
            {
                /*
                    if id == lowId:
                      ...
                    elif id == midId:
                      ...
                    else:
                      ...
                */

                int midId = lowId + 1;
                IfStatement inner = BuildIfStatement(midId, "==", codeById[midId], codeById[highId]);
                IfStatement outer = BuildIfStatement(lowId, "==", codeById[lowId], new Statement[] { inner });
                return outer;
            }

            if (lowId + 1 == highId)
            {
                /*
                    if id == lowId:
                      ...
                    else:
                      ...

                */
                return BuildIfStatement(lowId, "==", codeById[lowId], codeById[highId]);
            }

            /*
                if id < floor(mean):
                  recurse through lowId to floor(mean)
                else:
                  recurse through floor(mean) + 1 to highId

            */
            int midId1 = (lowId + highId) / 2;
            int midId2 = midId1 + 1;

            IfStatement lower = GenerateIfStatementBinarySearchTree(lowId, midId1, codeById);
            IfStatement upper = GenerateIfStatementBinarySearchTree(midId2, highId, codeById);
            return BuildIfStatement(midId2, "<", new Statement[] { lower }, new Statement[] { upper });
        }

        private IfStatement BuildIfStatement(int id, string op, Statement[] trueCode, Statement[] falseCode)
        {
            Token equalsToken = Token.CreateDummyToken(op);
            Variable variable = new Variable(Token.CreateDummyToken(ConditionVariableName), owner);
            variable.ApplyPrefix = false;
            Expression condition = new OpChain(new Expression[] { variable, InlineConstant.Of(id, owner) }, new Token[] { equalsToken });

            return new IfStatement(
                Token.CreateDummyToken("if"),
                condition,
                TrimBreak(trueCode),
                Token.CreateDummyToken("else"),
                TrimBreak(falseCode));
        }

        private Statement[] TrimBreak(Statement[] statements)
        {
            // TODO: compile time check for absence of break in switch statement code
            // aside from the one at the end of each case. This will simply be a limitation
            // of Pastel for the sake of Python compatibility.
            int length = statements.Length;
            if (length == 0) return statements;
            Statement last = statements[length - 1];
            if (last is BreakStatement)
            {
                Statement[] trimmed = new Statement[length - 1];
                for (int i = length - 2; i >= 0; --i)
                {
                    trimmed[i] = statements[i];
                }
                return trimmed;
            }
            return statements;
        }
    }
}
