using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Map
{
    public enum PathFinishState { Aborted, Failed, Success }
    public enum PathState { Idle, ReadyToCompute, Computing }

    public class GridCell
    {
        public int x;
        public int y;
        public Vector3 position;
        public float traverseWeight;
        public int color;
    }

    public class GridCellData
    {
        public GridCellData prev;
        public GridCellData foward;
        public GridCellData nextInQueue;
        public GridCellData prevInQueue;
        public GridCell cell;
        public bool computed;
        public float distanceFromStart;
        public float distanceFromEnd;
        public float weightDijkstra;
        public float weightGreedyFirst;
        public float weightAStar;

        public float GetWeight(PathType type)
        {
            return (new float[] { weightAStar, weightDijkstra, weightGreedyFirst })[(int)type];
        }
    }

    public enum PathType
    {
        AStar,
        Dijkstra,
        GreedyFirst
    }

    public class PathData
    {
        public volatile PathState state;
        public GridCell start;
        public GridCell end;
        public List<GridCell> path;

        public GridCellData startCellData;

        public GridCellData[][] mapData;
        public GridCell[][] map;

        public List<GridCellData> queue;
   
        public List<GridCellData> analizedCells; // just for show
        public bool waitThread; // just for show
        public bool multiThread;
        public PathType pathType;

        public void PrepareToCompute(GridCell[][] map)
        {
            this.map = map;

            if (mapData == null || mapData.Length != map.Length)
                mapData = new GridCellData[map.Length][];

            for (int i = 0; i < mapData.Length; i++)
            {
                if (mapData[i] == null)
                    mapData[i] = new GridCellData[map.Length];

                for (int j = 0; j < mapData.Length; j++)
                {
                    if (mapData[i][j] == null)
                        mapData[i][j] = new GridCellData();

                    mapData[i][j].cell = map[i][j];
                    mapData[i][j].computed = false;
                    mapData[i][j].nextInQueue = null;
                    mapData[i][j].prevInQueue = null;
                    mapData[i][j].prev = null;
                    mapData[i][j].foward = null;
                }
            }

            startCellData = mapData[start.y][start.x];
            startCellData.distanceFromEnd = PathFindingManager.GetDistanceFromEnd(start, end);
            startCellData.distanceFromStart = 0;
            startCellData.weightDijkstra = startCellData.distanceFromStart + startCellData.cell.traverseWeight;
            startCellData.weightGreedyFirst = startCellData.distanceFromEnd + startCellData.cell.traverseWeight;
            startCellData.weightAStar = startCellData.distanceFromEnd + startCellData.distanceFromStart + startCellData.cell.traverseWeight;
            startCellData.computed = true;

            if (analizedCells == null)
                analizedCells = new List<GridCellData>();
            if (path == null)
                path = new List<GridCell>();
            if (queue == null)
                queue = new List<GridCellData>();

            analizedCells.Clear();
            path.Clear();
            queue.Clear();
            queue.Add(startCellData);
        }
    }


    public class PathFindingManager : MonoBehaviour
    {
        public static PathFindingManager instance;

        int[][] computations = new int[][]
        {
            new int[]{ 0, 1 },
            new int[]{ 1, 1 },
            new int[]{ 1, 0 },
            new int[]{ 1, -1 },
            new int[]{ 0, -1 },
            new int[]{ -1, -1 },
            new int[]{ -1, 0 },
            new int[]{ -1, 1 },
        };

        private void Awake()
        {
            instance = this;
        }

        public void StartPathComputing(GridCell[][] map, GridCell start, GridCell end, PathData path, Action<bool> finishCallback)
        {
            lock (path)
            {
                if (path.state == PathState.Idle)
                {
                    path.state = PathState.Computing;
                    Action threadedAction = () => ComputePath(map, path, finishCallback);
                    Action coroutineAction = () => StartCoroutine(DelayedComputePath(map, path, finishCallback));
                    if (path.multiThread) ThreadManager.instance.RunOnChildThread(threadedAction);
                    else coroutineAction();
                }
                else if (path.state == PathState.Computing)
                {
                    UnityEngine.Debug.LogError("recompute");
                    path.state = PathState.ReadyToCompute;
                }
                path.start = start;
                path.end = end;
                path.map = map;
            }
        }

        IEnumerator DelayedComputePath(GridCell[][] map, PathData pathData, Action<bool> finishCallback)
        {
            yield return null;
            //System.Random rnd = new System.Random();
            pathData.state = PathState.Computing; 
            pathData.PrepareToCompute(map);
            GridCellData computingCell = pathData.startCellData;

            if (pathData.start.traverseWeight == -1 || pathData.end.traverseWeight == -1 || pathData.end == pathData.startCellData.cell) pathData.queue.Clear();

            int loops = 0;
            while (pathData.state == PathState.Computing && pathData.queue.Count > 0)
            {
                loops++;
                if (pathData.waitThread && loops % 20 == 0) yield return null;

                computingCell = pathData.queue[pathData.queue.Count - 1];
                pathData.queue.RemoveAt(pathData.queue.Count - 1);
                //computations = computations.OrderBy((x) => rnd.Next()).ToArray();
                for (int i = 0; i < computations.Length; i++)
                {
                    var comp = computations[(i + Time.frameCount) % computations.Length];
                    float weight = comp[1] == comp[1] || comp[1] == -comp[1] ? 1.45f : 1;
                    int x = computingCell.cell.x + comp[1];
                    int y = computingCell.cell.y + comp[0];

                    if (map.Length > y && y >= 0 && map.Length > x && x >= 0 && IsTraversable(map, computingCell.cell, map[y][x]))
                    {
                        GridCellData current = pathData.mapData[y][x];
                        if (ComputeCell(pathData, computingCell, current, weight))
                        {
                            pathData.queue.Clear();
                            computingCell = current;
                            break;
                        }
                    }
                }
            }

            if (computingCell.cell == pathData.end)
            {
                PreparePath(pathData, computingCell);
                if (pathData.waitThread) yield return new WaitForSeconds(1);
            }

            if (pathData.state == PathState.Computing)
            {
                pathData.state = PathState.Idle;
                finishCallback(computingCell.cell == pathData.end);
            } else if (pathData.state == PathState.ReadyToCompute)
            {
                StartCoroutine(DelayedComputePath(map, pathData, finishCallback));
            }
        }

        public void ComputePath(GridCell[][] map, PathData pathData, Action<bool> finishCallback)
        {
            lock (pathData) { pathData.state = PathState.Computing; }
            pathData.PrepareToCompute(map);

            GridCellData computingCell = pathData.startCellData;

            if (pathData.start.traverseWeight == -1 || pathData.end.traverseWeight == -1 || pathData.end == pathData.startCellData.cell) pathData.queue.Clear();

            int loops = 0;
            while (pathData.state == PathState.Computing && pathData.queue.Count > 0)
            {
                loops++;
                if (pathData.waitThread && loops % 3 == 0) Thread.Sleep(1);


                computingCell = pathData.queue[pathData.queue.Count - 1];
                pathData.queue.RemoveAt(pathData.queue.Count - 1);

                for (int i = 0; i < computations.Length; i++)
                {
                    int weight = i != 0 && i % 2 == 1 ? UnityEngine.Random.Range(14, 17) : 10;
                    int x = computingCell.cell.x + computations[i][1];
                    int y = computingCell.cell.y + computations[i][0];

                    if (map.Length > y && y >= 0 && map.Length > x && x >= 0 && /*map[y][x].traverseWeight >= 0 */IsTraversable(map, computingCell.cell, map[y][x]))
                    {
                        GridCellData current = pathData.mapData[y][x];
                        if (ComputeCell(pathData, computingCell, current, weight))
                        {
                            pathData.queue.Clear();
                            computingCell = current;
                            break;
                        }
                    }
                }
            }

            if (computingCell.cell == pathData.end)
            {
                PreparePath(pathData, computingCell);
                if (pathData.waitThread) Thread.Sleep(1000);
            }

            lock (pathData)
            {
                if (pathData.state == PathState.Computing)
                {
                    pathData.state = PathState.Idle;
                    ThreadManager.instance.RunOnMainThread(() => finishCallback(computingCell.cell == pathData.end));
                }

            }
            if (pathData.state == PathState.ReadyToCompute)
            {
                ComputePath(map, pathData, finishCallback);
            }
        }

        public void PreparePath(PathData pathData, GridCellData headPathData)
        {
            while (headPathData != null)
            {
                pathData.path.Add(headPathData.cell);
                headPathData = headPathData.prev;
                if (headPathData != null && (headPathData.cell == pathData.start || headPathData.cell == pathData.end))
                    break;
            }
        }

        public bool ComputeCell(PathData path, GridCellData computingCell, GridCellData current, float transitionWeight)
        {
            if (!current.computed)
            {
                current.prev = computingCell;
                current.distanceFromEnd = GetDistanceFromEnd(current.cell, path.end) * 8;
                current.distanceFromStart = computingCell.distanceFromStart + current.cell.traverseWeight * transitionWeight;
                current.weightDijkstra = current.distanceFromStart;
                current.weightGreedyFirst = current.distanceFromEnd + current.cell.traverseWeight * 40;
                current.weightAStar = current.distanceFromEnd + current.distanceFromStart;

                current.computed = true;
                path.analizedCells.Add(current);

                AddGridCellDataToQueue(path, current);
            }
            {
                //else
                //{
                //    float distanceFromStart = computingCell.distanceFromStart + current.cell.traverseWeight * transitionWeight;
                //    if (current.distanceFromStart > distanceFromStart)
                //    {
                //        current.prev = computingCell;
                //        current.distanceFromStart = distanceFromStart;
                //        current.weightDijkstra = current.distanceFromStart;
                //        current.weightGreedyFirst = current.distanceFromEnd + current.cell.traverseWeight * 40;
                //        current.weightAStar = current.distanceFromEnd + distanceFromStart;
                //        AddGridCellDataToQueue(path, current);
                //    }
                //}
            }



            if (current.cell == path.end)
                return true;
            else
                return false;
        }


        private void AddGridCellDataToQueue(PathData path, GridCellData data)
        {
            LinkedList<GridCellData> datas = new LinkedList<GridCellData>();
            
            int start = 0;
            int end = path.queue.Count;
            int index = 0;

            while (start < end)
            {
                index = (start + end) / 2;

                PathType type = path.pathType;
                if (data.GetWeight(type) < path.queue[index].GetWeight(type)) 
                {
                    start = index + 1;
                }
                else
                {
                    end = index;
                }
            }
            path.queue.Insert(index, data);
        }

        private bool IsTraversable(GridCell[][] map, GridCell from, GridCell to)
        {
            if (to.traverseWeight == -1)
                return false;
            else if (from.x == to.x || from.y == to.y)
                return true;
            else if (map[to.y][from.x].traverseWeight >= 0 && map[from.y][to.x].traverseWeight >= 0)
                return true;

            return false;
        }

        internal static float GetDistanceFromEnd(GridCell cell, GridCell end)
        {
            //int diffX = Mathf.Abs(cell.x - end.x);
            //int diffY = Mathf.Abs(cell.y - end.y);
            //return diffX + diffY;
            var x = Math.Abs(end.x - cell.x);
            var y = Math.Abs(end.y - cell.y);
            var max = x > y ? x : y;
            var min = x < y ? x : y;

            if (min < 0.04142135 * max )
                return  0.99f * max + 0.197f * min;
            else
                return 0.84f * max + 0.561f * min;

            //(float)Math.Pow(end.x - cell.x, 2) + (float)Math.Pow(end.y - cell.y, 2)
            //Mathf.Min(diffY, diffX) * 14 + Mathf.Max(diffY, diffX) * 10;
        }

    }

}
