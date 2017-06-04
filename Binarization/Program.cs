using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;

namespace Binarization{
    class Program{

        static string Mode = "";
        static string FilePath = "";
        static string Param = "";
        static Bitmap SrcBmp;
        static int W = 0, H = 0;
        static Color[,] Buffer;

        static void Main(string[] args) {
#if DEBUG
            args = new string[] {
                @".\5000c.jpg",
                "--var",
                "128x128;delta"
            };
#endif
            FetchArgments(args);
            LoadImage();
            // Process
            switch (Mode) {
                case "--static": {
                        Cell cell = new Cell(Buffer);
                        cell.ProcessStatic(int.Parse(Param));
                    }
                    break;
                case "--p": {
                        Cell cell = new Cell(Buffer);
                        cell.ProcessP(double.Parse(Param));
                    }
                    break;
                case "--mode": {
                        Cell cell = new Cell(Buffer);
                        cell.ProcessMode();
                    }
                    break;
                case "--delta": {
                        Cell cell = new Cell(Buffer);
                        cell.ProcessDelta();
                    }
                    break;
                case "--otsu": {
                        Cell cell = new Cell(Buffer);
                        cell.ProcessOtsu();
                    }
                    break;
                case "--var":
                    Color[,][,] subBuffers = SliceBuffer(int.Parse(Param.Split(';')[0].Split('x')[0]), int.Parse(Param.Split(';')[0].Split('x')[1]));
#if MultiThread
                    List<Thread> processes = new List<Thread>();
                    for (int i = 0; i < subBuffers.GetLength(0); i++) {
                        for (int j = 0; j < subBuffers.GetLength(1); j++) {
                            Color[,] subBuffer = subBuffers[i,j];
                            Cell cell = new Cell(ref subBuffer);
                            ThreadStart func;
                            switch (Param.Split(';')[1]) {
                                case "static":
                                    func = () => {
                                        cell.ProcessStatic(int.Parse(Param.Split(';')[2]));
                                        Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                                    };
                                    processes.Add(new Thread(func));
                                    break;
                                case "p":
                                    func = () => {
                                        cell.ProcessP(double.Parse(Param.Split(';')[2]));
                                        Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                                    };
                                    processes.Add(new Thread(func));
                                    break;
                                case "mode":
                                    func = () => {
                                        cell.ProcessMode();
                                        Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                                    };
                                    processes.Add(new Thread(func));
                                    break;
                                case "delta":
                                    func = () => {
                                        cell.ProcessDelta();
                                        Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                                    };
                                    processes.Add(new Thread(func));
                                    break;
                                case "otsu":
                                    func = () => {
                                        cell.ProcessOtsu();
                                        Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                                    };
                                    processes.Add(new Thread(func));
                                    break;
                                default:
                                    Console.WriteLine("Invaild mode");
                                    break;
                            }
                        }
                    }
                    for (int i = 0; i < processes.Count(); i++) {
                        processes[i].Start();
                    }
                    for (int i = 0; i < processes.Count(); i++) {
                        processes[i].Join();
                    }
#else
                    for (int i = 0; i < subBuffers.GetLength(0); i++) {
                        for (int j = 0; j < subBuffers.GetLength(1); j++) {
                            Color[,] subBuffer = subBuffers[i,j];
                            Cell cell = new Cell(subBuffer);
                            Console.WriteLine("{0}x{1} / {2}x{3}", i, j, subBuffers.GetLength(0), subBuffers.GetLength(1));
                            switch (Param.Split(';')[1]) {
                                case "static": 
                                        cell.ProcessStatic(int.Parse(Param.Split(';')[2]));
                                    break;
                                case "p": 
                                        cell.ProcessP(double.Parse(Param.Split(';')[2]));
                                    break;
                                case "mode": 
                                        cell.ProcessMode();
                                    break;
                                case "delta": 
                                        cell.ProcessDelta();
                                    break;
                                case "otsu": 
                                        cell.ProcessOtsu();
                                    break;
                                default:
                                    Console.WriteLine("Invaild mode");
                                    break;
                            }
                        }
                    }
#endif
                    // Colorは構造体なので値渡し，故に書き戻しが必要
                    Merge(subBuffers);
                    break;
                default:
                    Console.WriteLine("Invaild mode");
                    break;
            }
            Console.WriteLine("Processed.");
            SaveImage();
            Console.ReadKey();
        }

