using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Pastel.Parser;

namespace Pastel
{
    internal class ProjectConfig
    {
        public ProjectConfig()
        {
            this.Language = Language.NONE;
            this.Flags = new Dictionary<string, bool>();
            this.ExtensionTypeDefinitions = new List<string>();
            this.ExtensionPlatformValues = new Dictionary<string, string>();
            this.ExtensionPlatformValuesDefinitionTokens = new Dictionary<string, Token>();
            this.Imports = new HashSet<string>();
        }

        public override string ToString()
        {
            string s = "Pastel Config";
            if (this.Path != null)
            {
                s += ": " + this.Path;
            }
            return s;
        }

        public string Path { get; set; } // reflects only the top level config, not any base
        public string Directory { get { return System.IO.Path.GetDirectoryName(this.Path); } }

        public Language Language { get; set; }
        public Dictionary<string, bool> Flags { get; set; }
        public string Source { get; set; }
        public List<string> ExtensionTypeDefinitions { get; set; }
        public Dictionary<string, string> ExtensionPlatformValues { get; set; }
        public Dictionary<string, Token> ExtensionPlatformValuesDefinitionTokens { get; set; }
        public string OutputDirStructs { get; set; }
        public string OutputFileFunctions { get; set; }
        public string WrappingClassNameForFunctions { get; set; }
        public string? NamespaceForStructs { get; set; }
        public string? NamespaceForFunctions { get; set; }
        public HashSet<string> Imports { get; set; }

        public static ProjectConfig Parse(string path, string targetId)
        {
            string configContents = System.IO.File.ReadAllText(path);
            ProjectConfig config = new ProjectConfig() { Path = path };
            ParseImpl(config, configContents, path, targetId);
            return config;
        }

        // multiple colons are valid, but no colon is not.
        // if the value contains a colon, then only split on the first one.
        // results should be trimmed.
        private static string[] SplitOnColon(string s)
        {
            int colonIndex = s.IndexOf(':');
            if (colonIndex == -1) return null;
            string key = s.Substring(0, colonIndex).Trim();
            string value = s.Substring(colonIndex + 1).Trim();
            return new string[] { key, value };
        }

