using System;
using image_compressing;
using System.Drawing;

namespace neuro_alg
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] samples =
            {
                "305 453.jpg",
                "350 453.jpg",
                "467 604.jpg",
                "588 761.jpg",
                "668 864.gif",
                "736 952.jpg",
                "768 993.jpg",
                "791 1024.jpg",
                "815 1056.jpg",
                "869 1125.png",
                "1000 1293.jpg",
                "1200 1552.jpg",
                "1280 1656.jpg"
            };

            int iterations = 10;
            double average_MSE;

            for (int j = 0; j < samples.Length; j++)
            {
                average_MSE = 0;
                for (int i = 0; i < iterations; i++)
                {
                    NeuralCompressing.Compress(samples[j]);
                    NeuralCompressing.Decompress("compressed.nkr", "tree.nkr", "clasters.nkr");
                    average_MSE += NeuralCompressing.GetMSE((Bitmap)Image.FromFile(samples[j]), (Bitmap)Image.FromFile("final.bmp"));
                }
                average_MSE /= iterations;
                Console.WriteLine(average_MSE);
                Console.WriteLine(new String('-', 60));
            }
            Console.ReadKey();
        }
    }
}
