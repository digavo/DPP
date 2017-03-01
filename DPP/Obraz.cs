using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Linq;
using BitMiracle.LibTiff.Classic;

namespace DPP
{
    class Obraz
    {
        private Image<Bgr, Byte> inImg;
        private Image<Bgr, Byte> filterImg;
        private Image<Gray, Byte> edgesImg;
        private Image<Bgr, Byte> endImg;
        private Image<Gray, Byte> roads;
        private bool roadsExist;
        private int minPx = 0;
        private Image<Gray, Byte> roads_;
        private Image<Gray, Byte> edges_;

        public Obraz(string fileName)
        {
            if (fileName.ToLower().Contains("tif") || fileName.ToLower().Contains("tiff"))
                inImg = new Image<Bgr, byte>(1,1);
            else inImg = new Image<Bgr, byte>(fileName);
            
            filterImg = new Image<Bgr, byte>(inImg.Size);
            filterImg = inImg.Clone();
            edgesImg = new Image<Gray, byte>(inImg.Size);
            endImg = new Image<Bgr, byte>(inImg.Size);
            
            string scr = fileName.Insert(fileName.LastIndexOf('.'), "_r");
            if (File.Exists(scr))
            {
                roads = new Image<Gray, byte>(scr);
                roads_ = roads.Clone();
                roadsExist = true;
            }
            else
            {
                roads = new Image<Gray, byte>(inImg.Size);
                roadsExist = false;
            }
        }
        public bool ReadTiff(string fileName)
        {
            Bitmap result;
            using (Tiff tif = Tiff.Open(fileName, "r"))
            {
                FieldValue[] res = tif.GetField(TiffTag.IMAGELENGTH);
                int height = res[0].ToInt();

                res = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = res[0].ToInt();

                res = tif.GetField(TiffTag.BITSPERSAMPLE);
                short bpp = res[0].ToShort();
                if (bpp != 16)
                    return false;

                res = tif.GetField(TiffTag.SAMPLESPERPIXEL);
                short spp = res[0].ToShort();
                if (spp != 1)
                    return false;

                res = tif.GetField(TiffTag.PHOTOMETRIC);
                Photometric photo = (Photometric)res[0].ToInt();
                if (photo != Photometric.MINISBLACK && photo != Photometric.MINISWHITE)
                    return false;

                int stride = tif.ScanlineSize();
                byte[] buffer = new byte[stride];

                result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                byte[] buffer8Bit = null;

                for (int i = 0; i < height; i++)
                {
                    Rectangle imgRect = new Rectangle(0, i, width, 1);
                    BitmapData imgData = result.LockBits(imgRect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                    if (buffer8Bit == null)
                        buffer8Bit = new byte[imgData.Stride];
                    else
                        Array.Clear(buffer8Bit, 0, buffer8Bit.Length);
                    tif.ReadScanline(buffer, i);
                    convertBuffer(buffer, buffer8Bit);
                    Marshal.Copy(buffer8Bit, 0, imgData.Scan0, buffer8Bit.Length);
                    result.UnlockBits(imgData);
                }
            }
            try { inImg = new Image<Bgr, byte>(result);}
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " \n\n" + ex.Message + "\n\n" + ex.InnerException);
            }
            return true;
        }

