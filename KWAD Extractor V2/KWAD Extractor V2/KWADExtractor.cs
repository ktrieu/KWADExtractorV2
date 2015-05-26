using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class KWADExtractor
    {
        string extractDir;

        Dictionary<string, KWADLoader> loaders = new Dictionary<string, KWADLoader>();

        public KWADExtractor(string KWADDir, string extractDir)
        {
            this.extractDir = extractDir;
            Directory.CreateDirectory(extractDir);
            string[] files = Directory.GetFiles(KWADDir);
            foreach (string file in files)
            {
                loaders.Add(file, new KWADLoader(file));
            }
        }

        public void extract()
        {
            foreach (var pair in loaders)
            {
                KWADLoader loader = pair.Value;
                string name = pair.Key;
                Console.WriteLine("Extracting " + name);
                Console.WriteLine(loader.resourceCount + " files found");
                List<VirtualFile> files = loader.files;
                foreach (var file in files)
                {
                    string path = Path.Combine(extractDir, file.alias);
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    File.WriteAllBytes(path, loader.extractRange(file.offset, file.size));
                }
            }
        }

    }
}
