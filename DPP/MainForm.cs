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
        private string fileName = "";
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
            oknoWyboruPliku.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            oknoWyboruPliku.Filter = "Wszystkie obrazy|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff|"
                                   + "BMP|*.bmp|GIF|*.gif|JPG|*.jpg;*.jpeg|PNG|*.png|TIFF|*.tif;*.tiff";
            oknoWyboruPliku.Title = "Wczytaj obraz";
            oknoWyboruPliku.RestoreDirectory = true;
            if (oknoWyboruPliku.ShowDialog() == DialogResult.OK)
            {
                fileName = oknoWyboruPliku.FileName;
                ObrazSat = new Obraz(fileName);
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
                pictureBox1.Image = ObrazSat.metod1((int)numericUpDown13.Value, (int)numericUpDown12.Value, (int)numericUpDown14.Value, (int)numericUpDown15.Value);
            }
            catch (Exception ex) { MessageBox.Show("Błąd " + ex.ToString()); }
        }

        //sat_kam2 - 7/10; sat_kam3 - 13/18; sat_hro1b - ; sat_hro2 - 

        // Filtr krawędzi + Hough
        private void button10_Click(object sender, EventArgs e)
        {
            int szerokosc = 9;
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            
            int index = comboBox1.SelectedIndex;
            int tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { tr1, tr2, h1, h2, h3 };
            string fileName1 = "test1_1.txt", fileName2 = "test1_2_dokładniej2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, "Sobel + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName1, "tr1;  h1;  h2;  h3; com; cor; q; linie;" + Environment.NewLine);
                for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                    for (h1 = 30; h1 <= 150; h1 += 30) //5
                        for (h2 = 10; h2 <= 70; h2 += 15) //5
                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                
                                double[] pom = ObrazSat.Test1(index, tr1, tr2, h1, h2, h3,szerokosc);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { tr1, tr2, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}",
                                    tr1, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                            }
            }
            else
            {
                File.AppendAllText(fileName2, "Canny + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName2, "tr1; tr2; h1; h2; h3; | com; cor; q; linie;" + Environment.NewLine);
                //for (tr1 = 250; tr1 <= 300; tr1 += 25) //5
                //for (tr2 = 200; tr2 <= tr1; tr2 += 25)
                tr1 = 300; tr2 = 275;
                        for (h1 = 30; h1 <= 50; h1 += 10) 
                            for (h2 = 40; h2 <= 110; h2 += 5) //4
                                for (h3 = 4; h3 <= 10; h3 += 1) //5
                                {
                                    double[] pom = ObrazSat.Test1(index, tr1, tr2, h1, h2, h3,szerokosc);
                                    if (pom[0] > delta[0])
                                    {
                                        delta = pom;
                                        param = new int[] { tr1, tr2, h1, h2, h3 };
                                    }
                                    File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}",
                                    tr1, tr2, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
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
            int szerokosc = 9;
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            
            int index = comboBox1.SelectedIndex;
            int f1 = 15, f2 = 80, f3 = 80, tr1 = 180, tr2 = 100, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { f1, f2, f3, tr1, tr2, h1, h2, h3 };
            string fileName1 = "test2_1.txt", fileName2 = "test2_2.txt", wynik = (index == 0) ? "Sobel: " : "Canny: ";

            if (index == 0) //Sobel
            {
                File.AppendAllText(fileName1, "filtr b + Sobel + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName1, "f1; f2; f3; tr1;  h1;  h2;  h3; com; cor; q; linie;" + Environment.NewLine);
                for (f1 = 5; f1 <= 35; f1 += 10) //4
                    for (f2 = 40; f2 <= 120; f2 += 40) //3
                        for (f3 = 40; f3 <= 120; f3 += 40) //3
                            for (tr1 = 50; tr1 <= 200; tr1 += 50) //4
                                for (h1 = 30; h1 <= 150; h1 += 30) //5
                                    for (h2 = 10; h2 <= 70; h2 += 15) //5
                                        for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                double[] pom = ObrazSat.Test2(index, f1, f2, f3,tr1, tr2, h1, h2, h3);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName1, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}",
                                    f1, f2, f3, tr1, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                            }
            }
            else
            {
                File.AppendAllText(fileName2, "filtr b + Canny + Hough ---------------" + Environment.NewLine);
                File.AppendAllText(fileName2, "f1; f2; f3; tr1; tr2; h1; h2; h3; | com; cor; q; linie;" + Environment.NewLine);
                for (f1 = 5; f1 <= 35; f1 += 10) //4
                    for (f2 = 40; f2 <= 120; f2 += 40) //3
                        for (f3 = 40; f3 <= 120; f3 += 40) //3
                            for (tr1 = 150; tr1 <= 250; tr1 += 25) //5
                                for (tr2 = 100; tr2 <= tr1; tr2 += 25)
                                    for (h1 = 30; h1 <= 150; h1 += 30) //5
                                        for (h2 = 10; h2 <= 70; h2 += 15) //5
                                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                                {
                                    double[] pom = ObrazSat.Test2(index, f1, f2, f3, tr1, tr2, h1, h2, h3);
                                    if (pom[0] > delta[0])
                                    {
                                        delta = pom;
                                        param = new int[] { f1, f2, f3, tr1, tr2, h1, h2, h3 };
                                    }
                                    File.AppendAllText(fileName2, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10}; {11}",
                                    f1, f2, f3, tr1, tr2, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
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
            if (!ObrazSat.trueRoads())
            {
                MessageBox.Show("Brak wzorca dla danego obrazu");
                return;
            }
            int c1 = 60, c2 = 90, c3 = 15, h1 = 20, h2 = 60, h3 = 6;
            double[] delta = { -1, 0 };
            int[] param = { c1, c3 , h1, h2, h3 };
            string fileName = "test3.txt";
            
            File.AppendAllText(fileName, "kolor + Hough ---------------" + Environment.NewLine);
            File.AppendAllText(fileName, "c1; c3;  h1;  h2;  h3; com; cor; q; linie;" + Environment.NewLine);
            for (c1 = 60; c1 <= 70; c1 += 5) //3
                for (c3 = 15; c3 <= 25; c3 += 5) //3
                    //for (h1 = 30; h1 <= 150; h1 += 30) //5
                        for (h2 = 20; h2 <= 80; h2 += 15) //5
                            for (h3 = 0; h3 <= 8; h3 += 2) //5
                            {
                                //pictureBox1.Image = obrazek.Test3(c1, c2, c3, h1, h2, h3);
                                double[] pom = ObrazSat.Test3(c1, c2, c3, h1, h2, h3);
                                if (pom[0] > delta[0])
                                {
                                    delta = pom;
                                    param = new int[] { c1, c3, h1, h2, h3 };
                                }
                                File.AppendAllText(fileName, String.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}; {8}",
                                    c1, c3, h1, h2, h3, pom[0], pom[1], pom[2], pom[3]) + Environment.NewLine);
                            }
            /*numericUpDown10.Value = param[0];
            numericUpDown9.Value = param[1];
            numericUpDown3.Value = param[2];
            numericUpDown4.Value = param[3];
            numericUpDown5.Value = param[4];
            buttonHough_Click(sender, e);*/
            ObrazSat.Reset();
        }
    }
}
