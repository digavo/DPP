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

namespace DPP
{
    class Obraz
    {
        public Bitmap Img;
        public Bitmap Roads;
        public Bitmap PreImg;
        private Bitmap Edges;
        private Image<Gray, Byte> roads_;
        private Image<Gray, Byte> edges_;

        private double MinVal = 60, MaxVal = 90, MaxSat = 20;
        public void CzytajObraz(Stream str)
        {
            Img = new Bitmap(str);
            PreImg = Img;
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
            //System.Windows.Forms.MessageBox.Show("r:" + r + " g:" + g + " b:" + b + "\nR:" + R + " G:" + G + " B:" + B + "\nH:" + Hue + " V:" + Val + " Sa:" + Sat);
        }
        private void ZnajdzKolor(byte[] tab, int x1, int y1, int x2, int y2, int width, int depth) 
        {
            for (int i = x1; i < x2; i++)
            {
                for (int j = y1; j < y2; j++)
                {
                    int offset = ((j * width) + i) * depth;
                    float hue, sat, val;
                    RGBtoHSV(tab[offset + 2], tab[offset + 1], tab[offset + 0], out hue, out val, out sat);
                    int a = (int)(MaxSat / (100 - MaxVal)), b = (int)(-MaxVal * a);
                    if (val > MinVal && sat < MaxSat && sat > (int)(a * val + b))
                    {
                        tab[offset + 2] = (byte)255;
                        tab[offset + 1] = (byte)255;
                        tab[offset + 0] = (byte)255;
                    }
                    else
                    {
                        tab[offset + 2] = (byte)0;
                        tab[offset + 1] = (byte)0;
                        tab[offset + 0] = (byte)0;
                    }
                }
            }
        }
        

        //------ Podstawowe -----------------------
        public Bitmap BilateralFilter(int d = 15, double sColor = 80, double sSpace = 80)
        {
            Image<Bgr, Byte> img = new Image<Bgr, Byte>(Img);
            UMat outImg = new UMat();
            CvInvoke.BilateralFilter(img, outImg, d, sColor, sSpace);
            PreImg = outImg.Bitmap;
            img.Dispose();
            return outImg.Bitmap;
        }
        public Bitmap ZnajdzKolorWatki(double minVal, double maxVal, double maxSat) 
        {
            MinVal = minVal; MaxVal = maxVal; MaxSat = maxSat;
            Bitmap newImg = new Bitmap(Img);
            Rectangle rect = new Rectangle(0, 0, newImg.Width, newImg.Height);
            BitmapData data = newImg.LockBits(rect, ImageLockMode.ReadOnly, newImg.PixelFormat); 
            int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; 
            byte[] buffer = new byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length); 
            Parallel.Invoke(
                () => { ZnajdzKolor(buffer, 0, 0, data.Width / 2, data.Height / 2, data.Width, depth); }, //top left
                () => { ZnajdzKolor(buffer, data.Width / 2, 0, data.Width, data.Height / 2, data.Width, depth); }, //top - right
                () => { ZnajdzKolor(buffer, 0, data.Height / 2, data.Width / 2, data.Height, data.Width, depth); }, //bottom - left
                () => { ZnajdzKolor(buffer, data.Width / 2, data.Height / 2, data.Width, data.Height, data.Width, depth); }  //bottom - right
            );
            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            newImg.UnlockBits(data);
            PreImg = newImg;
            Edges = newImg;
            edges_ = new Image<Gray, Byte>(Edges);
            return newImg;
        }

        //------ Krawędzie -------------------------
        public Bitmap Sobel(double Threshold)
        {
            Image<Gray, Byte> img = new Image<Gray, Byte>(PreImg);
            Image<Gray, float> edges_x = img.Sobel(1, 0, 3);
            Image<Gray, float> edges_y = img.Sobel(0, 1, 3);
            UMat abs_x = new UMat(), abs_y = new UMat(), grad = new UMat();
            CvInvoke.ConvertScaleAbs(edges_x, abs_x, 1, 0);
            CvInvoke.ConvertScaleAbs(edges_y, abs_y, 1, 0);
            CvInvoke.AddWeighted(abs_x, 0.5, abs_y, 0.5, 0, grad);
            CvInvoke.Threshold(grad, grad, Threshold, 255, ThresholdType.Binary);
            Edges = grad.Bitmap;
            edges_ = new Image<Gray, Byte>(Edges);
            grad.Dispose();
            img.Dispose();
            edges_x.Dispose();
            edges_y.Dispose();
            return Edges;
        }
        public Bitmap Laplacian(double Threshold)
        {
            Image<Gray, Byte> img = new Image<Gray, Byte>(PreImg);
            UMat edges = new UMat();
            CvInvoke.Laplacian(img, edges, DepthType.Default);
            CvInvoke.ConvertScaleAbs(edges, edges, 1, 0);
            CvInvoke.Threshold(edges, edges, 10, 255, ThresholdType.Binary);
            Edges = edges.Bitmap;
            return Edges;
        }
        public Bitmap Canny(double Threshold, double ThresholdLinking)
        {
            Image<Gray, Byte> img = new Image<Gray, Byte>(PreImg);
            UMat edges = new UMat();
            CvInvoke.Canny(img, edges, Threshold, ThresholdLinking,3,true);
            Edges = edges.Bitmap;
            edges_ = new Image<Gray, Byte>(Edges);
            return Edges;
        }

