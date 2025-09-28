using UnityEngine;

// 돈 객체가 점유하고 있는 그리드 위치 정보를 저장하는 컴포넌트
public class MoneyGridInfo : MonoBehaviour
{
    private int gridLayer;
    private int gridX;
    private int gridZ;

    public void SetGridPosition(int layer, int x, int z)
    {
        gridLayer = layer;
        gridX = x;
        gridZ = z;
    }

    public int GetLayer() => gridLayer;
    public int GetX() => gridX;
    public int GetZ() => gridZ;
}