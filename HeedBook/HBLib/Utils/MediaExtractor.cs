using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using HBLib.Model;
using NAudio.Wave;

namespace HBLib.Utils
{
    public static class MediaExtractor
    {
        public static async Task<MediaFile> ExtractWaveAudioAsync(Stream videoStream)
        {
            var reader = new StreamMediaFoundationReader(videoStream);
            var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);

            var stream = new MemoryStream();

            using (var writer = new WaveFileWriter(stream, reader.WaveFormat))
            {
                await pcmStream.CopyToAsync(writer);
            }

            return new MediaFile("wav")
            {
                Content = stream.ToArray()
            };
        }

        public static List<MediaFile> ExtractFramesByTimeStep(Stream videoStream, ImageFormat outputFormat, TimeSpan timeStep)
        {
            var videoFrameExtractor = new VideoFrameExtractor(videoStream, outputFormat);

            var step = timeStep.Seconds;
            var duration = videoFrameExtractor.VideoDuration.TotalSeconds;
            var stepsCount = (int)(duration / step);

            var frames = new List<MediaFile>(stepsCount);

            for (var currentPosition = 0; duration >= currentPosition; currentPosition += step)
            {
                var position = new TimeSpan(hours: 0, minutes: 0, seconds: currentPosition);

                var frame = videoFrameExtractor.ExtractFrameByTimePosition(position);
                frame.Name = currentPosition.ToString("F") + "s";

                frames.Add(frame);
            }

            return frames;
        }
    }
}
