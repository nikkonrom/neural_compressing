using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using image_compressing;


namespace MSE_generator
{
    public static class BitmapExtension
    {
        public static void Save(this Bitmap bitmap, String fileName, ImageFormat imageFormat, long quality = 75L)
        {
            using (var encoderParameters = new EncoderParameters(1))
            using (encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
            {
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

                bitmap.Save(fileName, codecs.Single(codec => codec.FormatID == imageFormat.Guid),
                    encoderParameters);

            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string[] sampleStrings = Directory.GetFiles("misc");
            Bitmap image_I, image_K;
            FileStream fileStream = new FileStream("output.txt", FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);

            for (int i = 0; i < sampleStrings.Length; i++)
            {
                //sampleStrings[i] = Path.GetFileNameWithoutExtension(sampleStrings[i]) + ".tiff";
                image_I = Image.FromFile(sampleStrings[i]) as Bitmap;
                var jpeg_string = sampleStrings[i].Replace(".tiff", ".jpg");
                image_I.Save(jpeg_string, ImageFormat.Jpeg);
                image_K = Image.FromFile(jpeg_string) as Bitmap;

                double MSE = GetMSE(image_I, image_K);
                streamWriter.Write("{0}", MSE);
                Console.Write("{0}", MSE);
                //File.Delete(jpeg_string);
                NeuralCompressing.Compress(sampleStrings[i]);
                NeuralCompressing.Decompress("compressed.nkr", "tree.nkr", "clasters.nkr");
                MSE = GetMSE(image_I, Image.FromFile("final.bmp") as Bitmap);
                streamWriter.WriteLine(", {0}", MSE);
                //streamWriter.WriteLine(new string('-', 40));
                Console.WriteLine(" ,{0}", MSE);
                //Console.WriteLine(new string('-', 40));

            }
            streamWriter.Close();
            fileStream.Close();
            Console.ReadKey();

        }

        private static double GetMSE(Bitmap image_I, Bitmap image_K)
        {
            double MSE = 0;

            for (int i = 0; i < image_I.Width; i++)
            {
                for (int j = 0; j < image_I.Height; j++)
                {
                    Color color_I = image_I.GetPixel(i, j);
                    Color color_K = image_K.GetPixel(i, j);
                    MSE += Math.Pow(color_I.R - color_K.R, 2);
                    MSE += Math.Pow(color_I.G - color_K.G, 2);
                    MSE += Math.Pow(color_I.B - color_K.B, 2);
                }
            }

            MSE /= 3 * image_I.Width * image_I.Height;
            return MSE;
        }
    }
}
