// using System;
// using System.Drawing;
// using System.Drawing.Imaging;
// using System.IO;
// using HBLib.Model;
// using GleamTech.VideoUltimate;

// namespace HBLib.Utils
// {
//     public class VideoFrameExtractor
//     {
//         private readonly VideoFrameReader _frameReader;
//         private readonly ImageFormat _imageFormat;

//         public VideoFrameExtractor(Stream videoStream, ImageFormat imageFormat)
//         {
//             videoStream.Position = 0;
//             _frameReader = new VideoFrameReader(videoStream);

//             _imageFormat = imageFormat;
//         }

//         public TimeSpan VideoDuration => _frameReader.Duration;

//         public MediaFile ExtractFrameByTimePosition(TimeSpan position)
//         {
//             _frameReader.Seek(position.TotalSeconds);

//             if (!_frameReader.Read())
//                 return null;

//             var bitmapFrame = _frameReader.GetFrame();

//             return GetMediaFileFromBitmap(bitmapFrame);
//         }

//         private MediaFile GetMediaFileFromBitmap(Bitmap bitmap)
//         {
//             using (var memoryStream = new MemoryStream())
//             {
//                 bitmap.Save(memoryStream, _imageFormat);
//                 var imageFormat = _imageFormat.ToString().ToLower();

//                 return new MediaFile(imageFormat)
//                 {
//                     Content = memoryStream.ToArray()
//                 };
//             };
//         }
//     }
// }
