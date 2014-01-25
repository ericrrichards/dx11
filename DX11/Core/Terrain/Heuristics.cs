using System;
using System.Drawing;
using System.Linq;

namespace Core.Terrain {
    /// <summary>
    /// A* distance heuristic functions, derived from http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
    /// </summary>
    public class Heuristics {
        /// <summary>
        /// Delegate to allow pathfinding classes to select appropriate heuristic dynamically
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="goal">End position</param>
        /// <returns>Distance calculated by the heuristic</returns>
        public delegate float Distance(Point start, Point goal);
        /// <summary>
        /// Manhattan distance between two points, i.e distance on a square grid where one can only travel in horizontal and vertical directions
        /// http://en.wiktionary.org/wiki/Manhattan_distance
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static float ManhattanDistance(Point start, Point goal) {
            var dx = Math.Abs(start.X - goal.X);
            var dy = Math.Abs(start.Y - goal.Y);
            var h = dx + dy;

            return h;
        }
        /// <summary>
        /// Chebyshev distance between two points, i.e. distance on a square grid where travel is permitted horizontally, vertically, and along diagonals
        /// Distance in all directions is considered to have the same cost, as on a chessboard
        /// http://en.wikipedia.org/wiki/Chebyshev_distance
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static float DiagonalDistance(Point start, Point goal) {
            var dx = Math.Abs(start.X - goal.X);
            var dy = Math.Abs(start.Y - goal.Y);
            var h = Math.Max(dx, dy);

            return h;
        }
        /// <summary>
        /// Chebyshev distance between two points, i.e. distance on a square grid where travel is permitted horizontally, vertically, and along diagonals
        /// Travel along the diagonals is considered to be slightly more expensive than along ranks and files, as actual distance along diagonals 
        /// is sqrt(2), rather than 1 along ranks and files.
        /// http://en.wikipedia.org/wiki/Chebyshev_distance
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static float DiagonalDistance2(Point start, Point goal) {
            var dx = Math.Abs(start.X - goal.X);
            var dy = Math.Abs(start.Y - goal.Y);
            var h = (dx + dy) + (MathF.Sqrt2 - 2) * Math.Min(dx, dy);

            return h;
        }

        /// <summary>
        /// Euclidean distance between two points, for use when travel in any direction is allowed, rather than just along ranks, files and diagonals
        /// http://en.wikipedia.org/wiki/Euclidean_distance
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static float EuclideanDistance(Point start, Point goal) {
            var dx = (goal.X - start.X);
            var dy = (goal.Y - start.Y);
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Hexagonal distance between two points
        /// Assumes a hexagonal coordinate system in which one axis lies along the diagonals of the hexes
        /// http://3dmdesign.com/development/hexmap-coordinates-the-easy-way
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public static float HexDistance(Point start, Point goal) {
            var dx = start.X - goal.X;
            var dy = start.Y - goal.Y;
            var dz = dx - dy;
            var h = Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz)));

            return h;
        }
    }
}