using System;
using System.IO;
using System.Drawing;
using AForge.Neuro;
using AForge.Neuro.Learning;
using HuffmanCoding;
using System.Collections;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Linq;


namespace image_compressing
{
    static class NeuralCompressing
    {
        class Container
        {
            public int Number { get; set; }
            public double[] Value { get; set; }

            public Container(int number, double[] value)
            {
                Number = number;
                Value = value;
            }
        }

        public static void Compress(string path)
        {
            Bitmap original = LoadBitmap(path);
            string filename = "compressed.nkr";
            string clasters = "clasters.nkr";
            string tree = "tree.nkr";

            byte[][][] colors = BitmapToByteRgb(original);

            (double[][], long[][]) red_pallete, green_pallete, blue_pallete;

            red_pallete = OneDimensionImageClasterize(colors[0], original.Width, original.Height);
            green_pallete = OneDimensionImageClasterize(colors[1], original.Width, original.Height);
            blue_pallete = OneDimensionImageClasterize(colors[2], original.Width, original.Height);

            string sequence = GenerateSequence(red_pallete, green_pallete, blue_pallete);

            HuffmanTree coder = new HuffmanTree();
            coder.Build(sequence);
            BitArray encoded = coder.Encode(sequence);

            using (FileStream fs_clasters = new FileStream(clasters, FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(fs_clasters);
                int dimension = red_pallete.Item1[1].Length;
                writer.Write(original.Width);
                writer.Write(original.Height);

                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        writer.Write(red_pallete.Item1[i][j]);

                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        writer.Write(green_pallete.Item1[i][j]);

                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        writer.Write(blue_pallete.Item1[i][j]);


            }
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, encoded);
            }
            using (FileStream fs_tree = new FileStream(tree, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs_tree, coder);
            }

            return;

        }

