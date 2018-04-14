using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Map
{
    [CustomEditor(typeof(PathAgent)), CanEditMultipleObjects]
    public class PathAgentEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PathAgent myScript = (PathAgent)target;
            if (GUILayout.Button("LoopComputing"))
            {
                myScript.GoToPosition(Random.Range(0, myScript.mapSize), Random.Range(0, myScript.mapSize), true);
            }
            if (GUILayout.Button("Compute once"))
            {
                myScript.GoToPosition(Random.Range(0, myScript.mapSize), Random.Range(0, myScript.mapSize), false);
            }

        }
    }


    public class PathAgent : MonoBehaviour
    {
        public int mapSize = 100;
        public bool threadWait;
        public bool draw;
        public bool customMap;
        bool stopped;
        PathData pathData = new PathData();
        GridCell[][] map;
        Stopwatch stopwatch;
        [TextArea(3, 10)]
        public string description;

        private void Start()
        {
            if (customMap)
            {
                MapManager.instance.GetCustomMap(out map);
                mapSize = map.Length;
                pathData.map = map;
            }
            else
            {
                map = new GridCell[mapSize][];
                MapManager.instance.RegenerateMap(map);
                pathData.map = map;
            }
            //GoToPosition(Random.Range(0, mapSize), Random.Range(0, mapSize), true);
        }

        public void GoToPosition(int x, int y, bool loop)
        {
            GridCell start = map[Random.Range(0, mapSize)][Random.Range(0, mapSize)];
            GridCell end = map[y][x];
            stopped = false;
            pathData.waitThread = threadWait;

            PathFindingManager.instance.StartPathComputing(map, start, end, pathData, (bool success) =>
            {
                if (pathData.state == PathState.Idle)
                {
                    if (loop && !stopped)
                    {
                        if (customMap)
                        {
                            MapManager.instance.GetCustomMap(out map);
                            mapSize = map.Length;
                        }
                        else
                        {
                            MapManager.instance.RegenerateMap(map);
                        }

                        GoToPosition(Random.Range(0, mapSize), Random.Range(0, mapSize), loop);
                    }
                }

            });
        }

        void OnDrawGizmos()
        {
            if (!draw)
                return;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (pathData.map == null)
                return;

            if (!customMap)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(new Vector3(pathData.map.Length / 2, pathData.map.Length / 2, 0), Vector3.one * pathData.map.Length);
            }

            for (int i = 0; i < pathData.map.Length; i++)
            {
                for (int j = 0; j < pathData.map.Length; j++)
                {
                    if (pathData.map[i][j] != null)
                    {
                        if (pathData.map[i][j].traverseWeight == -1)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawCube(new Vector3(pathData.map[i][j].x, pathData.map[i][j].y, 0), Vector3.one);
                        }
                        //else
                        //{
                        //    Gizmos.color = new Color((float)pathData.map[i][j].traverseWeight / 1000, (float)pathData.map[i][j].traverseWeight / 1000, (float)pathData.map[i][j].traverseWeight / 1000);
                        //    Gizmos.DrawCube(new Vector3(pathData.map[i][j].x, pathData.map[i][j].y, 0), Vector3.one);
                        //}
                    }

                }
            }

            if (pathData.analizedCells != null)
            {
                for (int i = 0; i < pathData.analizedCells.Count; i++)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(new Vector3(pathData.analizedCells[i].cell.x, pathData.analizedCells[i].cell.y, 1), Vector3.one);
                }
            }

            if (pathData.path != null)
            {
                for (int i = 0; i < pathData.path.Count; i++)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawCube(new Vector3(pathData.path[i].x, pathData.path[i].y, 1), Vector3.one * 3f);
                }
            }
            if(pathData.end != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawCube(new Vector3(pathData.end.x, pathData.end.y, 1), Vector3.one * 3f);
            }
        }

        public void LoopRandomPosition()
        {
            GoToPosition(Random.Range(0, mapSize), Random.Range(0, mapSize), true);
        }

        public void StopComputing()
        {
            stopped = true;
        }

    }



}
