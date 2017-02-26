﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Saraff.Tiff;
using Saraff.Tiff.Core;
using System.Collections.ObjectModel;
using BitMiracle.LibTiff.Classic;

namespace DPP
{
    public partial class MainForm : Form
    {
        private string fileName = "";
        private Obraz obrazek;
        private Stream plik;
        public MainForm()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }
        private void buttonRead_Click(object sender, EventArgs e)
        {
            OpenFileDialog oknoWyboruPliku = new OpenFileDialog();
            oknoWyboruPliku.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            oknoWyboruPliku.Filter = "Wszystkie obrazy|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff|"
                                   + "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff";
            oknoWyboruPliku.Title = "Wczytaj obraz";
            oknoWyboruPliku.RestoreDirectory = true;
            if (oknoWyboruPliku.ShowDialog() == DialogResult.OK)
            {
                if (plik != null) plik.Close();
                fileName = oknoWyboruPliku.FileName;
                #region czytanie tiff
                if (fileName.ToLower().Contains("tif") || fileName.ToLower().Contains("tiff"))
                {
                    using (Tiff tiff = Tiff.Open(fileName, "r"))
                    {
                        int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                        int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                        byte[] scanline = new byte[tiff.ScanlineSize()];
                        ushort[] scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
                        //...
                    }
                    return;
                }
                #endregion
                obrazek = new Obraz();
                pictureBox0.Image = null;
                plik = File.Open(fileName, FileMode.Open);
                obrazek.CzytajObraz(plik);
                pictureBox0.Image = obrazek.Img;
                plik.Close();
            }
        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog oknoZapisuPliku = new SaveFileDialog();
            oknoZapisuPliku.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            oknoZapisuPliku.Filter = "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png";
            oknoZapisuPliku.Title = "Zapisz obraz";
            oknoZapisuPliku.FileName = "";
            oknoZapisuPliku.RestoreDirectory = true;
            if (oknoZapisuPliku.ShowDialog() == DialogResult.OK)
                try
                {
                    switch (oknoZapisuPliku.FilterIndex)
                    {
                        case 1:
                            pictureBox1.Image.Save(oknoZapisuPliku.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                            break;
                        case 2:
                            pictureBox1.Image.Save(oknoZapisuPliku.FileName, System.Drawing.Imaging.ImageFormat.Gif);
                            break;
                        case 3:
                            pictureBox1.Image.Save(oknoZapisuPliku.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;
                        case 4:
                            pictureBox1.Image.Save(oknoZapisuPliku.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            break;
                    }
                }
                catch { MessageBox.Show("Błąd zapisu"); }
        }
        private void buttonOrig_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.Img;
                obrazek.PreImg = obrazek.Img;
            }
            catch (Exception ex) { MessageBox.Show("Błąd - wczytaj obrazek"); }
        }
        
        // Filtr
        private void buttonFilter1_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.BilateralFilter((int)numericUpDown6.Value, (int)numericUpDown7.Value, (int)numericUpDown8.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonFilter2_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.ZnajdzKolorWatki((double)numericUpDown10.Value, (double)numericUpDown11.Value, (double)numericUpDown9.Value);

            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonFilter3_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.ZnajdzKolorWatki2((double)numericUpDown10.Value, (double)numericUpDown11.Value, (double)numericUpDown9.Value);

            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
       
        // Krawędzie----------------------------------
        private void buttonSobel_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.Sobel((double)numericUpDown1.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonLapl_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.Laplacian((double)numericUpDown1.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonCanny_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.Canny((double)numericUpDown1.Value, (double)numericUpDown2.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        // Inne -------------------------------------
        private void buttonHough_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.HoughLine((int)numericUpDown3.Value, (double)numericUpDown4.Value, (double)numericUpDown5.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonContours_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.FindContours();
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        
        // Filtr krawędzi + Hough
        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                string scr = fileName.Insert(fileName.LastIndexOf('.'), "_r");
                plik = File.Open(scr, FileMode.Open);
                obrazek.Roads = new Bitmap(plik);
                obrazek.BigerRoads(5);
            }
            catch (Exception ex) { MessageBox.Show("Brak obrazka lub wzorca"); return; }
            
            int index = comboBox1.SelectedIndex;
            int tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { tr1, tr2, h1, h2, h3 };
            string fileName1 = "test1_1.txt", fileName2 = "test1_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, "Sobel + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName1, "tr1;  h1;  h2;  h3; delta; linie;" + Environment.NewLine);
                for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                    for (h1 = 30; h1 <= 150; h1 += 30) //5
                        for (h2 = 10; h2 <= 70; h2 += 15) //5
                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                
                                double[] pom = obrazek.Test1(index, tr1, tr2, h1, h2, h3);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { tr1, tr2, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}",
                                    tr1, h1, h2, h3, pom[0], pom[1]) + Environment.NewLine);
                            }
            }
            else
            {
                File.AppendAllText(fileName2, "Canny + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName2, "tr1; tr2; h1; h2; h3; | delta; linie;" + Environment.NewLine);
                for (tr1 = 150; tr1 <= 250; tr1 += 25) //5
                    for (tr2 = 100; tr2 <= tr1; tr2 += 25)
                        for (h1 = 30; h1 <= 150; h1 += 30) //5
                            for (h2 = 10; h2 <= 70; h2 += 15) //5
                                for (h3 = 0; h3 <= 8; h3 += 2) //5
                                {
                                    double[] pom = obrazek.Test1(index, tr1, tr2, h1, h2, h3);
                                    if (pom[0] > delta[0])
                                    {
                                        delta = pom;
                                        param = new int[] { tr1, tr2, h1, h2, h3 };
                                    }
                                    File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}",
                                    tr1, tr2, h1, h2, h3, pom[0], pom[1]) + Environment.NewLine);
                                }
            }
            numericUpDown1.Value = param[0];
            numericUpDown2.Value = param[1];
            numericUpDown3.Value = param[2];
            numericUpDown4.Value = param[3];
            numericUpDown5.Value = param[4];
            if (index == 0) buttonSobel_Click(sender, e);
            else buttonCanny_Click(sender, e);
            buttonHough_Click(sender, e);
        }

        // Filtr bilateralny + Filtr krawędzi + Hough
        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                string scr = fileName.Insert(fileName.LastIndexOf('.'), "_r");
                plik = File.Open(scr, FileMode.Open);
                obrazek.Roads = new Bitmap(plik);
                obrazek.BigerRoads(5);
            }
            catch (Exception ex) { MessageBox.Show("Brak obrazka lub wzorca"); return; }
            
            int index = comboBox1.SelectedIndex;
            int f1 = 15, f2 = 80, f3 = 80, tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { f1, f2, f3, tr1, tr2, h1, h2, h3 };
            string fileName1 = "test2_1.txt", fileName2 = "test2_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, "filtr b + Sobel + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName1, "f1; f2; f3; tr1;  h1;  h2;  h3; delta; linie;" + Environment.NewLine);
                for (f1 = 5; f1 <= 35; f1 += 10) //4
                    for (f2 = 40; f2 <= 120; f2 += 40) //3
                        for (f3 = 40; f3 <= 120; f3 += 40) //3
                            for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                                for (h1 = 30; h1 <= 150; h1 += 30) //5
                                    for (h2 = 10; h2 <= 70; h2 += 15) //5
                                        for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                double[] pom = obrazek.Test2(index, f1, f2, f3,tr1, tr2, h1, h2, h3);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}",
                                    f1, f2, f3, tr1, h1, h2, h3, pom[0], pom[1]) + Environment.NewLine);
                            }
            }
            else
            {
                File.AppendAllText(fileName2, "filtr b + Canny + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName2, "f1; f2; f3; tr1; tr2; h1; h2; h3; | delta; linie;" + Environment.NewLine);
                for (f1 = 5; f1 <= 35; f1 += 10) //4
                    for (f2 = 40; f2 <= 120; f2 += 40) //3
                        for (f3 = 40; f3 <= 120; f3 += 40) //3
                            for (tr1 = 150; tr1 <= 250; tr1 += 25) //5
                                for (tr2 = 100; tr2 <= tr1; tr2 += 25)
                                    for (h1 = 30; h1 <= 150; h1 += 30) //5
                                        for (h2 = 10; h2 <= 70; h2 += 15) //5
                                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                                {
                                    double[] pom = obrazek.Test1(index, tr1, tr2, h1, h2, h3);
                                    if (pom[0] > delta[0])
                                    {
                                        delta = pom;
                                        param = new int[] { f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                    }
                                    File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}",
                                    f1, f2, f3, tr1, tr2, h1, h2, h3, pom[0], pom[1]) + Environment.NewLine);
                                }
            }
            numericUpDown1.Value = param[3];
            numericUpDown2.Value = param[4];
            numericUpDown3.Value = param[5];
            numericUpDown4.Value = param[6];
            numericUpDown5.Value = param[7];
            numericUpDown6.Value = param[0];
            numericUpDown7.Value = param[1];
            numericUpDown8.Value = param[2];
            buttonFilter1_Click(sender, e);
            if (index == 0) buttonSobel_Click(sender, e);
            else buttonCanny_Click(sender, e);
            buttonHough_Click(sender, e);
            
        }

        // Filtr koloru + Hough
        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                string scr = this.fileName.Insert(this.fileName.LastIndexOf('.'), "_r");
                plik = File.Open(scr, FileMode.Open);
                obrazek.Roads = new Bitmap(plik);
                obrazek.BigerRoads(5);
            }
            catch (Exception ex) { MessageBox.Show("Brak obrazka lub wzorca"); return; }
            int c1 = 60, c2 = 90, c3 = 15, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { c1, c3 , h1, h2, h3 };
            string fileName = "test3.txt";
            
            File.AppendAllText(fileName, "kolor + Hough ---------------" + Environment.NewLine);
            File.AppendAllText(fileName, "c1; c3;  h1;  h2;  h3; com; cor; q; linie;" + Environment.NewLine);
            h1 = 20;
            for (c1 = 60; c1 <= 70; c1 += 5) //3
                for (c3 = 15; c3 <= 25; c3 += 5) //3
                    //for (h1 = 30; h1 <= 150; h1 += 30) //5
                        for (h2 = 20; h2 <= 80; h2 += 15) //5
                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                //pictureBox1.Image = obrazek.Test3(c1, c2, c3, h1, h2, h3);
                                double[] pom = obrazek.Test3(c1, c2, c3, h1, h2, h3);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { c1, c3, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}",
                                    c1, c3, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                            }
            numericUpDown10.Value = param[0];
            numericUpDown9.Value = param[1];
            numericUpDown3.Value = param[2];
            numericUpDown4.Value = param[3];
            numericUpDown5.Value = param[4];
            buttonHough_Click(sender, e);
        }

        // metoda pikseli centralnych
        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = obrazek.metod1();
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

    }
}
