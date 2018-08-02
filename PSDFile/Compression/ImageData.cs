using System;
using System.Drawing;
using System.IO.Compression;

namespace PSDFile.Compression
{
    public abstract class ImageData
    {
        public int BitDepth { get; private set; }

        public int BytesPerRow { get; private set; }

        public Size Size { get; private set; }

        protected abstract bool AltersWrittenData { get; }

        protected ImageData(Size size, int bitDepth)
        {
            Size = size;
            BitDepth = bitDepth;
            BytesPerRow = Util.BytesPerRow(size, bitDepth);
        }

        /// <summary>
        /// Reads decompressed image data.
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Read()
        {
            var imageLongLength = (long) BytesPerRow * Size.Height;
            Util.CheckByteArrayLength(imageLongLength);

            var buffer = new byte[imageLongLength];
            Read(buffer);
            return buffer;
        }

        internal abstract void Read(byte[] buffer);

        /// <summary>
        /// Reads compressed image data.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] ReadCompressed();

        /// <summary>
        /// Writes rows of image data into compressed format.
        /// </summary>
        /// <param name="array"></param>
        public void Write(byte[] array)
        {
            var imageLength = (long) BytesPerRow * Size.Height;
            if (array.Length != imageLength)
            {
                throw new ArgumentException("Array length is not equal to image length.",
                    nameof(array));
            }

            if (AltersWrittenData)
            {
                array = (byte[]) array.Clone();
            }

            WriteInternal(array);
        }

        internal abstract void WriteInternal(byte[] array);
    }
}
