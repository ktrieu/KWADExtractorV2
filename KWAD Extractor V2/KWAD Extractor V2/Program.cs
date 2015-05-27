using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KWAD_Extractor_V2
{
    class Program
    {

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            KWADExtractor extractor = new KWADExtractor("KWAD/", "extracted/");
            watch.Stop();
            Console.WriteLine("Extraction complete. Press any key to exit...");
            Console.WriteLine("Extracted in: " + watch.ElapsedMilliseconds + "ms");
            Console.ReadKey();
        }
    }
}
