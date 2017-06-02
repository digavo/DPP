using System;
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
using System.Collections.ObjectModel;

namespace DPP
{
    public partial class MainForm : Form
    {
        private string nazwaPliku = "";
        private Obraz ObrazSat;
        private int licznik = 0;
        private bool czyMetoda = false;
        public MainForm()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
            tabControl1.Enabled = false;
            buttonSave.Enabled = buttonOrig.Enabled = button13.Enabled = button14.Enabled = button15.Enabled = button16.Enabled  = false;
            richTextBox1.Text = String.Format("  Com: Cor: Quality:\n");
        }
        // Menu
        private void buttonRead_Click(object sender, EventArgs e)
        {
            OpenFileDialog oknoWyboruPliku = new OpenFileDialog();
            oknoWyboruPliku.InitialDirectory = @"C:\Users\diga.vo\Source\Repos\DPP\DPP\bin\Img+R"; //System.IO.Directory.GetCurrentDirectory();
            oknoWyboruPliku.Filter = "Wszystkie obrazy|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff|"
                                   + "PNG|*.png|BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|TIFF|*.tif;*.tiff";
            oknoWyboruPliku.Title = "Wczytaj obraz";
            oknoWyboruPliku.RestoreDirectory = true;
            if (oknoWyboruPliku.ShowDialog() == DialogResult.OK)
            {
                nazwaPliku = oknoWyboruPliku.FileName;
                ObrazSat = new Obraz(nazwaPliku);
                /*if (fileName.ToLower().Contains("tif") || fileName.ToLower().Contains("tiff"))
                    if (!ObrazSat.ReadTiff(fileName))
                    {
                        MessageBox.Show("Błąd wczytania pliku, spróbuj ponownie");
                        tabControl1.Enabled = false;
                        buttonSave.Enabled = buttonOrig.Enabled = button13.Enabled = button14.Enabled = button15.Enabled = button16.Enabled = false;
                        return;
                    }*/
                pictureBox0.Image = ObrazSat.InImg;
                pictureBox1.Image = null;
                tabControl1.Enabled = true;
                buttonSave.Enabled = buttonOrig.Enabled = button13.Enabled = button14.Enabled = button15.Enabled = button16.Enabled =  true;
                if (!ObrazSat.trueRoads()) button17.Enabled = false;
                else button17.Enabled = true;
                
            }
        }
        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog oknoZapisuPliku = new SaveFileDialog();
            oknoZapisuPliku.InitialDirectory = @"C:\Users\diga.vo\Documents\2. Studia\MGR\Rysunki";//System.IO.Directory.GetCurrentDirectory();
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
                pictureBox1.Image = ObrazSat.InImg;
                ObrazSat.Reset();
            }
            catch (Exception ex) { MessageBox.Show("Błąd - wczytaj obrazek"); }
        }

        // Menu podglądu
        private void button13_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ObrazSat.FilterImg;
        }
        private void button14_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ObrazSat.EdgesImg;
        }
        private void button15_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ObrazSat.EndImg;
        }
        private void button16_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ObrazSat.Roads;
        }
        private void button19_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ObrazSat.LinesImg;
        }

        // Filtr / pre-processing
        private void buttonFilter1_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.BilateralFilter((int)numericUpDown6.Value, (int)numericUpDown7.Value, (int)numericUpDown8.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonFilter3_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.ColorFilter2((int)numericUpDown10.Value, (int)numericUpDown11.Value, (int)numericUpDown9.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonFilter2_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.ColorFilter((int)numericUpDown10.Value, (int)numericUpDown11.Value, (int)numericUpDown9.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        private void buttonFilter4_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.MedianFilter();
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        // Krawędzie----------------------------------
        private void buttonSobel_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.Sobel((double)numericUpDown1.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonLapl_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.Laplacian((double)numericUpDown1.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonCanny_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.Canny((double)numericUpDown1.Value, (double)numericUpDown2.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        // Inne -------------------------------------
        private void buttonHough_Click(object sender, EventArgs e)
        {
            czyMetoda = false;
            try
            {
                pictureBox1.Image = ObrazSat.HoughLine((int)numericUpDown3.Value, (double)numericUpDown4.Value, (double)numericUpDown5.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }
        private void buttonContours_Click(object sender, EventArgs e)
        {
            czyMetoda = false;
            try
            {
                pictureBox1.Image = ObrazSat.FindContours();
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        // Post processing
        private void buttonFilter5_Click(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = ObrazSat.PostColor();
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        // Jakość
        private void button17_Click(object sender, EventArgs e)
        {
            var wynik = ObrazSat.Quality(czyMetoda);
            licznik++;
            richTextBox1.Text += String.Format("{0}:   {1:F}     {2:F}     {3:F} \n", licznik, wynik[0], wynik[1], wynik[2]);
        }

        // metoda pikseli centralnych
        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                czyMetoda = true;   
                pictureBox1.Image = ObrazSat.metodaPix((int)numericUpDown13.Value, (int)numericUpDown12.Value, (int)numericUpDown14.Value, (int)numericUpDown15.Value,(int)numericUpDown16.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        //sat_kam2 - 7/10; sat_kam3 - 13/18; sat_hro1b - ; sat_hro2 - 

        // Filtr krawędzi + Hough
        private void button10_Click(object sender, EventArgs e)
        {
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            
            int index = comboBox1.SelectedIndex;
            int tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] pCom = { -1, -1, -1, tr1, tr2, h1, h2, h3 };
            double[] pCor = { -1, -1, -1, tr1, tr2, h1, h2, h3 };
            double[] pQ = { -1, -1, -1, tr1, tr2, h1, h2, h3 };
            double[] p2 = { -1, -1, -1, tr1, tr2, h1, h2, h3 };
            string fileName1 = "test1_1.txt", fileName2 = "test1_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, Environment.NewLine + "Sobel + Hough | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName1, "com; cor; q; tr1; ---; h1; h2; h3;" + Environment.NewLine);
                tr2 = 0;
                int count = 0; h1 = 30;

                for (tr1 = 75; tr1 <= 200; tr1 += 25) //6
                   for (h1 = 20; h1 <= 25; h1 += 5) //3
                    for (h2 = 25; h2 <= 100; h2 += 15) //6
                        for (h3 = 2; h3 <= 8; h3 += 2) //4
                        {
                            count++;
                            Console.WriteLine(" Numer: " + count);
                            bool notOk = true;
                            while (notOk)
                            {
                                try
                                {
                                    double[] pom = ObrazSat.Test1(index, tr1, tr2, h1, h2, h3);
                                    if (pom[0] > pCom[0])
                                        pCom = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                    if (pom[1] > pCor[1])
                                        pCor = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                    if (pom[2] > pQ[2])
                                        pQ = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                    if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                        p2 = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                    notOk = false;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                    //MessageBox.Show(ex.ToString());
                                    notOk = true;
                                }
                            }
                            //File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}",
                            //   f1, f2, f3, tr1, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                        }
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7]) + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(fileName2, Environment.NewLine + "Canny + Hough | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName2, "com; cor; q; tr1; tr2; h1; h2; h3;" + Environment.NewLine);
                tr2 = 0;
                int count = 0; h1 = 30;

                for (tr1 = 100; tr1 <= 250; tr1 += 25) //9
                    for (tr2 = 100; tr2 <= tr1; tr2 += 25) //
                        for (h1 = 20; h1 <= 25; h1 += 5) //3
                            for (h2 = 25; h2 <= 100; h2 += 15) //6
                                for (h3 = 2; h3 <= 8; h3 += 2) //4
                                {
                                    count++;
                                    Console.WriteLine(" Numer: " + count);
                                    bool notOk = true;
                                    while (notOk)
                                    {
                                        try
                                        {
                                            double[] pom = ObrazSat.Test1(index, tr1, tr2, h1, h2, h3);
                                            if (pom[0] > pCom[0])
                                                pCom = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                            if (pom[1] > pCor[1])
                                                pCor = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                            if (pom[2] > pQ[2])
                                                pQ = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                            if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                                p2 = new double[] { pom[0], pom[1], pom[2], tr1, tr2, h1, h2, h3 };
                                            notOk = false;
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                            //MessageBox.Show(ex.ToString());
                                            notOk = true;
                                        }
                                    }
                                    //File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}",
                                    //   f1, f2, f3, tr1, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                                }
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7]) + Environment.NewLine);
            }
            /*numericUpDown1.Value = param[0];
            numericUpDown2.Value = param[1];
            numericUpDown3.Value = param[2];
            numericUpDown4.Value = param[3];
            numericUpDown5.Value = param[4];
            if (index == 0) buttonSobel_Click(sender, e);
            else buttonCanny_Click(sender, e);
            buttonHough_Click(sender, e);*/

            MessageBox.Show("KONIEC " + wynik);
        }

        // Filtr bilateralny + Filtr krawędzi + Hough
        private void button9_Click(object sender, EventArgs e)
        {
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            
            int index = comboBox1.SelectedIndex;
            int f1 = 15, f2 = 80, f3 = 80, tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] pCom = { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            double[] pCor = { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            double[] pQ = { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            double[] p2 = { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            string fileName1 = "test2_1.txt", fileName2 = "test2_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";
            if (index == 0) //Sobel
            {
                tr2 = 0;
                File.AppendAllText(fileName1, Environment.NewLine + "filtr b + Sobel + Hough | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName1, "com; cor; q; f1; f2; f3; tr1; ---; h1;  h2;  h3;" + Environment.NewLine);
                int count = 0; h1 = 30;
                //for (f1 = 5; f1 <= 25; f1 += 5) //5 //dokładne g1 + ZMIEN FILENAME
                //    for (f2 = 60; f2 <= 100; f2 += 10) //5
                //        for (f3 = 60; f3 <= 100; f3 += 10) //5
                 //           for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                 //               //for (h1 = 30; h1 <= 150; h1 += 30) //5
                 //                   for (h2 = 60; h2 <= 90; h2 += 10) //4 // dla hro1 do 80 - 120
                 //                       for (h3 = 7; h3 <= 10; h3 += 1) //4
                
                for (f1 = 10; f1 <= 20; f1 += 5) //3  //ogólne + ZMIEN FILENAME
                    for (f2 = 70; f2 <= 90; f2 += 10) //3
                        for (f3 = 70; f3 <= 90; f3 += 10) //3
                            for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                                //for (h1 = 20; h1 <= 25; h1 += 5) //3
                                    for (h2 = 25; h2 <= 100; h2 += 15) //6
                                        for (h3 = 2; h3 <= 8; h3 += 2) //4
                                        {
                                            count++;
                                            Console.WriteLine(" Numer: "+count);
                                            bool notOk = true;
                                            while (notOk)
                                            {
                                                try
                                                {
                                                    double[] pom = ObrazSat.Test2(index, f1, f2, f3, tr1, tr2, h1, h2, h3);
                                                    /*if (pom[0] > pCom[0])
                                                        pCom = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                    if (pom[1] > pCor[1])
                                                        pCor = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                    if (pom[2] > pQ[2])
                                                        pQ = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                    if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                                        p2 = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };*/
                                                    File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9};",
                                                       pom[0], pom[1], pom[2], f1, f2, f3, tr1, h1, h2, h3) + Environment.NewLine);
                                                    notOk = false;
                                                }
                                                catch (Exception ex)
                                                { 
                                                    Console.WriteLine(" - Numer: "+count + " - " + ex.ToString());
                                                   // MessageBox.Show(ex.ToString());
                                                    notOk = true;
                                                }
                                            }
                                            
                                        }
                /*File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7], pCom[8], pCom[9], pCom[10]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7], pCor[8], pCor[9], pCor[10]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7], pQ[8], pQ[9], pQ[10]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7], p2[8], p2[9], p2[10]) + Environment.NewLine);*/

            }
            /*pCom = new double[] { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            pCor = new double[] { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };
            pQ = new double[] { -1, -1, -1, f1, f2, f3, tr1, tr2, h1, h2, h3 };*/
            else
            {
                File.AppendAllText(fileName2, Environment.NewLine + "filtr b + Canny + Hough | " +nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName2, "com; cor; q; f1; f2; f3; tr1; tr2; h1; h2; h3;" + Environment.NewLine);
                /*for (f1 = 5; f1 <= 35; f1 += 10) //4
                    for (f2 = 40; f2 <= 120; f2 += 40) //3
                        for (f3 = 40; f3 <= 120; f3 += 40) //3
                            for (tr1 = 150; tr1 <= 250; tr1 += 25) //5
                                for (tr2 = 100; tr2 <= tr1; tr2 += 25)
                                    for (h1 = 30; h1 <= 150; h1 += 30) //5
                                        for (h2 = 10; h2 <= 70; h2 += 15) //5
                                            for (h3 = 0; h3 <= 8; h3 += 2) //5*/
                int[] tr = { 100, 150, 150, 150, 150, 180, 180, 200, 200, 200 };
                int count = 0; h1 = 30;
                for (f1 = 10; f1 <= 20; f1 += 5) //3
                    for (f2 = 70; f2 <= 90; f2 += 10) //3
                        for (f3 = 70; f3 <= 90; f3 += 10) //3
                            for (int i = 0; i <= 8; i += 2) //5
                            {
                                tr1 = tr[i]; tr2 = tr[i + 1];
                                //for (h1 = 15; h1 <= 25; h1 += 5) //3
                                for (h2 = 25; h2 <= 100; h2 += 15) //6
                                    for (h3 = 2; h3 <= 8; h3 += 2) //4
                                    {
                                        count++;
                                        Console.WriteLine(" Numer: " + count);
                                        bool notOk = true;
                                        while (notOk)
                                        {
                                            try
                                            {
                                                double[] pom = ObrazSat.Test2(index, f1, f2, f3, tr1, tr2, h1, h2, h3);
                                                if (pom[0] > pCom[0])
                                                    pCom = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                if (pom[1] > pCor[1])
                                                    pCor = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                if (pom[2] > pQ[2])
                                                    pQ = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                                    p2 = new double[] { pom[0], pom[1], pom[2], f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                                //File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}; {11}",
                                                //f1, f2, f3, tr1, tr2, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                                                notOk = false;
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                               // MessageBox.Show(ex.ToString());
                                                notOk = true;
                                            }
                                        }
                                    }
                            }
                Console.WriteLine(" ------ koniec ");
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7], pCom[8], pCom[9], pCom[10]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7], pCor[8], pCor[9], pCor[10]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7], pQ[8], pQ[9], pQ[10]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7], p2[8], p2[9], p2[10]) + Environment.NewLine);
            }

            MessageBox.Show("KONIEC " + wynik);
            /*numericUpDown1.Value = param[3];
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
            buttonHough_Click(sender, e);*/
        }
        
        // metoda pikseli centralnych
        private void button8_Click(object sender, EventArgs e)
        {
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            #region DOKŁADNE
            /*int index = comboBox1.SelectedIndex;
            int tr1 = 180, tr2 = 100, h1 = 30, h2 = 0, Pmin = 0, Pmax = 100, tr3 = 60;
            string fileName1 = "testDok3_1.txt", fileName2 = "testDok3_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            ObrazSat.MedianFilter_Test();

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, Environment.NewLine + "Sobel + piksele | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName1, "com; cor; q; tr1; Pmin; Pmax; h1; h2;" + Environment.NewLine);
                tr2 = 0;
                int count = 0;
                for (tr1 = 30; tr1 <= 110; tr1 += 20) //5
                    for (Pmin = 1; Pmin < 4; Pmin++) //3
                        for (tr3 = 10; tr3 <= 120; tr3 += 20) //7
                            for (h1 = 10; h1 <= 50; h1 += 10) //5
                                for (h2 = 2; h2 <= 10; h2 += 2) //4
                                {
                                    count++;
                                    Console.WriteLine(" Numer: " + count);
                                    bool notOk = true;
                                    while (notOk)
                                    {
                                        try
                                        {
                                            double[] pom = ObrazSat.metodaPix_Test(index, tr1, tr2, Pmin, Pmax, h1, h2, tr3);
                                            notOk = false;
                                            File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                                            pom[0], pom[1], pom[2], tr1, Pmin, h1, h2, tr3) + Environment.NewLine);

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                            MessageBox.Show(ex.ToString());
                                            notOk = true;
                                        }
                                    }
                                }
            }
            else
            {
                File.AppendAllText(fileName2, Environment.NewLine + "Canny + piksele | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName2, "com; cor; q; tr1; tr2; Pmin; h1; h2; tr3;" + Environment.NewLine);
                tr2 = 0;
                int count = 0;
                int[] tr = { 50, 50, 50, 100, 50, 150 };
                for (int i = 0; i <= 4; i += 2) //3
                {
                    tr1 = tr[i]; tr2 = tr[i + 1];
                    for (Pmin = 0; Pmin <= 4; Pmin++)//4 // google1 /2 - z 4
                        for (tr3 = 40; tr3 <= 60; tr3 += 5) //5
                            for (h1 = 5; h1 <= 30; h1 += 5) //6
                               for (h2 = 4; h2 <= 14; h2 += 2) //6
                    //for (Pmin = 0; Pmin <= 4; Pmin++)//5 // hro1
                    //    for (tr3 = 80; tr3 <= 120; tr3 += 10) //5
                     //       for (h1 = 5; h1 <= 30; h1 += 5) //6
                     //           for (h2 = 4; h2 <= 14; h2 += 2) //6
                                {
                                    count++;
                                    Console.WriteLine(" Numer: " + count);
                                    bool notOk = true;
                                    while (notOk)
                                    {
                                        try
                                        {
                                            double[] pom = ObrazSat.metodaPix_Test(index, tr1, tr2, Pmin, Pmax, h1, h2, tr3);
                                            notOk = false;
                                            File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                                            pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3) + Environment.NewLine);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                            MessageBox.Show(ex.ToString());
                                            notOk = true;
                                        }
                                    }
                                }
                }
            }*/
            #endregion

            #region OGÓLNE
            
            int index = comboBox1.SelectedIndex;
            int tr1 = 180, tr2 = 100, h1 = 30, h2 = 0, Pmin = 0, Pmax = 50, tr3=60;
            double[] pCom = { -1, -1, -1, tr1, tr2, Pmin, Pmax, h1, h2};
            double[] pCor = { -1, -1, -1, tr1, tr2, Pmin, Pmax, h1, h2};
            double[] pQ = { -1, -1, -1, tr1, tr2, Pmin, Pmax, h1, h2};
            double[] p2 = { -1, -1, -1, tr1, tr2, Pmin, Pmax, h1, h2,};
            string fileName1 = "test3_1.txt", fileName2 = "test3_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            ObrazSat.MedianFilter_Test();

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, Environment.NewLine+"Sobel + piksele | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName1, "com; cor; q; tr1; ---; Pmin; Pmax; h1; h2; tr3" + Environment.NewLine);
                tr2 = 0;
                int count = 0;
                for (tr1 = 30; tr1 <= 110; tr1 += 20) //5
                    for (Pmin = 1; Pmin < 4; Pmin++) //3
                        for (tr3 = 10; tr3 <= 120; tr3 += 20) //7
                            for (h1 = 10; h1 <= 50; h1 += 10) //5
                                for (h2 = 2; h2 <= 10; h2 += 2) //4
                                {
                                    count++;
                                    Console.WriteLine(" Numer: " + count);
                                    bool notOk = true;
                                    while (notOk)
                                    {
                                        try
                                        {
                                            double[] pom = ObrazSat.metodaPix_Test(index, tr1, tr2, Pmin, Pmax, h1, h2, tr3);
                                            if (pom[0] > pCom[0])
                                                pCom = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[1] > pCor[1])
                                                pCor = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[2] > pQ[2])
                                                pQ = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                                p2 = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            notOk = false;
                                            //System.GC.Collect();

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                            //MessageBox.Show(ex.ToString());
                                            notOk = true;
                                        }
                                    }
                            }
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7], pCom[8]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7], pCor[8]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7], pQ[8]) + Environment.NewLine);
                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7], p2[8]) + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(fileName2, Environment.NewLine+ "Canny + piksele | " + nazwaPliku + Environment.NewLine);
                File.AppendAllText(fileName2, "com; cor; q; tr1; tr2; Pmin; Pmax; h1; h2; tr3" + Environment.NewLine);
                tr2 = 0;
                int count = 0;
                int[] tr = { 150, 150, 100, 150, 100, 100, 50, 100 };
                for (int i = 0; i <= 6; i += 2) //4
                {
                    tr1 = tr[i]; tr2 = tr[i + 1];
                    for (Pmin = 1; Pmin < 4; Pmin++)//3
                        for (tr3 = 10; tr3 <= 120; tr3 += 20) //7
                            for (h1 = 10; h1 <= 50; h1 += 10) //5
                                for (h2 = 2; h2 <= 10; h2 += 2) //4
                                {
                                    count++;
                                    Console.WriteLine(" Numer: " + count);
                                    bool notOk = true;
                                    while (notOk)
                                    {
                                        try
                                        {
                                            double[] pom = ObrazSat.metodaPix_Test(index, tr1, tr2, Pmin, Pmax, h1, h2, tr3);
                                            if (pom[0] > pCom[0])
                                                pCom = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[1] > pCor[1])
                                                pCor = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[2] > pQ[2])
                                                pQ = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            if (pom[0] >= p2[0] && pom[1] >= p2[1])
                                                p2 = new double[] { pom[0], pom[1], pom[2], tr1, tr2, Pmin, h1, h2, tr3 };
                                            notOk = false;
                                            //System.GC.Collect();
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(" - Numer: " + count + " - " + ex.ToString());
                                            //MessageBox.Show(ex.ToString());
                                            notOk = true;
                                        }
                                    }
                                }
                }
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pCom[0], pCom[1], pCom[2], pCom[3], pCom[4], pCom[5], pCom[6], pCom[7], pCom[8]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pCor[0], pCor[1], pCor[2], pCor[3], pCor[4], pCor[5], pCor[6], pCor[7], pCor[8]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                pQ[0], pQ[1], pQ[2], pQ[3], pQ[4], pQ[5], pQ[6], pQ[7], pQ[8]) + Environment.NewLine);
                File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8};",
                p2[0], p2[1], p2[2], p2[3], p2[4], p2[5], p2[6], p2[7], p2[8]) + Environment.NewLine);
            }
            #endregion
            MessageBox.Show("KONIEC " + wynik);
        }


    }
}