        public Bitmap InImg { get { return inImg.Bitmap; } }
        public Bitmap FilterImg { get { return filterImg.Bitmap; } }
        public Bitmap EdgesImg { get { return edgesImg.Bitmap; } }
        public Bitmap EndImg { get { return endImg.Bitmap; } }
        public Bitmap Roads { get { return roads.Bitmap; } }
        public void Reset()
        {
            filterImg = inImg.Clone();
        }
        private static void convertBuffer(byte[] buffer, byte[] buffer8Bit)
        {
            for (int src = 0, dst = 0; src < buffer.Length; dst++)
            {
                int value16 = buffer[src++];
                value16 = value16 + (buffer[src++] << 8);
                buffer8Bit[dst] = (byte)(value16 / 257.0 + 0.5);
            }
        }
        private void RGBtoHSV(int r, int g, int b, out float Hue, out float Val, out float Sat)
        {
            float R = (float)r, G = (float)g, B = (float)b;
            float cmax = Math.Max(R, Math.Max(G, B));
            float cmin = Math.Min(R, Math.Min(G, B));
            float delta = cmax - cmin;
            if (cmin == cmax) Hue = 0;
            else
            {
                if (cmax == R)
                    Hue = ((G - B) * 60) / delta;
                else if (cmax == G)
                    Hue = 120 + ((B - R) * 60) / delta;
                else Hue = 240 + ((R - G) * 60) / delta;
            }
            if (Hue < 0) Hue = Hue + 360;
            if (cmax == 0) Sat = 0;
            else Sat = (delta * 100) / cmax;
            Val = (100 * cmax) / 255;
        }
        private void RemoveColor(byte[] tab, int x1, int y1, int x2, int y2, int width, int depth, int MinVal, int MaxVal, int MaxSat)
        {
            for (int i = x1; i < x2; i++)
            {
                for (int j = y1; j < y2; j++)
                {
                    int offset = ((j * width) + i) * depth;
                    float hue, sat, val;
                    RGBtoHSV(tab[offset + 2], tab[offset + 1], tab[offset + 0], out hue, out val, out sat);
                    int a = (int)(MaxSat / (100 - MaxVal)), b = (int)(-MaxVal * a);
                    if (!(val > MinVal && sat < MaxSat && sat > (int)(a * val + b)))
                        tab[offset + 2] = tab[offset + 1] = tab[offset + 0] = (byte)0;
                }
            }
        }

        //------ Filtry -----------------------
        public Bitmap BilateralFilter(int d = 15, double sColor = 80, double sSpace = 80)
        {
            UMat outImg = new UMat();
            CvInvoke.BilateralFilter(inImg, outImg, d, sColor, sSpace);
            filterImg = outImg.ToImage<Bgr, Byte>();
            return outImg.Bitmap;
        }
        public Bitmap ColorFilter(int minVal, int maxVal, int maxSat)
        {
            Bitmap newImg = inImg.Clone().Bitmap;
            Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
            BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
            int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            byte[] buffer = new byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            Parallel.Invoke(
                () => { RemoveColor(buffer, 0, 0, data.Width / 2, data.Height / 2, data.Width, depth, minVal, maxVal, maxSat); }, //top left
                () => { RemoveColor(buffer, data.Width / 2, 0, data.Width, data.Height / 2, data.Width, depth, minVal, maxVal, maxSat); }, //top - right
                () => { RemoveColor(buffer, 0, data.Height / 2, data.Width / 2, data.Height, data.Width, depth, minVal, maxVal, maxSat); }, //bottom - left
                () => { RemoveColor(buffer, data.Width / 2, data.Height / 2, data.Width, data.Height, data.Width, depth, minVal, maxVal, maxSat); }  //bottom - right
            );
            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            newImg.UnlockBits(data);
            filterImg = new Image<Bgr, byte>(newImg);
            //edges_ = new Image<Gray, byte>(newImg);
            return newImg;
        }
        public Bitmap ColorFilter2(int minVal, int maxVal, int maxSat)
        {
            ColorFilter(minVal, maxVal, maxSat);
            filterImg = filterImg.Convert<Bgr, byte>().ThresholdBinary(new Bgr(0,0,0), new Bgr(255,255,255));
            edges_ = filterImg.Convert<Gray,byte>();
            /*Mat bw = new Mat();
            CvInvoke.CvtColor(inImg, bw, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(bw, bw, 40, 255, ThresholdType.Binary | ThresholdType.Otsu);
            filterImg = bw.ToImage<Bgr, byte>();
            edges_ = filterImg.Convert<Gray, byte>();
            edgesImg = edges_.Clone();*/
            return filterImg.Bitmap;
        }

