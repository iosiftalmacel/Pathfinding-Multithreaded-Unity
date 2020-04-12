using Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DrawMap : MonoBehaviour
{
    public Material pathMaterial;
    public PathData _pathData;
    public PathData pathData
    {
        get { return _pathData; }
        set { _pathData = value; drawMap = true; lastAnalisedCount = int.MaxValue; }
    }
    public RenderTexture mapTexture;
    public RenderTexture pathTexture;
    bool drawMap = true;
    int lastAnalisedCount = int.MaxValue;

    void OnPostRender()
    {
        if(drawMap) RenderMap();
        RenderPath();
    }

    void RenderMap()
    {
        RenderTexture.active = mapTexture;
        pathMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.width, 0);
        GL.Begin(GL.QUADS);

        GL.Color(Color.black);
        DrawSquare(0, 0, Screen.width);

        var size = (float)Screen.width / pathData.map.Length;

        for (int i = 0; i < pathData.map.Length; i++)
        {
            for (int j = 0; j < pathData.map.Length; j++)
            {
                if (pathData.map[i][j] != null)
                {
                    if (pathData.map[i][j].color == -1) GL.Color(Color.white);
                    else GL.Color(new Color(1f, 1f, 1f, Mathf.Lerp(0, 1, pathData.map[i][j].color / 255f)));

                    var x = pathData.map[i][j].x * size;
                    var y = pathData.map[i][j].y * size;

                    DrawSquare(x, y, size);
                }

            }
        }

        GL.End();
        GL.PopMatrix();
        drawMap = false;
    }

    void RenderPath()
    {
        bool redraw = pathData.analizedCells != null && lastAnalisedCount > pathData.analizedCells.Count ;
        RenderTexture.active = pathTexture;
        pathMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.width, 0);
        if(redraw) GL.Clear(true, true, Color.clear);
        GL.Begin(GL.QUADS);
        
        var size = (float)Screen.width / pathData.map.Length;

        if (pathData.analizedCells != null)
        {
            for (int i = (redraw ? 0 : lastAnalisedCount); i < pathData.analizedCells.Count; i++)
            {
                if (pathData.analizedCells.Count <= i) continue;
                GL.Color(new Color32(254, 233, 78, 255));

                var x = pathData.analizedCells[i].cell.x * size;
                var y = pathData.analizedCells[i].cell.y * size;

                DrawSquare(x, y, size);
            }
            lastAnalisedCount = pathData.analizedCells.Count;
        }
        

        if (pathData.path != null)
        {
            for (int i = 0; i < pathData.path.Count; i++)
            {
                GL.Color(new Color32(30, 170, 241, 255));

                var x = pathData.path[i].x * size;
                var y = pathData.path[i].y * size;

                DrawSquare(x, y, size * 2f, true);
            }
        }

        if (pathData.end != null)
        {
            GL.Color(new Color32(226, 32, 44, 255));

            var x = pathData.end.x * size;
            var y = pathData.end.y * size;

            DrawSquare(x, y, size * 4f, true);
        }

        GL.End();
        GL.PopMatrix();
    }

    void DrawSquare(float x, float y, float size, bool center = false)
    {
        if (center)
        {
            GL.Vertex3(x - size / 2, y - size / 2, 0);
            GL.Vertex3(x + size / 2, y - size / 2, 0);
            GL.Vertex3(x + size / 2, y + size / 2, 0);
            GL.Vertex3(x - size / 2, y + size / 2, 0);
        }
        else
        {
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x + size, y, 0);
            GL.Vertex3(x + size, y + size, 0);
            GL.Vertex3(x, y + size, 0);
        }
       
    }
}