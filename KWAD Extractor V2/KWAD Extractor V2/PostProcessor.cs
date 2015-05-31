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

        public PostProcessor(Dictionary<string, List<VirtualFile>> files)
        {
            this.files = files;
            foreach (List<VirtualFile> fileList in files.Values)
            {
                Console.WriteLine("Processing SRF1 files...");
                Parallel.ForEach(fileList.Where(file => file.type == "SRF1"), file =>
                    {
                        //processSrf(file);
                    });
                Console.WriteLine("Processing TEX1 files...");
                Parallel.ForEach(fileList.Where(file => file.type == "TEX1"), file =>
                    {
                        processTex(file);
                    });
            }
        }

        private void processBlob(VirtualFile file)
        {
            using (FileStream fStream = File.Open("extracted/" + file.alias, FileMode.Open, FileAccess.Read))
            using (FileStream ofStream = File.Open("processed/" + file.alias, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] temp = new byte[fStream.Length];
                fStream.Read(temp, sigSize, (int)fStream.Length - sigSize);
                ofStream.Write(temp, 0, temp.Length);
            }
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
                    using (MemoryStream dataStream = new MemoryStream(reader.ReadBytes(dataSize), 2, dataSize - 2)) //doesn't include the first two bytes of the array, because of zlibs header
                    using (MemoryStream tex = new MemoryStream())
                    using (DeflateStream defStream = new DeflateStream(dataStream, CompressionMode.Decompress))
                    {
                        defStream.CopyTo(tex);
                        string writePath = "processed/" + Path.ChangeExtension(file.alias, ".png");
                        if (compressed)
                        {
                            saveImage(writePath, width, height, Squish.DecompressImage(tex.ToArray(), width, height, SquishFlags.Dxt5));
                        }
                        else
                        {
                            saveImage(writePath, width, height, tex.ToArray());
                        }
                    }
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
                System.Windows.Media.Matrix affineTransform = new System.Windows.Media.Matrix(affineData[0], affineData[1], affineData[2], affineData[3], affineData[4], affineData[5]);
                string imagePath = (files[file.KWADPath])[texIndex].alias;
                Console.WriteLine("Opening {0}", imagePath);
                using (Bitmap bmp = new Bitmap("processed/" + Path.ChangeExtension(imagePath, ".png")))
                {
                    Rect imgRect = new Rect(0, 0, bmp.Width, bmp.Height);
                    imgRect = Rect.Transform(imgRect, affineTransform);
                    bmp.Clone(new Rectangle((int)imgRect.X, (int)imgRect.Y, (int)imgRect.Width, (int)imgRect.Height), bmp.PixelFormat).Save("processed/" + file.alias, ImageFormat.Png); //blame microsoft for this. Why would you need two rectangle classes?
                }
            }
        }
    }
}
