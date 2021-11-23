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

        // value from 0 to 100 to set it on progressbar when startint last long phase
        private const ushort progressOnLongPhaseStart = 70;

        // filepaths to keep for a thread
        private string inputFilePath, outputFilePath;

        // each N-th pin will change progress bar
        ushort refreshStep = 500;

        // rotation
        float degree, centerX, centerY;

        // working one of buttons' tasks
        private bool inProcess = false;
        #endregion

        private Board board;
        private Polyline pivotPoly;
        private Polygon selectedComponentFigure;

        public MainWindow()
        {
            InitializeComponent();
            board = new Board();

            // pivot to 0
            pivotPoly = new Polyline();
            for (int i = 0; i < 3; i++)
                pivotPoly.Points.Add(new Point(0, 0));
            pivotPoly.Stroke = Brushes.Red;

            selectedComponentFigure = new();
            for (int i = 0; i < 4; i++)
                selectedComponentFigure.Points.Add(new Point(0, 0));
            selectedComponentFigure.Stroke = Brushes.Red;

            //MoveSelectedComponentToDefault();

            cBoard.Children.Add(selectedComponentFigure);
        }

        #region buttons and their actions
        /// <summary>
        /// open file, read pins from it and fill listbox and board
        /// </summary>
        private void ThreadedOpeningAndReading()
        {
            WorkInProgress(true);
            ushort[] progresses = {10, 20, 50, 60, 70, 80, 90, 100};
            ushort currentProgress = 0;
            ShowProgressThreaded("Loading.\nReading file", progresses[currentProgress++]);
            // read pins to board object
            string path = inputFilePath;
            board.ReadFromFile(path);
            ShowProgressThreaded("Loading.\nReading file", progresses[currentProgress++]);

            string problems = "";
            int textLineCount = board.texts.Count;
            for (int ti = 0; ti < textLineCount; ti += refreshStep)
            {
                problems += board.LoadPins(ti, ti + refreshStep);
                ShowProgressThreaded("Loading\nfile data", (ushort)(progresses[currentProgress] + progresses[currentProgress+1] * (float)ti / textLineCount));
            }
            currentProgress++;

            if (board.Pins.Count > 0)
            {
                ShowProgressThreaded("Loading.\nDrawing pins", progresses[currentProgress++]);
                // draw pins to board canvas
                DrawBoardThreaded();

                ShowProgressThreaded("Loading.\nFilling\ncomponents\nlist", progresses[currentProgress++]);

                // write components to listbox
                var components = board.Pins.GroupBy(pin => pin.ComponentName)
                    .Select(group => new
                    {
                        component = group.Key,
                        pins = group.Count()
                    }
                    ).OrderByDescending(grp => grp.pins).ToList();

                ShowProgressThreaded("Loading.\nFilling\ncomponents\nlist", progresses[currentProgress++]);

                //Dispatcher.Invoke(() =>
                //{
                //    lbComponents.ItemsSource = components;
                //});
                
                int componentsCount = components.Count;
                // set progress step
                int refreshLBCStep = componentsCount / 5;
                for (int ci = 0; ci < componentsCount; ci++)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lbComponents.Items.Add($"Component {components[ci].component} has {components[ci].pins} pins");
                    });
                    if (ci % refreshLBCStep == 0)
                        ShowProgressThreaded("Loading.\nFilling\ncomponents\nlist", 
                            (ushort)(progresses[currentProgress] + progresses[currentProgress + 1] * (float)ci / componentsCount));
                }
                //currentProgress++;

                // clearing selection
                Dispatcher.Invoke(() =>
                {
                    MoveSelectedComponentToDefault();
                    lbComponents.SelectedIndex = -1;
                });
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
                //WorkInProgress(true);
                ShowProgressThreaded("Rotating", 0);
                CheckTextBoxes();
                if (board.Pins.Count > 0)
                {
                    //WorkInProgress(true);
                    ShowProgressThreaded("Rotating", 0);
                    new Thread(new ThreadStart(ThreadedRotation)).Start();
                }
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
            ShowProgressThreaded("Rotating", 10);
            WorkInProgress(true);
            int pinCount = board.Pins.Count;
            // write portions of pins and show progress with each portion
            for (int pi = 0; pi < pinCount; pi += refreshStep)
            {
                string info = "Rotating"; //$"Rotating\n{pi} of {pinCount}"
                ShowProgressThreaded(info, progress: (ushort)((progressOnLongPhaseStart) * (float)pi / pinCount));
                board.Turn(degree, centerX, centerY, pi, pi + refreshStep);
            }
            ShowProgressThreaded("Drawing", progressOnLongPhaseStart);
            DrawBoardThreaded();
            ShowProgressThreaded("Rotation\nfinished", 100);
            Dispatcher.Invoke(() =>
            {
                MoveSelectedComponentToDefault();
                MoveSelectedComponentVisuals();
            });

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


        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            WorkInProgress(true);
            if (e.Delta > 0)
            {
                board.Zoom *= 1.1f;
            }
            else
            {
                board.Zoom /= 1.1f;
            }
            DrawBoardThreaded();
            WorkInProgress(false);
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
        /// one of text boxes was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxChanged(object sender, TextChangedEventArgs e)
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

        /// <summary>
        /// get name of component selected in listbox
        /// </summary>
        /// <returns></returns>
        private string GetSelectedComponentName()
        {            
            string selectedComponentLine = lbComponents.SelectedItem.ToString();
            
            string componentName = selectedComponentLine.Split(" has ")[0];
            componentName = componentName.Replace("Component ", "");
            
            return componentName;
        }

        /// <summary>
        /// process selection in component list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbComponents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbComponents.SelectedIndex != -1)
            {
                MoveSelectedComponentVisuals(GetSelectedComponentName());
                tabControl.SelectedIndex = 1;
                cBoard.Focus();
            }
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

        #region drawing stuff
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
        /// drawing pixels for pins on bitmap for image
        /// </summary>
        /// <returns>bitmap with pixels</returns>
        WriteableBitmap DrawPixels()
        {
            ushort pixelSize = 1;
            //foreach (Pin pin in board.Pins)
            //    wb.SetPixel(pin.X, pin.Y, Color.Black);

            // Create the bitmap, with the dimensions of the image placeholder.
            WriteableBitmap wb = new WriteableBitmap((int)img.Width,
                (int)img.Height, 96, 96, PixelFormats.Bgra32, null);

            Random rand = new Random();

            foreach (Pin pin in board.Pins)
            {
                int px = (int)(board.BoardToCanvasX(pin.X) / board.Zoom);
                int py = (int)(board.BoardToCanvasY(pin.Y) / board.Zoom);
                for (int x = px; x < px + pixelSize; x++)
                {
                    for (int y = py; y < py + pixelSize; y++)
                    {
                        // one pixel is not enough while rectangle is too much. Lets try cross.
                        // ok, lets not
                        if (x >= 0 && y >= 0 && x < img.Width && y < img.Height )//&& (x-px) * (y-py) != 1)
                        {
                            int alpha = 0;
                            int red = 0;
                            int green = 0;
                            int blue = 0;

                            // Determine the pixel's color.
                            ushort intens = 255;
                            //red = intens;
                            //green = intens;
                            blue = intens;
                            alpha = 255;
                            // Set the pixel value.                    
                            byte[] colorData = { (byte)blue, (byte)green, (byte)red, (byte)alpha }; // B G R

                            Int32Rect rect = new Int32Rect(x, y, 1, 1);
                            int stride = (wb.PixelWidth * wb.Format.BitsPerPixel) / 8;
                            wb.WritePixels(rect, colorData, stride, 0);

                            //wb.WritePixels(.[y * wb.PixelWidth + x] = pixelColorValue;
                        }
                    }
                }
            }            

            // Show the bitmap in an Image element.
            return wb;
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

            //for (int pi = 0; pi < pinCount; pi++)
            ////foreach (Pin pin in board.Pins)
            //{
            //    Pin pin = board.Pins[pi];
            //    DrawPin(pin.X, pin.Y);
            //    if (pi % refreshStep == 0)
            //        ShowProgress("Loading.\nDrawing pins", (ushort)(progressOnPinningStart + (100 - progressOnPinningStart) * pi / pinCount));
            //}

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

            // lets write down the gravity center
            double gravCenterY = board.CenterOfGravity().Y;
            double posX = bMaxX * 1.05;
            double gravCenterX = board.CenterOfGravity().X;
            double posY =  bMinY - bMaxY * 0.05;

            // and lets write some hints
            //posY = gravCenterY + bMaxY * 0.25;
            //WriteOnBoard(posX, posY, $"Hints:\n1) click board to set pivot\n2) click component in listbox to\nhighlight it on board", color);
            WriteOnBoard(posX, gravCenterY, 
                $"Gravity center: ({gravCenterX}; {gravCenterY})\n\nHints:\n1) click board to set pivot\n2) click component in listbox to\nhighlight it on board"
                , color);
            WriteOnBoard(gravCenterX, posY, $"Gravity center: ({gravCenterX}; {gravCenterY})", color);
            DrawPointFigure(gravCenterX, gravCenterY, color);
            //DrawPointFigure(board.MaxX, posY, color);

            // readding service figures
            MovePivot();
            cBoard.Children.Add(pivotPoly);

            cBoard.Children.Add(selectedComponentFigure);

            MoveSelectedComponentVisuals();

            // draw pins as pixels
            img.Width = (bMaxX - bMinX);
            img.Height = (bMaxY - bMinY);
            img.Source = DrawPixels();
            img.Width *= board.Zoom;
            img.Height *= board.Zoom;
        }

        /// <summary>
        /// move visualization of component according to its name
        /// </summary>
        /// <param name="componentName"></param>
        private void MoveSelectedComponentVisuals(string componentName = "")
        {
            if (componentName == "")
            {
                // exit if selection empty
                if (lbComponents.SelectedIndex != -1)
                    componentName = GetSelectedComponentName();
                else
                {
                    return;
                }
            }

            List<Pin> thisComponentPins = board.Pins.Where(pin => pin.ComponentName == componentName).ToList();
            float MaxX = board.BoardToCanvasX(thisComponentPins.Max(pin => pin.X));
            float MaxY = board.BoardToCanvasY(thisComponentPins.Max(pin => pin.Y));
            float MinX = board.BoardToCanvasX(thisComponentPins.Min(pin => pin.X));
            float MinY = board.BoardToCanvasY(thisComponentPins.Min(pin => pin.Y));

            //selectedComponentFigure.Width = MaxX - MinX;
            //selectedComponentFigure.Height = Math.Abs(MaxY - MinY);
            //Canvas.SetLeft(selectedComponentFigure, MinX);
            //// just in case we change smth in the future and max will swap with min again
            //Canvas.SetTop(selectedComponentFigure, (MaxY < MinY) ? (MaxY) : (MinY));

            selectedComponentFigure.Points[0] = new(MaxX, MaxY);
            selectedComponentFigure.Points[1] = new(MaxX, MinY);
            selectedComponentFigure.Points[2] = new(MinX, MinY);
            selectedComponentFigure.Points[3] = new(MinX, MaxY);
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
                    int px = board.BoardToCanvasX(Helper.ParseGBFloat(tbAroundX.Text));
                    int py = board.BoardToCanvasY(Helper.ParseGBFloat(tbAroundY.Text));

                    pivotPoly.Points[0] = new Point(px + pivotRadius, py);
                    pivotPoly.Points[1] = new Point(px, py);

                    double angle = Math.PI * Helper.ParseGBFloat(tbDegree.Text) / 180;
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

        /// <summary>
        /// park pivot visual at zero
        /// </summary>
        private void PivotToDefault()
        {
            ////pivotPoly = new Polyline();
            //for (int i = 0; i < 3; i++)
            //    pivotPoly.Points.Add(new Point(0, 0));
            //pivotPoly.Stroke = Brushes.Red;
            for (int i = 0; i < 3; i++)
                pivotPoly.Points[i] = new(0, 0);
        }

        /// <summary>
        /// Move Selected Component To Default Position (usually 0)
        /// </summary>
        private void MoveSelectedComponentToDefault()
        {
            //// selected component to 0
            for (int i = 0; i < 4; i++)
                selectedComponentFigure.Points[i] = new(0, 0);
            //selectedComponentFigure = new Rectangle();
            //selectedComponentFigure.Stroke = Brushes.Red;
            //int tempSize = 0;
            //selectedComponentFigure.Width = tempSize;
            //selectedComponentFigure.Height = tempSize;
            //Canvas.SetLeft(selectedComponentFigure, tempSize);
            //Canvas.SetTop(selectedComponentFigure, tempSize);
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

            TextBlock textBlock = new();
            textBlock.Text = text;
            textBlock.Foreground = color;
            Canvas.SetLeft(textBlock, cx);
            Canvas.SetTop(textBlock, cy);
            cBoard.Children.Add(textBlock);
        }

        private void cBoard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //img.Width = cBoard.ActualWidth;
            //img.Height = cBoard.ActualHeight;
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

            Point point = new(x, y);
            Ellipse figure = new();
            //Rectangle fig = new Rectangle();

            figure.Width = radius * 2;
            figure.Height = radius * 2;

            figure.StrokeThickness = 1;
            figure.Stroke = color;
            figure.Margin = new Thickness(point.X - radius, point.Y - radius, 0, 0);
            cBoard.Children.Add(figure);
        }
        #endregion
    }
}


