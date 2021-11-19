using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace PinBoard
{
    public class Board
    {
        public Board()
        {
            Pins = new List<Pin>();
        }

        public List<Pin> Pins;

        /// <summary>
        /// Add Pin
        /// </summary>
        /// <param name="px">Pin x</param>
        /// <param name="py">Pin y</param>
        /// <param name="names">Joined names of component and pin</param>
        public void AddPin(float px, float py, string names)
        {
            string[] namesplit  = names.Split('_');
            // After N_PIN there may be some text that can't be split like this.
            // But I dont know what you want to do if its like this.
            // So I'm not adding any specific processing of that error here.
            Pins.Add(new Pin {X = px, Y = py, ComponentName = namesplit[0], Name = namesplit[1]});
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
        /// <param name="degree"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Turn(float degreeTurn, float xTurn, float yTurn)
        {
            foreach(Pin pin in Pins)
            {
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

            using (StreamReader sr = new StreamReader(readPath))
            {
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    string pinDesc = fileLine.Replace("N_PIN ", "");
                    if(fileLine.Length > pinDesc.Length)
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
                                if (TryParseGBFloat(px, out pxf) && TryParseGBFloat(py, out pyf))
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

        public static bool TryParseGBFloat(string txt, out float number)
        {
            return float.TryParse(txt, NumberStyles.Float, CultureInfo.GetCultureInfo("en-GB"), out number);
        }
    }
}
