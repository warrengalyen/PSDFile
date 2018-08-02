using System;
using System.IO;
using System.Text;

namespace PSDFile
{
    /// <summary>
    /// Writes the actual length in front of the data block upon disposal.
    /// </summary>
    public class PsdBlockLengthWriter : IDisposable
    {

        private bool disposed = false;

        private long lengthPosition;
        private long startPosition;
        private bool hasLongLength;
        private PsdBinaryWriter writer;

        public PsdBlockLengthWriter(PsdBinaryWriter writer) : this(writer, false)
        {
        }

        public PsdBlockLengthWriter(PsdBinaryWriter writer, bool hasLongLength)
        {
            this.writer = writer;
            this.hasLongLength = hasLongLength;

            // Store position so that we can return to it when the length is known.
            lengthPosition = writer.BaseStream.Position;

            // Write a sentinel value as a placeholder for the length.
            writer.Write((UInt32)0xFEEDFEED);
            if (hasLongLength)
            {
                writer.Write((UInt32)0xFEEDFEED);
            }

            // Store the start position of the data block so that we can calculate
            // its length when we're done writing.
            startPosition = writer.BaseStream.Position;
        }

        public void Write()
        {
            var endPosition = writer.BaseStream.Position;

            writer.BaseStream.Position = lengthPosition;
            long length = endPosition - startPosition;
            if (hasLongLength)
            {
                writer.Write(length);
            }
            else
            {
                writer.Write((UInt32)length);
            }

            writer.BaseStream.Position = endPosition;
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                Write();
                this.disposed = true;
            }
        }
    }
}
