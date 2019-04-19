using System;
using System.IO;

namespace HBLib.Utils
{
    public static class StreamUtils
    {
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

        public static void CopyToOptimized(this MemoryStream src, Stream dest)
        {
            dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
        }

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
        
        public static bool MoveToSequence(this Stream src, byte[] sequence)
        {
            if (sequence == null || sequence.Length == 0)
                return false;

            var buffer = new byte[sequence.Length];
            int len = 0;

            do
            {
                len = src.Read(buffer);
            } while (len > 0 && !buffer.isMatch(sequence, 0));

            return len != 0; // Если достигли конца потока и ничего не нашли - false! 
        }
        
        public static bool WriteUntilSequence(this Stream src, Stream target, byte[] sequence)
        {
            if (sequence == null || sequence.Length == 0)
                return false;
            var buffer = new byte[sequence.Length];

            int len = 0;

            do
            {
                len = src.Read(buffer);
                target.Write(buffer);
            } while (len > 0 && !buffer.isMatch(sequence, 0));
            
            return !(len == 0 && !buffer.isMatch(sequence, 0)); // Если "уперлись" в конец потока и притом ничего не нашли - false! 
        }
    }
}