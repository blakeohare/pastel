using Pastel.Transpilers;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    internal static class TranspilationHelperCodeUtil
    {
        public static string InjectTranspilationHelpers(AbstractTranspiler transpiler, string userCode)
        {
            Dictionary<string, string> codeChunks = GetCodeChunks(transpiler, userCode);
            string[] chunkOrder = GetChunkOrder(codeChunks, userCode);

            List<string> finalCodeBuilder = [];
            foreach (string chunkId in chunkOrder)
            {
                finalCodeBuilder.Add(codeChunks[chunkId]);
            }

            string finalCode = string.Join("\n\n", finalCodeBuilder);
            return finalCode;
        }

        private static Dictionary<string, string> GetCodeChunks(AbstractTranspiler transpiler, string userCode)
        {
            string resourcePath = transpiler.HelperCodeResourcePath;
            Dictionary<string, string> output = [];
            if (resourcePath == null) return output;

            string helperCode = ResourceReader.ReadTextFile(resourcePath);

            string? currentId = null;
            List<string> currentChunk = [];
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

        private static string[] GetChunkOrder(Dictionary<string, string> chunksByMarker, string userCode)
        {
            HashSet<string> allMarkers = [.. chunksByMarker.Keys];
            List<string> nonPstPrefixThings = [];
            List<string> pstPrefixedThings = [];
            foreach (string marker in chunksByMarker.Keys.Where(m => m.Length > 0).OrderBy(m => m))
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
            Dictionary<string, string[]> dependencies = [];
            foreach (string marker in chunksByMarker.Keys.OrderBy(m => m))
            {
                string code = chunksByMarker[marker];
                dependencies[marker] = FindUsedMarkers(code, nonPstPrefixThings, pstPrefixedThings, allMarkers, marker);
            }

            dependencies["PST_RegisterExtensibleCallback"] = ["PST_ExtCallbacks"];
            dependencies[""] = [.. dependencies[""], "PST_RegisterExtensibleCallback"];

            List<string> orderedKeys = [];

            PopulateOrderedChunkKeys("", orderedKeys, dependencies, new Dictionary<string, int>());

            return [.. orderedKeys];
        }

        private static void PopulateOrderedChunkKeys(
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
                throw new System.InvalidOperationException("Dependency loop: " + currentItem + " depends on itself indirectly.");
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

        private static string[] FindUsedMarkers(
            string code,
            IList<string> nonPstMarkers,
            IList<string> pstMarkers,
            HashSet<string> allMarkers,
            string exclusion)
        {
            List<string> usedMarkers = [];
            foreach (string nonPstMarker in nonPstMarkers)
            {
                if (exclusion != nonPstMarker && code.Contains(nonPstMarker))
                {
                    usedMarkers.Add(nonPstMarker);
                }
            }

            string[] pstParts = code.Split("PST");
            for (int i = 1; i < pstParts.Length; ++i)
            {
                string markerName = GetMarkerNameHacky(pstParts[i]);
                if (allMarkers.Contains(markerName) && exclusion != markerName)
                {
                    usedMarkers.Add(markerName);
                }
            }

            return [.. usedMarkers];
        }

        private static string GetMarkerNameHacky(string potentialMarkerNameWithoutPst)
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
