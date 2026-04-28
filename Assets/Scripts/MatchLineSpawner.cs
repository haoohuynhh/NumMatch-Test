using System.Collections;
using UnityEngine;

public class MatchLineSpawner : MonoBehaviour
{
    [Header("Line Settings")]
    public LineRenderer linePrefab;
    public float lineDuration = 0.35f;
    public float zOffset = -0.1f;

    [Header("Fallback Settings")]
    public Color fallbackColor = new Color(1f, 1f, 0.8f, 1f);
    public float fallbackWidth = 0.08f;

    public void SpawnMatchLine(Cellv2 a, Cellv2 b)
    {
        if (a == null || b == null) return;

        SpawnSingleLine(a.transform.position, b.transform.position);
    }

    public void SpawnMatchLine(Cellv2 a, Cellv2 b, GridManager gridManager)
    {
        if (a == null || b == null) return;
        if (gridManager == null)
        {
            SpawnSingleLine(a.transform.position, b.transform.position);
            return;
        }

        if (IsStraightLineClear(a, b, gridManager))
        {
            SpawnSingleLine(a.transform.position, b.transform.position);
            return;
        }

        SpawnWrapLine(a, b, gridManager);
    }

    public void SpawnRowLine(GridManager gridManager, int row)
    {
        if (gridManager == null) return;

        Cellv2 left = gridManager.GetCell(0, row);
        Cellv2 right = gridManager.GetCell(gridManager.columns - 1, row);
        if (left == null || right == null) return;

        SpawnSingleLine(left.transform.position, right.transform.position);
    }

    private void SpawnSingleLine(Vector3 start, Vector3 end)
    {
        start.z += zOffset;
        end.z += zOffset;

        LineRenderer line = null;

        if (linePrefab != null)
        {
            line = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        }
        else
        {
            var go = new GameObject("MatchLine");
            go.transform.SetParent(transform);
            line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = fallbackColor;
            line.endColor = fallbackColor;
            line.startWidth = fallbackWidth;
            line.endWidth = fallbackWidth;
            line.positionCount = 2;
        }

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        if (lineDuration > 0f)
            StartCoroutine(DisableAndDestroy(line.gameObject, lineDuration));
    }

    private bool IsStraightLineClear(Cellv2 a, Cellv2 b, GridManager gridManager)
    {
        int x1 = a.gridX, y1 = a.gridY;
        int x2 = b.gridX, y2 = b.gridY;
        int dx = x2 - x1, dy = y2 - y1;

        if (!(dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy)))
            return false;

        int stepX = (dx == 0) ? 0 : (dx > 0 ? 1 : -1);
        int stepY = (dy == 0) ? 0 : (dy > 0 ? 1 : -1);
        int cx = x1 + stepX, cy = y1 + stepY;

        while (cx != x2 || cy != y2)
        {
            Cellv2 mid = gridManager.GetCell(cx, cy);
            if (mid != null && !mid.IsEmpty() && !mid.isMatched) return false;
            cx += stepX;
            cy += stepY;
        }

        return true;
    }

    private void SpawnWrapLine(Cellv2 a, Cellv2 b, GridManager gridManager)
    {
        int columns = gridManager.columns;

        int idx1 = a.gridY * columns + a.gridX;
        int idx2 = b.gridY * columns + b.gridX;

        Cellv2 startCell = idx1 <= idx2 ? a : b;
        Cellv2 endCell = idx1 <= idx2 ? b : a;

        int startRow = startCell.gridY;
        int startCol = startCell.gridX;
        int endRow = endCell.gridY;
        int endCol = endCell.gridX;

        if (startRow == endRow)
        {
            SpawnSingleLine(startCell.transform.position, endCell.transform.position);
            return;
        }

        Vector3 rowEndPos;
        if (TryGetCellPosition(gridManager, columns - 1, startRow, out rowEndPos))
        {
            Vector3 startPos = startCell.transform.position;
            if (startPos != rowEndPos)
                SpawnSingleLine(startPos, rowEndPos);
        }

        for (int row = startRow + 1; row < endRow; row++)
        {
            Vector3 rowStartPos;
            Vector3 rowLastPos;
            if (TryGetCellPosition(gridManager, 0, row, out rowStartPos) &&
                TryGetCellPosition(gridManager, columns - 1, row, out rowLastPos))
            {
                if (rowStartPos != rowLastPos)
                    SpawnSingleLine(rowStartPos, rowLastPos);
            }
        }

        Vector3 endRowStartPos;
        if (TryGetCellPosition(gridManager, 0, endRow, out endRowStartPos))
        {
            Vector3 endPos = endCell.transform.position;
            if (endRowStartPos != endPos)
                SpawnSingleLine(endRowStartPos, endPos);
        }
    }

    private bool TryGetCellPosition(GridManager gridManager, int x, int y, out Vector3 position)
    {
        position = Vector3.zero;
        if (gridManager == null) return false;

        Cellv2 cell = gridManager.GetCell(x, y);
        if (cell == null) return false;

        position = cell.transform.position;
        return true;
    }

    private IEnumerator DisableAndDestroy(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            Destroy(target);
    }
}
