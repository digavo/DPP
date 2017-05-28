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
using Emgu.CV.Features2D;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using System.Windows.Forms;

namespace DPP
{
    class Obraz
    {
        private Image<Bgr, Byte> inImg;
        private Image<Bgr, Byte> filterImg;
        private Image<Gray, Byte> edgesImg;
        private Image<Bgr, Byte> endImg;
        private Image<Gray, Byte> linesImg;
        private Image<Gray, Byte> roads;
        private bool roadsExist;
        private int minPx = 0, maxPx = 100000;
        private LineSegment2D[] lines;

        public Obraz(string fileName)
        {
            inImg = new Image<Bgr, byte>(@fileName);
            filterImg = new Image<Bgr, byte>(inImg.Size);
            filterImg = inImg.Clone();
            edgesImg = new Image<Gray, byte>(inImg.Size);
            endImg = new Image<Bgr, byte>(inImg.Size);
            linesImg = new Image<Gray, byte>(inImg.Size);
            string scr = fileName.Insert(fileName.LastIndexOf('.'), "_r");
            Console.WriteLine(scr);
            if (File.Exists(scr))
            {
                roads = new Image<Gray, byte>(scr);
                roadsExist = true;
            }
            else
            {
                roads = new Image<Gray, byte>(inImg.Size);
                roadsExist = false;
            }

        }
        /*public bool ReadTiff(string fileName)
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
        */
        public Bitmap InImg { get { return inImg.Bitmap; } }
        public Bitmap FilterImg { get { return filterImg.Bitmap; } }
        public Bitmap EdgesImg { get { return edgesImg.Bitmap; } }
        public Bitmap EndImg { get { return endImg.Bitmap; } }
        public Bitmap Roads { get { return roads.Bitmap; } }
        public Bitmap LinesImg { get { return linesImg.Bitmap; } }
        public void Reset()
        {
            filterImg = inImg.Clone();
            filterImg = inImg.Clone();
            edgesImg = new Image<Gray, byte>(inImg.Size);
            endImg = new Image<Bgr, byte>(inImg.Size);
            linesImg = new Image<Gray, byte>(inImg.Size);
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
            return newImg;
        }
        public Bitmap ColorFilter2(int minVal, int maxVal, int maxSat)
        {
            /*ColorFilter(minVal, maxVal, maxSat);
            filterImg = filterImg.Convert<Bgr, byte>().ThresholdBinary(new Bgr(0,0,0), new Bgr(255,255,255));
            edges_ = filterImg.Convert<Gray,byte>();
            edgesImg = filterImg.Convert<Gray, byte>();
            return filterImg.Bitmap;
            */
            Mat bw = new Mat();
            CvInvoke.CvtColor(inImg, bw, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(bw, bw, 100, 255, ThresholdType.Binary | ThresholdType.Otsu);
            filterImg = bw.ToImage<Bgr, byte>();
            edgesImg = filterImg.Convert<Gray, byte>();
            return edgesImg.Bitmap;

        }

        public Bitmap MedianFilter()
        {
            CvInvoke.MedianBlur(inImg, filterImg, 3);
            return filterImg.Bitmap;
        }
        public void MedianFilter_Test()
        {
            CvInvoke.MedianBlur(inImg, filterImg, 3);
        }

        //------ Krawędzie -------------------------
        public Bitmap Laplacian(double Threshold)
        {
            Image<Gray, byte> img = filterImg.Convert<Gray, byte>();
            UMat edges = new UMat();
            CvInvoke.Laplacian(img, edges, DepthType.Default);
            CvInvoke.ConvertScaleAbs(edges, edges, 1, 0);
            CvInvoke.Threshold(edges, edges, 10, 255, ThresholdType.Binary);
            edgesImg = edges.ToImage<Gray, byte>(); 
            edges.Dispose();
            return edgesImg.Bitmap;
        }
        public Bitmap Sobel(double Threshold)
        {
            using (Image<Gray, byte> img = filterImg.Convert<Gray, byte>())
            {
                Image<Gray, float> edges_x = img.Sobel(1, 0, 3);
                Image<Gray, float> edges_y = img.Sobel(0, 1, 3);
                UMat abs_x = new UMat(), abs_y = new UMat(), grad = new UMat();
                CvInvoke.ConvertScaleAbs(edges_x, abs_x, 1, 0);
                CvInvoke.ConvertScaleAbs(edges_y, abs_y, 1, 0);
                CvInvoke.AddWeighted(abs_x, 0.5, abs_y, 0.5, 0, grad);
                CvInvoke.Threshold(grad, grad, Threshold, 255, ThresholdType.Binary);
                edgesImg = grad.ToImage<Gray, byte>();
                abs_x.Dispose();
                abs_y.Dispose();
                grad.Dispose();
                img.Dispose();
                edges_x.Dispose();
                edges_y.Dispose();
            }
            return edgesImg.Bitmap;
        }
        public Bitmap Canny(double Threshold, double ThresholdLinking)
        {
            CvInvoke.Canny(filterImg.Convert<Gray, byte>(), edgesImg, Threshold, ThresholdLinking, 3, true);
            //CvInvoke.Canny(filterImg, edgesImg, Threshold, ThresholdLinking, 3, true);
            
            return edgesImg.Bitmap;
        }
        public void Sobel_Test(double Threshold)
        {
            using (Image<Gray, byte> img = filterImg.Convert<Gray, byte>())
            {
                Image<Gray, float> edges_x = img.Sobel(1, 0, 3);
                Image<Gray, float> edges_y = img.Sobel(0, 1, 3);
                UMat abs_x = new UMat(), abs_y = new UMat(), grad = new UMat();
                CvInvoke.ConvertScaleAbs(edges_x, abs_x, 1, 0);
                CvInvoke.ConvertScaleAbs(edges_y, abs_y, 1, 0);
                CvInvoke.AddWeighted(abs_x, 0.5, abs_y, 0.5, 0, grad);
                CvInvoke.Threshold(grad, grad, Threshold, 255, ThresholdType.Binary);
                edgesImg = grad.ToImage<Gray, byte>();
                abs_x.Dispose();
                abs_y.Dispose();
                grad.Dispose();
                img.Dispose();
                edges_x.Dispose();
                edges_y.Dispose();
            }
        }
        public void Canny_Test(double Threshold, double ThresholdLinking)
        {
            CvInvoke.Canny(filterImg.Convert<Gray, byte>(), edgesImg, Threshold, ThresholdLinking, 3, true);
        }

        //------ Linie i prostokąty ----------------
        public Bitmap HoughLine(int threshold, double minLineWidth, double gapSize)
        {
            lines = CvInvoke.HoughLinesP(
                edgesImg,
                1, //Distance resolution in pixel-related units
                Math.PI / 180.0, //Angle resolution measured in radians. 45
                threshold, //threshold
                minLineWidth, //min Line width
                gapSize); //gap between lines
            linesImg.SetZero();
            endImg = inImg.Clone();
            //Console.Write("lini: "+lines.Count()+"   |  ");
            ConnectLines(lines);
            linesImg.SetZero();
            foreach (LineSegment2D line in lines)
            {
                endImg.Draw(line, new Bgr(Color.Yellow), 2);
                linesImg.Draw(line, new Gray(255), 1);
            }
            ConnectFillLines(lines);
            //Console.Write("lini2: " + lines.Count()+"\n");
            return endImg.Bitmap;
        }
        public LineSegment2D[] HoughLineTests(int threshold, double minLineWidth, double gapSize)
        {
            try
            {
                LineSegment2D[] HLines = edgesImg.HoughLinesBinary(1, Math.PI / 180.0, threshold, minLineWidth, gapSize)[0];
                return HLines;
            }
            catch(Exception ex)
            {
                Console.WriteLine("hough" + ex.ToString());
                return new LineSegment2D[0] ;
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
                            /*if (approxContour.Size == 3) // triangle
                            {
                                Point[] pts = approxContour.ToArray();
                                triangleList.Add(new Triangle2DF(
                                    pts[0],
                                    pts[1],
                                    pts[2]
                                    ));
                            }
                            else*/
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
                outImg.Draw(box, new Bgr(Color.Red), 2);
            foreach (RotatedRect box in box2List)
                outImg.Draw(box, new Bgr(Color.Blue), 2);
            foreach (Triangle2DF tr in triangleList)
                outImg.Draw(tr, new Bgr(Color.Yellow), 2);
            endImg = outImg.Clone();
            outImg.Dispose();
            return endImg.Bitmap;
        }
        
        //------ Filtr po --------------------------
        public Bitmap PostColor()
        {
            #region wykluczenie obszarów zieleni
            Image<Gray, Byte> outimg = new Image<Gray, byte>(inImg.Size);
            Mat bw = new Mat();
            CvInvoke.CvtColor(inImg, bw, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(bw, bw, 40, 255, ThresholdType.Binary | ThresholdType.Otsu);
            CvInvoke.Min(linesImg, bw, outimg);
            #endregion
            return outimg.Bitmap;
            // usunąć też linie które są w tym obszarze
        }

        //------ Jakość ----------------------------
        public double[] Quality(bool m)
        {
            #region wyznaczenie miar jakości
            //TP - poprawnie wykryte, FP - niepoprawnie wykryte, FN - niewykryte (powinny być wykryte)
            double com = 0, cor = 0, q = 0;
            int TP = 0, FP = 0, FN = 0;
            Image<Gray, Byte> refImg = roads.Clone();
            //if (m) refImg = roads.Erode(2);
            TP = refImg.CountNonzero()[0];
            refImg = refImg.Sub(linesImg);
            FN = refImg.CountNonzero()[0];
            TP = TP - FN;
            int whitePxs2 = refImg.CountNonzero()[0], linePxs = linesImg.CountNonzero()[0];
            FP = linePxs - TP;
            com = 100 * (double)TP / (double)(TP + FN);
            cor = 100 * (double)TP / (double)(TP + FP);
            q = 100 * (double)TP / (double)(TP + FP + FN);
            refImg.Dispose();
            #endregion
            return new double[] { com, cor, q };
        }

        //------ Metoda 1 --------------------------
        List<Punkt> centralPixels;
        private void metodaPixWątek(byte[] tab, int x1, int y1, int x2, int y2, int width, int depth) 
        {
            centralPixels = new List<Punkt>();
            int[,] dist = new int[x2, y2]; //odległości od krawędzi
            for (int i = x1; i < x2; i++)
                for (int j = y1; j < y2; j++)
                {
                    int k = ((j * width) + i) * depth;
                    dist[i, j] = tab[((j * width) + i) * depth];
                    tab[k] = tab[k + 1] = tab[k + 2] = 0;
                    if (dist[i, j] < minPx) dist[i, j] = 0; //? usunięcie pikseli o małej odległości
                }
            #region maksima lokalne w 3x3
            int mSize = 3, mCenter = (mSize - 1) / 2;
            for (int i = x1 + mCenter; i < x2 - mCenter; i++)
                for (int j = y1 + mCenter; j < y2 - mCenter; j++)
                {
                    if (dist[i, j] == 0) continue;
                    List<int> pom2 = new List<int>();
                    int m = dist[i, j], n = 1;
                    for (int ii = i - mCenter; ii <= i + mCenter; ii++)
                        for (int jj = j - mCenter; jj <= j + mCenter; jj++)
                        {
                            if (ii == i & jj == j) continue;
                            //dodać wyrzucanie pix?
                            if (dist[ii, jj] > m) break;
                            else n++;
                        }
                    if (n==9 && m > minPx && m < maxPx)
                    {
                        int k = ((j * width) + i) * depth;
                        centralPixels.Add(new Punkt(m, i, j));
                        //dist[i, j] = m * 200; //dodać wyrzucanie pix?
                        tab[k] = tab[k + 1] = tab[k + 2] = 255;
                    }
                }
            #endregion
            dist = null;
        }

        List<int> parents;
        private void metodaPixSegmentacja(byte[] tab, int x1, int y1, int x2, int y2, int width, int depth)
        {
            parents = new List<int>();
            parents.Add(0);
            int[,] label = new int[x2, y2]; //etykiety, tab - białe obszary do podziału
            //int[,] we = new int[x2, y2];

            int number = 1;
            int mSize = 3, mCenter = (mSize - 1) / 2;
            for (int j = y1; j < y2; j++)
            {
                for (int i = x1; i < x2; i++)
                {
                    int k = ((j * width) + i) * depth;
                    if (tab[k] == 0) // czarny piksel
                    {
                        label[i, j] = 0;
                        continue;
                    }
                    int min;
                    // sąsiedzi, którzy już mają wartości:
                    List<Punkt> s = new List<Punkt>();
                    if (i - 1 >= 0) s.Add(new Punkt(label[i - 1, j], i - 1, j));
                    if (j - 1 >= 0 && i - 1 >= 0) s.Add(new Punkt(label[i - 1, j - 1], i - 1, j - 1));
                    if (j - 1 >= 0) s.Add(new Punkt(label[i, j - 1], i, j - 1));
                    if (i + 1 < x2 && j - 1 >= 0) s.Add(new Punkt(label[i + 1, j - 1], i + 1, j - 1));

                    if (s.Where(c => c.v == 0).Count() == s.Count()) // nie ma sąsiada z numerem = wszyscy sąsiedzi są czarni
                    {
                        parents.Add(number);
                        label[i, j] = number;
                        number++;
                    }
                    else
                    {
                        min = s.Where(c => c.v > 0).OrderBy(c => c.v).First().v;
                        label[i, j] = min;
                        foreach (var p in s)
                        {
                            if (p.v == 0) continue;
                            //if (parents[p.v] == p.v) // ta etykieta nie ma jeszcze powiązania
                            parents[p.v] = min;
                            //else //ta etykieta należy już do innej grupy
                            //Union(p.v, min);
                        }
                    }
                    //zapisz pikselowi centralnemu etykietę
                    //int index = centralPixels.FindIndex(c => c.x == i && c.y == j);
                    //if (index > -1) centralPixels[index].v = label[i, j];
                }
            }
            for (int i = 1; i < parents.Count(); i++)
            {
                if (parents[i] != i)
                    Find(i);
            }
            /*foreach (var s in parents)
                Console.Write(" " + s);
            Console.WriteLine();*/

            for (int j = y1; j < y2; j++)
                for (int i = x1; i < x2; i++)
                    if (label[i, j] != 0) 
                    {
                        int k = ((j * width) + i) * depth;
                        label[i, j] = parents[label[i, j]];
                        tab[k] = tab[k + 1] = tab[k + 2] = (byte)(label[i, j]);
                    }
        }
        int Find (int a)
        {
            if (parents[a] == a) return a;
            int b = Find(parents[a]);
            parents[a] = b;
            return b;
        }
        void Union (int a, int b)
        {
            int pa = Find(a);
            int pb = Find(b);
            parents[pa] = pb;
        }

        class Punkt
        {
            public int v;
            public int x;
            public int y;
            public Punkt(int a, int b,int c) { v = a;x = b;y = c; }
        }
        public Bitmap metodaPix(int minp, int maxp, int h1, int h2, int tr3)
        {
            minPx = minp; maxPx = maxp;
            Image<Gray, byte> outImg;
            Image<Gray, byte> binary = edgesImg.Not().ThresholdBinary(new Gray(100), new Gray(255));
            Image<Gray, float> dist = new Image<Gray, float>(binary.Size); //odległość euklidesowa

            CvInvoke.DistanceTransform(binary, dist, null, DistType.L2, 5);
            //CvInvoke.Normalize(dist, dist, 0, 255, NormType.MinMax); // do pokazywania !!!!
            using (Bitmap newImg = new Bitmap(dist.Convert<Bgr, byte>().Bitmap))
            {
                Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
                BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[data.Width * data.Height * depth];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                Parallel.Invoke(
                    () => { metodaPixWątek(buffer, 0, 0, data.Width, data.Height, data.Width, depth); });
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                newImg.UnlockBits(data);
                outImg = new Image<Gray, byte>(newImg);
            }
            #region wykluczenie obszarów zieleni, i za ciemnych i za mocnych
            using (Image<Hsv, byte> hsv = inImg.Convert<Hsv, byte>())
            {
                Image<Gray, byte>[] channels = hsv.Split();
                channels[1] = channels[1].InRange(new Gray(100), new Gray(255)); //sat
                channels[2] = channels[2].InRange(new Gray(0), new Gray(100));
                linesImg = channels[1].Or(channels[2]).Not();
                CvInvoke.Min(outImg, channels[1].Or(channels[2]).Not(), outImg);
            }
            #endregion

            edgesImg = outImg.Clone();
            endImg = edgesImg.Convert<Bgr, byte>();

            //edgesImg i endImg - zawiera piksele centralne
            #region segmentacja
            foreach (var pkt in centralPixels)
            {
                if (((int)outImg.Data[pkt.y, pkt.x, 0]) != 0)
                   CvInvoke.Circle(endImg, new Point(pkt.x, pkt.y), (int)Math.Floor(dist[new Point(pkt.x,pkt.y)].Intensity), new MCvScalar(255, 255, 255), -1);
            }
            // na obrazie we:
            //using (Bitmap newImg = new Bitmap(inImg.Bitmap))
            using (Bitmap newImg = new Bitmap(endImg.Bitmap))
            {
                Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
                BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[data.Width * data.Height * depth];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                Parallel.Invoke(
                    () => { metodaPixSegmentacja(buffer, 0, 0, data.Width, data.Height, data.Width, depth); });
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                newImg.UnlockBits(data);
                endImg = new Image<Bgr, byte>(newImg);

            }
            //edgesImg = binary.Clone().Not();
            //return endImg.Bitmap;

            // przejście pikseli centralnych i sumowanie ich okręgów i ilości dla tych samych etykiet
            List<Punkt> segmenty = new List<Punkt>(); //v - numer etykiety, x - suma okręgów, y - liczba pikseli
            foreach (var p in centralPixels)
            {
                int etykieta = endImg.Data[p.y, p.x, 0]; //numer etykiety dla piksela
                if (etykieta == 0)//to pole czarne
                {
                    p.v = 0;
                    continue;
                }
                int i = segmenty.FindIndex(c => c.v == etykieta); //czy jest segment o danej etykiecie
                //Console.WriteLine(p.v + " = " + p.x + ", " + p.y+ ", e "+etykieta+" " +i );
                if (i != -1) //jeżeli istnieje o takiej wartości to dodaj piksel
                {
                    segmenty[i].x += p.v;
                    segmenty[i].y++;
                }
                else segmenty.Add(new Punkt(etykieta, p.v, 1));
                p.v = etykieta;
            }
            centralPixels.RemoveAll(c => c.v == 0);
            foreach (var s in segmenty)
            {
                double wyn = (double)s.y * (double)s.y / (double)s.x;
                
                if (wyn < tr3)
                    centralPixels.RemoveAll(c => c.v == s.v);
                else Console.Write(" e " +s.v +", n "+s.y +", sum/n "+s.x/s.y+", w "+ wyn + "\n");
                
            }
            edgesImg.SetZero();
            endImg.SetZero();
            foreach (var p in centralPixels)
            {
                edgesImg.Data[p.y, p.x, 0] = 255;
                endImg.Data[p.y, p.x, 0] = 255;
                endImg.Data[p.y, p.x, 1] = 255;
                endImg.Data[p.y, p.x, 2] = 255;
            }
            #endregion

            //edgesImg ma kropki, usuną te kropki, które są w regionach za małych
            //endImg = inImg.Clone();

            //----------linie--------------
            lines = HoughLineTests(30, h1, h2); 

            ConnectLines(lines, 100);

            BoldLines(lines, dist);
            
            #region szkieletyzacja
            /*binary = endImg.Convert<Gray, byte>();//edgesImg.Clone();
            Image<Gray, Byte> eroded = new Image<Gray, byte>(InImg.Size);
            Image<Gray, Byte> dilated = new Image<Gray, byte>(InImg.Size);
            Image<Gray, Byte> skel = new Image<Gray, byte>(InImg.Size);
            skel.SetValue(0);
            bool done = false;
            while (!done)
            {
                eroded = binary.Erode(1);
                dilated = eroded.Dilate(1);
                dilated = binary.Sub(dilated);
                CvInvoke.BitwiseOr(skel,dilated,skel);
                binary = eroded.Clone();
                if (CvInvoke.CountNonZero(binary) == 0) done = true;
            }
            endImg = skel.Convert<Bgr,byte>();*/
            #endregion

            edgesImg = binary.Clone().Not();
            binary.Dispose(); dist.Dispose(); outImg.Dispose();
            return endImg.Bitmap;
        }

        public double[] metodaPix_Test(int rodzaj, int tr1, int tr2, int minp, int maxp, int h1, int h2, int tr3)
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
            minPx = minp; maxPx = maxp;
            Image<Gray, byte> outImg;
            Image<Gray, byte> binary = edgesImg.Not().ThresholdBinary(new Gray(100), new Gray(255));
            Image<Gray, float> dist = new Image<Gray, float>(binary.Size); //odległość euklidesowa

            CvInvoke.DistanceTransform(binary, dist, null, DistType.L2, 5);
            using (Bitmap newImg = new Bitmap(dist.Convert<Bgr, byte>().Bitmap))
            {
                Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
                BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[data.Width * data.Height * depth];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                Parallel.Invoke(
                    () => { metodaPixWątek(buffer, 0, 0, data.Width, data.Height, data.Width, depth); });
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                newImg.UnlockBits(data);
                outImg = new Image<Gray, byte>(newImg);
            }
            #region wykluczenie obszarów zieleni, i za ciemnych i za mocnych
            using (Image<Hsv, byte> hsv = inImg.Convert<Hsv, byte>())
            {
                Image<Gray, byte>[] channels = hsv.Split();
                channels[1] = channels[1].InRange(new Gray(100), new Gray(255)); //sat
                channels[2] = channels[2].InRange(new Gray(0), new Gray(100));
                linesImg = channels[1].Or(channels[2]).Not();
                CvInvoke.Min(outImg, channels[1].Or(channels[2]).Not(), outImg);
            }
            #endregion

            edgesImg = outImg.Clone();
            endImg = edgesImg.Convert<Bgr, byte>();

            #region segmentacja
            foreach (var pkt in centralPixels)
            {
                if (((int)outImg.Data[pkt.y, pkt.x, 0]) != 0)
                    CvInvoke.Circle(endImg, new Point(pkt.x, pkt.y), (int)Math.Floor(dist[new Point(pkt.x, pkt.y)].Intensity), new MCvScalar(255, 255, 255), -1);
            }

            using (Bitmap newImg = new Bitmap(endImg.Bitmap))
            {
                Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
                BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat);
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[data.Width * data.Height * depth];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                Parallel.Invoke(
                    () => { metodaPixSegmentacja(buffer, 0, 0, data.Width, data.Height, data.Width, depth); });
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                newImg.UnlockBits(data);
                endImg = new Image<Bgr, byte>(newImg);

            }

            // przejście pikseli centralnych i sumowanie ich okręgów i ilości dla tych samych etykiet
            List<Punkt> segmenty = new List<Punkt>(); //v - numer etykiety, x - suma okręgów, y - liczba pikseli
            foreach (var p in centralPixels)
            {
                int etykieta = endImg.Data[p.y, p.x, 0]; //numer etykiety dla piksela
                if (etykieta == 0)//to pole czarne
                {
                    p.v = 0;
                    continue;
                }
                int i = segmenty.FindIndex(c => c.v == etykieta); //czy jest segment o danej etykiecie
                //Console.WriteLine(p.v + " = " + p.x + ", " + p.y+ ", e "+etykieta+" " +i );
                if (i != -1) //jeżeli istnieje o takiej wartości to dodaj piksel
                {
                    segmenty[i].x += p.v;
                    segmenty[i].y++;
                }
                else segmenty.Add(new Punkt(etykieta, p.v, 1));
                p.v = etykieta;
            }
            centralPixels.RemoveAll(c => c.v == 0);
            foreach (var s in segmenty)
            {
                double wyn = (double)s.y * (double)s.y / (double)s.x;

                if (wyn < tr3)
                    centralPixels.RemoveAll(c => c.v == s.v);
                //else Console.Write(" e " + s.v + ", n " + s.y + ", sum/n " + s.x / s.y + ", w " + wyn + "\n");

            }
            edgesImg.SetZero();
            foreach (var p in centralPixels)
                edgesImg.Data[p.y, p.x, 0] = 255;
            
            #endregion

            //----------linie--------------
            lines = HoughLineTests(30, h1, h2);
            ConnectLines(lines, 100);
            BoldLines_Test(lines, dist);
            
            edgesImg = binary.Clone().Not();
            binary.Dispose(); dist.Dispose(); outImg.Dispose();
            return Quality(true);
        }

        // łączenie lini
        public void ConnectLines(LineSegment2D[] linesToConnect, double dmax = 50)
        {
            dmax = dmax * dmax;
            List<LineSegment2D> newLines = new List<LineSegment2D>();
            int n=linesToConnect.Count();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) 
                {
                    if (i == j) continue;
                    LineSegment2D l1 = linesToConnect[i];
                    LineSegment2D l2 = linesToConnect[j];
                    double A1 = l1.P1.Y - l1.P2.Y, B1 = l1.P2.X - l1.P1.X, C1 = l1.P1.X * l1.P2.Y - l1.P1.Y * l1.P2.X;
                    double A2 = l2.P1.Y - l2.P2.Y, B2 = l2.P2.X - l2.P1.X, C2 = l2.P1.X * l2.P2.Y - l2.P1.Y * l2.P2.X;
                    double M1 = A1 * B2 - A2 * B1, M2 = A1 * A2 + B1 * B2;
                    if (M2 == 0) continue;
                    double tan = Math.Abs(M1 / M2);

                    if (tan<0.26)
                    {
                        double d1 = Math.Pow(l1.P1.X - l2.P1.X, 2) + Math.Pow(l1.P1.Y - l2.P1.Y, 2);
                        double d2 = Math.Pow(l1.P2.X - l2.P1.X, 2) + Math.Pow(l1.P2.Y - l2.P1.Y, 2);
                        double d3 = Math.Pow(l1.P1.X - l2.P2.X, 2) + Math.Pow(l1.P1.Y - l2.P2.Y, 2);
                        double d4 = Math.Pow(l1.P2.X - l2.P2.X, 2) + Math.Pow(l1.P2.Y - l2.P2.Y, 2);
                        double d = Math.Min(d1, Math.Min(d2, Math.Min(d3, d4)));
                        if (d>dmax) continue;
                        double dP1, dP2;
                        Point[] pts = { l1.P1, l1.P2, l2.P1, l2.P2 };
                        int pMin1 = 0, pMin2 = 0;
                        if (d == d1) { pMin1 = 0; pMin2 = 2; }
                        else if (d == d2) { pMin1 = 1; pMin2 = 2; }
                        else if (d == d3) { pMin1 = 0; pMin2 = 3; }
                        else if (d == d4) { pMin1 = 1; pMin2 = 3; }
                        Point p1 = pts[pMin1], p2 = pts[pMin2];
                        dP1 = (A1 * p2.X + B1 * p2.Y + C1) / (Math.Sqrt(Math.Pow(A1, 2) + Math.Pow(B1, 2)));
                        dP2 = (A2 * p1.X + B2 * p1.Y + C2) / (Math.Sqrt(Math.Pow(A2, 2) + Math.Pow(B2, 2)));
                        double A3 = p1.Y - p2.Y, B3 = p2.X - p1.X, C3 = p1.X * p2.Y - p1.Y * p2.X;
                        double M31 = A1 * B3 - A3 * B1, M32 = A1 * A3 + B1 * B3;
                            
                        if (M32 == 0 && Math.Max(dP1, dP2) < 10) newLines.Add(new LineSegment2D(p1, p2));
                        double tan3 = Math.Abs(M31 / M32);
                        if (tan3 < 0.17 && Math.Max(dP1, dP2) < 10) newLines.Add(new LineSegment2D(p1, p2));
                        
                    }
                    //Console.WriteLine(""+a1.X+" " +a1.Y+ " ; " + a2.X + " " + a2.Y+"  ;  "+Math.Abs((a1.X-a2.X)/(1+a1.X*a2.X))+"  " + Math.Abs((a1.Y - a2.Y) / (1 + a1.Y * a2.Y)));
                    //Console.WriteLine("" + (-A1/B1) + " " + (-A2/B2)+"  ;  " + tan);
                }

            /*endImg = inImg.Clone();
            int lineWidth = 1;
            foreach (LineSegment2D line in lines)
            {
                endImg.Draw(line, new Bgr(Color.Yellow), 2);
                linesImg.Draw(line, new Gray(255), lineWidth);
            }
            foreach(LineSegment2D line in newLines)
            {
                endImg.Draw(line, new Bgr(Color.Red), 2); 
                linesImg.Draw(line, new Gray(255), lineWidth);
            }*/
            //Console.WriteLine("połączone linie: " + newLines.Count());
            lines = lines.Concat(newLines).ToArray();
            //return endImg.Bitmap;
            
        }
        public void ConnectFillLines(LineSegment2D[] linesToConnect, double dmax = 50)
        {
            dmax = dmax * dmax;
            List<RotatedRect> boxList = new List<RotatedRect>();
            List<VectorOfPoint> polyList = new List<VectorOfPoint>();
            int n = linesToConnect.Count();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    LineSegment2D l1 = linesToConnect[i];
                    LineSegment2D l2 = linesToConnect[j];
                    double A1 = l1.P1.Y - l1.P2.Y, B1 = l1.P2.X - l1.P1.X, C1 = l1.P1.X * l1.P2.Y - l1.P1.Y * l1.P2.X;
                    double A2 = l2.P1.Y - l2.P2.Y, B2 = l2.P2.X - l2.P1.X, C2 = l2.P1.X * l2.P2.Y - l2.P1.Y * l2.P2.X;
                    double M1 = A1 * B2 - A2 * B1, M2 = A1 * A2 + B1 * B2;
                    if (M2 == 0) continue; 
                    double tan = Math.Abs(M1 / M2); //kąt między liniami

                    //double angle = 180 - Math.Abs(l1.GetExteriorAngleDegree(l2));
                    
                    if (tan < 0.26) // kąt między liniami jest mniejszy niż ~14 stopni
                    {
                        double d1 = Math.Pow(l1.P1.X - l2.P1.X, 2) + Math.Pow(l1.P1.Y - l2.P1.Y, 2);
                        double d2 = Math.Pow(l1.P2.X - l2.P1.X, 2) + Math.Pow(l1.P2.Y - l2.P1.Y, 2);
                        double d3 = Math.Pow(l1.P1.X - l2.P2.X, 2) + Math.Pow(l1.P1.Y - l2.P2.Y, 2);
                        double d4 = Math.Pow(l1.P2.X - l2.P2.X, 2) + Math.Pow(l1.P2.Y - l2.P2.Y, 2);
                        double d = Math.Min(d1, Math.Min(d2, Math.Min(d3, d4)));
                        if (d > dmax) continue;
                        PointF[] pts = { l1.P1, l1.P2, l2.P1, l2.P2 };
                        //int pMax1 = 0, pMax2 = 0;
                        
                        /*double dM = Math.Max(d1, Math.Max(d2, Math.Max(d3, d4)));
                        if (dM == d1) { pMax1 = 0; pMax2 = 2; }
                        else if (dM == d2) { pMax1 = 1; pMax2 = 2; }
                        else if (dM == d3) { pMax1 = 0; pMax2 = 3; }
                        else if (dM == d4) { pMax1 = 1; pMax2 = 3; }*/
                        int pMin1 = 0, pMin2 = 0;
                        if (d == d1) { pMin1 = 0; pMin2 = 2; }
                        else if (d == d2) { pMin1 = 1; pMin2 = 2; }
                        else if (d == d3) { pMin1 = 0; pMin2 = 3; }
                        else if (d == d4) { pMin1 = 1; pMin2 = 3; }

                        double dP1, dP2, eps = 3;
                        //Console.WriteLine(" linia1: " +l1.P1 + ", "+l1.P2+ " linia2: "+l2.P1+ ", "+l2.P2+"\n  max: " + d1+ " " + d2 + " " + d3 + " " +d4 + "   "+dM);
                        PointF p1 = pts[pMin1], p2 = pts[pMin2];
                        dP1 = (A1 * p2.X + B1 * p2.Y + C1) / (Math.Sqrt(Math.Pow(A1, 2) + Math.Pow(B1, 2))); //odległość pkt l2... do prostej 1
                        dP2 = (A2 * p1.X + B2 * p1.Y + C2) / (Math.Sqrt(Math.Pow(A2, 2) + Math.Pow(B2, 2))); //odległość pkt l1... do prostej 2
                        double A3 = p1.Y - p2.Y, B3 = p2.X - p1.X, C3 = p1.X * p2.Y - p1.Y * p2.X;
                        double M31 = A1 * B3 - A3 * B1, M32 = A1 * A3 + B1 * B3;
                        double tan3 = Math.Abs(M31 / M32);
                        if (tan3 > 0.26) // to zrób czworobok
                        {
                            RotatedRect rec = CvInvoke.MinAreaRect(pts);
                            double angle1 = Math.Atan2(pts[0].Y - pts[1].Y, pts[0].X - pts[1].X) * 180 / Math.PI;
                            double angle2 = Math.Atan2(pts[2].Y - pts[3].Y, pts[2].X - pts[3].X) * 180 / Math.PI;
                            if (angle1 < 0) angle1 += 360;
                            if (angle1 > 180) angle1 -= 180;
                            if (angle2 < 0) angle2 += 360;
                            if (angle2 > 180) angle2 -= 180;

                            double recAngle = (rec.Angle < 0) ? rec.Angle + 180 : rec.Angle;
                            if (rec.Size.Width < rec.Size.Height)
                                recAngle += 90;
                            if (recAngle > 180) recAngle -= 180;
                            double angleS = (angle1 + angle2) / 2;
                            //Console.WriteLine(" a1 " + angle1+ ", a2 "+angle2 + ", aS " + (angle1+angle2)/2+", rec " + recAngle+ ", war "+ Math.Abs(recAngle - angleS));
                            if (Math.Abs(recAngle - angleS) < eps) 
                                boxList.Add(rec);
                            /*else //wykryty prostokąt jest błędny, nie jest wzdłuż linii
                            {
                                PointF pM1 = pts[pMax1], pM2 = pts[pMax2], pS = new PointF((pM1.X + pM2.X) / 2, (pM1.Y + pM2.Y) / 2);
                                double a = Math.Max(dP1, dP2), c = Math.Sqrt(Math.Pow(pM1.X - pM2.X, 2) + Math.Pow(pM1.X - pM2.X, 2)), b = Math.Sqrt(Math.Pow(c, 2) - Math.Pow(a, 2));
                                double angle = Math.Atan2(pts[0].Y - pts[1].Y, pts[0].X - pts[1].X) * 180 / Math.PI;
                                boxList.Add(new RotatedRect(pS, new SizeF((float)b, (float)a), (float)angle));
                            }*/
                            //Console.WriteLine("d1  P1 " + p1 + "; P2 " + p2 + " | c " + c + ", a " + a + ", b " + b + " | pS " + pS.ToString());
                        }
                        //Console.WriteLine("l1 P1 "+l1.P1.X + ", " + l1.P1.Y + "; P2 "+l1.P2.X + " " + l1.P2.Y +"\nl2 P1 "+ l2.P1.X + ", " + l2.P1.Y + "; P2 " + l2.P2.X + " " + l2.P2.Y + "\n " + d1 + "  " +d2 + "  " +d3 + "  " +d4  + "  -  " + d + " | " + dmin);
                    }
                    //Console.WriteLine(""+a1.X+" " +a1.Y+ " ; " + a2.X + " " + a2.Y+"  ;  "+Math.Abs((a1.X-a2.X)/(1+a1.X*a2.X))+"  " + Math.Abs((a1.Y - a2.Y) / (1 + a1.Y * a2.Y)));
                    //Console.WriteLine("" + (-A1/B1) + " " + (-A2/B2)+"  ;  " + tan);
                }

