using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace PinBoard
{
    /// <summary>
    /// Viewing Board
    /// </summary>
    public class BoardView
    {
        public Board board;

        // zooming coeficient to improve view
        public float Zoom = 1.5f;
        private readonly float minZoom = 0.3f;
        private readonly float maxZoom = 10;
        private readonly float zoomCoef = 1.05f;

        public BoardView(Board b)
        {
            board = b;
        }

        /// <summary>
        /// Width multiplied by zoom
        /// </summary>
        public float Width { get { return (board.MaxX - board.MinX + 1) * Zoom; } }

        /// <summary>
        /// Height multiplied by zoom
        /// </summary>
        public float Height { get { return (board.MaxY - board.MinY + 1) * Zoom; } }

        /// <summary>
        /// transform board coordinates to canvas coordinates
        /// </summary>
        /// <param name="bx"></param>
        /// <returns></returns>
        public float BoardToCanvasX(float bx)
        {
            // as screen coordinates are mirrored along Y axis, some transformations are necessary
            return (bx - board.MinX) * Zoom;
        }

        /// <summary>
        /// transform board coordinates to canvas coordinates
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public float BoardToCanvasY(float by)
        {
            return (board.MaxY - by) * Zoom;
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
            float x = (float)(board.MinX + cx / Zoom);
            float y = (float)(board.MaxY - cy / Zoom);
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

        public void ZoomOut()
        {
            if (Zoom > minZoom)
                Zoom /= zoomCoef;
        }

        public void ZoomIn()
        {
            if (Zoom < maxZoom)
                Zoom *= zoomCoef;
        }
    }
}
