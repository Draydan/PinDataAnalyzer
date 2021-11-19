using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                //pin.X = (float)(xTurn + xRel * Math.Cos(rad) + yRel * Math.Sin(rad));
                //pin.Y = (float)(yTurn + yRel * Math.Cos(rad) - xRel * Math.Sin(rad));
                pin.X = (float)(xTurn + xRel * Math.Cos(rad) - yRel * Math.Sin(rad));
                pin.Y = (float)(yTurn + xRel * Math.Sin(rad) + yRel * Math.Cos(rad));
            }
        }
    }
}
