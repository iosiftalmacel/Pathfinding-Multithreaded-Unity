//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.Profiling;

//[CustomEditor(typeof(PathFindingManager))]
//public class PathFindingManagerEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        PathFindingManager myScript = (PathFindingManager)target;
//        if (GUILayout.Button("Build Object"))
//        {
//            myScript.Awake();
//        }
//    }
//}
//public class PathFindingManager : MonoBehaviour {
//    public static PathFindingManager instance;
//    public GridCell[,] map;
//    int[,] computations = new int[,]
//    {
//        { 0, 1 },
//        { 1, 1 },
//        { 1, 0 },
//        { 1, -1 },
//        { 0, -1 },
//        { -1, -1 },
//        { -1, 0 },
//        { -1, 1 },
//    };
//    public enum PathState { Idle, ReadyToCompute, Computing }

//    [Serializable]
//    public class GridCell
//    {
//        public int x;
//        public int y;
//        public Vector3 position;
//        public bool traversable;
//    }
//    [Serializable]
//    public class GridCellData
//    {
//        public GridCellData prev;
//        public GridCellData foward;
//        public GridCell cell;
//        public int distanceFromStart;
//        public int distanceFromEnd;
//        public int weight
//        {
//            get { return distanceFromStart + distanceFromEnd; }
//        }
//    }

//    public class Path
//    {
//        public GridCell start;
//        public GridCell end;

//        public GridCellData startCellData;
//        public PathState state;
//    }

//    public void Awake()
//    {
//        instance = this;

//        map = new GridCell[100, 100];

//        for (int i = 0; i < map.GetLength(1); i++)
//        {
//            for (int j = 0; j < map.GetLength(0); j++)
//            {
//                map[j, i] = new GridCell();
//                map[j, i].x = i;
//                map[j, i].y = j;
//                map[j, i].traversable =  UnityEngine.Random.Range(0, 5) != 1;
//            }
//        }

//        Profiler.BeginSample("Test");
//        Path path = new Path();
//        UnityEngine.Random.Range(0, 10);
//        path.start = map[UnityEngine.Random.Range(9, 10), UnityEngine.Random.Range(9, 10)];
//        path.end = map[UnityEngine.Random.Range(99, 100), UnityEngine.Random.Range(99, 100)];

//        ComputePath(path);
//        Profiler.EndSample();

//    }

//    private void Update()
//    {
//        Awake();
//    }

//    //public void StartPathComputing(Path path, Action finishCallback)
//    //{
//    //    lock (path)
//    //    {
//    //        if (path.state == PathState.Computing)
//    //        {
//    //            path.state = PathState.ReadyToCompute;
//    //        }
//    //        else
//    //        {
//    //            ThreadManager.instance.RunOnChildThread(() =>
//    //            {
//    //                ComputePath(path, finishCallback);
//    //            });
//    //        }
//    //    }
//    //}

//    void OnDrawGizmos()
//    {
//        if (map == null)
//            return;

//        for (int i = 0; i < map.GetLength(0); i++)
//        {
//            for (int j = 0; j < map.GetLength(1); j++)
//            {
//                Gizmos.color = map[i, j].traversable ? Color.green : Color.red;
//                Gizmos.DrawCube(new Vector3(map[i, j].x, map[i, j].y, 0), Vector3.one);
//            }
//        }


//        for (int i = 0; i < analized.Count; i++)
//        {
//            Gizmos.color = Color.magenta;
//            Gizmos.DrawCube(new Vector3(analized[i].cell.x, analized[i].cell.y, 1), Vector3.one);
//        }


//        for (int i = 0; i < percorso.Count; i++)
//        {
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawCube(new Vector3(percorso[i].cell.x, percorso[i].cell.y, 1), Vector3.one);
//        }
//    }
//    public List<GridCellData> percorso;

//    public GridCellData[,] mapData;
//    public List<GridCellData> priorityQueue;
//    public List<GridCellData> analized;
//    public void ComputePath(Path path/*, Action finishCallback*/)
//    {
//        lock (path)
//        {
//            path.state = PathState.Computing;
//        }
//        percorso = new List<GridCellData>();
//        analized = new List<GridCellData>();
//        path.startCellData = new GridCellData();
//        path.startCellData.cell = path.start;
//        path.startCellData.distanceFromEnd = GetDistanceFromEnd(path.start, path.end);
//        path.startCellData.distanceFromStart = 0;

