﻿using System;
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
        // full text of file
        private string text;

        public Board()
        {
            Pins = new List<Pin>();
        }

        public List<Pin> Pins;

        //public delegate void UpdateProgressDelegate();

        /// <summary>
        /// Add Pin
        /// </summary>
        /// <param name="px">Pin x</param>
        /// <param name="py">Pin y</param>
        /// <param name="names">Joined names of component and pin</param>
        public void AddPin(float px, float py, string names)
        {
            string[] namesplit = names.Split('_');
            // After N_PIN there may be some text that can't be split like this.
            // But I dont know what you want to do if its like this.
            // So I'm not adding any specific processing of that error here.
            Pins.Add(new Pin { X = px, Y = py, ComponentName = namesplit[0], Name = namesplit[1] });
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
            for (int pi = startPin; pi <= endPin && pi < Pins.Count; pi++)
            {
                Pin pin = Pins[pi];
                
                float x0 = pin.X;
                float y0 = pin.Y;
                // coordinates relative to turn point
                float xRel = x0 - xTurn;
                float yRel = y0 - yTurn;
                // turn angle in radians
                double rad = Math.PI * degreeTurn / 180;

                //not own rotation
                //pin.X = (float)(xTurn + xRel * Math.Cos(rad) + yRel * Math.Sin(rad));
                //pin.Y = (float)(yTurn + yRel * Math.Cos(rad) - xRel * Math.Sin(rad));

                // own rotation
                pin.X = (float)(xTurn + xRel * Math.Cos(rad) - yRel * Math.Sin(rad));
                pin.Y = (float)(yTurn + xRel * Math.Sin(rad) + yRel * Math.Cos(rad));
            }
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
                text = "";
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    text += fileLine + "\n";
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
                                    AddPin(pxf, pyf, names);
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
            }
            return returnText;
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
            for (int pi = startPin; pi <= endPin && pi < Pins.Count; pi++)
            {
                Pin pin = Pins[pi];
                Regex rex = new(@"N_PIN\s" + pin.ComponentName + "_" + pin.Name + @"\s\d+\.\d+\s\d+\.\d+");
                text = rex.Replace(text, 
                    $"N_PIN {pin.ComponentName}_{pin.Name} {Math.Round(pin.X, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))} "
                    + $"{Math.Round(pin.Y, 3).ToString(CultureInfo.GetCultureInfo("en-GB"))}");
            }

            if (endPin >= Pins.Count - 1)
                using (StreamWriter sw = new(path, append: false))
                {
                    sw.Write(text);
                }
        }

        /// <summary>
        /// minimal x of all pins for board drawing
        /// </summary>
        public float MinX
        {
            get { return (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.X)); }
        }

        /// <summary>
        /// maximal y of all pins for board drawing
        /// </summary>
        public float MaxY
        {
            get { return (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.Y)); }
        }

        /// <summary>
        /// minimal y of all pins for board drawing
        /// </summary>
        public float MinY
        {
            get { return (Pins.Count == 0) ? (0) : (Pins.Min(pin => pin.Y)); }
        }
        /// <summary>
        /// maximal x of all pins for board drawing
        /// </summary>
        public float MaxX
        {
            get { return (Pins.Count == 0) ? (0) : (Pins.Max(pin => pin.X)); }
        }

        public int BoardToCanvasX(double bx)
        {
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            return (int)(bx - this.MinX);
        }

        public int BoardToCanvasY(double by)
        {
            return (int)(this.MaxY - by);
        }

        public PointF CanvasToBoardCoordinates(double cx, double cy)
        {
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            float x = (float)(this.MinX + cx);
            float y = (float)(this.MaxY - cy);
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

    }
}