            //endImg = inImg.Clone();
            //linesImg.SetZero();
            /*foreach (LineSegment2D line in lines)
            {
                endImg.Draw(line, new Bgr(Color.Yellow), lineWidth);
                linesImg.Draw(line, new Gray(255), lineWidth);
            }*/
            //Console.WriteLine("Prostokątów: " + boxList.Count());
            double suma = boxList.Sum(x => (x.Size.Width<x.Size.Height)? x.Size.Width : x.Size.Height);
            suma = suma / boxList.Count(); // średnia szerokość prostokątów
            boxList.RemoveAll(x => ((x.Size.Width < x.Size.Height) ? x.Size.Width : x.Size.Height) > suma);

            foreach (var rec in boxList)
            {
                //endImg.Draw(rec, new Bgr(Color.Red), 1);
                linesImg.Draw(rec, new Gray(255), 0);
                //Console.WriteLine(boxList[0].Center+" " + boxList[0].Angle+" " + boxList[0].Size);
            }
            /*foreach (var p in polyList)
            {
                endImg.DrawPolyline(p.ToArray(), true, new Bgr(Color.Red), 1);
                linesImg.FillConvexPoly(p.ToArray(), new Gray(255));
            }*/
            //return endImg.Bitmap;
        }
        public Bitmap BoldLines(LineSegment2D[] linesBold, Image<Gray, float> dist)
        {
            //Console.WriteLine(" --------------- pogrubianie ----------------- ");
            int n = linesBold.Count();
            int[] lineWidth = new int[n];
            for (int i = 0; i < n; i++)
            {
                LineSegment2D line = linesBold[i];
                double p1 = dist[line.P1].Intensity, p2 = dist[line.P2].Intensity;
                /*if (p1 > roadWidth * 2 || p2 > roadWidth * 2)
                {
                    lineWidth[i] = 0;
                    continue;
                }*/
                lineWidth[i] = (int)((p1+p2)/2);
                //Console.WriteLine(line.P1.X + " " + line.P1.Y + "; " + line.P2.X + " " + line.P2.Y+" ----------- "+lineWidth[i] + " (" + p1+", "+p2+") "+roadWidth);
            }
            
            linesImg.SetZero();
            for (int i = 0; i < n; i++)
            {
                endImg.Draw(linesBold[i], new Bgr(Color.Yellow), lineWidth[i]);
                linesImg.Draw(linesBold[i], new Gray(255), lineWidth[i]);
            }
            return endImg.Bitmap;
        }
        public void BoldLines_Test(LineSegment2D[] linesBold, Image<Gray, float> dist)
        {
            int n = linesBold.Count();
            int[] lineWidth = new int[n];
            for (int i = 0; i < n; i++)
            {
                LineSegment2D line = linesBold[i];
                double p1 = dist[line.P1].Intensity, p2 = dist[line.P2].Intensity;
                lineWidth[i] = (int)((p1 + p2) / 2);
            }
            linesImg.SetZero();
            for (int i = 0; i < n; i++)
            {
                //endImg.Draw(linesBold[i], new Bgr(Color.Yellow), lineWidth[i]);
                linesImg.Draw(linesBold[i], new Gray(255), lineWidth[i]);
            }
        }

