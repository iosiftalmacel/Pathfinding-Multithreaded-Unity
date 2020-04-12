using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager instance;
        public Texture2D texture;
        private GridCell[][] map;

        private void Awake()
        {
            instance = this;
        }

        public void RegenerateMap(GridCell[][] map, int size)
        {
            var tileSizeOne = Random.Range(0.2f, 1f);
            var tileSizeTwo = Random.Range(7f, 12f);
            var tileSizeThree = Random.Range(1f, 4f);
            var tileSizeFour = Random.Range(2f, 5f);
            var random = Random.Range(0, 1000);

            if (map == null || map.Length != size)
                map = new GridCell[size][];
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
                    var one = Mathf.PerlinNoise(((float)(i + random) / (float)300) * tileSizeOne, ((float)(j + random) / (float)300) * tileSizeOne) / 2;
                    var two = Mathf.PerlinNoise(((float)(i + random) / (float)300) * tileSizeTwo, ((float)(j + random) / (float)300) * tileSizeTwo) / 1;
                    var three = Mathf.PerlinNoise(((float)(i + random) / (float)300) * tileSizeThree, ((float)(j + random) / (float)300) * tileSizeThree) ;
                    var four = Mathf.PerlinNoise(((float)(i + random) / (float)300) * tileSizeFour, ((float)(j + random) / (float)300) * tileSizeFour);
                    map[i][j].color = (int)Mathf.Lerp(0, 255, Mathf.Clamp((Mathf.Pow((four + one) / 2, two) + (three / 1.2f - 0.5f) * 2f), 0, 1));
                    map[i][j].traverseWeight = Mathf.Lerp(1, 20f, map[i][j].color / 255f);
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
                    map[i][j].color = (int)(texture.GetPixel(i, j).grayscale * 255);
                    map[i][j].traverseWeight = Mathf.Lerp(1, 20f, texture.GetPixel(i, j).grayscale);
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


