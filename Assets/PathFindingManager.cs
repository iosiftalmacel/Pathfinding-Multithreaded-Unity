using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Map
{
    public enum PathFinishState { Aborted, Failed, Success }
    public enum PathState { Idle, ReadyToCompute, Computing }

    public class GridCell
    {
        public int x;
        public int y;
        public Vector3 position;
        public int traverseWeight;
    }

    public class GridCellData
    {
        public GridCellData prev;
        public GridCellData foward;
        public GridCellData nextInQueue;
        public GridCellData prevInQueue;
        public GridCell cell;
        public bool computed;
        public int distanceFromStart;
        public int distanceFromEnd;
        public int weight;
        public int queueWeight;
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
            startCellData.weight = startCellData.distanceFromEnd + startCellData.cell.traverseWeight;
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
                    ThreadManager.instance.RunOnChildThread(() =>
                    {
                        Stopwatch stopwatch = null;
                        if (!path.waitThread)
                        {
                            stopwatch = new Stopwatch();
                            stopwatch.Start();
                        }

                        ComputePath(map, path, finishCallback);

                        if (!path.waitThread && stopwatch != null)
                        {
                            stopwatch.Stop();
                            UnityEngine.Debug.LogError(stopwatch.ElapsedMilliseconds);
                        }
                    });
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



        public void ComputePath(GridCell[][] map, PathData pathData, Action<bool> finishCallback)
        {
            lock (pathData) { pathData.state = PathState.Computing; }
            pathData.PrepareToCompute(map);

            GridCellData computingCell = pathData.startCellData;

            if (pathData.start.traverseWeight == -1 || pathData.end.traverseWeight == -1 || pathData.end == pathData.startCellData.cell) pathData.queue.Clear();

            while (pathData.state == PathState.Computing && pathData.queue.Count > 0)
            {
                if (pathData.waitThread) Thread.Sleep(1);

                GetGridCellDataFromQueue(pathData, ref computingCell);

                for (int i = 0; i < computations.Length; i++)
                {
                    int weight = i != 0 && i % 2 == 1 ? 14 : 10;
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

        public bool ComputeCell(PathData path, GridCellData computingCell, GridCellData current, int transitionWeight)
        {
            if (!current.computed)
            {
                current.prev = computingCell;
                current.distanceFromEnd = GetDistanceFromEnd(current.cell, path.end);
                current.distanceFromStart = computingCell.distanceFromStart + transitionWeight;
                current.weight = current.distanceFromEnd + current.distanceFromStart + current.cell.traverseWeight;
                current.queueWeight = current.weight + current.distanceFromEnd;
              
                current.computed = true;
                path.analizedCells.Add(current);

                AddGridCellDataToQueue(path, current);
            }
            //else if (current.distanceFromStart > computingCell.distanceFromStart + weight)
            //{
            //    current.prev = computingCell;
            //    current.distanceFromStart = computingCell.distanceFromStart + weight;
            //    current.computed = true;
            //    //AddGridCellDataToQueue(ref queueHead, current);
            //}

            if (current.cell == path.end)
                return true;
            else
                return false;
        }

        private void GetGridCellDataFromQueue(PathData path, ref GridCellData computingCell)
        {
            computingCell = path.queue[path.queue.Count - 1];
            path.queue.RemoveAt(path.queue.Count - 1);
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
                // depending on what you want you may use "data.weight < path.queue[index].weight"
                // This way is faster but is more linear
                if (data.queueWeight < path.queue[index].queueWeight) 
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

        internal static int GetDistanceFromEnd(GridCell cell, GridCell end)
        {
            int diffX = Mathf.Abs(cell.x - end.x);
            int diffY = Mathf.Abs(cell.y - end.y);

            return Mathf.Min(diffY, diffX) * 14 + Mathf.Max(diffY, diffX) * 10;
        }

    }

}
