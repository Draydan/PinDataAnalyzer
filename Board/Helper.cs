using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinBoard
{
    public class Helper
    {
        /// <summary>
        /// Try Parse float with GB culture
        /// </summary>
        /// <param name="txt">input string with float number</param>
        /// <param name="number">output number as float</param>
        /// <returns>boolean success of operation</returns>
        public static bool TryParseGBFloat(string txt, out float number)
        {
            return float.TryParse(txt, NumberStyles.Float, CultureInfo.GetCultureInfo("en-GB"), out number);
        }

        /// <summary>
        /// Parse float with GB culture
        /// </summary>
        /// <param name="txt">input string with float number</param>        
        /// <returns>output number as float</returns>
        public static float ParseGBFloat(string txt)
        {
            return float.Parse(txt, NumberStyles.Float, CultureInfo.GetCultureInfo("en-GB"));
        }
    }
}
