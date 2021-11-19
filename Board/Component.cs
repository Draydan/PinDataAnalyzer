using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinBoard
{
    /// <summary>
    /// Component class
    /// </summary>
    class Component
    {
        public string Name;

        /// <summary>
        /// Pins to the Component
        /// </summary>
        public List<Pin> Pins;

        /// <summary>
        /// Calculate the centre of gravity of all the points on the board.
        /// </summary>
        /// <returns></returns>
        Point CenterOfGravity()
        {
            return new Point(
                (int)Math.Round(Pins.Average(pin => pin.X)),
                (int)Math.Round(Pins.Average(pin => pin.Y))
                );
        }
    }
}
