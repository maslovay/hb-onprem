using System;

namespace ExtractFramesFromVideo
{
    public class FFMpegSettings
    {
        public String LocalVideoPath { get; set; }

        public String LocalFramesPath { get; set; }

        public String LocalTempPath { get; set; }
    }
}