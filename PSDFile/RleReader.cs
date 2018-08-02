using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSDFile
{
    public class RleReader
    {
        private Stream stream;

        public RleReader(Stream stream)
        {
            this.stream = stream;
        }

        unsafe public int Read(byte[] buffer, int offset, int count)
        {
            if (!Util.CheckBufferBounds(buffer, offset, count))
                throw new ArgumentOutOfRangeException();

            // Pin the entire buffer now, so that we don't keep pinning and unpinning
            // for each RLE packet.
            fixed (byte* pBuffer = &buffer[0])
            {
                int bytesLeft = count;
                int bufferIndex = offset;
                while (bytesLeft > 0)
                {
                    // ReadByte returns an unsigned byte, but we want a signed  byte.
                    var flagCounter = unchecked((sbyte)stream.ReadByte());

                    // Raw packet
                    if (flagCounter > 0)
                    {
                        var readLength = flagCounter + 1;
                        if (bytesLeft < readLength)
                            throw new RleException("Raw packet overruns the decode window.");

                        stream.Read(buffer, bufferIndex, readLength);

                        bufferIndex += readLength;
                        bytesLeft -= readLength;
                    }
                    // RLE packet
                    else if (flagCounter > -128)
                    {
                        var runLength = 1 - flagCounter;
                        var byteValue = (byte) stream.ReadByte();
                        if (runLength > bytesLeft)
                            throw new RleException("RLE packet overruns the decode window.");

                        byte* ptr = pBuffer + bufferIndex;
                        byte* pEnd = ptr + runLength;
                        while (ptr < pEnd)
                        {
                            *ptr = byteValue;
                            ptr++;
                        }

                        bufferIndex += runLength;
                        bytesLeft -= runLength;
                    }
                    else
                    {
                        // The canoical PackBits algorithm will never emit 0x80 (-128), but
                        // some programs do. Simply skip over the byte.
                    }
                }

                Debug.Assert(bytesLeft == 0);
                return count - bytesLeft;
            }
        }
    }
}