        //------ Krawędzie -------------------------
        public Bitmap Laplacian(double Threshold)
        {
            Image<Gray, byte> img = filterImg.Convert<Gray, byte>();
            UMat edges = new UMat();
            CvInvoke.Laplacian(img, edges, DepthType.Default);
            CvInvoke.ConvertScaleAbs(edges, edges, 1, 0);
            CvInvoke.Threshold(edges, edges, 10, 255, ThresholdType.Binary);
            edgesImg = edges.ToImage<Gray, byte>(); ;
            edges_ = edgesImg.Clone();
            edges.Dispose();
            return edgesImg.Bitmap;
        }
        public Bitmap Sobel(double Threshold)
        {
            Image<Gray, byte> img = filterImg.Convert<Gray, byte>();
            Image<Gray, float> edges_x = img.Sobel(1, 0, 3);
            Image<Gray, float> edges_y = img.Sobel(0, 1, 3);
            UMat abs_x = new UMat(), abs_y = new UMat(), grad = new UMat();
            CvInvoke.ConvertScaleAbs(edges_x, abs_x, 1, 0);
            CvInvoke.ConvertScaleAbs(edges_y, abs_y, 1, 0);
            CvInvoke.AddWeighted(abs_x, 0.5, abs_y, 0.5, 0, grad);
            CvInvoke.Threshold(grad, grad, Threshold, 255, ThresholdType.Binary);
            edgesImg = grad.ToImage<Gray, byte>();
            edges_ = edgesImg.Clone();
            grad.Dispose();
            img.Dispose();
            edges_x.Dispose();
            edges_y.Dispose();
            return edgesImg.Bitmap;
        }
        public Bitmap Canny(double Threshold, double ThresholdLinking)
        {
            Image<Gray, byte> img = filterImg.Convert<Gray, byte>();
            UMat edges = new UMat();
            CvInvoke.Canny(img, edges, Threshold, ThresholdLinking,3,true);
            edgesImg = edges.ToImage<Gray, byte>();
            edges_ = edgesImg.Clone();
            edges.Dispose();
            return edgesImg.Bitmap;
        } 

