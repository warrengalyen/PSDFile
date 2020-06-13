using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using PSDFile.Compression;

namespace PSDFile
{
    [DebuggerDisplay("Name = {Name}")]
    public class Layer
    {
        internal PsdFile PsdFile { get; private set; }

        /// <summary>
        /// The rectangle containing the contents of the layer.
        /// </summary>
        public Rectangle Rect { get; set; }

        /// <summary>
        /// Image channels.
        /// </summary>
        public ChannelList Channels { get; private set; }

        /// <summary>
        /// Returns alpha channel if it exists, otherwise null.
        /// </summary>
        public Channel AlphaChannel => Channels.SingleOrDefault(x => x.ID == -1);

        private string blendModeKey;
        /// <summary>
        /// Photoshop blend mode key for the layer
        /// </summary>
        public string BlendModeKey
        {
            get => blendModeKey;
            set
            {
                if (value.Length != 4)
                {
                    throw new ArgumentException(
                      $"{nameof(BlendModeKey)} must be 4 characters in length.");
                }
                blendModeKey = value;
            }
        }

        /// <summary>
        /// 0 = transparent ... 255 = opaque
        /// </summary>
        public byte Opacity { get; set; }

        /// <summary>
        /// false = base, true = non-base
        /// </summary>
        public bool Clipping { get; set; }

        private static int protectTransBit = BitVector32.CreateMask();
        private static int visibleBit = BitVector32.CreateMask(protectTransBit);
        BitVector32 flags = new BitVector32();

        /// <summary>
        /// If true, the layer is visible.
        /// </summary>
        public bool Visible
        {
            get => !flags[visibleBit];
            set => flags[visibleBit] = !value;
        }

        /// <summary>
        /// Protect the transparency
        /// </summary>
        public bool ProtectTrans
        {
            get => flags[protectTransBit];
            set => flags[protectTransBit] = value;
        }

        /// <summary>
        /// The descriptive layer name
        /// </summary>
        public string Name { get; set; }

        public BlendingRanges BlendingRangesData { get; set; }

        public MaskInfo Masks { get; set; }

        public List<LayerInfo> AdditionalInfo { get; set; }

        ///////////////////////////////////////////////////////////////////////////

        public Layer(PsdFile psdFile)
        {
            PsdFile = psdFile;
            Rect = Rectangle.Empty;
            Channels = new ChannelList();
            BlendModeKey = PsdBlendMode.Normal;
            AdditionalInfo = new List<LayerInfo>();
        }

