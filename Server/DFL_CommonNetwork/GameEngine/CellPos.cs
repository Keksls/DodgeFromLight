namespace DFLCommonNetwork.GameEngine
{
    public struct CellPos
    {
        public int X { get; set; }
        public int Y { get; set; }

        public CellPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(CellPos cell)
        {
            return cell.X == X && cell.Y == Y;
        }

        public bool Equals(int x, int y)
        {
            return x == X && y == Y;
        }

        public override string ToString()
        {
            return X + "_" + Y;
        }

        public bool IsInLine(CellPos cell)
        {
            return X == cell.X || Y == cell.Y;
        }

        public bool IsInOrientation(CellPos cell, Orientation orientation)
        {
            switch (orientation)
            {
                default:
                case Orientation.None:
                    return false;

                case Orientation.Down:
                    return cell.X == X && cell.Y > Y;

                case Orientation.Up:
                    return cell.X == X && cell.Y < Y;

                case Orientation.Right:
                    return cell.Y == Y && cell.X < X;

                case Orientation.Left:
                    return cell.Y == Y && cell.X > X;
            }
        }
    }
}