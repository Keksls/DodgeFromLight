using System.Collections.Generic;
using System;
using System.Linq;
using DFLCommonNetwork.GameEngine;

namespace SlyvekGameEngine.GameEngine.Map
{
    public static class PathFinding
    {
        /// <summary>
        /// Find a Path between two points.
        /// </summary>
        /// <PFram name="grid">Grid to search.</PFram>
        /// <PFram name="startPos">Starting position.</PFram>
        /// <PFram name="targetPos">Ending position.</PFram>
        /// <PFram name="ignorePrices">If true, will ignore tile price (how much it "cost" to walk on).</PFram>
        /// <returns>List of points that represent the Path to walk.</returns>
        public static List<CellPos> FindPath(Grid map, Cell startPos, Cell targetPos, bool Horizontal, bool ignorePrices = false, bool ignoreUnwalkable = false)
        {
            // find Path
            List<Cell> nodes_Path = _ImpFindPath(map, startPos, targetPos, Horizontal, ignorePrices, ignoreUnwalkable);
            // convert to a list of points and return
            List<CellPos> ret = new List<CellPos>();

            if (nodes_Path != null)
            {
                nodes_Path.Insert(0, map.Cells[startPos.X, startPos.Y]);
                for (int i = 1; i < nodes_Path.Count; i++)
                {
                    ret.Add(new CellPos(nodes_Path[i].X, nodes_Path[i].Y));
                }
            }
            return ret;
        }
        public static List<CellPos> FindPathMonster(Grid map, Cell startPos, Cell targetPos, bool Horizontal, bool ignorePrices = false, bool ignoreUnwalkable = false)
        {
            // find Path
            bool tmp = map.Cells[targetPos.X, targetPos.Y].Walkable;
            map.Cells[targetPos.X, targetPos.Y].Walkable = true;
            List<Cell> nodes_Path = _ImpFindPath(map, startPos, targetPos, Horizontal, ignorePrices, ignoreUnwalkable);
            map.Cells[targetPos.X, targetPos.Y].Walkable = tmp;

            // convert to a list of points and return
            List<CellPos> ret = new List<CellPos>();
            if (nodes_Path != null)
            {
                nodes_Path.Insert(0, map.Cells[startPos.X, startPos.Y]);
                for (int i = 1; i < nodes_Path.Count; i++)
                {
                    ret.Add(new CellPos(nodes_Path[i].X, nodes_Path[i].Y));
                }
            }
            try
            {
                ret.Remove(ret.First(r => r.X == targetPos.X && r.Y == targetPos.Y));
            }
            catch { }
            return ret;
        }

        /// <summary>
        /// Internal function that implements the Path-finding algorithm.
        /// </summary>
        /// <PFram name="grid">Grid to search.</PFram>
        /// <PFram name="startPos">Starting position.</PFram>
        /// <PFram name="targetPos">Ending position.</PFram>
        /// <PFram name="ignorePrices">If true, will ignore tile price (how much it "cost" to walk on).</PFram>
        /// <returns>List of grid nodes that represent the Path to walk.</returns>
        private static List<Cell> _ImpFindPath(Grid map, Cell startNode, Cell targetNode, bool Horizontal = false, bool ignorePrices = false, bool ignoreUnwalkable = false)
        {
            List<Cell> openSet = new List<Cell>();
            HashSet<Cell> closedSet = new HashSet<Cell>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Cell currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                foreach (Cell neighbour in map.GetNeighbours(currentNode, map, Horizontal))
                {
                    if (!neighbour.IsWalkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) * (ignorePrices ? 1 : (int)(10.0f * ((neighbour.IsWalkable || ignoreUnwalkable) ? 1f : 0f)));
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrace Path between two points.
        /// </summary>
        /// <PFram name="startNode">Starting node.</PFram>
        /// <PFram name="endNode">Ending (target) node.</PFram>
        /// <returns>Retraced Path between nodes.</returns>
        private static List<Cell> RetracePath(Cell startNode, Cell endNode)
        {
            List<Cell> Path = new List<Cell>();
            Cell currentNode = endNode;

            while (currentNode != startNode)
            {
                Path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            Path.Reverse();
            return Path;
        }

        /// <summary>
        /// Get distance between two nodes.
        /// </summary>
        /// <PFram name="nodeA">First node.</PFram>
        /// <PFram name="nodeB">Second node.</PFram>
        /// <returns>Distance between nodes.</returns>
        private static int GetDistance(Cell nodeA, Cell nodeB)
        {
            int dstX = Math.Abs(nodeA.X - nodeB.X);
            int dstY = Math.Abs(nodeA.Y - nodeB.Y);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
}