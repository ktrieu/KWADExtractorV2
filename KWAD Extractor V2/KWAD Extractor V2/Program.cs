using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles("KWAD/");
            Dictionary<string, KWADLoader> loaders = new Dictionary<string, KWADLoader>();
            foreach (string file in files)
            {
                loaders.Add(file, new KWADLoader(file));
            }
            Console.WriteLine("Extraction complete. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
