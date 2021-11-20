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
//using System.Drawing;

namespace PinDataAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Board board;

        // value from 0 to 100 to set it on progressbar when startint drawing pins
        private const ushort progressOnPinningStart = 30;

        public MainWindow()
        {
            InitializeComponent();
            board = new Board();
        }

        private void ShowProgress(string info, ushort progress)
        {
            lbInfo.Content = info;
            pbInfo.Value = progress;
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
                ShowProgress("Loading.\nReading file", 0);
                // read pins to board object
                string path = ofd.FileName;
                string problems = board.ReadPinsFromFile(path);
                if(problems != string.Empty)
                    MessageBox.Show("While reading pins, some lines were misformed:\n" + problems);

                ShowProgress("Loading.\nFilling components list", 10);
                // write components to listbox
                var components = board.Pins.GroupBy(pin => pin.ComponentName)
                    .Select( group => new
                    {
                        component = group.Key,
                        pins = group.Count()
                    }
                    ).OrderBy(grp => grp.component).ToList();

                lbComponents.ItemsSource = components;

                ShowProgress("Loading.\nDrawing pins", progressOnPinningStart);
                // draw pins to board canvas
                DrawBoard();
                ShowProgress("Loading\nfinished", 100);
            }
        }

        /// <summary>
        /// draw pins on board canvas
        /// </summary>
        void DrawBoard()
        {            
            cBoard.Children.Clear();
            // draw pins to board canvas
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            float minX = board.Pins.Min(pin => pin.X);
            float maxY = board.Pins.Max(pin => pin.Y);

            int pinCount = board.Pins.Count;

            // each N-th pin will change progress bar
            ushort refreshStep = 2000;

            for (int pi = 0; pi < pinCount; pi++)
            //foreach (Pin pin in board.Pins)
            {
                Pin pin = board.Pins[pi];
                DrawPin(pin.X, pin.Y);
                if (pi % refreshStep == 0)
                    ShowProgress("Loading.\nDrawing pins", (ushort)(progressOnPinningStart + (100 - progressOnPinningStart) * pi / pinCount));
            }
            // lets write all extremal coordinates on board
            
            SolidColorBrush color = Brushes.Red;
            WriteOnBoard(board.MinX, board.MinY, $"({board.MinX}; {board.MinY})", color);
            DrawPointFigure(board.MinX, board.MinY, color);

            //WriteOnBoard(board.MaxX, board.MaxY, $"({board.MaxX}; {board.MinY})", color);
            //DrawPoint(x, y, Brushes.Blue);

            WriteOnBoard(board.MinX, board.MaxY, $"({board.MinX}; {board.MaxY})", color);
            DrawPointFigure(board.MinX, board.MaxY, color);
        }

        /// <summary>
        /// write text on board canvas
        /// </summary>
        /// <param name="bx">board x</param>
        /// <param name="by">board y</param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        void WriteOnBoard(double bx, double by, string text, SolidColorBrush color)
        {
            //ushort shift = 5;

            int cx = board.BoardToCanvasX(bx);
            int cy = board.BoardToCanvasY(by);

            //(int)(pin.X - minX), (int)(maxY - pin.Y)

            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = color;
            Canvas.SetLeft(textBlock, cx);
            Canvas.SetTop(textBlock, cy);
            cBoard.Children.Add(textBlock);
        }

        /// <summary>
        /// drawing pin
        /// </summary>
        /// <param name="x">board x</param>
        /// <param name="y">board y</param>
        void DrawPin(float x, float y)
        {
            DrawPointFigure(x, y, Brushes.Blue);
        }

        /// <summary>
        /// drawing colored "point"
        /// </summary>
        /// <param name="x">board x</param>
        /// <param name="y">board y</param>
        /// <param name="color">color</param>
        void DrawPointFigure(double x, double y, SolidColorBrush color, int width = 1)
        {
            DrawPointFigure((int)x, (int)y, color, width);
        }

        /// <summary>
        /// drawing colored "point"
        /// </summary>
        /// <param name="bx">board x</param>
        /// <param name="by">board y</param>
        /// <param name="color">color</param>
        void DrawPointFigure(int bx, int by, SolidColorBrush color, int width = 1)
        {
            int radius = width;

            int x = board.BoardToCanvasX(bx);
            int y = board.BoardToCanvasY(by);

            Point point = new Point(x, y);
            Ellipse figure = new Ellipse();
            //Rectangle fig = new Rectangle();

            figure.Width = radius * 2;
            figure.Height = radius * 2;

            figure.StrokeThickness = 1;
            figure.Stroke = color;
            figure.Margin = new Thickness(point.X - radius, point.Y - radius, 0, 0);
            cBoard.Children.Add(figure);
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
                if (!Helper.TryParseGBFloat(tbDegree.Text, out degree))
                {
                    MessageBox.Show("Bad format of degree");
                    return;
                }
                if (!Helper.TryParseGBFloat(tbAroundX.Text, out centerX))
                {
                    MessageBox.Show("Bad format of pivot x");
                    return;
                }                
                if (!Helper.TryParseGBFloat(tbAroundY.Text, out centerY))
                {
                    MessageBox.Show("Bad format of pivot y");
                    return;
                }
                board.Turn(degree, centerX, centerY);
                DrawBoard();
                ShowProgress("Rotation\nfinished", 100);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// moving mouse, showing coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (board.Pins.Count > 0)
            {
                // as screen coordinates are mirrored along Y axis, some transformations are necessary
                Point mp = e.GetPosition(cBoard);
                System.Drawing.PointF p = board.CanvasToBoardCoordinates(mp.X, mp.Y);
                lbInfo.Content = $"Mouse:\nX={p.X};\nY={p.Y}";
            }
        }

        /// <summary>
        /// click to get coordinates for rotation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (board.Pins.Count > 0)
            {
                Point mp = e.GetPosition(cBoard);
                System.Drawing.PointF p = board.CanvasToBoardCoordinates(mp.X, mp.Y);
                tbAroundX.Text = ((int)p.X).ToString();
                tbAroundY.Text = ((int)p.Y).ToString();

                //DrawPointFigure(mp.X, mp.Y, Brushes.Yellow, 10);
            }
        }

        private void bChooseOutputFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            
            if ((bool)fd.ShowDialog())
            {
                int pinCount = board.Pins.Count;

                // each N-th pin will change progress bar
                ushort refreshStep = 1000;

                for (int pi = 0; pi < pinCount; pi += refreshStep)
                {
                    board.WritePinsToFile(fd.FileName, pi, pi + refreshStep);
                    ShowProgress("Writing file", (ushort)(100 * pi / pinCount));
                }
                //MessageBox.Show("File written");
                ShowProgress("File written", 100);
            }
        }
    }
}
