using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class Region{

    [SerializeField]
    public List<Tile> tiles = new List<Tile>();
    public RegionGrade regionGrade;

    public void GetRegionGrade()
    {
        int value = Random.Range(0,100);
        if(value < 25)
        {
            regionGrade = RegionGrade.LEGEND;
            return;
        }
        if(value < 50)
        {
            regionGrade = RegionGrade.UNIQUE;
            return;
        }
        if(value < 75)
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