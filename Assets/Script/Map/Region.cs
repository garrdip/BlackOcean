using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

public class Region{
    [SerializeField]
    public List<Tile> tiles = new List<Tile>();
    public RegionGrade regionGrade;

    public void GetRegionGrade()
    {
        int value = Random.Range(0,100);
        if(value < 2)
        {
            regionGrade = RegionGrade.LEGEND;
            return;
        }
        if(value < 10)
        {
            regionGrade = RegionGrade.UNIQUE;
            return;
        }
        if(value < 30)
        {
            regionGrade = RegionGrade.RARE;
            return;
        }
        regionGrade = RegionGrade.NORMAL;
    }
}

[System.Serializable]
public class Tile{
    public Vector3 coordinate;
    public bool occupation;

    public Tile(Vector3 coor)
    {
        coordinate = coor;
        occupation = false;
    }
    public Tile(){}
}