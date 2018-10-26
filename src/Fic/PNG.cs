using System;
using static System.Buffers.Binary.BinaryPrimitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AcidChicken.Fic
{
    public class PNG
    {
        const long _signature = 727905341920923785;

        Chunk[] _chunks;

        public PNG(Stream stream)
        {
            #region signature
            {
                Span<byte> signature = stackalloc byte[8];
                if (stream.Read(signature) != signature.Length ||
                    ToInt64LittleEndian(signature) != _signature)
                    throw new BadImageFormatException("Invalid PNG signature.");
            }
            #endregion

            #region chunks
            {
                var list = new List<Chunk>();
                do list.Add(Chunk.FromStream(stream));
                while (list.LastOrDefault().Type != ChunkType.IEND);
                _chunks = list.ToArray();
            }
            #endregion
        }

        struct Chunk
        {
            Chunk(int length, ChunkType type, byte[] data)
            {
                Length = length;
                Type = type;
                Data = data;
            }

            public int Length { get; }

            public ChunkType Type { get; }

            public byte[] Data { get; }

            public static Chunk FromStream(Stream stream)
            {
                var intbuf = true ? stackalloc byte[4] : null;

                Span<byte> Read(Span<byte> buffer) =>
                    stream.Read(buffer) == buffer.Length ?
                    buffer :
                    throw new BadImageFormatException("Invalid chunk.");

                int ReadInt32(Span<byte> buffer) =>
                    ToInt32LittleEndian(Read(buffer));

                var length = ReadInt32(intbuf);

                var type = (ChunkType)ReadInt32(intbuf);

                var data = new byte[length];

                var times = length >> 12;
                for (var i = times; i >= 0; i++)
                {
                    var datbuf = times == 0 ?
                        stackalloc byte[length] :
                        stackalloc byte[4096];
                    Read(datbuf).CopyTo(data.AsSpan(i << 12, datbuf.Length));
                }

                return new Chunk(length, type, data);
            }
        }

        enum ChunkType
        {
            IEND = 0x49454e44
        }
    }
}
