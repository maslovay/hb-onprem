using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using HBLib.Utils;

namespace HBLib.Utils
{
    public static class StreamUtils
    {
        /// <summary>
        /// Optimized copying of stream contents
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this Stream src, Stream dest)
        {
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), 0x2000) : 0x2000;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);           
        }
        
        /// <summary>
        /// Optimized copying of stream contents for MemoryStream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this MemoryStream src, Stream dest)
        {
            int bufSize = 8192;
            var currentBuffer = src.GetBuffer();
            
            if (currentBuffer.Length > bufSize)
            {
                byte[] sub = null;
                while (src.Length - src.Position > bufSize)
                {
                    sub = currentBuffer.SubArray(src.Position, bufSize);
                    dest.Write(sub);
                    src.Position += bufSize;
                }
                
                sub = currentBuffer.SubArray(src.Position, src.Length - src.Position);
                dest.Write(sub);

                src.Position = src.Length;
            } else 
                dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
        }

        /// <summary>
        /// Optimized copying of stream contents to MemoryStream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyToOptimized(this Stream src, MemoryStream dest)
        {
            if (src.CanSeek)
            {
                int pos = (int)dest.Position;
                int length = (int)(src.Length - src.Position) + pos;
                dest.SetLength(length); 

                while(pos < length)                
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            }
            else
                src.CopyTo((Stream)dest);
        }
        
        /// <summary>
        /// Moves to a position to a position of a given sequence
        /// </summary>
        /// <param name="src"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static bool MoveToSequence(this Stream src, byte[] sequence)
        {
            long prevPosition = src.Position;
            if (sequence == null || sequence.Length == 0)
                return false;

            var buffer = new byte[sequence.Length];
            int len = 0;

            do
            {
                prevPosition = src.Position;
                len = src.Read(buffer);

                if (!buffer.isMatch(sequence, 0))
                    src.Position = prevPosition + 1;
                else
                    break;
            } while (len > 0);

            return len != 0; // FALSE if we reached an end of stream without any results
        }
        
        /// <summary>
        /// Writes to a target stream until we will reach a given sequence
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static bool WriteUntilSequence(this Stream src, Stream target, byte[] sequence)
        {
            if (sequence == null || sequence.Length == 0)
                return false;
            var buffer = new byte[sequence.Length];

            long prevPosition = src.Position;
            int len = 0;

            do
            {
                prevPosition = src.Position;
                len = src.Read(buffer);
                
                if (len > 0)
                    target.WriteByte(buffer[0]);
                
                if (!buffer.isMatch(sequence, 0))
                    src.Position = prevPosition + 1;
                else
                    break;
            } while (len > 0);
            
            return !(len == 0 && !buffer.isMatch(sequence, 0)); // FALSE if we reached an end of stream without any results
        }
    }
}