        //------ Linie i prostokąty ----------------
        public Bitmap HoughLine(int threshold, double minLineWidth, double gapSize)
        {
            LineSegment2D[] HLines = CvInvoke.HoughLinesP(
                edgesImg,
                1, //Distance resolution in pixel-related units
                Math.PI / 180.0, //Angle resolution measured in radians. 45
                threshold, //threshold
                minLineWidth, //min Line width
                gapSize); //gap between lines
            using (Image<Bgr, Byte> outImg = inImg.Clone())
            {
                foreach (LineSegment2D line in HLines)
                    outImg.Draw(line, new Bgr(Color.Yellow), 2);
                endImg = outImg.Clone();
            }
            return endImg.Bitmap;
        }
        public LineSegment2D[] HoughLineTests(int threshold, double minLineWidth, double gapSize)
        {
            try
            {
                LineSegment2D[] HLines = edges_.HoughLinesBinary(1, Math.PI / 180.0, threshold, minLineWidth, gapSize)[0];
                return HLines;
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("hough" + ex.ToString());
                return new LineSegment2D[0];
            }
        }
        public Bitmap FindContours()
        {
            Image<Bgr, Byte> outImg = inImg.Clone();
            List<RotatedRect> boxList = new List<RotatedRect>();
            List<RotatedRect> box2List = new List<RotatedRect>();
            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(edgesImg.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple); 
                for (int i = 0; i < contours.Size; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        if (CvInvoke.ContourArea(approxContour, false) > 10) 
                        {
                            if (approxContour.Size == 3) // triangle
                            {
                                Point[] pts = approxContour.ToArray();
                                triangleList.Add(new Triangle2DF(
                                    pts[0],
                                    pts[1],
                                    pts[2]
                                    ));
                            }
                            else
                            if (approxContour.Size == 4) 
                            {
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edg = PointCollection.PolyLine(pts, true);
                                for (int j = 0; j < edg.Length; j++)
                                {
                                    double angle = Math.Abs(
                                        edg[(j + 1) % edg.Length].GetExteriorAngleDegree(edg[j]));
                                    if (angle < 60 || angle > 120)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                            }
                            else if (approxContour.Size > 4 && approxContour.Size < 6)
                            {
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edg = PointCollection.PolyLine(pts, true);
                                for (int j = 0; j < edg.Length; j++)
                                {
                                    double angle = Math.Abs(
                                        edg[(j + 1) % edg.Length].GetExteriorAngleDegree(edg[j]));
                                    if (angle < 50 || angle > 130)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                if (isRectangle) box2List.Add(CvInvoke.MinAreaRect(approxContour));
                            }
                        }
                    }
                }
            }
            foreach (RotatedRect box in boxList)
                outImg.Draw(box, new Bgr(Color.Red), 1);
            foreach (RotatedRect box in box2List)
                outImg.Draw(box, new Bgr(Color.Blue), 1);
            foreach (Triangle2DF tr in triangleList)
                outImg.Draw(tr, new Bgr(Color.Yellow), 1);
            endImg = outImg.Clone();
            outImg.Dispose();
            return endImg.Bitmap;
        }

        
        //------ Metoda 1 --------------------------
        private void metod1Wątek(byte[] tab, int x1, int y1, int x2, int y2, int width, int depth) // canny 80, 50
        {
            int[,] pom = new int[x2, y2];
            UnionFind unia = new UnionFind();
            int count = 0;
            #region obliczanie odległości od krawędzi - sąsiedztwo 8
            pom[0, 0] = 0;
            for (int i = x1 + 1; i < x2; i++)
                pom[i, 0] = 0;
            for (int i = y1 + 1; i < y2; i++)
                pom[0, i] = 0;

            for (int i = x1 + 1; i < x2; i++)
                for (int j = y1 + 1; j < y2; j++)
                {
                    int k = ((j * width) + i) * depth;
                    if (tab[k] == 255)
                    {
                        pom[i, j] = 0;
                        tab[k] = tab[k + 1] = tab[k + 2] = (byte)0;
                        continue;
                    }
                    int m = Math.Min(pom[i - 1, j], pom[i, j - 1]);
                    m = Math.Min(m, pom[i - 1, j - 1]);
                    if (j + 1 < y2) m = Math.Min(m, pom[i - 1, j + 1]);
                    m++;
                    pom[i, j] = m;
                    tab[k] = tab[k + 1] = tab[k + 2] = (byte)(Math.Min(25 * m, 255));
                }
            for (int i = x2 - 2; i > x1; i--)
                for (int j = y2 - 2; j > y1; j--)
                {
                    int k = ((j * width) + i) * depth;
                    int m = Math.Min(pom[i + 1, j], pom[i, j + 1]);
                    m = Math.Min(m, pom[i + 1, j + 1]);
                    if (j - 1 > y1) m = Math.Min(m, pom[i + 1, j - 1]);
                    m = Math.Min(m, pom[i, j] - 1);
                    m++;
                    pom[i, j] = m;
                    tab[k] = tab[k + 1] = tab[k + 2] = (byte)(Math.Min(25 * m, 255));
                }
            #endregion
            #region piksele centralne z 3x3
            for (int i = x1 + 1; i < x2 - 1; i++)
                for (int j = y1 + 1; j < y2 - 1; j++)
                {
                    int k = ((j * width) + i) * depth;
                    tab[k] = tab[k + 1] = tab[k + 2] = 0;
                    List<int> pom2 = new List<int> {pom[i, j - 1], pom[i, j], pom[i, j + 1],
                                                    pom[i - 1, j - 1], pom[i - 1, j], pom[i - 1, j + 1],
                                                    pom[i + 1, j - 1], pom[i + 1, j], pom[i + 1, j + 1],};
                    int m = pom2.Max();
                    if (pom[i, j] == m && m > minPx && m < (minPx+8))
                    {
                        tab[k] = tab[k + 1] = tab[k + 2] = 255;
                        pom[i, j] = m * 100;
                        unia.Add(count,i,j);
                        count++;
                    }
                }
            #endregion
            #region grupowanie
            count = 0;
            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                    pom[i, j] = 0;
            
            foreach(var cp in unia.lista)
                pom[cp.x, cp.y] = cp.index;

            int p = 5;
            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                {
                    List<int> index = new List<int>();
                    int pi = p, pj = p;
                    if (i + p >= x2 || j + p >= y2)
                    {
                        pi = x2 - i;
                        pj = y2 - j;
                    }
                    for (int ii = i; ii < i + pi; ii++)
                        for (int jj = j; jj < j + pj; jj++)
                            if (pom[ii, jj] > 0)
                                index.Add(pom[ii, jj]);
                    for (int k = index.Count() - 1; k > 0; k--)
                    unia.Union(index[0], index[k]);
                    
                }
            Rgb[] kolory = new Rgb[6] { new Rgb(Color.Blue), new Rgb(Color.Yellow), new Rgb(Color.Violet), new Rgb(Color.Green),  new Rgb(Color.Red), new Rgb(Color.Pink) };
            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                {
                    if (pom[i,j] > 0)
                    {
                        int k = ((j * width) + i) * depth;
                        int m = unia.Find(unia.lista[pom[i, j]]).group;
                        int n = m%6;
                        if (unia.Find(unia.lista[pom[i, j]]).c < 15) continue;

                        tab[k] = tab[k + 1] = tab[k + 2] = (byte)255;
                        //tab[k] = (byte) kolory[n].Blue; 
                        //tab[k + 1] = (byte) kolory[n].Green;
                        //tab[k + 2] = (byte) kolory[n].Red;
                    }
                }
            #endregion



        }
        public Bitmap metod1(int minp) 
        {
            minPx = minp;
            Bitmap newImg = new Bitmap(edgesImg.Convert<Bgr,byte>().Bitmap);
            Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
            BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
            int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            byte[] buffer = new byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            Parallel.Invoke(
                () => { metod1Wątek(buffer, 0, 0, data.Width, data.Height, data.Width, depth); }
            );
            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            newImg.UnlockBits(data);
            //Image<Bgr, byte> outImg = new Image<Bgr, byte>(newImg.Size);
            
            Image<Gray, byte> outImg = new Image<Gray, byte>(newImg.Size);
            outImg = new Image<Gray, byte>(newImg);
            #region wykluczenie obszarów zieleni
            Mat bw = new Mat();
            CvInvoke.CvtColor(inImg, bw, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(bw, bw, 40, 255, ThresholdType.Binary | ThresholdType.Otsu);
            CvInvoke.Min(new Image<Gray, byte>(newImg), bw, outImg);
            #endregion
            endImg = outImg.Convert<Bgr,byte>();
            return outImg.Bitmap;
            /*
            Image<Gray, byte> gray = inImg.Convert<Gray, byte>();
            Image<Gray, byte> binary = edgesImg.Not().ThresholdBinary(new Gray(100), new Gray(255));
            Image<Gray, float> dist = new Image<Gray, float>(binary.Size);
            CvInvoke.DistanceTransform(binary, dist, null, DistType.L2, 5);
            CvInvoke.Normalize(dist, dist, 0, 255, NormType.MinMax);
            endImg = dist.Convert<Bgr, byte>();
            return dist.Bitmap;*/
        }
        
        //------ Testy -----------------------------
        public bool trueRoads ()
        {
            if (!roadsExist) return false;
            //roads_ = roads.Dilate(it);
            return true;
        }

        private int popTr1 = -1, popTr2 = -1;
        // krawędzie + Hough
        public double[] Test1 (int rodzaj, int tr1, int tr2, int trH, int h1, int h2)
        {
            if (!roadsExist) return new double[] { 0 };
            if (popTr1 != tr1 || popTr2 != tr2)
            {
                if (rodzaj == 0)
                    Sobel(tr1);
                else if (rodzaj == 1)
                    Canny(tr1, tr2);
                popTr1 = tr1;
                popTr2 = tr2;
            }
            LineSegment2D[] lines = HoughLineTests(trH, h1, h2);
            #region wyznaczenie miar jakości
            double result = 0;
            Image<Gray, Byte> gray = roads_.Copy();
            Image<Gray, Byte> white = roads_.CopyBlank();
            white.SetZero();
            int whitePxs = gray.CountNonzero()[0];
            foreach (LineSegment2D line in lines)
            {
                gray.Draw(line, new Gray(0), 1);
                white.Draw(line, new Gray(255), 1);
            }
            int whitePxs2 = gray.CountNonzero()[0], linePxs = white.CountNonzero()[0];
            result = ((double)(whitePxs - whitePxs2)*100) / (double)linePxs;
            //System.Windows.Forms.MessageBox.Show(whitePxs +" " +whitePxs2 +"\n"+(whitePxs-whitePxs2)*100+" / " + linePxs+" = " + result+"\n" + ((whitePxs - whitePxs2)*100) / linePxs);
            /*System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            System.Windows.Forms.PictureBox pictureBox = new System.Windows.Forms.PictureBox();
            pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBox.Image = Roads;
            pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            form.Controls.Add(pictureBox);
            form.ShowDialog();*/
            gray.Dispose();
            white.Dispose();
            #endregion
            return new double[] { result, lines.GetLength(0)};
        }

        private int popD = -1, popColor = -1, popSpace = -1;
        // filtr bilateralny + krawędzie + Hough
        public double[] Test2(int rodzaj, int d, int color, int space, int tr1, int tr2, int trH, int h1, int h2)
        {
            if (!roadsExist) return new double[] { 0 };
            double result = 0;
            if (popD != d || popColor != color || popSpace != space)
            {
                BilateralFilter(d, color, space);
                popD = d; popColor = color; popSpace = space;
            }

            if (popTr1 != tr1 || popTr2 != tr2)
            {
                if (rodzaj == 0)
                    Sobel(tr1);
                else if (rodzaj == 1)
                    Canny(tr1, tr2);
                popTr1 = tr1;
                popTr2 = tr2;
            }
            LineSegment2D[] lines = HoughLineTests(trH, h1, h2);
            #region błąd wyznaczonych lini w stosunku do prawidłowych 
            Image<Gray, Byte> gray = roads_.Copy();
            Image<Gray, Byte> white = roads_.CopyBlank();
            white.SetZero();
            int whitePxs = gray.CountNonzero()[0];
            foreach (LineSegment2D line in lines)
            {
                gray.Draw(line, new Gray(0), 1);
                white.Draw(line, new Gray(255), 1);
            }
            int whitePxs2 = gray.CountNonzero()[0], linePxs = white.CountNonzero()[0];
            result = ((double)(whitePxs - whitePxs2) * 100) / (double)linePxs;
            //System.Windows.Forms.MessageBox.Show(whitePxs +" " +whitePxs2 +"\n"+(whitePxs-whitePxs2)*100+" / " + linePxs+" = " + result+"\n" + ((whitePxs - whitePxs2)*100) / linePxs);
            gray.Dispose();
            white.Dispose();
            #endregion
            return new double[] { result, lines.GetLength(0) };
        }

        // filtr koloru + krawędzie + Hough
        public double[] Test3(int minVal, int maxVal, int maxSat, int trH, double h1, double h2)
        {
            if (!roadsExist) return new double[] { 0 };
            ColorFilter2(minVal, maxVal, maxSat);
            LineSegment2D[] lines = HoughLineTests(trH, h1, h2);

            #region wyznaczenie miar jakości
            //TP - poprawnie wykryte, FP - niepoprawnie wykryte, FN - niewykryte (powinny być wykryte)
            double com = 0, cor = 0, q = 0;
            int TP = 0, FP = 0, FN = 0;
            Image<Gray, Byte> refImg = roads_.Copy();
            Image<Gray, Byte> outImg = roads_.CopyBlank();
            outImg.SetZero();
            TP = refImg.CountNonzero()[0];
            foreach (LineSegment2D line in lines)
            {
                refImg.Draw(line, new Gray(0), 1); // białe drogi poprawne - znalezione, pozostałe biało = FN
                outImg.Draw(line, new Gray(255), 1); // czarny obraz, białe znalezione linie
            }
            FN = refImg.CountNonzero()[0];
            TP = TP - FN;
            FP = outImg.CountNonzero()[0] - TP;
            int whitePxs2 = refImg.CountNonzero()[0], linePxs = outImg.CountNonzero()[0];
            com = 100 * (double)TP / (double)(TP + FP);
            cor = 100 * (double)TP / (double)(TP + FN);
            q = 100 * (double)TP / (double)(TP + FP + FN);
            refImg.Dispose();
            outImg.Dispose();
            #endregion
            return new double[] { com, cor, q, lines.GetLength(0) };
        }
    }
    class UnionFind
    {
        public struct CentralPixel
        {
            public int index;
            public int group;
            public int x;
            public int y;
            public int c;
        }
        public List<CentralPixel> lista = new List<CentralPixel>();
        public void Add (int i, int xx, int yy)
        {
            lista.Add(new CentralPixel() { index = i, group = i, x = xx, y = yy, c = 1 });
        }
        public void Union (int a, int b)
        {
            CentralPixel fa = Find(lista[a]);
            CentralPixel fb = Find(lista[b]);
            if (fa.index == fb.index) return;

            if (fa.c < fb.c)
            {
                lista[fa.index] = new CentralPixel() { index = fa.index, group = fb.index, x = fa.x, y = fa.y, c = fa.c };
                lista[fb.index] = new CentralPixel() { index = fb.index, group = fb.index, x = fb.x, y = fb.y, c = fb.c + fa.c };
            }
            else
            {
                lista[fb.index] = new CentralPixel() { index = fb.index, group = fa.index, x = fb.x, y = fb.y, c = fb.c };
                lista[fa.index] = new CentralPixel() { index = fa.index, group = fa.index, x = fa.x, y = fa.y, c = fb.c + fa.c };
            }
                }
        public CentralPixel Find (CentralPixel a)
        {
            if (a.group == a.index) return a;
            CentralPixel fa = Find(lista.Find(k => k.index == a.group));
            lista[a.index] = new CentralPixel() { index = a.index, group = fa.index, x = a.x, y = a.y, c = a.c };
            return fa;
        }

        public override string ToString()
        {
            string s = "i g c\n";
            foreach (var p in lista)
            {
                s += p.index + " " + p.group + " " + p.c + "\n";
            }
            return s;
        }

    }
}
