using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSDFile
{
    public class Thumbnail : RawImageResource
    {
        public Bitmap Image { get; private set; }

        public Thumbnail(ResourceID id, string name) : base(id, name)
        {
        }

        public Thumbnail(PsdBinaryReader psdReader, ResourceID id, string name, int numBytes)
            : base(psdReader, "8BIM", id, name, numBytes)
        {
            using (var memoryStream = new MemoryStream(Data))
            {
                using (var reader = new PsdBinaryReader(memoryStream, psdReader))
                {
                    const int HEADER_LENGTH = 28;
                    var format = reader.ReadUInt32();
                    var width = reader.ReadUInt32();
                    var height = reader.ReadUInt32();
                    var widthBytes = reader.ReadUInt32();
                    var size = reader.ReadUInt32();
                    var compressedSize = reader.ReadUInt32();
                    var bitPerPixel = reader.ReadUInt16();
                    var planes = reader.ReadUInt16();

                    // Raw RGB bitmal
                    if (format == 0)
                    {
                        Image = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
                    }
                    // JPEG bitmap
                    else if (format == 1)
                    {
                        byte[] imgData = reader.ReadBytes(numBytes - HEADER_LENGTH);
                        using (MemoryStream stream = new MemoryStream(imgData))
                        {
                            var bitmap = new Bitmap(stream);
                            Image = (Bitmap)bitmap.Clone();
                        }

                        // Reverse BGR pixels from old thumbnail format
                        if (id == ResourceID.ThumbnailBgr)
                        {
                            //for(int y=0;y<m_thumbnailImage.Height;y++)
                            //  for (int x = 0; x < m_thumbnailImage.Width; x++)
                            //  {
                            //    Color c=m_thumbnailImage.GetPixel(x,y);
                            //    Color c2=Color.FromArgb(c.B, c.G, c.R);
                            //    m_thumbnailImage.SetPixel(x, y, c);
                            //  }
                        }
                    }
                    else
                    {
                        throw new PsdInvalidException("Unknown thumbnail format.");
                    }
                }   
            }  
        }
    }
}
