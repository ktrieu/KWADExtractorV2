using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * A static util class, so I can put things like logging and file IO into the same place. Sticking it all in one place is probably
 * ill-advised, but it didn't seem worth making a separate class for everything.
 */

namespace KWAD_Extractor_V2
{
    class Util
    {
        public static FileStream getFileStream(string path, FileMode mode, FileAccess access)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            FileStream fStream = null;
            try
            {
                fStream = File.Open(path, mode, access);
            }
            catch (IOException e)
            {
                //Don't want to spam the log here, and it seems like a very silent thing
                //Console.WriteLine("Could not acquire file handle for {0}... retrying", path); 
                //Console.WriteLine(e.Message);
            }
            return fStream;
        }
    }
}
