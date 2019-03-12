using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DlibDotNet;
using Rectangle = System.Drawing.Rectangle;

namespace HBLib.Utils
{
    public static class FaceDetection
    {
        public static Boolean IsFaceDetected(string path)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var img = Dlib.LoadImage<byte>(path))
            {
                Dlib.PyramidUp(img);
                var dets = detector.Operator(img);
                return dets.Length > 0;
            }
        }

        public static MemoryStream CreateAvatar(String path, Rectangle rectangle)
        {
            var rect = new System.Drawing.Rectangle(rectangle.Top,
                rectangle.Left - 20,
                rectangle.Width + 30,
                rectangle.Height + 30);
            var image = Image.FromFile(path) as Bitmap;
            var target = new Bitmap(rectangle.Width, rectangle.Height);

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
    }
}
