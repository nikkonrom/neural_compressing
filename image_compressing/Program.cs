using System;
using image_compressing;

namespace neuro_alg
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter a filename with extension");
            string filename = Console.ReadLine();
            NeuralCompressing.Compress(filename);
            NeuralCompressing.Decompress("compressed.nkr", "tree.nkr", "clasters.nkr");
        }
    }
}