        public static void Decompress(string path_source, string path_tree, string path_clasters)
        {
            int height;
            int width;

            BinaryFormatter formatter = new BinaryFormatter();

            HuffmanTree decoder;
            BitArray decoded;
            using (FileStream fs_tree = new FileStream(path_tree, FileMode.Open))
            {
                decoder = (HuffmanTree)formatter.Deserialize(fs_tree);
            }
            using (FileStream fs = new FileStream(path_source, FileMode.Open))
            {
                decoded = (BitArray)formatter.Deserialize(fs);
            }
            int dimension = 16;
            double[][] red_clasters = new double[dimension * dimension][], green_clasters = new double[dimension * dimension][], blue_clasters = new double[dimension * dimension][];

            for (int i = 0; i < dimension * dimension; i++)
            {
                red_clasters[i] = new double[dimension];
                green_clasters[i] = new double[dimension];
                blue_clasters[i] = new double[dimension];
            }

            using (FileStream fs = new FileStream(path_clasters, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(fs);
                width = reader.ReadInt32();
                height = reader.ReadInt32();

                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        red_clasters[i][j] = reader.ReadDouble();
                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        green_clasters[i][j] = reader.ReadDouble();
                for (int i = 0; i < dimension * dimension; i++)
                    for (int j = 0; j < dimension; j++)
                        blue_clasters[i][j] = reader.ReadDouble();

            }

            string sequence = decoder.Decode(decoded);

            (double[][] clasters, long[][] dictionary) red_pallete, green_pallete, blue_pallete;

            red_pallete.clasters = red_clasters;
            green_pallete.clasters = green_clasters;
            blue_pallete.clasters = blue_clasters;

            var chunksize = 3;

            var match = (from Match m in Regex.Matches(sequence, @".{1," + chunksize + "}")
                         select m.Value).ToArray();

            red_pallete.dictionary = new long[match.Length / 3][];
            green_pallete.dictionary = new long[match.Length / 3][];
            blue_pallete.dictionary = new long[match.Length / 3][];

            for (int i = 0; i < match.Length / 3; i++)
            {
                red_pallete.dictionary[i] = new long[2];
                green_pallete.dictionary[i] = new long[2];
                blue_pallete.dictionary[i] = new long[2];
            }

            for (int i = 0, j = 0; i < match.Length; i += 3, j++)
            {
                red_pallete.dictionary[j][1] = Convert.ToInt64(match[i]);
                green_pallete.dictionary[j][1] = Convert.ToInt64(match[i + 1]);
                blue_pallete.dictionary[j][1] = Convert.ToInt64(match[i + 2]);
            }

            Bitmap reconstructed = PalletsToBitmap(red_pallete, green_pallete, blue_pallete, width, height);

            reconstructed.Save("final.bmp");

        }

        private static string GenerateSequence(
            (double[][] clasters, long[][] dictionary) pallete_red,
            (double[][] clasters, long[][] dictionary) pallete_green,
            (double[][] clasters, long[][] dictionary) pallete_blue)
        {
            //List < Container > list= new List<Container>();
            StringBuilder sb = new StringBuilder(String.Empty);
            for (int i = 0; i < pallete_red.dictionary.Length; i++)
            {

                sb.Append(string.Format("{0:000}", pallete_red.dictionary[i][1]));
                sb.Append(string.Format("{0:000}", pallete_green.dictionary[i][1]));
                sb.Append(string.Format("{0:000}", pallete_blue.dictionary[i][1]));
                /*if (!list.Exists(delegate (Container box) { return box.Number == pallete.dictionary[i][1]; }))
                    list.Add(new Container((int)pallete.dictionary[i][1], pallete.clasters[pallete.dictionary[i][1]]));*/

            }
            return sb.ToString();

        }

        private static (double[][], long[][]) OneDimensionImageClasterize(byte[][] colors, int width, int height)
        {

            int iterations_count = GetNumberOfBlocks(width, height); //количество блоков 4*4 изображения
            int neurons_count = 256;    // количество нейронов

            #region Создание и обучение сети

            double[][] blocks = new double[iterations_count][];  //массив, содержащий входные данные сети (массив блоков 4*4 цветов изображения

            for (int d = 0; d < iterations_count; d++)
            {
                blocks[d] = new double[16];
            }


            Neuron.RandRange = new AForge.Range(0, 255);     // рандомазация весов нейронов

            DistanceNetwork network = new DistanceNetwork(16, neurons_count);    //создание нейронной сети

            SOMLearning trainer = new SOMLearning(network);     //создание тренера (объекта, обучающего сеть)
            trainer.LearningRadius = 2.5;   //установка радиуса затрагивания соседних нейронов
            trainer.LearningRate = 0.15;    //установка интенсивности обучения

            int current_block = 0;
            for (int i = 0; i < width; i += 4)
            {
                for (int j = 0; j < height; j += 4)
                {
                    blocks[current_block] = GetBlock(colors, i, j);
                    current_block++; //обучение сети
                }
                //Console.WriteLine(current_block);             
            }
            for (int i = 0; i < 1; i++)
            {
                trainer.RunEpoch(blocks);
            }
            

            #endregion

            #region Формирование кодовой книги изображения

            long[][] dictionary = new long[iterations_count][];

            for (int i = 0; i < iterations_count; i++)
            {
                dictionary[i] = new long[2];
            }

            double[] weights = new double[neurons_count];

            for (int i = 0; i < neurons_count; i++)
            {
                foreach (double weight in network.Layers[0].Neurons[i].Weights)
                    weights[i] += Math.Pow(weight, 2);
                weights[i] = Math.Sqrt(weights[i]);
            }


            // определение номера кластера для каждого блока
            for (long index_of_block = 0; index_of_block < blocks.Length; index_of_block++)
            {
                double block_vector_length = 0;

                foreach (double value in blocks[index_of_block])
                    block_vector_length += Math.Pow(value, 2);

                block_vector_length = Math.Sqrt(block_vector_length);

                (double, long) evklid_distance;
                evklid_distance.Item1 = Math.Abs(block_vector_length - weights[0]);
                evklid_distance.Item2 = 0;

                double temp_distance;

                for (long index = 1; index < weights.Length; index++)
                {
                    temp_distance = Math.Abs(block_vector_length - weights[index]);
                    if (temp_distance < evklid_distance.Item1)
                    {
                        evklid_distance.Item1 = temp_distance;
                        evklid_distance.Item2 = index;
                    }
                }
                dictionary[index_of_block][0] = index_of_block;
                dictionary[index_of_block][1] = evklid_distance.Item2;
            }

            #endregion

            #region Подготовка всех данных, необходимых для передачи сжатого изображения

            double[][] clasters = new double[network.Layers[0].Neurons.Length][];
            for (int counter = 0; counter < network.Layers[0].Neurons.Length; counter++)
            {
                clasters[counter] = network.Layers[0].Neurons[counter].Weights;
            }
            #endregion


            return (clasters, dictionary);
        }

        private static Bitmap LoadBitmap(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return new Bitmap(fs);
        }
        private static byte[][][] BitmapToByteRgb(Bitmap bmp)
        {
            int width = bmp.Width,
                height = bmp.Height;
            byte[][][] res = new byte[3][][];

            for (int i = 0; i < 3; i++)
            {
                res[i] = new byte[width][];
                for (int j = 0; j < width; j++)
                {
                    res[i][j] = new byte[height];
                }
            }

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    Color color = bmp.GetPixel(x, y);
                    res[0][x][y] = color.R;
                    res[1][x][y] = color.G;
                    res[2][x][y] = color.B;
                }
            }
            return res;
        }

        private static Bitmap PalletsToBitmap
            ((double[][], long[][]) red_pallete, (double[][], long[][]) green_pallete, (double[][], long[][]) blue_pallete,
            int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            int current_block = 0;
            for (int x = 0; x < width; x += 4)
            {
                for (int y = 0; y < height; y += 4)
                {
                    double[] source_red = red_pallete.Item1[red_pallete.Item2[current_block][1]];
                    double[] source_green = green_pallete.Item1[green_pallete.Item2[current_block][1]];
                    double[] source_blue = blue_pallete.Item1[blue_pallete.Item2[current_block][1]];
                    SetBlock(ref bmp, source_red, source_green, source_blue, x, y);
                    current_block++;
                }
            }
            return bmp;
        }

        private static double[] GetBlock(byte[][] source, int x, int y)
        {
            double[] block = new double[16];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    try
                    {
                        block[i * 4 + j] = source[x + i][y + j];
                    }
                    catch (IndexOutOfRangeException exeption)
                    {
                        continue;
                    }

                }
            }
            return block;
        }

        private static void SetBlock(ref Bitmap bmp, double[] source_red, double[] source_green, double[] source_blue, int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    byte r = Convert.ToByte(source_red[i * 4 + j]);
                    byte g = Convert.ToByte(source_green[i * 4 + j]);
                    byte b = Convert.ToByte(source_blue[i * 4 + j]);
                    Color color = Color.FromArgb(r, g, b);

                    try
                    {
                        bmp.SetPixel(x + i, y + j, color);
                    }
                    catch (ArgumentOutOfRangeException exeption)
                    {
                        continue;
                    }
                }
            }

        }

        private static int GetNumberOfBlocks(int width, int height)
        {
            int count = width * height / 16;
            if (height % 4 == 0 && width % 4 == 0)
                return count;
            else
            {
                if (width % 4 != 0)
                    count += width / 4 + 1;
                if (height % 4 != 0)
                    count += height / 4;
            }
            return count;
        }
    }
}