        private static string CanonicalizeDirectory(string directory, string relativePath)
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, relativePath));
        }

        private static object GetCanonicalJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null: return null;
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Number: return element.GetInt32();
                case JsonValueKind.String: return element.GetString();
                case JsonValueKind.Array:
                    List<object> items = new List<object>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        items.Add(GetCanonicalJsonValue(item));
                    }
                    return items.ToArray();
                case JsonValueKind.Object:
                    Dictionary<string, object> obj = new Dictionary<string, object>();
                    foreach (JsonProperty kvp in element.EnumerateObject())
                    {
                        obj.Add(kvp.Name, GetCanonicalJsonValue(kvp.Value));
                    }
                    return obj;
                default: throw new InvalidOperationException();
            }
        }

        private static Dictionary<string, object> ParseConfigFile(string targetId, string content)
        {
            Dictionary<string, object> root;
            try
            {
                System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(content);
                root = GetCanonicalJsonValue(doc.RootElement) as Dictionary<string, object>;
            }
            catch (Exception)
            {
                return null;
            }
            Dictionary<string, object> flattened = new Dictionary<string, object>(root);
            Dictionary<string, object> platformValues = new Dictionary<string, object>();
            if (targetId != null && root.ContainsKey("targets"))
            {
                platformValues = ((root["targets"] as object[]) ?? new object[0])
                                    .OfType<Dictionary<string, object>>()
                                    .FirstOrDefault(target =>
                                        target.ContainsKey("name") &&
                                        (target["name"] as string) == targetId);
                if (platformValues == null)
                {
                    throw new UserErrorException("Target not found: '" + targetId + "'");
                }
                root.Remove("targets");
            }

            foreach (string key in platformValues.Keys.Where(k => k != "name"))
            {
                switch (key)
                {
                    case "flags":
                        if (!root.ContainsKey(key) || !(root[key] is object[]))
                        {
                            root[key] = Array.Empty<object>();
                        }
                        root[key] = ((object[])root[key]).Concat(platformValues[key] as object[] ?? new object[0]).ToArray();
                        break;
                    default:
                        flattened[key] = platformValues[key];
                        break;
                }
            }

            return flattened;
        }

        private static void ParseImpl(ProjectConfig config, string configContents, string originalPath, string targetId)
        {
            string directory = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(originalPath));
            Dictionary<string, object> data = ParseConfigFile(targetId, configContents);
            if (data == null) throw new UserErrorException("Invalid JSON document: " + originalPath);

            string source = data.ContainsKey("source") ? data["source"] as string : null;
            if (source == null) throw new UserErrorException("Build file is missing a string 'source' field.");
            config.Source = System.IO.Path.IsPathFullyQualified(source)
                ? source
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(originalPath, "..", source));

            if (data.ContainsKey("imports") && data["imports"] is object[])
            {
                foreach (string import in ((object[])data["imports"]).OfType<string>())
                {
                    config.Imports.Add(import);
                }
            }

            Dictionary<string, string> outputInfo = new Dictionary<string, string>();
            if (data.ContainsKey("output") && data["output"] is Dictionary<string, object>)
            {
                Dictionary<string, object> output = (Dictionary<string, object>)data["output"];
                foreach (string key in output.Keys)
                {
                    if (output[key] is string)
                    {
                        outputInfo[key] = (string)output[key];
                    }
                }
            }

            switch (((data.ContainsKey("language") ? data["language"] : null) as string ?? "").ToLower())
            {
                case "c":
                    config.Language = Language.C;
                    break;
                case "commonscript":
                    config.Language = Language.COMMONSCRIPT;
                    break;
                case "cs":
                case "csharp":
                case "c#":
                    config.Language = Language.CSHARP;
                    break;
                case "go":
                case "golang":
                    config.Language = Language.GO;
                    break;
                case "java":
                    config.Language = Language.JAVA;
                    break;
                case "js":
                case "javascript":
                    config.Language = Language.JAVASCRIPT;
                    break;
                case "php":
                    config.Language = Language.PHP;
                    break;
                case "python":
                case "py":
                    config.Language = Language.PYTHON;
                    break;
                default:
                    throw new UserErrorException("No valid language was defined in the build file. Choices: c# java javascript php python");
            }

            if (outputInfo.ContainsKey("structs-path"))
            {
                config.OutputDirStructs = CanonicalizeDirectory(directory, outputInfo["structs-path"]);
            }
            if (outputInfo.ContainsKey("functions-path"))
            {
                config.OutputFileFunctions = CanonicalizeDirectory(directory, outputInfo["functions-path"]);
            }
            if (outputInfo.ContainsKey("functions-wrapper-class"))
            {
                config.WrappingClassNameForFunctions = outputInfo["functions-wrapper-class"];
            }
            if (outputInfo.ContainsKey("namespace"))
            {
                // TODO: Merge these properties.
                string ns = outputInfo["namespace"];
                config.NamespaceForFunctions = ns;
                config.NamespaceForStructs = ns;
            }

            Dictionary<string, object>[] flagList =
                ((data.ContainsKey("flags")
                    ? data["flags"]
                    : null) as object[] ?? new object[0])
                .OfType<Dictionary<string, object>>()
                .ToArray();
            foreach (Dictionary<string, object> flag in flagList)
            {
                string name = (flag.ContainsKey("name") ? flag["name"] : "") as string ?? "";
                bool value = flag.ContainsKey("value") ? (flag["value"] is bool) ? ((bool)flag["value"]) : false : false;
                config.Flags[name] = value;
            }

            string[] extensionTypeDefs =
                ((data.ContainsKey("extensionTypes")
                    ? data["extensionTypes"]
                    : null) as object[] ?? [])
                .OfType<string>()
                .ToArray();
            foreach (string extensionTypeDef in extensionTypeDefs)
            {
                config.ExtensionTypeDefinitions.Add(extensionTypeDef);
            }

            string[] extensions =
                ((data.ContainsKey("extensions")
                    ? data["extensions"]
                    : null) as object[] ?? [])
                .OfType<string>()
                .ToArray();
            foreach (string extension in extensions)
            {
                string[] parts = SplitOnColon(extension);
                config.ExtensionPlatformValues[parts[0]] = parts[1].Trim();
                config.ExtensionPlatformValuesDefinitionTokens[parts[0].Trim()] = new Token("", originalPath, 1, 0, TokenType.PUNCTUATION);
            }
        }

        public List<ExtensibleFunction> GetExtensibleFunctions()
        {
            string typeDefinitionsRawCode = string.Join("\n", this.ExtensionTypeDefinitions);
            List<ExtensibleFunction> output = new List<ExtensibleFunction>();
            List<ExtensibleFunction> typeDefinitions = ExtensibleFunctionMetadataParser.Parse(this.Path, typeDefinitionsRawCode);
            Dictionary<string, ExtensibleFunction> typeDefinitionsLookup = new Dictionary<string, ExtensibleFunction>();
            foreach (ExtensibleFunction exFn in typeDefinitions)
            {
                typeDefinitionsLookup[exFn.Name] = exFn;
            }

            foreach (string exFunctionName in this.ExtensionPlatformValues.Keys.OrderBy(k => k))
            {
                if (!typeDefinitionsLookup.ContainsKey(exFunctionName))
                {
                    Token throwToken = this.ExtensionPlatformValuesDefinitionTokens[exFunctionName];
                    throw new ParserException(throwToken, "No type information defined for extensible function '" + exFunctionName + "'");
                }

                ExtensibleFunction exFn = typeDefinitionsLookup[exFunctionName];
                exFn.Translation = this.ExtensionPlatformValues[exFunctionName];
                output.Add(exFn);
            }
            return output;
        }
    }
}
