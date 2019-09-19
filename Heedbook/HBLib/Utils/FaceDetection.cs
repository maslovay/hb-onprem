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
        public static Boolean IsFaceDetected(String path, out Int32 faceLength)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var img = Dlib.LoadImage<Byte>(path))
            {
                Dlib.PyramidUp(img);
                var dets = detector.Operator(img);
                faceLength = dets.Length;
                return dets.Length > 0;
            }
        }

        public static Boolean IsFaceDetected(Byte[] image, out Int32 faceLength)
        {
            Bitmap bmp;

            using (var ms = new MemoryStream(image))
            {
                bmp = new Bitmap(ms);
            }

            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);

            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var img =
                Dlib.LoadImageData<RgbPixel>(image, (UInt32) bmp.Height, (UInt32) bmp.Width, (UInt32) data.Stride))
            {
                Dlib.PyramidUp(img);
                var dets = detector.Operator(img);
                faceLength = dets.Length;
                return dets.Length > 0;
            }
        }

        public static MemoryStream CreateAvatar(String path, Rectangle rectangle)
        {
            var rect = new Rectangle(rectangle.Top,
                rectangle.Left - 20,
                rectangle.Width + 30,
                rectangle.Height + 30);
            var image = Image.FromFile(path) as Bitmap;
            var target = new Bitmap(rectangle.Width, rectangle.Height);

            using (var g = Graphics.FromImage(target))
            {
                g.DrawImage(image, new Rectangle(0,
                        0,
                        target.Width,
                        target.Height),
                    rect,
                    GraphicsUnit.Pixel);
                var stream = new MemoryStream();
                target.Save(stream, ImageFormat.Jpeg);
                image.Dispose();
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }
}