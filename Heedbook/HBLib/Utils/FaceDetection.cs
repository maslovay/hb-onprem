using DlibDotNet;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Rectangle = System.Drawing.Rectangle;

namespace HBLib.Utils
{
    public static class FaceDetection
    {
        public static Boolean IsFaceDetected(string path, out int faceLength)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var img = Dlib.LoadImage<byte>(path))
            {
                Dlib.PyramidUp(img);
                var dets = detector.Operator(img);
                faceLength = dets.Length;
                return dets.Length > 0;
            }
        }

        public static Boolean IsFaceDetected(byte[] image, out int faceLength)
        {
            Bitmap bmp;

            using (var ms = new MemoryStream(image))
            {
                bmp = new Bitmap(ms);
            }
            var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var img = Dlib.LoadImageData<RgbPixel>(image, (uint)bmp.Height, (uint)bmp.Width, (uint)data.Stride))
            {
                Dlib.PyramidUp(img);
                var dets = detector.Operator(img);
                faceLength = dets.Length;
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