        public Layer(PsdBinaryReader reader, PsdFile psdFile)
          : this(psdFile)
        {
            Util.DebugMessage(reader.BaseStream, "Load, Begin, Layer");

            Rect = reader.ReadRectangle();

            //-----------------------------------------------------------------------
            // Read channel headers.  Image data comes later, after the layer header.

            int numberOfChannels = reader.ReadUInt16();
            for (int channel = 0; channel < numberOfChannels; channel++)
            {
                var ch = new Channel(reader, this);
                Channels.Add(ch);
            }

            //-----------------------------------------------------------------------
            // 

            var signature = reader.ReadAsciiChars(4);
            if (signature != "8BIM")
                throw (new PsdInvalidException("Invalid signature in layer header."));

            BlendModeKey = reader.ReadAsciiChars(4);
            Opacity = reader.ReadByte();
            Clipping = reader.ReadBoolean();

            var flagsByte = reader.ReadByte();
            flags = new BitVector32(flagsByte);
            reader.ReadByte(); //padding

            //-----------------------------------------------------------------------

            // This is the total size of the MaskData, the BlendingRangesData, the 
            // Name and the AdjustmentLayerInfo.
            var extraDataSize = reader.ReadUInt32();
            var extraDataStartPosition = reader.BaseStream.Position;

            Masks = new MaskInfo(reader, this);
            BlendingRangesData = new BlendingRanges(reader, this);
            Name = reader.ReadPascalString(4);

            //-----------------------------------------------------------------------
            // Process Additional Layer Information

            long adjustmentLayerEndPos = extraDataStartPosition + extraDataSize;
            while (reader.BaseStream.Position < adjustmentLayerEndPos)
            {
                var layerInfo = LayerInfoFactory.Load(reader,
                  psdFile: this.PsdFile,
                  globalLayerInfo: false);
                AdditionalInfo.Add(layerInfo);
            }

            foreach (var adjustmentInfo in AdditionalInfo)
            {
                switch (adjustmentInfo.Key)
                {
                    case "luni":
                        Name = ((LayerUnicodeName)adjustmentInfo).Name;
                        break;
                }
            }

            Util.DebugMessage(reader.BaseStream, $"Load, End, Layer, {Name}");

            PsdFile.LoadContext.OnLoadLayerHeader(this);
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create ImageData for any missing channels.
        /// </summary>
        public void CreateMissingChannels()
        {
            var channelCount = this.PsdFile.ColorMode.MinChannelCount();
            for (short id = 0; id < channelCount; id++)
            {
                if (!this.Channels.ContainsId(id))
                {
                    var size = this.Rect.Height * this.Rect.Width;

                    var ch = new Channel(id, this);
                    ch.ImageData = new byte[size];
                    unsafe
                    {
                        fixed (byte* ptr = &ch.ImageData[0])
                        {
                            Util.Fill(ptr, ptr + size, (byte)255);
                        }
                    }

                    this.Channels.Add(ch);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        public void PrepareSave()
        {
            foreach (var ch in Channels)
            {
                ch.CompressImageData();
            }

            // Create or update the Unicode layer name to be consistent with the
            // ANSI layer name.
            var layerUnicodeNames = AdditionalInfo.Where(x => x is LayerUnicodeName);
            if (layerUnicodeNames.Count() > 1)
            {
                throw new PsdInvalidException(
                  $"{nameof(Layer)} can only have one {nameof(LayerUnicodeName)}.");
            }

            var layerUnicodeName = (LayerUnicodeName)layerUnicodeNames.FirstOrDefault();
            if (layerUnicodeName == null)
            {
                layerUnicodeName = new LayerUnicodeName(Name);
                AdditionalInfo.Add(layerUnicodeName);
            }
            else if (layerUnicodeName.Name != Name)
            {
                layerUnicodeName.Name = Name;
            }
        }

        public void Save(PsdBinaryWriter writer)
        {
            Util.DebugMessage(writer.BaseStream, "Save, Begin, Layer");

            writer.Write(Rect);

            //-----------------------------------------------------------------------

            writer.Write((short)Channels.Count);
            foreach (var ch in Channels)
                ch.Save(writer);

            //-----------------------------------------------------------------------

            writer.WriteAsciiChars("8BIM");
            writer.WriteAsciiChars(BlendModeKey);
            writer.Write(Opacity);
            writer.Write(Clipping);

            writer.Write((byte)flags.Data);
            writer.Write((byte)0);

            //-----------------------------------------------------------------------

            using (new PsdBlockLengthWriter(writer))
            {
                Masks.Save(writer);
                BlendingRangesData.Save(writer);

                var namePosition = writer.BaseStream.Position;

                // Legacy layer name is limited to 31 bytes.  Unicode layer name
                // can be much longer.
                writer.WritePascalString(Name, 4, 31);

                foreach (LayerInfo info in AdditionalInfo)
                {
                    info.Save(writer,
                      globalLayerInfo: false,
                      isLargeDocument: PsdFile.IsLargeDocument);
                }
            }

            Util.DebugMessage(writer.BaseStream, $"Save, End, Layer, {Name}");
        }


        public bool HasImage
        {
            get
            {
                var sectionInfo = (LayerSectionInfo)this.AdditionalInfo.FirstOrDefault(info => info is LayerSectionInfo);
                if (sectionInfo != null && sectionInfo.SectionType != LayerSectionType.Layer)
                    return false;
                if (Rect.Width == 0 || Rect.Height == 0)
                    return false;
                return true;
            }
        }

        public int Width => Rect.Width;
        public int Height => Rect.Height;

        public unsafe bool SetBitmap(Bitmap bmp, ImageReplaceOption option = ImageReplaceOption.KeepCenter, ImageCompression compress = ImageCompression.Raw)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb && bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                return false;
            }
            if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Bitmap clone = new Bitmap(bmp.Width, bmp.Height,PixelFormat.Format32bppPArgb);

                using (Graphics gr = Graphics.FromImage(clone))
                {
                    gr.DrawImage(bmp, new Rectangle(0, 0, clone.Width, clone.Height));
                }

                bmp = clone;
            }

            if (Channels.Count == 0)
            {
                for (int i = -1; i < 3; i++)
                {
                    var ch = new Channel((short)i, this);
                    ch.ImageCompression = compress;
                    Channels.Add(ch);
                }
            }

            if (Channels.Count != 4 && Channels.Count != 3)
            {
                return false;
            }
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmpData.Stride;
            int scanBytes = stride * bmp.Height;

            var c = scanBytes / 4;
            List<byte> b1 = new List<byte>(c);
            List<byte> b2 = new List<byte>(c);
            List<byte> b3 = new List<byte>(c);
            List<byte> b4 = new List<byte>(c);

            var ptr = (int*)bmpData.Scan0;
            for (int i = 0; i < c; i++)
            {
                b1.Add((byte)((ptr[i] & 0xFF000000) >> 24)); // A
                b2.Add((byte)((ptr[i] & 0x00FF0000) >> 16)); // R
                b3.Add((byte)((ptr[i] & 0x0000FF00) >> 8)); // G
                b4.Add((byte)((ptr[i] & 0x000000FF))); // B
            }

            ptr = null;
            bmp.UnlockBits(bmpData);

            if (Channels.Count >= 4)
            {
                Channels.GetId(-1).ImageData = b1.ToArray(); //A
                Channels.GetId(0).ImageData = b2.ToArray(); //R
                Channels.GetId(1).ImageData = b3.ToArray(); //G
                Channels.GetId(2).ImageData = b4.ToArray(); //B

                Channels[3].ImageDataRaw = null;
                Channels[2].ImageDataRaw = null;
                Channels[1].ImageDataRaw = null;
                Channels[0].ImageDataRaw = null;
            }
            else
            {
                Channels.GetId(0).ImageData = b2.ToArray();
                Channels.GetId(1).ImageData = b3.ToArray();
                Channels.GetId(2).ImageData = b4.ToArray();

                Channels[2].ImageDataRaw = null;
                Channels[1].ImageDataRaw = null;
                Channels[0].ImageDataRaw = null;
            }

            //Keep center
            switch (option)
            {
                case ImageReplaceOption.KeepCenter:
                    var newRect = new Rectangle(Rect.X, Rect.Y, bmp.Width, bmp.Height);
                    newRect.AlignCenter(Rect);
                    Rect = newRect;
                    break;
                case ImageReplaceOption.KeepTopLeft:
                    Rect = new Rectangle(Rect.X, Rect.Y, bmp.Width, bmp.Height);
                    break;
                case ImageReplaceOption.KeepSize:
                    //TODO
                    break;
                default:
                    break;
            }

            Parallel.ForEach(Channels,
                channel =>
                {
                    channel.ImageCompression = compress;
                    channel.CompressImageData();
                }
            );
            return true;
        }

        public unsafe Bitmap GetBitmap()
        {
            if (HasImage == false)
                return null;

            byte[] data = Channels.MergeChannels(Width, Height);
            var channelCount = Channels.Count;
            var pitch = Width * Channels.Count;
            var w = Width;
            var h = Height;

            //var format = channelCount == 3 ? TextureFormat.RGB24 : TextureFormat.ARGB32;
            //var tex = new Texture2D(w, h, format, false);
            /*
            var colors = new Color[data.Length / channelCount];
            
            var k = 0;
            for (var y = h - 1; y >= 0; --y)
            {
                for (var x = 0; x < pitch; x += channelCount)
                {
                    var n = x + y * pitch;

                    var c = Color.FromArgb(1, 1, 1, 1);
                    if (channelCount == 4)
                    {
                        c = new Color();
                        c.B = data[n++];
                        c.G = data[n++];
                        c.R = data[n++];
                        c.A = (byte)System.Math.Round(data[n++] / 255f * Opacity * 255f);
                    }
                    else
                    {
                        c.B = data[n++];
                        c.G = data[n++];
                        c.R = data[n++];
                        c.A = (byte)System.Math.Round(Opacity * 255f);
                    }
                    colors[k++] = c;
                }
            }
            */
            Bitmap bmp;
            fixed (byte* p = data)
            {
                IntPtr ptr = (IntPtr)p;
                bmp = new Bitmap(Width, Height, pitch, channelCount == 4 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb, ptr);
            }
            return bmp;
        }

    }

    public enum ImageReplaceOption
    {
        KeepCenter,
        KeepTopLeft,
        KeepSize,
    }
}
