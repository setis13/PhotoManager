using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoManager.Helpers {
    public static class ScreenshotHelper {
        public static void GetJpgImage(UIElement source, string fullPath) {
            try {
                var byteArray = GetJpgImage(source, 1, 90);
                // Open file for reading
                var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

                // Writes a block of bytes to this stream using data from a byte array.
                fileStream.Write(byteArray, 0, byteArray.Length);

                // close file stream
                fileStream.Close();

                return;
            } catch (Exception exception) {
                // Error
                Console.WriteLine("Exception caught in process: {0}", exception);
            }
        }

        ///
        /// Gets a JPG "screenshot" of the current UIElement
        ///
        /// UIElement to screenshot
        /// Scale to render the screenshot
        /// JPG Quality
        /// Byte array of JPG data
        public static byte[] GetJpgImage(UIElement source, double scale, int quality) {
            double actualHeight = source.RenderSize.Height;
            double actualWidth = source.RenderSize.Width;

            double renderHeight = actualHeight * scale;
            double renderWidth = actualWidth * scale;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96,
                                                                     PixelFormats.Pbgra32);
            VisualBrush sourceBrush = new VisualBrush(source);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext) {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawRectangle(sourceBrush, null,
                                             new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
            }
            renderTarget.Render(drawingVisual);

            JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
            jpgEncoder.QualityLevel = quality;
            jpgEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

            Byte[] _imageArray;

            using (MemoryStream outputStream = new MemoryStream()) {
                jpgEncoder.Save(outputStream);
                _imageArray = outputStream.ToArray();
            }

            return _imageArray;
        }
    }
}