        //------ Linie i prostokąty ----------------
        public Bitmap HoughLine(int threshold, double minLineWidth, double gapSize)
        {
            Image<Gray, Byte> edges = new Image<Gray, Byte>(Edges);
            LineSegment2D[] HLines = CvInvoke.HoughLinesP(
                edges,
                1, //Distance resolution in pixel-related units
                Math.PI / 180.0, //Angle resolution measured in radians. 45
                threshold, //threshold
                minLineWidth, //min Line width
                gapSize); //gap between lines
            edges.Dispose();
            Image<Bgr, Byte> outImage = new Image<Bgr, Byte>(Img).Copy();
            foreach (LineSegment2D line in HLines)
                outImage.Draw(line, new Bgr(Color.Yellow), 2);
            return outImage.ToBitmap();
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
                return null;
            }
        }
        public Bitmap FindContours()
        {
            Image<Gray, Byte> gray= new Image<Gray, Byte>(Img);
            Image<Bgr, Byte> outImage = new Image<Bgr, Byte>(gray.ToBitmap());
            Image<Gray, Byte> edges = new Image<Gray, Byte>(Edges);

            #region rectangles
            List<RotatedRect> boxList = new List<RotatedRect>();
            List<RotatedRect> box2List = new List<RotatedRect>();
            List<Triangle2DF> triangleList = new List<Triangle2DF>();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(edges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple); 
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
            #endregion
            foreach (RotatedRect box in boxList)
                outImage.Draw(box, new Bgr(Color.Red), 2);
            foreach (RotatedRect box in box2List)
                outImage.Draw(box, new Bgr(Color.Blue), 2);

            return outImage.ToBitmap();
        }
        
        //------ Testy -----------------------------
        public void BigerRoads (int it)
        {
            Image<Gray, Byte> gr = new Image<Gray, Byte>(Roads);
            Image<Gray, Byte> outImg_ = new Image<Gray, Byte>(Roads.Size);
            outImg_ = gr.Dilate(it);
            Roads = outImg_.Bitmap;
            roads_ = new Image<Gray, Byte>(Roads);
        }
        
        private int popTr1 = -1, popTr2 = -1;
        // krawędzie + Hough
        public double[] Test1 (int rodzaj, int tr1, int tr2, int trH, int h1, int h2)
        {
            double result = 0;
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
            double result = 0;
            ZnajdzKolorWatki(minVal, maxVal, maxSat);
            
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
            //System.Windows.Forms.MessageBox.Show("\n"+minVal+" "+maxVal+" " +maxSat+" " + trH + " " +h1 + " " + h2+"\n"+whitePxs +" " +whitePxs2 +"\n"+(whitePxs-whitePxs2)*100+" / " + linePxs+" = " + result+"\n" + ((whitePxs - whitePxs2)*100) / linePxs);

            /*System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            System.Windows.Forms.PictureBox pictureBox = new System.Windows.Forms.PictureBox();
            pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBox.Image = roads_.Bitmap;
            pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            form.Controls.Add(pictureBox);
            form.ShowDialog();*/

            gray.Dispose();
            white.Dispose();
            #endregion
            return new double[] { result, lines.GetLength(0) };
            //return gray.Bitmap;
        }

    }
}
/*
 * Image<Gray, Byte> edges = new Image<Gray, Byte>(Edges);
           VectorOfPointF lines = new VectorOfPointF();
           CvInvoke.HoughLines(
               edges,
               lines,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               threshold); //threshold
           var linesList = new List<LineSegment2D>();
           for (var i = 0; i < lines.Size; i++)
           {
               var rho = lines[i].X;
               var theta = lines[i].Y;
               var pt1 = new Point();
               var pt2 = new Point();
               var a = Math.Cos(theta);
               var b = Math.Sin(theta);
               var x0 = a * rho;
               var y0 = b * rho;
               pt1.X = (int)Math.Round(x0 + edges.Width * (-b));
               pt1.Y = (int)Math.Round(y0 + edges.Height * (a));
               pt2.X = (int)Math.Round(x0 - edges.Width * (-b));
               pt2.Y = (int)Math.Round(y0 - edges.Height * (a));

               linesList.Add(new LineSegment2D(pt1, pt2));
           }

           Image<Bgr, Byte> outImage = new Image<Bgr, Byte>(Img).Copy();
           foreach (LineSegment2D line in linesList)
               outImage.Draw(line, new Bgr(Color.Yellow), 2);
           return outImage.ToBitmap();
           */
