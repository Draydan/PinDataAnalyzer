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
        private BoardView boardView;
        private Polyline pivotPoly;
        private Polygon selectedComponentFigure;

        public MainWindow()
        {
            InitializeComponent();
            board = new Board();
            boardView = new BoardView(board);

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
            ushort[] progresses = { 10, 20, 50, 60, 70, 80, 90, 100 };
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
                ShowProgressThreaded("Loading\nfile data", (ushort)(progresses[currentProgress] + progresses[currentProgress + 1] * (float)ti / textLineCount));
            }
            currentProgress++;

            //            #region убрать 

            //#warning ТЕСТ! заполнение нужно потом убрать!!!

            //            {
            //                float minx = board.MinX, maxx = board.MaxX, miny = board.MinY, maxy = board.MaxY;
            //                if (maxx == 0 && maxy == 0)
            //                {
            //                    maxx = 100;
            //                    maxy = 100;
            //                }

            //                for (int xi = (int)minx; xi < maxx; xi += 1)
            //                    for (int yi = (int)miny; yi < maxy; yi += 2)
            //                    {
            //                        board.texts.Add($"N_PIN {1}_{1} {xi} {yi}");
            //                        board.AddPin(xi, yi, "1_1", board.texts.Count-1);
            //                    }
            //            }
            //            #endregion

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
                int refreshLBCStep = componentsCount / 5 + 1;
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
        private void bLoadFile_Click(object sender, RoutedEventArgs e)
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

            board.Turn(degree, centerX, centerY);
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
            MessageBox.Show("File written successfully");
            WorkInProgress(false);
        }

        /// <summary>
        /// Process click of button to write file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWriteFile_Click(object sender, RoutedEventArgs e)
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

        #region visual controls, their events and actions

        /// <summary>
        /// mouse wheeling zooms in and out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (board.Pins.Count > 0)
            {
                WorkInProgress(true);
                Point prevMousePoint = e.GetPosition(cBoard);
                System.Drawing.PointF prevBoardMousePoint = boardView.CanvasToBoardCoordinates(prevMousePoint.X, prevMousePoint.Y);

                if (e.Delta > 0)
                {
                    boardView.ZoomIn();
                }
                else
                {
                    boardView.ZoomOut();
                }
                //DrawBoardThreaded();
                img.Width = boardView.Width;
                img.Height = boardView.Height;
                //cboardView.Width = img.Width + 300;
                //cboardView.Height = img.Height + 100;
                ReDrawCanvas();
                ReDrawImage();

                // aim zoom proportionally to mouse cursor after redraw
                Point newMousePoint = e.GetPosition(svBoard);
                //double xProportion = mp.X / svBoard.ActualWidth;
                //double yProportion = mp.Y / svBoard.ActualHeight;

                double newCanvasMousePointX = boardView.BoardToCanvasX(prevBoardMousePoint.X);
                double newCanvasMousePointY = boardView.BoardToCanvasY(prevBoardMousePoint.Y);

                // new scrolled position shold leave mouse "board" coordinate the same
                // scrollsize = screensize = gridsize
                // scrollpos + mousepos = canvaspos
                // scrollpos = canvaspos - mousepos

                svBoard.ScrollToHorizontalOffset(newCanvasMousePointX - newMousePoint.X);
                svBoard.ScrollToVerticalOffset(newCanvasMousePointY - newMousePoint.Y);
                WorkInProgress(false);
            }
        }

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
                    System.Drawing.PointF p = boardView.CanvasToBoardCoordinates(mp.X, mp.Y);
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
                    System.Drawing.PointF p = boardView.CanvasToBoardCoordinates(mp.X, mp.Y);
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

        /// <summary>
        /// click cb-AA, change AA mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAA_Click(object sender, RoutedEventArgs e)
        {
            if (board.Pins.Count > 0)
            {
                ReDrawImage();
            }
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
            // maybe draw pixels around pixel for more uniform visual distribution
            ushort pixelRadius = (ushort)(((bool)cbAA.IsChecked) ? 2 : 1);

            // internal experimental parameter to try show big blurry pixels a bit "smaller"
            bool smallerPixels = false;

            // make this pseudocircle less sizeable visually, by cutting blur at some radius.
            // margin to cut
            //float cut = 0.5f;

            int imgWid = (int)Math.Round(img.Width, 0);
            int imgHei = (int)Math.Round(img.Height, 0);
            //foreach (Pin pin in board.Pins)
            //    wb.SetPixel(pin.X, pin.Y, Color.Black);

            int dpi = 96;
            // Create the bitmap, with the dimensions of the image placeholder.
            WriteableBitmap wb = new WriteableBitmap((int)imgWid,
                (int)imgHei, dpi, dpi, PixelFormats.Bgra32, null);

            // color map to fill and draw
            ushort[,] map = new ushort[imgWid, imgHei];

            // color of pins
            ushort alpha = 0;
            ushort red = 0;
            ushort green = 0;
            ushort blue = 255;

            // set colors in an array for pins
            foreach (Pin pin in board.Pins)
            {
                int px = (int)pin.X;
                int py = (int)pin.Y;

                float fpx, fpy;
                fpx = (pin.X - board.MinX) * boardView.Zoom;
                fpy = (board.MaxY - pin.Y) * boardView.Zoom;
                px = (int)Math.Round(fpx, 0);
                py = (int)Math.Round(fpy, 0);                

                for (int xi = px - pixelRadius + 1; xi < px + pixelRadius; xi++)                
                {
                    for (int yi = py - pixelRadius + 1; yi < py + pixelRadius; yi++)                    
                    {
                        // one pixel is not enough while rectangle is too much. Lets try cross.
                        // ok, lets not
                        if (xi >= 0 && yi >= 0 && xi < imgWid && yi < imgHei)//&& (x-px) * (y-py) != 1)
                        {
                            // Determine the pixel's color.
                            ushort intens = 255;

                            if (pixelRadius > 1)
                            {
                                //double distance = Math.Sqrt(Math.Pow(xi - fpx, 2) + Math.Pow(yi - fpy, 2));

                                // simplified distance metric for faster calculations
                                // this metric is legit math metric
                                double distance = Math.Max(Math.Abs(xi - fpx), Math.Abs(yi - fpy));
                                // changing intensity of alpha of color according to distance of current point to pin coordinat (float)
                                //alpha = (int)(((pixelRadius + 1 - Math.Abs(xi - fpx)) * intens + (pixelRadius + 0.5 - Math.Abs(yi - fpy)) * intens) / 2);
                                //alpha = ((intens * (pixelRadius - Math.Abs()
                                //if (Math.Abs(xi - fpx) > 1 || Math.Abs(yi - fpy) > 1) alpha = 0;
                                
                                if (smallerPixels)
                                {
                                    if (distance < 1)
                                        alpha = (ushort)(intens * ((pixelRadius - 1) - (distance)) / (pixelRadius - 1));
                                    else
                                        alpha = 0;
                                }
                                else
                                {
                                    alpha = (ushort)(intens * (pixelRadius - distance) / pixelRadius);                                    
                                }
                                if(alpha > map[xi, yi])
                                    map[xi, yi] = alpha;
                            }
                            else
                                map[px, py] = 255;
                        }
                    }
                }
            }
            
            // draw pixels on bitmap according to colors in array
            for (int xi = 0; xi < (int)imgWid; xi++)            
            {
                for (int yi = 0; yi < (int)imgHei; yi++)
                {
                    alpha = map[xi, yi];
                    if (alpha > 0)
                    {
                        // Set the pixel value.                    
                        byte[] colorData = { (byte)blue, (byte)green, (byte)red, (byte)alpha}; // B G R

                        Int32Rect rect = new Int32Rect(xi, yi, 1, 1);
                        int stride = wb.PixelWidth * wb.Format.BitsPerPixel / 8;
                        wb.WritePixels(rect, colorData, stride, 0);
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
            ReDrawCanvas();
            ReDrawImage();
        }

        /// <summary>
        /// redraw image with pins
        /// </summary>
        private void ReDrawImage()
        {
            int bMaxX, bMinX, bMaxY, bMinY;
            bMaxX = (int)Math.Round(board.MaxX);
            bMaxY = (int)Math.Round(board.MaxY);
            bMinX = (int)Math.Round(board.MinX);
            bMinY = (int)Math.Round(board.MinY);

            // draw pins as pixels
            img.Width = bMaxX - bMinX + 1;
            img.Height = bMaxY - bMinY + 1;
            img.Width = boardView.Width;
            img.Height = boardView.Height;

            img.Source = DrawPixels();

            img.Width = boardView.Width;
            img.Height = boardView.Height;
        }

        /// <summary>
        /// redraw canvas with auxillary info
        /// </summary>
        private void ReDrawCanvas()
        {
            cBoard.Children.Clear();

            // draw pins to board canvas
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            //float minX = board.Pins.Min(pin => pin.X);
            //float maxY = board.Pins.Max(pin => pin.Y);

            //int pinCount = board.Pins.Count;

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
            float gravCenterY = board.CenterOfGravity().Y;
            float posX = bMaxX * 1.05f;
            float gravCenterX = board.CenterOfGravity().X;
            float posY = bMinY - bMaxY * 0.05f;

            // and lets write some hints
            //posY = gravCenterY + bMaxY * 0.25;
            //WriteOnBoard(posX, posY, $"Hints:\n1) click board to set pivot\n2) click component in listbox to\nhighlight it on board", color);
            WriteOnBoard(posX, gravCenterY,
                $"Gravity center: ({gravCenterX}; {gravCenterY})\n\n"
                + "Hints:\n"
                + "1) click board to set pivot\n"
                + "2) click component in listbox to\nhighlight it on board\n"
                + "3) use mouse wheel to zoom in and out\n"
                + "4) when scrolls are active,\nzooming is aimed to mouse position\n"
                + "5) click checkbox for wider pins (bolder, more weight)\n"                
                , color);
            WriteOnBoard(gravCenterX, posY, $"Gravity center: ({gravCenterX}; {gravCenterY})", color);
            DrawPointFigure(gravCenterX, gravCenterY, color);
            //DrawPointFigure(board.MaxX, posY, color);

            // readding service figures
            MovePivot();
            cBoard.Children.Add(pivotPoly);

            cBoard.Children.Add(selectedComponentFigure);

            MoveSelectedComponentVisuals();
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
            float MaxX = boardView.BoardToCanvasX(thisComponentPins.Max(pin => pin.X));
            float MaxY = boardView.BoardToCanvasY(thisComponentPins.Max(pin => pin.Y));
            float MinX = boardView.BoardToCanvasX(thisComponentPins.Min(pin => pin.X));
            float MinY = boardView.BoardToCanvasY(thisComponentPins.Min(pin => pin.Y));

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
                    int px = (int)Math.Round(boardView.BoardToCanvasX(Helper.ParseGBFloat(tbAroundX.Text)), 0);
                    int py = (int)Math.Round(boardView.BoardToCanvasY(Helper.ParseGBFloat(tbAroundY.Text)), 0);

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
        void WriteOnBoard(float bx, float by, string text, SolidColorBrush color)
        {
            //ushort shift = 5;

            int cx = (int)boardView.BoardToCanvasX(bx);
            int cy = (int)boardView.BoardToCanvasY(by);

            //(int)(pin.X - minX), (int)(maxY - pin.Y)

            TextBlock textBlock = new();
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
        void DrawPointFigure(float x, float y, SolidColorBrush color, int width = 1)
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

            int x = (int)Math.Round(boardView.BoardToCanvasX(bx), 0);
            int y = (int)Math.Round(boardView.BoardToCanvasY(by), 0);

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