//        for (int i = 0; i < computations.GetLength(0); i++)
//        {
//            int x = path.end.x + computations[i, 1];
//            int y = path.end.y + computations[i, 0];
//            if (map.GetLength(0) > y && y >= 0 && map.GetLength(1) > x && x >= 0) {
//                if (!IsTraversable(map[y, x], path.end))
//                    return;
//            } 
//        }

//        mapData = new GridCellData[map.GetLength(0), map.GetLength(1)];
//        mapData[path.start.x, path.start.y] = path.startCellData;

//        priorityQueue = new List<GridCellData>();
//        priorityQueue.Add(path.startCellData);
//        int m = 0;
//        while (path.state == PathState.Computing && priorityQueue.Count > 0 && m < 5000)
//        {
//            m++;

//            Hashtable s = new Hashtable();
//            s[""] = 2;
//            GridCellData computingCell = priorityQueue[0];
//            priorityQueue.RemoveAt(0);

//            for (int i = 0; i < computations.GetLength(0); i++)
//            {
//                int weight = (Mathf.Abs(computations[i, 0]) + Mathf.Abs(computations[i, 1])) == 2 ? 14 : 10;
//                int x = computingCell.cell.x + computations[i, 1];
//                int y = computingCell.cell.y + computations[i, 0];

//                if (map.GetLength(0) > y && y >= 0 && map.GetLength(1) > x && x >= 0 && IsTraversable(computingCell.cell, map[y, x]))
//                {
//                    GridCellData current = mapData[y, x];
//                    if (current == null)
//                    {
//                        current = mapData[y, x] = new GridCellData();
//                        current.prev = computingCell;
//                        current.cell = map[y, x];
//                        current.distanceFromEnd = GetDistanceFromEnd(current.cell, path.end);
//                        current.distanceFromStart = computingCell.distanceFromStart + weight;
//                        AddGridCellDataToQueue(priorityQueue, current);
//                        analized.Add(current);
//                    }
//                    else if (current.distanceFromStart > computingCell.distanceFromStart + weight && current.prev != path.startCellData)
//                    {
//                        current.prev = computingCell;
//                        current.distanceFromStart = computingCell.distanceFromStart + weight;
//                        priorityQueue.Remove(current);
//                        AddGridCellDataToQueue(priorityQueue, current);
//                    }

//                    if(current.cell == path.end)
//                    {
//                        percorso = new List<GridCellData>();
//                        while(current.prev != null)
//                        {
//                            percorso.Add(current);
//                            //Debug.LogError(current.cell.x + " " + current.cell.y);
//                            current = current.prev;
//                        }
//                        priorityQueue.Clear();
//                        //Debug.LogError("finish");
//                        m = 102;
//                        //Debug.Break();
//                        return;
//                    }
//                }
//            }

//        }
//        Debug.LogError(m);
//        //if (path.state == PathState.ReadyToCompute)
//        //    ComputePath(path, finishCallback);
//    }

//    public void AddGridCellDataToQueue(List<GridCellData> list, GridCellData data)
//    {
//        int indexToInsert = 0;
//        for (int i = 0; i < list.Count; i++)
//        {
//            if (list[i].weight < data.weight || (list[i].weight == data.weight && list[i].distanceFromEnd < data.distanceFromEnd))
//                indexToInsert++;
//            else
//                break;
//        }
//        list.Insert(indexToInsert, data);
//    }

//    public bool IsTraversable(GridCell from, GridCell to)
//    {
//        if (!to.traversable)
//            return false;
//        else if (from.x == to.x || from.y == to.y)
//            return true;
//        else if (map[to.y, from.x].traversable && map[from.y, to.x].traversable)
//            return true;

//        return false;
//    }

//    public int GetDistanceFromEnd(GridCell cell, GridCell end)
//    {
//        int diffX = Mathf.Abs(cell.x - end.x);
//        int diffY = Mathf.Abs(cell.y - end.y);

//        return Mathf.Min(diffY, diffX) * 14 + Mathf.Max(diffY, diffX) * 10;
//    }

//}
