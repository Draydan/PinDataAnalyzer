using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PinBoard
{
    public class Board
    {
        // full text of file in lines        
        public List<string> texts;

        public Board()
        {
            Pins = new List<Pin>();
        }

        public List<Pin> Pins;
        // zooming coeficient to improve view
        public float Zoom = 1.5f;
        private readonly float minZoom = 0.3f;
        private readonly float maxZoom = 10;
        private readonly float zoomCoef = 1.05f;
        private float maxX, minX, maxY, minY;

        //public delegate void UpdateProgressDelegate();

        /// <summary>
        /// Add Pin
        /// </summary>
        /// <param name="px">Pin x</param>
        /// <param name="py">Pin y</param>
        /// <param name="names">Joined names of component and pin</param>       
        /// <param name="lineIndex">Line Index In File</param>
        public void AddPin(float px, float py, string names, int lineIndex)
        {
            string[] namesplit = names.Split('_');
            // After N_PIN there may be some text that can't be split like this.
            // But I dont know what you want to do if its like this.
            // So I'm not adding any specific processing of that error here.
            Pins.Add(new Pin { X = px, Y = py, ComponentName = namesplit[0], Name = namesplit[1], lineIndexInFile = lineIndex });
            ReCalculateExtremes();
        }

        /// <summary>
        /// Calculate the centre of gravity of all the points on the board.
        /// </summary>
        /// <returns></returns>
        public PointF CenterOfGravity()
        {
            return new PointF(
                (float)Math.Round(Pins.Average(pin => pin.X)),
                (float)Math.Round(Pins.Average(pin => pin.Y)));
        }

        /// <summary>
        /// Rotate the points, by ‘n’ degrees, around a defined point in x and y
        /// </summary>
        /// <param name="degreeTurn"></param>
        /// <param name="xTurn"></param>
        /// <param name="yTurn"></param>
        public void Turn(float degreeTurn, float xTurn, float yTurn)
        {
            Turn(degreeTurn, xTurn, yTurn, 0, Pins.Count - 1);
        }

        /// <summary>
        /// Rotate part of the points, by ‘n’ degrees, around a defined point in x and y
        /// </summary>
        /// <param name="degree"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Turn(float degreeTurn, float xTurn, float yTurn, int startPin, int endPin)
        {
            // turn angle in radians
            double rad = Math.PI * degreeTurn / 180f;
            double sin = Math.Sin(rad);
            double cos = Math.Cos(rad);

            for (int pi = startPin; pi < endPin && pi < Pins.Count; pi++)
            {
                Pin pin = Pins[pi];
                                
                double x0 = pin.X;
                double y0 = pin.Y;
                // coordinates relative to turn point
                double xRel = x0 - xTurn;
                double yRel = y0 - yTurn;

                //not own rotation
                //pin.X = (xTurn + xRel * cos + yRel * sin);
                //pin.Y = (yTurn + yRel * cos - xRel * sin);

                //pin.X = (float)Math.Round((xTurn + xRel * cos + yRel * sin), 6);
                //pin.Y = (float)Math.Round((yTurn + yRel * cos - xRel * sin), 6);

                // own rotation
                pin.X = (float)(xTurn + xRel * cos - yRel * sin);
                pin.Y = (float)(yTurn + xRel * sin + yRel * cos);
            }
            ReCalculateExtremes();
        }

        /// <summary>
        /// Read pins from file and return any misformed data as string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string ReadPinsFromFile(string path)
        {
            Pins = new List<Pin>();

            string readPath = path;
            //@"C:\Users\YuOko\OneDrive\Desktop\Work\ROBATEST\ТЗ_Robat1.txt";
            string returnText = "";

            using (StreamReader sr = new(readPath))
            {
                texts = new List<string>();
                int lineIndex = 0;
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    // saving file text for further saving
                    
                    texts.Add(fileLine);                    

                    string pinDesc = fileLine.Replace("N_PIN ", "");
                    if (fileLine.Length > pinDesc.Length)
                    {
                        string[] pinData = pinDesc.Split(' ');

                        //  ignoring any unconventional pin descriptions
                        if (pinData.Length == 3)
                        {
                            string names = pinData[0];
                            string px = pinData[1];
                            string py = pinData[2];

                            try
                            {
                                // adding pin
                                float pxf, pyf;
                                if (Helper.TryParseGBFloat(px, out pxf) && Helper.TryParseGBFloat(py, out pyf))
                                    AddPin(pxf, pyf, names, lineIndex);
                                else
                                    //  catching any unparseable pin descriptions
                                    returnText += pinDesc + "\n";
                            }
                            catch
                            {
                                //  catching any problematic pin descriptions
                                returnText += pinDesc + "\n";
                            }
                        }
                        else
                        {
                            returnText += pinDesc + "\n";
                        }
                    }
                    lineIndex++;
                }
            }
            return returnText;
        }
        /// <summary>
        /// Read file
        /// </summary>
        /// <param name="path"></param>
        public void ReadFromFile(string path)
        {
            Pins = new List<Pin>();

            string readPath = path;

            using (StreamReader sr = new(readPath))
            {
                texts = new List<string>();                
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    // saving file text for further saving
                    texts.Add(fileLine);
                }
            }
        }

        /// <summary>
        /// Read portion of pins from saved text and return any misformed data as string
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <returns>joined text of wrongful data lines</returns> 
        public string LoadPins(int startLine, int endLine)
        {
            if (startLine == 0)
                Pins = new List<Pin>();

            string returnText = "";

            //texts = new List<string>();

            for (int li = startLine; li < texts.Count && li < endLine; li++)
            {
                string fileLine = texts[li];

                string pinDesc = fileLine.Replace("N_PIN ", "");
                if (fileLine.Length > pinDesc.Length)
                {
                    string[] pinData = pinDesc.Split(' ');

                    //  ignoring any unconventional pin descriptions
                    if (pinData.Length == 3)
                    {
                        string names = pinData[0];
                        string px = pinData[1];
                        string py = pinData[2];

                        try
                        {
                            // adding pin
                            float pxf, pyf;
                            if (Helper.TryParseGBFloat(px, out pxf) && Helper.TryParseGBFloat(py, out pyf))
                                AddPin(pxf, pyf, names, li);
                            else
                                //  catching any unparseable pin descriptions
                                returnText += pinDesc + "\n";
                        }
                        catch
                        {
                            //  catching any problematic pin descriptions
                            returnText += pinDesc + "\n";
                        }
                    }
                    else
                    {
                        returnText += pinDesc + "\n";
                    }
                }
            }

            ReCalculateExtremes();

            return returnText;
        }

        /// <summary>
        /// recalculate mins and maxs of pins' coordinates
        /// </summary>
        private void ReCalculateExtremes()
        {
            maxX = (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.X));
            maxY = (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.Y));
            minX = (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.X));
            minY = (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.Y));
        }

        /// <summary>
        /// Write portion of pins' coordinates into new file
        /// </summary>
        /// <param name="path">path to write</param>
        /// <param name="startPin">portion start</param>
        /// <param name="endPin">portion end</param>
        public void WritePinsToFile(string path, int startPin, int endPin)
        {
            //            foreach(Pin pin in Pins)
            for (int pi = startPin; pi < endPin && pi < Pins.Count; pi++)
            {
                Pin pin = Pins[pi];
                //if (pin.ComponentName == "X2J2")
                //    Console.WriteLine("found you");                
                //Regex rex = new(@"N_PIN\s" + pin.ComponentName + "_" + pin.Name + @"\s-?\d+\.\d+\s-?\d+\.\d+");
                ////string textLine =
                //int ind = texts.FindIndex(lin => lin.Contains(pin.ComponentName + "_" + pin.Name));
                ////text = rex.Replace(text, 
                ////    $"N_PIN {pin.ComponentName}_{pin.Name} {Math.Round(pin.X, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))} "
                ////    + $"{Math.Round(pin.Y, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))}");

                texts[pin.lineIndexInFile] =  $"N_PIN {pin.ComponentName}_{pin.Name} {Math.Round(pin.X, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))} "
                    + $"{Math.Round(pin.Y, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))}";

            }

            if (endPin >= Pins.Count - 1)
                using (StreamWriter sw = new(path, append: false))
                {
                    foreach(string textLine in texts)
                        sw.WriteLine(textLine);
                }
        }

        public void ZoomOut()
        {
            if(Zoom > minZoom)
                Zoom /= zoomCoef;
        }

        public void ZoomIn()
        {
            if (Zoom < maxZoom)
                Zoom *= zoomCoef;
        }

        /// <summary>
        /// minimal x of all pins for board drawing
        /// </summary>
        public float MinX
        {
            //get { return (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.X)); }
            get
            {
                return minX;
            }
        }

        /// <summary>
        /// maximal y of all pins for board drawing
        /// </summary>
        public float MaxY
        {
            //get { return (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.Y)); }
            get
            {
                return maxY;
            }
        }

        /// <summary>
        /// minimal y of all pins for board drawing
        /// </summary>
        public float MinY
        {
            //get { return (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.Y)); }
            get { return minY; }
        }
        /// <summary>
        /// maximal x of all pins for board drawing
        /// </summary>
        public float MaxX
        {
            //get { return (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.X)); }
            get
            {
                return maxX;
            }
        }

        /// <summary>
        /// Width multiplied by zoom
        /// </summary>
        public float Width { get { return (MaxX - MinX + 1) * Zoom; }}

        /// <summary>
        /// Height multiplied by zoom
        /// </summary>
        public float Height { get { return (MaxY - MinY + 1) * Zoom; } }

        /// <summary>
        /// transform board coordinates to canvas coordinates
        /// </summary>
        /// <param name="bx"></param>
        /// <returns></returns>
        public float BoardToCanvasX(float bx)
        {
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            return (bx - MinX) * Zoom;
        }

        /// <summary>
        /// transform board coordinates to canvas coordinates
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public float BoardToCanvasY(float by)
        {
            return (MaxY - by) * Zoom;
        }

        /// <summary>
        /// transform canvas coordinates to board coordinates
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <returns></returns>
        public PointF CanvasToBoardCoordinates(float cx, float cy)
        {
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            float x = (float)(MinX + cx / Zoom);
            float y = (float)(MaxY - cy / Zoom);
            return new PointF(x, y);
        }

        /// <summary>
        /// transform coordinates on canvas into board plane
        /// </summary>
        /// <param name="mp">Point on canvas</param>
        /// <returns></returns>
        public PointF CanvasToBoardCoordinates(Point mp)
        {
            return CanvasToBoardCoordinates(mp.X, mp.Y);
        }

        /// <summary>
        /// transform coordinates on canvas into board plane
        /// </summary>
        /// <param name="cx">x on canvas</param>
        /// <param name="cy">y on canvas</param>
        /// <returns></returns>
        public PointF CanvasToBoardCoordinates(int cx, int cy)
        {
            return CanvasToBoardCoordinates(cx, cy);
        }

        /// <summary>
        /// transform coordinates on canvas into board plane
        /// </summary>
        /// <param name="cx">x on canvas</param>
        /// <param name="cy">y on canvas</param>
        /// <returns></returns>
        public PointF CanvasToBoardCoordinates(double cx, double cy)
        {
            return CanvasToBoardCoordinates((float)cx, (float)cy);
        }

    }
}
