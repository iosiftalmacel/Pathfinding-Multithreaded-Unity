using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Map
{
    public class PathAgent : MonoBehaviour
    {
        public int mapSize = 300;
        public bool threadWait;
        public bool multiThread;
        public bool draw;
        public bool customMap;
        public PathType pathType;
        public bool stopped;
        public DrawMap drawer;


        PathData pathData = new PathData();
        GridCell[][] map = new GridCell[300][];

        private void Start()
        {
            if (customMap)
            {
                
                MapManager.instance.GetCustomMap(out map);
                pathData.map = map;
            }
            else GenerateMap();
        }

        public void GoToPosition(int x, int y, bool loop)
        {
            GridCell start = map[Random.Range(0, map.Length)][Random.Range(0, map.Length)];
            GridCell end = map[y][x];

            //start = map[20][20];
            //end = map[200][150];

            stopped = false;
            pathData.multiThread = multiThread;
            pathData.waitThread = threadWait;
            pathData.pathType = pathType;

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
                            MapManager.instance.RegenerateMap(map, mapSize);
                        }

                        GoToPosition(Random.Range(0, mapSize), Random.Range(0, mapSize), loop);
                    }
                }

            });
            drawer.pathData = pathData;
        }

        void GenerateMap()
        {
            map = new GridCell[mapSize][];
            MapManager.instance.RegenerateMap(map, mapSize);
            pathData.map = map;
        }
    }
}
