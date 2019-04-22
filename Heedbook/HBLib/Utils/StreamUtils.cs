using System;
using System.IO;

namespace HBLib.Utils
{
    public static class StreamUtils
    {
        /// <summary>
        ///     Optimized copying of stream contents
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this Stream src, Stream dest)
        {
            var size = src.CanSeek ? Math.Min((Int32) (src.Length - src.Position), 0x2000) : 0x2000;
            var buffer = new Byte[size];
            Int32 n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }

        /// <summary>
        ///     Optimized copying of stream contents for MemoryStream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this MemoryStream src, Stream dest)
        {
            dest.Write(src.GetBuffer(), (Int32) src.Position, (Int32) (src.Length - src.Position));
        }

        /// <summary>
        ///     Optimized copying of stream contents to MemoryStream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this Stream src, MemoryStream dest)
        {
            if (src.CanSeek)
            {
                var pos = (Int32) dest.Position;
                var length = (Int32) (src.Length - src.Position) + pos;
                dest.SetLength(length);

                while (pos < length)
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            }
            else
            {
                src.CopyTo(dest);
            }
        }

        /// <summary>
        ///     Moves to a position to a position of a given sequence
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static Boolean MoveToSequence(this Stream src, Byte[] sequence)
        {
            if (sequence == null || sequence.Length == 0)
                return false;

            var buffer = new Byte[sequence.Length];
            var len = 0;

            do
            {
                len = src.Read(buffer);
            } while (len > 0 && !buffer.isMatch(sequence, 0));

            return len != 0; // FALSE if we reached an end of stream without any results
        }

        /// <summary>
        ///     Writes to a target stream until we will reach a given sequence
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static Boolean WriteUntilSequence(this Stream src, Stream target, Byte[] sequence)
        {
            if (sequence == null || sequence.Length == 0)
                return false;
            var buffer = new Byte[sequence.Length];

            var len = 0;

            do
            {
                len = src.Read(buffer);
                target.Write(buffer);
            } while (len > 0 && !buffer.isMatch(sequence, 0));

            return
                !(len == 0 && !buffer.isMatch(sequence, 0)); // FALSE if we reached an end of stream without any results
        }
    }
}