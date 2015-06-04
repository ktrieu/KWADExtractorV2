using ImageMagick;
using ManagedSquish;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KWAD_Extractor_V2
{
    class PostProcessor
    {
        private int sigSize = 12;
        private int srfCompressionBoolOffset = 20;

        Encoding encoding = Encoding.UTF8;

        Dictionary<string, List<VirtualFile>> files;

        Dictionary<string, List<CachedSRF>> srfCache = new Dictionary<string, List<CachedSRF>>();

        private static Object lockObj = new Object();

        public PostProcessor(Dictionary<string, List<VirtualFile>> files)
        {
            this.files = files;
            foreach (List<VirtualFile> fileList in files.Values)
            {
                Console.WriteLine("Processing SRF1 files...");
                Parallel.ForEach(fileList.Where(file => file.type == "SRF1"), file =>
                    {
                        processSrf(file);
                    });
                Console.WriteLine("Processing TEX1 files...");
                Parallel.ForEach(fileList.Where(file => file.type == "TEX1"), file =>
                    {
                        processTex(file);
                    });
                Parallel.ForEach(fileList.Where(file => file.type == "BLOB"), file =>
                {
                    processBlob(file);
                });
            }
            processSrfCache();
        }

        private void processBlob(VirtualFile file)
        {
            Directory.CreateDirectory("processed/" + Path.GetDirectoryName(file.alias));
            using (FileStream fStream = File.Open("extracted/" + file.alias, FileMode.Open, FileAccess.Read))
            using (FileStream ofStream = File.Open("processed/" + file.alias, FileMode.Create, FileAccess.Write))
            {
                byte[] temp = new byte[fStream.Length];
                fStream.Read(temp, 0, temp.Length);
                ofStream.Write(temp, sigSize, temp.Length - sigSize);
            }
            switch (Path.GetExtension(file.alias))
            {
                case ".fsb":
                    processFsb(file);
                    break;
            }
        }

        private void processFsb(VirtualFile file)
        {
            string password = "mint78run52";
            Process decfsb = new Process();
            decfsb.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\exes\\decfsb.exe"; //this is horrible, but most likely the only way it works
            decfsb.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory() + "\\processed\\" + Path.GetDirectoryName(file.alias);
            decfsb.StartInfo.Arguments = Path.GetFileName(file.alias) + " " + Path.GetFileNameWithoutExtension(file.alias) + "_dec.fsb" +  " " + password;
            decfsb.StartInfo.UseShellExecute = false;
            decfsb.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            decfsb.StartInfo.RedirectStandardOutput = true;
            decfsb.Start();
            decfsb.WaitForExit();
            Process fsbExt = new Process();
            fsbExt.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\exes\\fsbext.exe"; //this is horrible, but most likely the only way it works
            fsbExt.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory() + "\\processed\\" + Path.GetDirectoryName(file.alias);
            Directory.CreateDirectory(fsbExt.StartInfo.WorkingDirectory + "/" + Path.GetFileNameWithoutExtension(file.alias));
            fsbExt.StartInfo.Arguments = "-d " + Path.GetFileNameWithoutExtension(file.alias) + " " + Path.GetFileNameWithoutExtension(file.alias) + "_dec.fsb";
            fsbExt.StartInfo.UseShellExecute = false;
            fsbExt.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            fsbExt.StartInfo.RedirectStandardOutput = true;
            fsbExt.Start();
        }   

        private void processSrf(VirtualFile file)
        {
            using (FileStream fStream = File.Open("extracted/" + file.alias, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fStream))
            {
                reader.ReadBytes(srfCompressionBoolOffset); //throw away some bytes we're not currently using
                bool compressed = Convert.ToBoolean(reader.ReadInt32());
                int mipCount = reader.ReadInt32();
                int mipDataSize = reader.ReadInt32();
                for (int i = 0; i < mipCount; i++)
                {
                    int size = reader.ReadInt32();
                    int width = reader.ReadInt32();
                    int height = reader.ReadInt32(); //We're throwing these away for now, but they're being read so we can use them later and to advance the reader
                    int dataSize = reader.ReadInt32();
                    MemoryStream tex = new MemoryStream();
                    using (MemoryStream dataStream = new MemoryStream(reader.ReadBytes(dataSize), 2, dataSize - 2)) //doesn't include the first two bytes of the array, because of zlibs header
                    using (DeflateStream defStream = new DeflateStream(dataStream, CompressionMode.Decompress))
                    {
                        defStream.CopyTo(tex);
                    }
                    string writePath = "processed/" + Path.ChangeExtension(file.alias, ".png");
                    if (compressed)
                    {
                        saveImage(writePath, width, height, Squish.DecompressImage(tex.ToArray(), width, height, SquishFlags.Dxt5));
                    }
                    else
                    {
                        saveImage(writePath, width, height, tex.ToArray());
                    }
                    tex.Dispose();
                }
            }
        }

        private unsafe void saveImage(string path, int width, int height, byte[] img)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            fixed (byte* dataPtr = img)
            {
                using (Bitmap bmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, new IntPtr(dataPtr)))
                {
                    bmp.Save(path, ImageFormat.Png);
                }
            }
        }

        private void processTex(VirtualFile file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName("processed/" + file.alias));
            using (FileStream fStream = File.Open("extracted/" + file.alias, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fStream))
            {
                float[] affineData = new float[6];
                int texIndex, width, height;
                reader.ReadBytes(sigSize); //throw away header
                texIndex = reader.ReadInt32();
                width = reader.ReadInt32();
                height = reader.ReadInt32();
                affineData[0] = reader.ReadSingle();
                affineData[1] = reader.ReadSingle();
                affineData[2] = reader.ReadSingle();
                affineData[3] = reader.ReadSingle();
                affineData[4] = reader.ReadSingle();
                affineData[5] = reader.ReadSingle();
                string imagePath = (files[file.KWADPath])[texIndex].alias;
                lock (lockObj)
                {
                    if (!srfCache.ContainsKey(imagePath))
                    {
                        srfCache.Add(imagePath, new List<CachedSRF>());
                    }
                    srfCache[imagePath].Add(new CachedSRF(affineData, imagePath, file.alias));
                }
            }
        }

        private void processSrfCache()
        {
            foreach (string key in srfCache.Keys)
            {
                List<CachedSRF> srfs = srfCache[key];
                using (FileStream fStream = File.Open(Path.ChangeExtension("processed/" + key, ".png"), FileMode.Open, FileAccess.Read))
                using (MagickImage atlas = new MagickImage(fStream))
                {
                    Rect imgRect = new Rect(0, 0, atlas.Width, atlas.Height);
                    Parallel.ForEach(srfs, srf =>
                        {
                            if (!File.Exists("processed/" + srf.alias))
                            {
                                System.Windows.Media.Matrix affineTransform = new System.Windows.Media.Matrix(srf.transform[0], srf.transform[1], srf.transform[2], srf.transform[3], srf.transform[4], srf.transform[5]);
                                Rect transformRect = Rect.Transform(imgRect, affineTransform);
                                transformRect.X *= (int)imgRect.Width;
                                transformRect.Y *= (int)imgRect.Height;
                                Rectangle rectangle = new Rectangle((int)transformRect.X, (int)transformRect.Y, (int)transformRect.Width, (int)transformRect.Height);
                                using (MagickImage sprite = new MagickImage(atlas))
                                {
                                    MagickGeometry geo = new MagickGeometry(rectangle);
                                    geo.IgnoreAspectRatio = true;
                                    sprite.Crop(geo);
                                    sprite.Write(new FileStream("processed/" + srf.alias, FileMode.OpenOrCreate), MagickFormat.Png);
                                }
                            }
                        });         
                }
            }
        }
    }
}
