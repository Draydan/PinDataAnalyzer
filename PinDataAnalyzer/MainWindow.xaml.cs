﻿using System;
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
using System.Threading;
//using System.Drawing;

namespace PinDataAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region constants and parameters
        // radius of drawed segment
        private const ushort pivotRadius = 40;

        // value from 0 to 100 to set it on progressbar when startint drawing pins
        private const ushort progressOnPinningStart = 70;

        // filepaths to keep for a thread
        private string inputFilePath, outputFilePath;

        // each N-th pin will change progress bar
        ushort refreshStep = 1000;

        // rotation
        float degree, centerX, centerY;

        // working one of buttons' tasks
        private bool inProcess = false;
        #endregion

        private Board board;
        private Polyline pivotPoly;
        private Rectangle componentSelected;

        public MainWindow()
        {
            InitializeComponent();
            board = new Board();

            PivotToDefault();
            componentSelected = new Rectangle();
            componentSelected.Stroke = Brushes.Orange;
            cBoard.Children.Add(componentSelected);
        }

        #region buttons and their actions
        /// <summary>
        /// open file, read pins from it and fill listbox and board
        /// </summary>
        private void ThreadedOpeningAndReading()
        {
            WorkInProgress(true);
            ShowProgressThreaded("Loading.\nReading file", 10);
            // read pins to board object
            string path = inputFilePath;
            string problems = board.ReadPinsFromFile(path);

            if (board.Pins.Count > 0)
            {
                ShowProgressThreaded("Loading.\nFilling\ncomponents\nlist", 25);

                // write components to listbox
                var components = board.Pins.GroupBy(pin => pin.ComponentName)
                    .Select(group => new
                    {
                        component = group.Key,
                        pins = group.Count()
                    }
                    ).OrderBy(grp => grp.component).ToList();

                ShowProgressThreaded("Loading.\nFilling\ncomponents\nlist", 40);

                Dispatcher.Invoke(() =>
                {
                    lbComponents.ItemsSource = components;
                });

                ShowProgressThreaded("Loading.\nDrawing pins", progressOnPinningStart);
                // draw pins to board canvas
                DrawBoardThreaded();
            }
            else
            {
                // clearing if no pins loaded
                Dispatcher.Invoke(() =>
                {
                    lbComponents.Items.Clear();
                    cBoard.Children.Clear();
                });
            }
            ShowProgressThreaded($"Loading\nfinished\n{board.Pins.Count} Pins\nloaded", 100);
            
            WorkInProgress(false);
            if (problems != string.Empty)
                MessageBox.Show("While reading pins, some lines were misformed:\n" + problems);
        }

        /// <summary>
        /// open file, read pins from it and fill listbox and board in separate thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonChooseInputFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if ((bool)ofd.ShowDialog())
                {
                    ShowProgressThreaded("Loading.\nReading file", 0);
                    inputFilePath = ofd.FileName;
                    new Thread(new ThreadStart(ThreadedOpeningAndReading)).Start();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
                CheckTextBoxes();
                if(board.Pins.Count>0)
                    new Thread(new ThreadStart(ThreadedRotation)).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        /// <summary>
        /// Rotation of pins, drawing and showing progress
        /// </summary>
        private void ThreadedRotation()
        {
            WorkInProgress(true);

            int pinCount = board.Pins.Count;
            // write portions of pins and show progress with each portion
            for (int pi = 0; pi < pinCount; pi += refreshStep)
            {
                board.Turn(degree, centerX, centerY, pi, pi + refreshStep);
                ShowProgressThreaded("Rotating", progress: (ushort)((progressOnPinningStart) * pi / pinCount));
            }
            ShowProgressThreaded("Drawing", progressOnPinningStart);
            DrawBoardThreaded();
            ShowProgressThreaded("Rotation\nfinished", 100);
            WorkInProgress(false);
        }

        /// <summary>
        /// Threaded Process click of button to write file
        /// </summary>
        private void ThreadedWritingProgress()
        {
            WorkInProgress(true);
            int pinCount = board.Pins.Count;
            // write portions of pins and show progress with each portion
            for (int pi = 0; pi < pinCount; pi += refreshStep)
            {
                board.WritePinsToFile(outputFilePath, pi, pi + refreshStep);
                ShowProgressThreaded("Writing file", progress: (ushort)(100 * pi / pinCount));
            }
            ShowProgressThreaded("File written", 100);
            WorkInProgress(false);
        }

        /// <summary>
        /// Process click of button to write file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bChooseOutputFile_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                SaveFileDialog fd = new SaveFileDialog();
                if ((bool)fd.ShowDialog())
                {
                    outputFilePath = fd.FileName;
                    Thread tred = new Thread(new ThreadStart(ThreadedWritingProgress));
                    tred.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }            
        }

        #endregion

        #region visual controls

        /// <summary>
        /// Set progress bar and write some info on label from another thread
        /// </summary>
        /// <param name="info"></param>
        /// <param name="progress"></param>
        private void ShowProgressThreaded(string info, ushort progress)
        {
            Dispatcher.Invoke(() =>
                    {
                        ShowProgress(info, progress);
                    });
        }

        /// <summary>
        /// Set progress bar and write some info on label
        /// </summary>
        /// <param name="info"></param>
        /// <param name="progress"></param>
        private void ShowProgress(string info, ushort progress)
        {
            lbInfo.Content = info;
            pbInfo.Value = progress;
        }

        /// <summary>
        /// draw pins on board canvas in a thread
        /// </summary>
        void DrawBoardThreaded()
        {
            Dispatcher.Invoke(() =>
            {
                DrawBoard();
            });
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

            for (int pi = 0; pi < pinCount; pi++)
            //foreach (Pin pin in board.Pins)
            {
                Pin pin = board.Pins[pi];
                DrawPin(pin.X, pin.Y);
                if (pi % refreshStep == 0)
                    ShowProgressThreaded("Loading.\nDrawing pins", (ushort)(progressOnPinningStart + (100 - progressOnPinningStart) * pi / pinCount));
            }

            // lets write all extremal coordinates on board
            int bMaxX, bMinX, bMaxY, bMinY;
            bMaxX = (int)Math.Round(board.MaxX);
            bMaxY = (int)Math.Round(board.MaxY);
            bMinX = (int)Math.Round(board.MinX);
            bMinY = (int)Math.Round(board.MinY);

            SolidColorBrush color = Brushes.Red;
            WriteOnBoard(bMinX, bMinY, $"({bMinX}; {bMinY})", color);
            DrawPointFigure(bMinX, bMinY, color);

            WriteOnBoard(bMaxX, bMaxY, $"({bMaxX}; {bMaxY})", color);
            DrawPointFigure(bMaxX, bMaxY, color);

            WriteOnBoard(bMinX, bMaxY, $"({bMinX}; {bMaxY})", color);
            DrawPointFigure(bMinX, bMaxY, color);

            WriteOnBoard(bMaxX, bMinY, $"({bMaxX}; {bMinY})", color);
            DrawPointFigure(bMaxX, bMinY, color);

            double posY = board.CenterOfGravity().Y;
            double posX = board.MaxX * 1.05;
            WriteOnBoard(posX, posY, $"Gravity center: ({board.CenterOfGravity().X}; {posY})", color);
            //DrawPointFigure(board.MaxX, posY, color);

            MovePivot();
            cBoard.Children.Add(pivotPoly);
        }

        /// <summary>
        /// move visualization of component according to its name
        /// </summary>
        /// <param name="componentName"></param>
        private void MoveSelectedComponentVisuals(string componentName)
        {
            List<Pin> thisComponentPins = board.Pins.Where(pin => pin.ComponentName == componentName).ToList();
            float MaxX = board.BoardToCanvasX(thisComponentPins.Max(pin => pin.X));
            float MaxY = board.BoardToCanvasY(thisComponentPins.Max(pin => pin.Y));
            float MinX = board.BoardToCanvasX(thisComponentPins.Min(pin => pin.X));
            float MinY = board.BoardToCanvasY(thisComponentPins.Min(pin => pin.Y));

            componentSelected.Width = MaxX - MinX;
            componentSelected.Height = Math.Abs(MaxY - MinY);
            Canvas.SetLeft(componentSelected, MinX);
            // just in case we change smth in the future and max will swap with min again
            Canvas.SetTop(componentSelected, (MaxY<MinY)?(MaxY):(MinY));
        }

        /// <summary>
        /// Move Pivot Polygon on canvas in accordance with textboxes' values
        /// </summary>
        private void MovePivot()
        {
            try
            {
                if (board != null && tbDegree != null && tbAroundX != null && tbAroundY != null)
                {
                    int px = board.BoardToCanvasX(int.Parse(tbAroundX.Text));
                    int py = board.BoardToCanvasY(int.Parse(tbAroundY.Text));

                    pivotPoly.Points[0] = new Point(px + pivotRadius, py);
                    pivotPoly.Points[1] = new Point(px, py);

                    double angle = Math.PI * int.Parse(tbDegree.Text) / 180;
                    pivotPoly.Points[2] = new Point(
                        px + pivotRadius * Math.Cos(angle),
                        py - pivotRadius * Math.Sin(angle));
                }
            }
            catch
            {
                PivotToDefault();
            }
        }

        private void PivotToDefault()
        {
            pivotPoly = new Polyline();
            for (int i = 0; i < 3; i++)
                pivotPoly.Points.Add(new Point(0, 0));
            pivotPoly.Stroke = Brushes.Red;
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
        /// moving mouse, showing coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (!inProcess)
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
            if (!inProcess)
                if (board.Pins.Count > 0)
                {
                    Point mp = e.GetPosition(cBoard);
                    System.Drawing.PointF p = board.CanvasToBoardCoordinates(mp.X, mp.Y);
                    tbAroundX.Text = ((int)p.X).ToString();
                    tbAroundY.Text = ((int)p.Y).ToString();

                    //DrawPointFigure(mp.X, mp.Y, Brushes.Yellow, 10);
                    MovePivot();
                }
        }

        /// <summary>
        /// one of text boxes was confirmed with Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxConfirmed(object sender, TextChangedEventArgs e)
        {
            CheckTextBoxes();
            MovePivot();
        }

        /// <summary>
        /// checking text boxes for format of values
        /// </summary>
        private void CheckTextBoxes()
        {
            if (tbDegree != null && tbAroundX != null && tbAroundY != null)
            {
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
            }
        }

        private void lbComponents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedComponentLine = lbComponents.SelectedItem.ToString();
            //MessageBox.Show(selectedComponentLine);
            string componentName = selectedComponentLine.Split(", pins")[0];
            componentName = componentName.Replace("{ component = ", "");
            //{ component = "BL13", pins = 1 }
            MoveSelectedComponentVisuals(componentName);
            cBoard.Focus();
        }

        /// <summary>
        /// disable some controls when work is in progress
        /// </summary>
        /// <param name="wip">work IS in progress</param>
        private void WorkInProgress(bool wip)
        {
            inProcess = wip;
            Dispatcher.Invoke(() =>
            {
                ButtonsEnability(!wip);
            });
        }

        /// <summary>
        /// enable or disable all buttons
        /// </summary>
        /// <param name="be">enability status to set</param>
        private void ButtonsEnability(bool be)
        {
            IEnumerable<Button> buttons = MainGrid.Children.OfType<Button>();
            foreach (Button butn in buttons)
                butn.IsEnabled = be;
        }

        /// <summary>
        /// enable or disable buttons specifically
        /// </summary>
        /// <param name="beOpen">enability status of "Open File" to set</param>
        /// <param name="beRotate">enability status of "Rotate" to set</param>
        /// <param name="beWrite">enability status of "Write File" to set</param>
        private void ButtonsEnability(bool beOpen, bool beRotate, bool beWrite)
        {
            bLoadFile.IsEnabled = beOpen;
            bRotate.IsEnabled = beRotate;
            bWriteFile.IsEnabled = beWrite;
        }
        #endregion
    }
}
