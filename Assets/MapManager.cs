using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Map
{
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapManager myScript = (MapManager)target;
            if (GUILayout.Button("Regenerate"))
            {
                myScript.RegenerateMap(myScript.GetMap());
            }
        }
    }
    public class MapManager : MonoBehaviour
    {
        public static MapManager instance;
        public Texture2D texture;
        private GridCell[][] map = new GridCell[300][];

        void Awake()
        {
            instance = this;
            RegenerateMap(map);
        }

        public void RegenerateMap(GridCell[][] map)
        {
            if (map == null)
                map = new GridCell[300][];
            for (int i = 0; i < map.Length; i++)
            {
                if(map[i] == null)
                    map[i] = new GridCell[map.Length];

                for (int j = 0; j < map.Length; j++)
                {
                    if (map[i][j] == null)
                        map[i][j] = new GridCell();
                    map[i][j].x = j;
                    map[i][j].y = i;
                    map[i][j].traverseWeight = Random.Range(0, 5) != 2 ? 1 : -1;
                }
            }
        }

        public void GetCustomMap(out GridCell[][] map)
        {
            map = new GridCell[texture.width][];
            for (int i = 0; i < map.Length; i++)
            {
                map[i] = new GridCell[texture.width];

                for (int j = 0; j < map.Length; j++)
                {
                    map[i][j] = new GridCell();
                    map[i][j].x = j;
                    map[i][j].y = i;
                    map[i][j].traverseWeight = (int)(texture.GetPixel(i, j).grayscale * 1000);
                }
            }
        }

        // Update is called once per frame
        public GridCell[][] GetMap()
        {
            return map;
        }
    }
}


