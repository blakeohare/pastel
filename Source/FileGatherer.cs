using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel
{
    class FileGatherer
    {
        private string rootDirectory;
        private string fileExtension;

        public FileGatherer(string rootDirectory, string fileExtension)
        {
            this.rootDirectory = System.IO.Path.GetFullPath(rootDirectory);
            this.fileExtension = fileExtension.ToLower();
        }

        public Dictionary<string, string> GatherFiles()
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            this.GatherFilesImpl(this.rootDirectory, output);
            return output;
        }

        private void GatherFilesImpl(string currentDirectory, Dictionary<string, string> output)
        {
            foreach (string filename in System.IO.Directory.GetFiles(currentDirectory))
            {
                if (filename.ToLower().EndsWith(this.fileExtension))
                {
                    string absolutePath = System.IO.Path.Combine(currentDirectory, filename);
                    string relativePath = absolutePath.Substring(this.rootDirectory.Length + 1);
                    output[relativePath.Replace('\\', '/')] = System.IO.File.ReadAllText(absolutePath);
                }
            }

            foreach (string directory in System.IO.Directory.GetDirectories(currentDirectory))
            {
                this.GatherFilesImpl(System.IO.Path.Combine(currentDirectory, directory), output);
            }
        }
    }
}
