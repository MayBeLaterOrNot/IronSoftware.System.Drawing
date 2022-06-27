﻿using SkiaSharp;
using System;

namespace IronSoftware.Drawing
{
    public static class IronBitmap
    {
        /// <summary>
        /// Resize an image with scale.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <param name="scale">Scale of new image 0 - 1.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap Resize(this AnyBitmap bitmap, float scale)
        {
            SKBitmap originalBitmap = bitmap;
            SKBitmap toBitmap = new SKBitmap((int)(originalBitmap.Width * scale), (int)(originalBitmap.Height * scale), originalBitmap.ColorType, originalBitmap.AlphaType);

            using (SKCanvas canvas = new SKCanvas(toBitmap))
            {
                // Draw a bitmap rescaled
#if NETFRAMEWORK
                canvas.SetMatrix(SKMatrix.MakeScale(scale, scale));
#else
                canvas.SetMatrix(SKMatrix.CreateScale(scale, scale));
#endif
                canvas.DrawBitmap(originalBitmap, 0, 0);
                canvas.ResetMatrix();
                canvas.Flush();
            }

            return toBitmap;
        }

        /// <summary>
        /// Resize an image with width and height.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <param name="width">Width ot the new resized image.</param>
        /// <param name="height">Height ot the new resized image.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap Resize(this AnyBitmap bitmap, int width, int height)
        {
            SKBitmap originalBitmap = bitmap;
            SKBitmap toBitmap = new SKBitmap(width, height, originalBitmap.ColorType, originalBitmap.AlphaType);
            originalBitmap.ExtractSubset(toBitmap, new CropRectangle(0, 0, width, height));

            return toBitmap;
        }

        /// <summary>
        /// Resize an image with width and height.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <param name="width">Width ot the new resized image.</param>
        /// <param name="height">Height ot the new resized image.</param>
        /// <param name="ratio">Ratio of new image 0 - 1.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap Resize(this AnyBitmap bitmap, int width, int height, float ratio)
        {
            SKBitmap originalBitmap = bitmap;
            SKBitmap toBitmap = new SKBitmap(width, height, originalBitmap.ColorType, originalBitmap.AlphaType);

            using (SKCanvas canvas = new SKCanvas(toBitmap))
            {
                // Draw a bitmap rescaled
#if NETFRAMEWORK
                canvas.SetMatrix(SKMatrix.MakeScale(ratio, ratio));
#else
                canvas.SetMatrix(SKMatrix.CreateScale(ratio, ratio));
#endif
                canvas.DrawBitmap(originalBitmap, 0, 0);
                canvas.ResetMatrix();
                canvas.Flush();
            }

            return toBitmap;
        }

        /// <summary>
        /// Resize an image with width and height.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <param name="cropArea">CropArea to crop an image.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap CropImage(this AnyBitmap bitmap, CropRectangle cropArea)
        {
            if (cropArea != null)
            {
                SKRect cropRect = ValidateCropArea(bitmap, cropArea);
                SKBitmap croppedBitmap = new SKBitmap((int)cropRect.Width, (int)cropRect.Height);

                SKRect dest = new SKRect(0, 0, cropRect.Width, cropRect.Height);
                SKRect source = new SKRect(cropRect.Left, cropRect.Top, cropRect.Right, cropRect.Bottom);
                try
                {
                    using (SKCanvas canvas = new SKCanvas(croppedBitmap))
                    {
                        canvas.DrawBitmap(bitmap, source, dest);
                    }

                    return croppedBitmap;
                }
                catch (OutOfMemoryException)
                {
                    try { croppedBitmap.Dispose(); } catch { }
                    throw new Exception("Crop Rectangle is larger than the input image.");
                }
            }
            else
            {
                return bitmap;
            }
        }

        /// <summary>
        /// Rotate an image.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <param name="angle">Angle for rotate image. Default (null): Try to find the image angle.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap RotateImage(this AnyBitmap bitmap, double? angle = null)
        {
            double skewAngle = angle ?? SkewImageLib.GetSkewAngle(bitmap);
            double radians = Math.PI * skewAngle / 180;
            float sine = (float)Math.Abs(Math.Sin(radians));
            float cosine = (float)Math.Abs(Math.Cos(radians));

            int originalWidth = ((SKBitmap)bitmap).Width;
            int originalHeight = ((SKBitmap)bitmap).Height;
            int rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            int rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);

            SKBitmap rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);

            using (SKCanvas canvas = new SKCanvas(rotatedBitmap))
            {
                canvas.Clear();
                canvas.Translate(rotatedWidth / 2, rotatedHeight / 2);
                canvas.RotateDegrees((float)angle);
                canvas.Translate(-originalWidth / 2, -originalHeight / 2);
                canvas.DrawBitmap(bitmap, new SKPoint());
            }

