using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using PinBoard;
using Microsoft.Win32;
using System.Globalization;

namespace PinDataAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Board board;
        public MainWindow()
        {
            InitializeComponent();
            board = new Board();
        }

        private void buttonProcess_Click(object sender, RoutedEventArgs e)
        {
            string readPath = @"C:\Users\YuOko\OneDrive\Desktop\Work\ROBATEST\ТЗ_Robat1.txt";
            DateTime start = DateTime.Now;
            string text = "";
            using (StreamReader sr = new StreamReader(readPath))
            {                
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    text += fileLine;
                }
            }
            DateTime end = DateTime.Now;
            MessageBox.Show(string.Format("start {0}, end {1}", start, end));
        }

        /// <summary>
        /// open file, read pins from it and fill listbox and board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonChooseInputFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if ((bool)ofd.ShowDialog())
            {
                // read pins to board object
                string path = ofd.FileName;
                string problems = board.ReadPinsFromFile(path);
                if(problems != string.Empty)
                    MessageBox.Show("While reading pins, some lines were misformed:\n" + problems);

                // write components to listbox
                var components = board.Pins.GroupBy(pin => pin.ComponentName)
                    .Select( group => new
                    {
                        component = group.Key,
                        pins = group.Count()
                    }
                    ).OrderBy(grp => grp.component).ToList();

                lbComponents.ItemsSource = components;

                // draw pins to board canvas
                DrawBoard();
            }
        }

        /// <summary>
        /// draw pins on board canvas
        /// </summary>
        void DrawBoard()
        {
            //cBoard.
            // draw pins to board canvas
            foreach (Pin pin in board.Pins)
            {
                DrawPin((int)pin.X, (int)pin.Y);
            }
        }

        /// <summary>
        /// drawing circle for pin
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void DrawPin(int x, int y)
        {
            int radius = 1;

            Point point = new Point(x, y);
            Ellipse ellipse = new Ellipse();

            ellipse.Width = radius * 2;
            ellipse.Height = radius * 2;

            ellipse.StrokeThickness = 1;
            ellipse.Stroke = Brushes.Blue;
            ellipse.Margin = new Thickness(point.X - radius, point.Y - radius, 0, 0);
            cBoard.Children.Add(ellipse);
        }

        /// <summary>
        /// rotate board with pins
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bRotate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                float degree, centerX, centerY;
                if (!Board.TryParseGBFloat(tbDegree.Text, out degree))
                {
                    MessageBox.Show("Bad format of degree");
                    return;
                }
                if (!Board.TryParseGBFloat(tbAroundX.Text, out centerX))
                {
                    MessageBox.Show("Bad format of pivot x");
                    return;
                }                
                if (!Board.TryParseGBFloat(tbAroundY.Text, out centerY))
                {
                    MessageBox.Show("Bad format of pivot y");
                    return;
                }
                board.Turn(degree, centerX, centerY);

                DrawBoard();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
