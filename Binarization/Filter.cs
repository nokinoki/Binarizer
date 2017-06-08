using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binarization {
    class Filter {
        public double[,] Matrix;
        public int W = 0;
        public int H = 0;

        public static Filter NormalDelta {
            get {
                double[,] filter = new double[,] {
                    {0, 0, 0},
                    {0, -1, 1},
                    {0, 0, 0}
                };
                return new Filter(filter);
            }
        }
        public static Filter Sobel {
            get {
                double[,] filter = new double[,] {
                    {-1, 0, 1},
                    {-2, 0, 2},
                    {-1, 0, 1}
                };
                return new Filter(filter);
            }
        }
        public static Filter Prewitt {
            get {
                double[,] filter = new double[,] {
                    {-1, 0, 1},
                    {-1, 0, 1},
                    {-1, 0, 1}
                };
                return new Filter(filter);
            }
        }
        public static Filter Laplacian {
            get {
                double[,] filter = new double[,] {
                    {1, 1, 1},
                    {1, -8, 1},
                    {1, 1, 1}
                };
                return new Filter(filter);
            }
        }

        public Filter(double[,] matrix) {
            Matrix = matrix;
            W = matrix.GetLength(0);
            H = matrix.GetLength(1);
            if (W % 2 == 0 || H % 2 == 0) {
                throw new ArgumentException("Odd filter.");
            }
            ShowMatrix();
        }
        
        public void ShowMatrix() {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Filter Matrix is");
            for (int i = 0; i < Matrix.GetLength(0); i++) {
                for (int j = 0; j < Matrix.GetLength(1); j++) {
                    Console.Write("\t" + Matrix[i, j]);
                }
                Console.WriteLine();
            }
            Console.ResetColor();
        }

        public static Filter FromName(string filterName) {
            switch (filterName) {
                case "delta":
                    return NormalDelta;
                case "sobel":
                    return Sobel;
                case "prewitt":
                    return Prewitt;
                case "laplacian":
                    return Laplacian;
                default:
                    throw new ArgumentException("Invalid argment.");
            }
        }
    }
}