            return rotatedBitmap;
        }

        /// <summary>
        /// Find angle of the image.
        /// </summary>
        /// <param name="bitmap">Original bitmap to resize.</param>
        /// <return>Angle of the image in double.</return>
        public static double GetSkewAngle(this AnyBitmap bitmap)
        {
            return SkewImageLib.GetSkewAngle(bitmap);
        }

        /// <summary>
        /// Trim white space of the image.
        /// </summary>
        /// <param name="bitmap">Original bitmap to trim.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static SKBitmap Trim(this AnyBitmap bitmap)
        {

            try
            {
                SKBitmap originalBitmap = bitmap;

                int newLeft = DetermineLeft(originalBitmap);
                int newRight = DetermineRight(originalBitmap);
                int newBottom = DetermineBottom(originalBitmap);
                int newTop = DetermineTop(originalBitmap);

                return CropImage(bitmap, CreateCropRectangle(newLeft, newRight, newBottom, newTop));
            }
            catch
            {
                return bitmap.Clone();
            }
        }

        /// <summary>
        /// Add border to the image.
        /// </summary>
        /// <param name="bitmap">Original bitmap to trim.</param>
        /// <param name="color">Background color of the border.</param>
        /// <param name="size">Size of the border in pixel.</param>
        /// <return>IronSoftware.Drawing.AnyBitmap.</return>
        public static AnyBitmap AddBorder(this AnyBitmap bitmap, IronSoftware.Drawing.Color color, int size)
        {
            SKBitmap originalBitmap = bitmap;
            int maxWidth = originalBitmap.Width + size * 2;
            int maxHeight = originalBitmap.Height + size * 2;
            SKBitmap toBitmap = new SKBitmap(maxWidth, maxHeight);

            var ratioX = (double)maxWidth / originalBitmap.Width;
            var ratioY = (double)maxHeight / originalBitmap.Height;
            var ratio = (float)Math.Min(ratioX, ratioY);

            using (SKCanvas canvas = new SKCanvas(toBitmap))
            {

#if NETFRAMEWORK
                canvas.SetMatrix(SKMatrix.MakeScale(ratio, ratio));
#else
                canvas.SetMatrix(SKMatrix.CreateScale(ratio, ratio));
#endif
                canvas.Clear(color);
                int x = (toBitmap.Width - originalBitmap.Width) / 2;
                int y = (toBitmap.Height - originalBitmap.Height) / 2;
                canvas.DrawBitmap(bitmap, x, y);
                canvas.ResetMatrix();
                canvas.Flush();
            }

            return toBitmap;
        }

#region Private Method

        private static CropRectangle ValidateCropArea(SKBitmap img, CropRectangle CropArea)
        {
            int maxWidth = img.Width;
            int maxHeight = img.Height;

            int cropAreaX = CropArea.X > 0 ? CropArea.X : 0;
            int cropAreaY = CropArea.Y > 0 ? CropArea.Y : 0;
            int cropAreaWidth = CropArea.Width > 0 ? CropArea.Width : img.Width;
            int cropAreaHeight = CropArea.Height > 0 ? CropArea.Height : img.Height;

            int croppedWidth = cropAreaX + cropAreaWidth;
            int croppedHeight = cropAreaY + cropAreaHeight;

            int newWidth = cropAreaWidth;
            int newHeight = cropAreaHeight;
            if (croppedWidth > maxWidth)
            {
                newWidth = maxWidth - cropAreaX;
            }
            if (croppedHeight > maxHeight)
            {
                newHeight = maxHeight - cropAreaY;
            }
            return new CropRectangle(cropAreaX, cropAreaY, newWidth, newHeight);
        }

        private static bool DifferentColor(Color source, Color target)
        {
            return !IsTransparent(source) && (source.R != target.R || source.G != target.G || source.B != target.B || source.A != target.A);
        }

        private static bool IsTransparent(Color source)
        {
            return (SKColor)source == SKColors.Transparent;
        }

        private static int DetermineRight(SKBitmap originalBitmap)
        {
            int result = -1;
            for (int x = originalBitmap.Width - 1; x >= 0; x--)
            {
                for (int y = 0; y < originalBitmap.Height; y++)
                {
                    SKColor color = originalBitmap.GetPixel(x, y);
                    if (color != SKColors.White)
                    {
                        result = x;
                        break;
                    }
                }
                if (result != -1)
                    break;
            }

            return result;
        }

        private static int DetermineLeft(SKBitmap originalBitmap)
        {
            int result = -1;
            for (int x = 0; x < originalBitmap.Width; x++)
            {
                for (int y = 0; y < originalBitmap.Height; y++)
                {
                    SKColor color = originalBitmap.GetPixel(x, y);
                    if (DifferentColor(color, Color.White))
                    {
                        result = x;
                        break;
                    }
                }
                if (result != -1)
                    break;
            }

            return result;
        }

        private static int DetermineTop(SKBitmap originalBitmap)
        {
            int newTop = -1;
            for (int y = originalBitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < originalBitmap.Width; x++)
                {
                    SKColor color = originalBitmap.GetPixel(x, y);
                    if (DifferentColor(color, Color.White))
                    {
                        newTop = y;
                        break;
                    }
                }
                if (newTop != -1)
                    break;
            }

            return newTop;
        }

        private static int DetermineBottom(SKBitmap originalBitmap)
        {
            int newBottom = -1;
            for (int y = 0; y < originalBitmap.Height; y++)
            {
                for (int x = 0; x < originalBitmap.Width; x++)
                {
                    SKColor color = originalBitmap.GetPixel(x, y);
                    if (DifferentColor(color, Color.White))
                    {
                        newBottom = y;
                        break;
                    }
                }
                if (newBottom != -1)
                    break;
            }

            return newBottom;
        }

        private static CropRectangle CreateCropRectangle(int left, int right, int bottom, int top)
        {
            return new CropRectangle(left, bottom, right - left, top - bottom);
        }

        #endregion
    }
}