        static void ShowHelp() {
            Console.WriteLine(
@"画像を二値化して保存します．

Binarization FilePath Mode [Param]

  Mode
    --static
      固定値閾値
      Param
        border
        border: int
    --p
      割合で閾値決定
      Param
        rate
        rate: float
    --mode
      ピークを検出して閾値
    --delta
      変化量最高点を閾値
    --otsu
      大津アルゴリズムで閾値
    --var
      小区間に分割しそれぞれで二値化
      Param
        wxh;submode;subparam
        w, h: int
        subparam: Mode
        subparam: Pram
");
        }

        static void FetchArgments(string[] args) {
            if (args.Length < 1 || args[0] == "--help") {
                ShowHelp();
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (args.Length < 3) {
                Console.WriteLine("Invald Argments");
                Console.ReadKey();
                Environment.Exit(1);
            }

            FilePath = args[0];
            Mode = args[1];
            Param = args[2];
            Console.WriteLine("initialize.");
        }

        static void LoadImage() {
            SrcBmp = new Bitmap(FilePath);
            W = SrcBmp.Width;
            H = SrcBmp.Height;
            Buffer = new Color[W, H];
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    Buffer[j, i] = SrcBmp.GetPixel(j, i);
                }
            }
            Console.WriteLine("Loaded.");
        }

        static void SaveImage() {
            for (int i = 0; i < H; i++) {
                for (int j = 0; j < W; j++) {
                    SrcBmp.SetPixel(j, i, Buffer[j, i]);
                }
            }
            SrcBmp.Save(Path.GetDirectoryName(FilePath) + @"\c_" + Path.GetFileName(FilePath), ImageFormat.Png);
            Console.WriteLine("Saved.");
        }

        static Color[,][,] SliceBuffer(int w, int h) {
            // 画素を分割 （wxh pxごとに分割）
            int cellW = w;
            int cellH = h;
            int cellCountW = W % cellW > 0 ? W / cellW + 1 : W / cellW;
            int cellCountH = H % cellH > 0 ? H / cellH + 1 : H / cellH;
            Color[,][,] subBuffers = new Color[cellCountW, cellCountH][,];
            for (int i = 0; i < cellCountW; i++) {
                for (int j = 0; j < cellCountH; j++) {
                    Color[,] subBuffer = new Color[cellW, cellH];
                    CopyRect(Buffer, i * cellW, j * cellH, subBuffer, 0, 0, cellW, cellH);
                    subBuffers[i,j] = subBuffer;
                }
            }
            return subBuffers;
        }

        static void Merge(Color[,][,] subBuffers) {
            for (int i = 0; i < subBuffers.GetLength(0); i++) {
                for (int j = 0; j < subBuffers.GetLength(1); j++) {
                    int w = subBuffers[i, j].GetLength(0);
                    int h = subBuffers[i, j].GetLength(1);
                    CopyRect(subBuffers[i, j], 0, 0, Buffer, w * i, h * j, w, h);
                }
            }
        }

        static void CopyRect(Color[,] src, int x, int y, Color[,] dest, int xd, int yd, int w, int h) {
            // top-left : [x,y]
            for(int i = 0; i< w; i++) {
                for(int j = 0; j < h; j++) {
                    // 書き込み先が範囲外なら何もしない
                    if (i + xd < dest.GetLength(0) && j + yd < dest.GetLength(1)) {
                        // 書き込むデータがなければ黒
                        if (i + x < src.GetLength(0) && j + y < src.GetLength(1)) {
                            dest[i + xd, j + yd] = src[i + x, j + y];

                        } else {
                            dest[i + xd, j + yd] = Color.Black;
                        }
                    }
                }
            }
        }
    }
}
