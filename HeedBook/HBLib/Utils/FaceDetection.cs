using DlibDotNet;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Rectangle = DlibDotNet.Rectangle;

namespace HBLib.Utils
{
    public static class FaceDetection
    {
        public static Boolean IsFaceDetected(String localPath)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            {
                using (var img = Dlib.LoadImage<Byte>(localPath))
                {
                    Dlib.PyramidUp(img);
                    var detectionResult = detector.Operator(img).Length;
                    return detectionResult > 0;
                }
            }
        }

        public static MemoryStream CreateAvatar(String path)
        {
            var rectangle = GetFaceRectangle(path);
            var rect = new System.Drawing.Rectangle(rectangle.TopLeft.X,
                rectangle.TopLeft.Y - 20,
                (Int32)rectangle.Width + 30,
                (Int32)rectangle.Height + 30);
            var image = Image.FromFile(path) as Bitmap;
            var target = new Bitmap((Int32)rectangle.Width, (Int32)rectangle.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(image, new System.Drawing.Rectangle(0,
                        0,
                        target.Width,
                        target.Height),
                    rect,
                    GraphicsUnit.Pixel);
                var stream = new MemoryStream();
                target.Save(stream, ImageFormat.Jpeg);
                image.Dispose();
                return stream;
            }
        }

        private static Rectangle GetFaceRectangle(String path)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            {
                using (var img = Dlib.LoadImage<byte>(path))
                {
                    var rectangles = detector.Operator(img);
                    return EnumerableExtensions.Any(rectangles) ? rectangles[0] : Rectangle.Empty;
                }
            }
        }
    }
}
