using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

[System.Serializable]
public class SaveData
{
    public SaveDataPlayer[] players = new SaveDataPlayer[3];
    public SaveDataMap map = new SaveDataMap();
}

[System.Serializable]
public class SaveDataPlayer
{
    public ulong ownerSteamId;
    public bool isActive;
    public Character character = new Character();
    public int HP, MaxHP;
    public List<Card> cards = new List<Card>();
}

[System.Serializable]
public class SaveDataMap
{
    public List<SaveDataMapRoom> hexagonMapRooms = new List<SaveDataMapRoom>();
    public List<SaveDataRegion> regions = new List<SaveDataRegion>();
    public (int,int) currentRoom;
}

[System.Serializable]
public class SaveDataMapRoom
{
    public RoomType roomType = RoomType.UNDEFINED; // 방 타입
    public (int,int) coordinate; // 각 방의 고유 좌표계 값
    public (float,float,float) position; // 인게임 좌표계 값
    public bool isRegion = false; // 거점지역 구분값
    public bool isActive = false; // 방 활성화 상태 구분값
    public bool isComplete = false; // 방 정복 완료 상태 구분값

    public SaveDataMapRoom(HexagonMapRoom hexagonMapRoom)
    {
        roomType = hexagonMapRoom.roomType;
        coordinate = (hexagonMapRoom.coordinate.x,hexagonMapRoom.coordinate.y);
        position = (hexagonMapRoom.position.x,hexagonMapRoom.position.y,hexagonMapRoom.position.z);
        isRegion = hexagonMapRoom.isRegion;
        isActive = hexagonMapRoom.isActive;
        isComplete = hexagonMapRoom.isComplete;
    }
}

[System.Serializable]
public class SaveDataRegion
{
    public List<SaveDataTile> tiles = new List<SaveDataTile>();
    public RegionGrade regionGrade;

}

[System.Serializable]
public class SaveDataTile
{
    public (int,int) coordinate;
    public bool occupation;

    public SaveDataTile(Tile tile)
    {
        coordinate = ((int)tile.coordinate.x,(int)tile.coordinate.y);
        occupation = tile.occupation;
    }
}