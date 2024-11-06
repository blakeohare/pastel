using System.Collections.Generic;

namespace Pastel
{
    internal class ExtensionSet
    {
        public Dictionary<string, string> ExtensibleFunctionTranslations { get; private set; }
        public Dictionary<string, ExtensibleFunction> ExtensionLookup { get; private set; }
        private List<ExtensibleFunction> extensibleFunctions = new List<ExtensibleFunction>();
        private bool extensibleFunctionsLocked = false;

        public void LockExtensibleFunctions()
        {
            if (this.extensibleFunctionsLocked) throw new System.InvalidOperationException();
            this.extensibleFunctionsLocked = true;
            this.ExtensionLookup = new Dictionary<string, ExtensibleFunction>();
        }

        public void AddExtensibleFunction(ExtensibleFunction fn, string translation)
        {
            if (this.extensibleFunctionsLocked) throw new System.InvalidOperationException();
            this.extensibleFunctions.Add(fn);
            this.ExtensibleFunctionTranslations[fn.Name] = translation;
            this.ExtensionLookup[fn.Name] = fn;
        }
    }
}
