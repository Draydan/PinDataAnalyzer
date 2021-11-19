using NUnit.Framework;
using PinBoard;
using System;
using System.Drawing;

namespace TestProject
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// Testing Board Turns
        /// </summary>
        [Test]
        public void Test1_CheckBoardTurn()
        {
            Board brd = new Board();
            brd.Pins.Add(new Pin { X = 20, Y = 20, ComponentName = "a1", Name = "p1" });

            float degree = 45;
            brd.Turn(degree, 10, 10);
            Pin pin = brd.Pins[0];
            Assert.AreEqual(10, pin.X, $"x after {degree} degree turn turned out wrong");
            Assert.AreEqual(24.14, Math.Round(pin.Y, 2), $"Y after {degree} degree turn turned out wrong");            

            degree = -45;
            brd.Turn(degree, 10, 10);            
            Assert.AreEqual(20, pin.X, $"X after {degree} degree turn turned out wrong");
            Assert.AreEqual(20, pin.Y, $"Y after {degree} degree turn turned out wrong");

            degree = 90;
            brd.Turn(degree, 10, 10);
            Assert.AreEqual(0, pin.X, $"X after {degree} degree turn turned out wrong");
            Assert.AreEqual(20, pin.Y, $"Y after {degree} degree turn turned out wrong");
            //Assert.Fail("feil");
        }

        /// <summary>
        /// Testing center of gravity of board pins
        /// </summary>
        [Test]
        public void Test2_CheckBoardPinsCenter()
        {
            Board brd = new Board();
            brd.Pins.Add(new Pin { X = 20, Y = 20, ComponentName = "a1", Name = "p1" });
            brd.Pins.Add(new Pin { X = 30, Y = 10, ComponentName = "a1", Name = "p2" });
            PointF cnt = brd.CenterOfGravity();
            Assert.AreEqual(25, cnt.X, "X of center turned out wrong");
            Assert.AreEqual(15, cnt.Y, "Y of center turned out wrong");
        }
    }
}