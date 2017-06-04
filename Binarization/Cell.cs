using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binarization {
    class Cell {

        public int W = 0, H = 0;
        public Color[,] Buffer;

        public Cell(ref Color[,] buffer) {
            W = buffer.GetLength(0);
            H = buffer.GetLength(1);
            Buffer = buffer;
        }

        public void ProcessStatic(int p) {
            // pで二値化
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Binarize (p={0})", p);
            Console.ResetColor();
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    BinarizeCore(ref Buffer[j, i], p);
                }
            }
        }

        public void ProcessP(double rate) {
            // 各輝度を数え上げる
            double[] statistic = new double[256];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    int luminuce = GetLuminunce(Buffer[j, i]);
                    statistic[luminuce] += 1.0;
                }
            }

            // rateまでの積割合がどの輝度かを求める
            int p = 0;
            for (double total = 0; total < rate * W * H; p++) {
                total += statistic[p];
            }
            // pで二値化
            ProcessStatic(p);
        }

        public void ProcessMode() {

            // 各輝度を数え上げる
            double[] statistic = new double[256];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    int luminuce = GetLuminunce(Buffer[j, i]);
                    statistic[luminuce] += 1.0;
                }
            }

            // 頂点抽出
            List<(int, int)> peeks = FindPeeks(statistic);
            int p = (peeks[0].Item1 + peeks[1].Item1) / 2;

            // pで二値化
            ProcessStatic(p);
        }

        public void ProcessDelta() {
            // グレースケール
            int[,] luminunceBuffer = new int[W, H];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    luminunceBuffer[j, i] = GetLuminunce(Buffer[j, i]);
                }
            }
            // コピー
            Color[,] temp0 = new Color[W, H];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    temp0[j, i] = Buffer[j, i];
                }
            }
            // X微分
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    temp0[j, i] = Color.FromArgb(Math.Abs(luminunceBuffer[j == 0 ? j : j - 1, i] - luminunceBuffer[j, i]), 0, 0);
                }
            }
            // Y微分
            for (int i = 0; i < W; i++) {
                for (int j = 0; j < H; j++) {
                    temp0[i, j] = Color.FromArgb(temp0[i, j].R, Math.Abs(luminunceBuffer[i, j == 0 ? j : j - 1] - luminunceBuffer[i, j]), 0);
                }
            }
            // 変化量
            int[,] delta = new int[W, H];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    int average = (temp0[j, i].A + temp0[j, i].G) / 2;
                    delta[j, i] = average;
                }
            }
            (int, int) borderPixel = (0, 0);
            int deltaMax = 0;
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    if (delta[j, i] > deltaMax) {
                        deltaMax = delta[j, i];
                        borderPixel = (j, i);
                    }
                }
            }
            // 閾値
            int p = luminunceBuffer[borderPixel.Item1, borderPixel.Item2];
            // pで二値化
            ProcessStatic(p);
        }

        public void ProcessOtsu() {
            // Refered https://www58.atwiki.jp/dooooornob/pages/46.html

            // 各輝度を数え上げる
            double[] statistic = new double[256];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    int luminuce = GetLuminunce(Buffer[j, i]);
                    statistic[luminuce] += 1.0;
                }
            }

            // 頂点抽出
            List<(int, int)> peeks = FindPeeks(statistic);

            // 閾値を総当たりで調べる
            double average = statistic.Average();
            double standerdDivision = StandardDivision(statistic, average);
            double count = H * W;
            int bestBorder = 0;
            double bestScore = 0;
            for (int i = 1; i < 256; i++) {
                double[] division1 = new double[i];
                double[] division2 = new double[256 - i];
                Array.Copy(statistic, division1, i);
                Array.Copy(statistic, i, division2, 0, 256 - i);
                double count1 = division1.Sum();
                double count2 = division2.Sum();
                double average1 = division1.Average();
                double average2 = division2.Average();
                double standardDivision1 = StandardDivision(division1, average1);
                double standardDivision2 = StandardDivision(division2, average2);

                double globalVariance = count1 / count * (average1 - average) * (average1 - average)
                    + count2 / count * (average2 - average) * (average2 - average);
                double lovalVariance = count1 / count * standardDivision1 * standardDivision1
                    + count1 / count * standardDivision1 * standardDivision1;
                if (bestScore < globalVariance) {
                    bestScore = globalVariance;
                    bestBorder = i;
                }
            }
            int p = bestBorder;

            // pで二値化
            ProcessStatic(p);
        }

        public void BinarizeCore(ref Color pixel, int border) {
            int luminunce = GetLuminunce(pixel);
            //pixel = Color.FromArgb(255, luminunce, luminunce, luminunce);
            if (luminunce < border) {
                pixel = Color.White;
            } else {
                pixel = Color.Black;
            }
        }

        int GetLuminunce(Color pixel) {
            int luminuce = (int)(0.2126 * pixel.R + 0.7152 * pixel.G + 0.0722 * pixel.B);
            if (luminuce > 255) luminuce = 255;
            if (luminuce < 0) luminuce = 0;
            return luminuce;
        }

        void Output(double[][] data) {
#if MultiThread
            return;
#endif
            using (StreamWriter sw = new StreamWriter(@".\data.txt")) {
                for (int i = 0; i < data[0].Length; i++) {
                    for (int j = 0; j < data.Length; j++) {
                        sw.Write(data[j][i]);
                        sw.Write("\t");
                    }
                    sw.WriteLine();
                }
            }
            //Process.Start("notepad", @".\data.txt");
        }

        (double[], double[]) Dtf(double[] signal) {
            double[] re = new double[256];
            double[] im = new double[256];
            for (int i = 0; i < 256; i++) {
                for (int n = 0; n < 256; n++) {
                    re[n] += signal[i] * Math.Cos(2 * Math.PI * n * i / 256.0);
                    im[n] += -signal[i] * Math.Sin(2 * Math.PI * n * i / 256.0);
                }
            }
            return (re, im);
        }

        (double[], double[]) IDtf((double[], double[]) freq) {
            double[] re = new double[256];
            double[] im = new double[256];
            for (int i = 0; i < 256; i++) {
                for (int n = 0; n < 256; n++) {
                    re[n] += (freq.Item1[i] * Math.Cos(2 * Math.PI * i * n / 256.0)
                            - freq.Item2[i] * Math.Sin(2 * Math.PI * i * n / 256.0)) / 256.0;
                    im[n] += (freq.Item1[i] * Math.Sin(2 * Math.PI * i * n / 256.0)
                            + freq.Item2[i] * Math.Cos(2 * Math.PI * i * n / 256.0)) / 256.0;
                }
            }
            return (re, im);
        }

        List<(int, int)> FindPeeks(double[] statistic) {
            // 輝度グラフのノイズ除去
            (double[], double[]) temp0 = Dtf(statistic);
            (double[], double[]) temp1 = Dtf(statistic);
            // 高周波数切り落とし
            for (int i = 0; i < 240; i++) {
                temp1.Item1[i] = 0;
                temp1.Item2[i] = 0;
            }
            (double[], double[]) temp2 = IDtf(temp1);
            double[] temp3 = new double[256];
            for (int i = 0; i < temp3.Length; i++) {
                temp3[i] = temp2.Item1[i] * temp2.Item2[i];
            }
            Output(new double[][] { statistic, temp0.Item1, temp0.Item2, temp2.Item1, temp2.Item2, temp3 });
            // スケール ※負は考慮しない
            int[] temp4 = new int[256];
            double max = temp3.Max();
            for (int i = 0; i < 256; i++) {
                temp4[i] = (int)(temp3[i] / max * 254);
            }
            // 山の検出
            List<(int, int)> peek = new List<(int, int)>();
            List<(int, int)> buf = new List<(int, int)>();
            for (int i = 255; i >= 0; i--) {
                int tempMax = temp4.Max();
                for (int j = 0; j < temp4.Length; j++) {
                    if (temp4[j] == tempMax) {
                        // 頂点判定
                        bool topFlag = true;
                        for (int k = 0; k < buf.Count(); k++) {
                            if (Math.Abs(j - buf[k].Item1) <= 2) {
                                topFlag = false;
                                break;
                            }
                        }
                        if (topFlag) peek.Add((j, temp4[j]));
                        // 裾野をbuffer
                        buf.Add((j, temp4[j]));
                        temp4[j] = 0;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Found peek. {0},{1}...", peek[0].Item1, peek[1].Item1);
            Console.ResetColor();
            return peek;
        }

        double StandardDivision(double[] statistic, double average) {
            double standardDivision = 0;
            for (int j = 0; j < statistic.Length; j++) {
                standardDivision += (statistic[j] - average) * (statistic[j] - average);
            }
            return Math.Sqrt(standardDivision / 256);
        }
    }
}