        //------ Testy -----------------------------
        public bool trueRoads ()
        {
            return roadsExist;
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
            lines = HoughLineTests(trH, h1, h2);
            ConnectLines(lines);
            linesImg.SetZero();
            foreach (LineSegment2D line in lines)
                linesImg.Draw(line, new Gray(255), 1);
            ConnectFillLines(lines);
            double[] pom = Quality(false);
            return new double[] { pom[0], pom[1], pom[2], lines.GetLength(0) };
        }

        private int popD = -1, popColor = -1, popSpace = -1;
        // filtr bilateralny + krawędzie + Hough
        public double[] Test2(int rodzaj, int d, int color, int space, int tr1, int tr2, int trH, int h1, int h2)
        {
            if (!roadsExist) return new double[] { 0 };
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
            try
            {
                lines = HoughLineTests(trH, h1, h2);
                ConnectLines(lines);
                linesImg.SetZero();
                foreach (LineSegment2D line in lines)
                    linesImg.Draw(line, new Gray(255), 1);
                ConnectFillLines(lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show("");
            }
            
            double[] pom = Quality(false);
            return new double[] { pom[0], pom[1], pom[2], lines.GetLength(0) };
        }

        // filtr koloru + krawędzie + Hough
        public double[] Test3(int minVal, int maxVal, int maxSat, int trH, double h1, double h2)
        {
            if (!roadsExist) return new double[] { 0 };
            ColorFilter2(minVal, maxVal, maxSat);
            lines = HoughLineTests(trH, h1, h2);
            linesImg.SetZero();
            foreach (LineSegment2D line in lines)
                linesImg.Draw(line, new Gray(255), 1);
            double[] pom = Quality(false);
            return new double[] { pom[0], pom[1], pom[2], lines.GetLength(0) };
        }

    }
    
}

/*
            PictureBox pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.Image = refImg.Bitmap;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

            Form form = new Form();
            form.Controls.Add(pictureBox);
            form.ShowDialog();
            
            pictureBox.Image = outImg.Bitmap;
            form = new Form();
            form.Controls.Add(pictureBox);
            form.ShowDialog();
            */
