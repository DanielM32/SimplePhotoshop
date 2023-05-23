using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using static System.Net.Mime.MediaTypeNames;
using System.DirectoryServices.ActiveDirectory;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Net.WebRequestMethods;

namespace SPhotoshop
{
    public partial class MainWindow : Window
    {
        //For new functional filters 
        private bool isDragging = false;  // Whether or not a point is currently being dragged.
        private Point dragStartPos;       // The starting position of the point being dragged.
        //Original Bitmaps
        private BitmapImage OriginalBitmap;
        private WriteableBitmap ModifiedBitmap;
        //Methods
        private void _saveImage(BitmapSource image, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 100;
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }
        //Function Filters
        private void _inversion(BitmapSource source)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);         // blue
                pixels[i + 1] = (byte)(255 - pixels[i + 1]); // green
                pixels[i + 2] = (byte)(255 - pixels[i + 2]); // red

            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _contrastenhancement(BitmapSource source, float contrast)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            float factor = (259f * (contrast + 255f)) / (255f * (259f - contrast));

            for (int i = 0; i < pixels.Length; i += 4)
            {
                int blue = pixels[i];
                int green = pixels[i + 1];
                int red = pixels[i + 2];

                blue = (int)Math.Min(255, Math.Max(0, (factor * (blue - 128) + 128)));
                green = (int)Math.Min(255, Math.Max(0, (factor * (green - 128) + 128)));
                red = (int)Math.Min(255, Math.Max(0, (factor * (red - 128) + 128)));

                pixels[i] = (byte)blue;
                pixels[i + 1] = (byte)green;
                pixels[i + 2] = (byte)red;

            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);

        }

        private void _brightnesscorrection(BitmapSource source, int brightness)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)Math.Min(255, Math.Max(0, pixels[i] + brightness));
                pixels[i + 1] = (byte)Math.Min(255, Math.Max(0, pixels[i + 1] + brightness));
                pixels[i + 2] = (byte)Math.Min(255, Math.Max(0, pixels[i + 2] + brightness));

            }

            BitmapSource result = BitmapSource.Create(
                ModifiedBitmap.PixelWidth, ModifiedBitmap.PixelHeight,
                ModifiedBitmap.DpiX, ModifiedBitmap.DpiY,
                ModifiedBitmap.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _gammacorrection(BitmapSource source, double gamma)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            byte[] gammaTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                gammaTable[i] = (byte)Math.Min(255, Math.Max(0, Math.Pow(i / 255.0, gamma) * 255.0));
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = gammaTable[pixels[i]];
                pixels[i + 1] = gammaTable[pixels[i + 1]];
                pixels[i + 2] = gammaTable[pixels[i + 2]];

            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
            source.Format, null, pixels, stride);
            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        //Convolution Filters

        double[] kernelCoefficients = {
    1.0, 1.0, 1.0,
    1.0, 1.0, 1.0,
    1.0, 1.0, 1.0
};

        private void _blur(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernelCoefficients = {
        1, 1, 1,
        1, 1, 1,
        1, 1, 1
    };
            int kernelSize = 3;
            int kernelRadius = kernelSize / 2;

            byte[] resultPixels = new byte[height * stride];

            double kernelCoefficientSum = kernelCoefficients.Sum();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double r = 0;
                    double g = 0;
                    double b = 0;

                    for (int j = -kernelRadius; j <= kernelRadius; j++)
                    {
                        for (int i = -kernelRadius; i <= kernelRadius; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            if (xIndex < 0 || xIndex >= width || yIndex < 0 || yIndex >= height)
                            {
                                continue;
                            }

                            int kernelIndex = ((j + kernelRadius) * kernelSize) + (i + kernelRadius);
                            double kernelCoefficient = kernelCoefficients[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);
                            double r2 = pixels[pixelIndex2 + 2];
                            double g2 = pixels[pixelIndex2 + 1];
                            double b2 = pixels[pixelIndex2];

                            r += kernelCoefficient * r2;
                            g += kernelCoefficient * g2;
                            b += kernelCoefficient * b2;
                        }
                    }

                    resultPixels[pixelIndex] = (byte)Math.Min(255, Math.Max(0, b / kernelCoefficientSum));
                    resultPixels[pixelIndex + 1] = (byte)Math.Min(255, Math.Max(0, g / kernelCoefficientSum));
                    resultPixels[pixelIndex + 2] = (byte)Math.Min(255, Math.Max(0, r / kernelCoefficientSum));
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }


        private void _sharpen(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernelCoefficients = {
        0, -1, 0,
        -1, 5, -1,
        0, -1, 0
    };
            int kernelSize = 3;
            int kernelRadius = kernelSize / 2;

            byte[] resultPixels = new byte[height * stride];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double r = 0;
                    double g = 0;
                    double b = 0;

                    for (int j = -kernelRadius; j <= kernelRadius; j++)
                    {
                        for (int i = -kernelRadius; i <= kernelRadius; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            if (xIndex < 0 || xIndex >= width || yIndex < 0 || yIndex >= height)
                            {
                                continue;
                            }

                            int kernelIndex = ((j + kernelRadius) * kernelSize) + (i + kernelRadius);
                            double kernelCoefficient = kernelCoefficients[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);
                            double r2 = pixels[pixelIndex2 + 2];
                            double g2 = pixels[pixelIndex2 + 1];
                            double b2 = pixels[pixelIndex2];

                            r += kernelCoefficient * r2;
                            g += kernelCoefficient * g2;
                            b += kernelCoefficient * b2;
                        }
                    }

                    resultPixels[pixelIndex] = (byte)Math.Min(255, Math.Max(0, b));
                    resultPixels[pixelIndex + 1] = (byte)Math.Min(255, Math.Max(0, g));
                    resultPixels[pixelIndex + 2] = (byte)Math.Min(255, Math.Max(0, r));
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _gaussian(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernelCoefficients = {
        1, 2, 1,
        2, 4, 2,
        1, 2, 1
    };
            int kernelSize = 3;
            int kernelRadius = kernelSize / 2;

            byte[] resultPixels = new byte[height * stride];

            double kernelCoefficientSum = kernelCoefficients.Sum();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double r = 0;
                    double g = 0;
                    double b = 0;

                    for (int j = -kernelRadius; j <= kernelRadius; j++)
                    {
                        for (int i = -kernelRadius; i <= kernelRadius; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            if (xIndex < 0 || xIndex >= width || yIndex < 0 || yIndex >= height)
                            {
                                continue;
                            }

                            int kernelIndex = ((j + kernelRadius) * kernelSize) + (i + kernelRadius);
                            double kernelCoefficient = kernelCoefficients[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);
                            double r2 = pixels[pixelIndex2 + 2];
                            double g2 = pixels[pixelIndex2 + 1];
                            double b2 = pixels[pixelIndex2];

                            r += kernelCoefficient * r2;
                            g += kernelCoefficient * g2;
                            b += kernelCoefficient * b2;
                        }
                    }

                    resultPixels[pixelIndex] = (byte)Math.Min(255, Math.Max(0, b / kernelCoefficientSum));
                    resultPixels[pixelIndex + 1] = (byte)Math.Min(255, Math.Max(0, g / kernelCoefficientSum));
                    resultPixels[pixelIndex + 2] = (byte)Math.Min(255, Math.Max(0, r / kernelCoefficientSum));
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _sobeledge(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] horizontalKernel = {
        -1, 0, 1,
        -2, 0, 2,
        -1, 0, 1
    };

            double[] verticalKernel = {
        -1, -2, -1,
         0,  0,  0,
         1,  2,  1
    };

            byte[] resultPixels = new byte[height * stride];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double horizontalValue = 0;
                    double verticalValue = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            int kernelIndex = ((j + 1) * 3) + (i + 1);

                            double kernelValueH = horizontalKernel[kernelIndex];
                            double kernelValueV = verticalKernel[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);

                            double pixelValue = (double)(pixels[pixelIndex2 + 2] + pixels[pixelIndex2 + 1] + pixels[pixelIndex2]) / 3.0;

                            horizontalValue += pixelValue * kernelValueH;
                            verticalValue += pixelValue * kernelValueV;
                        }
                    }

                    double magnitude = Math.Sqrt((horizontalValue * horizontalValue) + (verticalValue * verticalValue));
                    byte resultValue = (byte)Math.Min(255, Math.Max(0, magnitude));

                    resultPixels[pixelIndex] = resultValue;
                    resultPixels[pixelIndex + 1] = resultValue;
                    resultPixels[pixelIndex + 2] = resultValue;
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _standardemboss(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernel = {
        -1, -1,  0,
        -1,  0,  1,
         0,  1,  1
    };

            byte[] resultPixels = new byte[height * stride];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double sum = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            int kernelIndex = ((j + 1) * 3) + (i + 1);

                            double kernelValue = kernel[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);

                            double pixelValue = (double)(pixels[pixelIndex2 + 2] + pixels[pixelIndex2 + 1] + pixels[pixelIndex2]) / 3.0;

                            sum += pixelValue * kernelValue;
                        }
                    }

                    byte resultValue = (byte)Math.Min(255, Math.Max(0, sum + 128));

                    resultPixels[pixelIndex] = resultValue;
                    resultPixels[pixelIndex + 1] = resultValue;
                    resultPixels[pixelIndex + 2] = resultValue;
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private void _southemboss(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernel = {
        -1, -1, -1,
         0,  1,  0,
         1,  1,  1
    };

            byte[] resultPixels = new byte[height * stride];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    double sum = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            int kernelIndex = ((j + 1) * 3) + (i + 1);

                            double kernelValue = kernel[kernelIndex];

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);

                            double pixelValue = (double)(pixels[pixelIndex2 + 2] + pixels[pixelIndex2 + 1] + pixels[pixelIndex2]) / 3.0;

                            sum += pixelValue * kernelValue;
                        }
                    }

                    byte resultR = (byte)Math.Min(255, Math.Max(0, sum + pixels[pixelIndex + 2]));
                    byte resultG = (byte)Math.Min(255, Math.Max(0, sum + pixels[pixelIndex + 1]));
                    byte resultB = (byte)Math.Min(255, Math.Max(0, sum + pixels[pixelIndex]));

                    resultPixels[pixelIndex] = resultB;
                    resultPixels[pixelIndex + 1] = resultG;
                    resultPixels[pixelIndex + 2] = resultR;
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }


        public MainWindow()
        {
            InitializeComponent();
            //FilterGraph.Points = new PointCollection()
            //{
            //new Point(0, 255),
            //new Point(255, 0)
            //};
        }
        //Buttons and their actions
        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                OriginalBitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                OriginalImage.Source = OriginalBitmap;
                ModifiedBitmap = new WriteableBitmap(OriginalBitmap);
                ModifiedImage.Source = ModifiedBitmap;
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
            dialog.DefaultExt = "*.jpg";
            dialog.AddExtension = true;

            if (dialog.ShowDialog() == true)
            {
                _saveImage(ModifiedBitmap, dialog.FileName);
            }

        }

        private void RevertChanges_Click(object sender, RoutedEventArgs e)
        {
            ModifiedImage.Source = OriginalBitmap;
            ModifiedBitmap = new WriteableBitmap(OriginalBitmap);
        }

        private void Inversion_Click(object sender, RoutedEventArgs e)
        {
            if (ModifiedBitmap != null)
            {
                _inversion(ModifiedBitmap);
            }
        }

        private void BrightnessPlus_Click(object sender, RoutedEventArgs e)
        {
            _brightnesscorrection(ModifiedBitmap, 30);
        }

        private void BrightnessMinus_Click(object sender, RoutedEventArgs e)
        {
            _brightnesscorrection(ModifiedBitmap, -30);

        }

        private void ContrastPlus_Click(object sender, RoutedEventArgs e)
        {
            _contrastenhancement(ModifiedBitmap, 50);
        }

        private void ContrastMinus_Click(object sender, RoutedEventArgs e)
        {
            _contrastenhancement(ModifiedBitmap, -50);
        }

        private void GammaPlus_Click(object sender, RoutedEventArgs e)
        {
            _gammacorrection(ModifiedBitmap, 1.5);
        }

        private void GammaMinus_Click(object sender, RoutedEventArgs e)
        {
            _gammacorrection(ModifiedBitmap, 0.5);
        }

        private void Blur_Click(object sender, RoutedEventArgs e)
        {
            _blur(ModifiedBitmap);
        }

        private void Sharpen_Click(object sender, RoutedEventArgs e)
        {
            _sharpen(ModifiedBitmap);
        }

        private void Gaussian_Click(object sender, RoutedEventArgs e)
        {
            _gaussian(ModifiedBitmap);
        }

        private void Sobel_Click(object sender, RoutedEventArgs e)
        {
            _sobeledge(ModifiedBitmap);
        }

        private void Emboss_Click(object sender, RoutedEventArgs e)
        {
            _standardemboss(ModifiedBitmap);
        }

        private void Median_Click(object sender, RoutedEventArgs e)
        {
            _median(ModifiedBitmap);
        }

        private void SouthEmboss_Click(object sender, RoutedEventArgs e)
        {
            _southemboss(ModifiedBitmap);
        }

        private void GrayScale_Click(object sender, RoutedEventArgs e)
        {
            _grayscale(ModifiedBitmap);
        }

        private void Popularity_Click(object sender, RoutedEventArgs e)
        {
            _popularityquantization(ModifiedBitmap, 32);
        }

        private void OrderedDithering_Click(object sender, RoutedEventArgs e)
        {
            _ordereddithering(ModifiedBitmap, 4);
        }

        private void YCB_Click(object sender, RoutedEventArgs e)
        {
            _ycb(ModifiedBitmap);
        }

        //Task 1 section

        private void FilterCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Point mousePos = e.GetPosition(FilterCanvas);

            int closestIndex = -1;
            double closestDistance = double.MaxValue;
            PointCollection points = FilterGraph.Points;
            for (int i = 0; i < points.Count; i++)
            {
                double distance = (points[i] - mousePos).Length;
                if (distance < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }


            if (closestDistance < 10)
            {
                isDragging = true;
                dragStartPos = points[closestIndex];
            }

            else
            {
                int insertIndex = 0;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    if ((points[i].X <= mousePos.X && points[i + 1].X >= mousePos.X) || (points[i].X >= mousePos.X && points[i + 1].X <= mousePos.X))
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
                points.Insert(insertIndex, mousePos);

                FilterGraph.Points = points;
            }
        }

        private void FilterCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(FilterCanvas);


            if (isDragging)
            {
                PointCollection points = FilterGraph.Points;
                int draggedIndex = points.IndexOf(dragStartPos);
                points[draggedIndex] = mousePos;

                if (draggedIndex > 0 && draggedIndex < points.Count - 1)
                {
                    if (points[draggedIndex].Y < points[draggedIndex - 1].Y)
                    {
                        points[draggedIndex] = new Point(points[draggedIndex].X, points[draggedIndex - 1].Y);
                    }
                    else if (points[draggedIndex].Y > points[draggedIndex + 1].Y)
                    {
                        points[draggedIndex] = new Point(points[draggedIndex].X, points[draggedIndex + 1].Y);
                        FilterGraph.Points = points;
                    }
                }
            }
        }

        private void FilterCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // If a point was being dragged, stop dragging it.
            if (isDragging)
            {
                isDragging = false;
            }
            // Otherwise, delete the closest point to the mouse position.
            else
            {
                Point mousePos = e.GetPosition(FilterCanvas);
                int closestIndex = -1;
                double closestDistance = double.MaxValue;
                PointCollection points = FilterGraph.Points;
                for (int i = 1; i < points.Count - 1; i++)
                {
                    double distance = (points[i] - mousePos).Length;
                    if (distance < closestDistance)
                    {
                        closestIndex = i;
                        closestDistance = distance;
                    }
                }

                // If the closest point is within a certain threshold, delete it.
                if (closestDistance < 10)
                {
                    // Don't allow the leftmost and rightmost points to be deleted.
                    if (closestIndex != 0 && closestIndex != points.Count - 1)
                    {
                        points.RemoveAt(closestIndex);
                        FilterGraph.Points = points;
                    }
                }
            }
        }


        //Laboratory part 1
        private void _median(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            double[] kernelCoefficients = {
        1, 1, 1,
        1, 1, 1,
        1, 1, 1
    };
            int kernelSize = 3;
            int kernelRadius = kernelSize / 2;

            byte[] resultPixels = new byte[height * stride];

            double kernelCoefficientSum = kernelCoefficients.Sum();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * stride) + (x * 4);

                    int[] reds = new int[kernelSize * kernelSize];
                    int[] greens = new int[kernelSize * kernelSize];
                    int[] blues = new int[kernelSize * kernelSize];

                    int index = 0;
                    for (int j = -kernelRadius; j <= kernelRadius; j++)
                    {
                        for (int i = -kernelRadius; i <= kernelRadius; i++)
                        {
                            int xIndex = x + i;
                            int yIndex = y + j;

                            if (xIndex < 0 || xIndex >= width || yIndex < 0 || yIndex >= height)
                            {
                                continue;
                            }

                            int pixelIndex2 = (yIndex * stride) + (xIndex * 4);
                            reds[index] = pixels[pixelIndex2 + 2];
                            greens[index] = pixels[pixelIndex2 + 1];
                            blues[index] = pixels[pixelIndex2];
                            index++;
                        }
                    }

                    Array.Sort(reds);
                    Array.Sort(greens);
                    Array.Sort(blues);

                    int medianIndex = reds.Length / 2;
                    byte medianRed = (byte)reds[medianIndex];
                    byte medianGreen = (byte)greens[medianIndex];
                    byte medianBlue = (byte)blues[medianIndex];

                    resultPixels[pixelIndex] = medianBlue;
                    resultPixels[pixelIndex + 1] = medianGreen;
                    resultPixels[pixelIndex + 2] = medianRed;
                    resultPixels[pixelIndex + 3] = pixels[pixelIndex + 3];
                }
            }

            BitmapSource result = BitmapSource.Create(
                width, height, source.DpiX, source.DpiY, source.Format, null, resultPixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        //Second Task
        //Gray Scale

        private void _grayscale(BitmapSource source)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];

                byte luminance = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);

                pixels[i] = luminance;     // blue
                pixels[i + 1] = luminance; // green
                pixels[i + 2] = luminance; // red
            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        //Popularity colour quantization algorithm

        private void _popularityquantization(BitmapSource source, int colorCount)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            Dictionary<Color, int> frequency = new Dictionary<Color, int>();

            for (int i = 0; i < pixels.Length; i += 4)
            {
                Color color = Color.FromArgb(255, pixels[i + 2], pixels[i + 1], pixels[i]);
                if (frequency.ContainsKey(color))
                {
                    frequency[color]++;
                }
                else
                {
                    frequency[color] = 1;
                }
            }

            List<Color> quantizedColors = frequency
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .Take(colorCount)
                .ToList();

            for (int i = 0; i < pixels.Length; i += 4)
            {
                Color originalcol = Color.FromArgb(255, pixels[i + 2], pixels[i + 1], pixels[i]);
                Color nearestcol = FindNearestColor(originalcol, quantizedColors);

                pixels[i] = nearestcol.B;     
                pixels[i + 1] = nearestcol.G; 
                pixels[i + 2] = nearestcol.R; 
            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private Color FindNearestColor(Color target, List<Color> colors)
        {
            int mindist = int.MaxValue;
            Color nearestcol = default(Color);

            foreach (Color color in colors)
            {
                int distancesquared = (color.R - target.R) * (color.R - target.R) +
                                       (color.G - target.G) * (color.G - target.G) +
                                       (color.B - target.B) * (color.B - target.B);

                if (distancesquared < mindist)
                {
                    mindist = distancesquared;
                    nearestcol = color;
                }
            }

            return nearestcol;
        }

        //Ordered Dithering

        private void _ordereddithering(BitmapSource source, int ditherSize)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            double scale = 255.0 / (ditherSize * ditherSize - 1);

            int[,] ditherMatrix = GenerateDitherMatrix(ditherSize);

            for (int y = 0; y < source.PixelHeight; y++)
            {
                for (int x = 0; x < source.PixelWidth; x++)
                {
                    int index = y * stride + x * 4;

                    for (int i = 0; i < 3; i++)
                    {
                        int threshold = ditherMatrix[y % ditherSize, x % ditherSize];
                        int channelValue = pixels[index + i];
                        int adjustedValue = (int)Math.Round(channelValue / scale + threshold) * (int)scale; //TODO add  gray levels and change the formula here

                        pixels[index + i] = (byte)Math.Min(255, Math.Max(0, adjustedValue)); //TODO store the thresholds and compare to the colour instead
                    }
                }
            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }


        private int[,] GenerateDitherMatrix(int size)
        {
            if (size != 2 && size != 3 && size != 4 && size != 6)
            {
                throw new ArgumentException("Invalid dither matrix size.");
            }

            int[,] basematrix;

            if (size == 2)
            {
                basematrix = new int[,]
                {
            { 0, 2 },
            { 3, 1 }
                };
            }
            else if (size == 3)
            {
                basematrix = new int[,]
                {
            { 0, 7, 3 },
            { 6, 5, 2 },
            { 4, 1, 8 }
                };
            }
            else if (size == 4)
            {
                basematrix = new int[,]
                {
        {  0,  8,  2, 10 },
        { 12,  4, 14,  6 },
        {  3, 11,  1,  9 },
        { 15,  7, 13,  5 }
                };
            }
            else
            {
                basematrix = new int[,] //this to remove
                {
            {  0, 48, 12, 60,  3, 51, 15, 63 },
            { 32, 16, 44, 28, 35, 19, 47, 31 },
            {  8, 56,  4, 52, 11, 59,  7, 55 },
            { 40, 24, 36, 20, 43, 27, 39, 23 },
            {  2, 50, 14, 62,  1, 49, 13, 61 },
            { 34, 18, 46, 30, 33, 17, 45, 29 },
            { 10, 58,  6, 54,  9, 57,  5, 53 },
            { 42, 26, 38, 22, 41, 25, 37, 21 }
                };
            }

            if (size == 6)
            {
                int[,] scaledmatrix = new int[6, 6];
                for (int y = 0; y < 6; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        scaledmatrix[y, x] = basematrix[y % 4, x % 4] * 2;
                    }
                }
                basematrix = scaledmatrix;
            }

            return basematrix;
        }

        private void _ycb(BitmapSource source)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte r = pixels[i + 2];
                byte g = pixels[i + 1];
                byte b = pixels[i];

                // RGB to ycbcr
                double y = 0.299 * r + 0.587 * g + 0.114 * b;
                double cb = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b;
                double cr = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b;

                y = Dither(y);
                cb = Dither(cb);
                cr = Dither(cr);

                // Back to rgb
                r = (byte)Math.Max(0, Math.Min(255, y + 1.402 * (cr - 128)));
                g = (byte)Math.Max(0, Math.Min(255, y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128)));
                b = (byte)Math.Max(0, Math.Min(255, y + 1.772 * (cb - 128)));

                pixels[i + 2] = r;
                pixels[i + 1] = g;
                pixels[i] = b;
            }

            BitmapSource result = BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                source.Format, null, pixels, stride);

            ModifiedImage.Source = result;
            ModifiedBitmap = new WriteableBitmap(result);
        }

        private double Dither(double value)
        {
            int threshold = 128;
            return value >= threshold ? 255 : 0;
        }


        //LAB3 Rasterization
        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
        }

        public class LineCanvas : Canvas
        {
            private Point? _startPoint;
            private Point? _endPoint;
            private Line _currentLine;

            public LineCanvas()
            {
                MouseDown += LineCanvas_MouseDown;
                MouseMove += LineCanvas_MouseMove;
                MouseUp += LineCanvas_MouseUp;
            }

            private void LineCanvas_MouseDown(object sender, MouseButtonEventArgs e)
            {
                // Your logic for handling mouse down events, e.g. starting a new line or selecting an existing one
            }

            private void LineCanvas_MouseMove(object sender, MouseEventArgs e)
            {
                // Your logic for handling mouse move events, e.g. updating line endpoints
            }

            private void LineCanvas_MouseUp(object sender, MouseButtonEventArgs e)
            {
                // Your logic for handling mouse up events, e.g. finalizing line drawing or updating endpoints
            }

            protected override void OnRender(DrawingContext dc)
            {
                // Your logic for custom rendering, e.g. rendering lines using the MidpointLine algorithm
            }

            private void PutPixel(DrawingContext dc, int x, int y, int thickness, Color color)
            {
                dc.DrawRectangle(new SolidColorBrush(color), null, new Rect(x - thickness / 2, y - thickness / 2, thickness, thickness));
            }

            private void MidpointLine(DrawingContext dc, int x1, int y1, int x2, int y2, int thickness, Color color)
            {
                // Your MidpointLine implementation, using PutPixel to draw points
            }
        }

    }
}