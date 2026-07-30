using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectD;

// JSON(JsonUtility) 직렬화 대상 — 튜플·프로퍼티·딕셔너리는 직렬화되지 않으므로 public 필드만 사용할 것
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
    public bool isActive; // JSON 역직렬화 시 빈 슬롯도 기본 인스턴스로 채워지므로, 실제 저장된 플레이어인지 이 플래그로 구분
    public Character character = new Character();
    public int HP, MaxHP;
    public List<Card> cards = new List<Card>();
}

[System.Serializable]
public class SaveDataMap
{
    public List<SaveDataMapRoom> hexagonMapRooms = new List<SaveDataMapRoom>();
    public List<SaveDataRegion> regions = new List<SaveDataRegion>();
    public Vector2Int currentRoom;
}

[System.Serializable]
public class SaveDataMapRoom
{
    public RoomType roomType = RoomType.UNDEFINED; // 방 타입
    public Vector2Int coordinate; // 각 방의 고유 좌표계 값
    public Vector3 position; // 인게임 좌표계 값
    public bool isRegion = false; // 거점지역 구분값
    public bool isActive = false; // 방 활성화 상태 구분값


    public SaveDataMapRoom(HexagonMapRoom hexagonMapRoom)
    {
        roomType = hexagonMapRoom.roomType;
        coordinate = hexagonMapRoom.coordinate;
        position = hexagonMapRoom.position;
        isRegion = hexagonMapRoom.isRegion;
        isActive = hexagonMapRoom.isActive;
    }

    public SaveDataMapRoom(){} // JsonUtility 역직렬화용 기본 생성자
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
    public Vector2Int coordinate;
    public bool occupation;

    public SaveDataTile(Tile tile)
    {
        coordinate = new Vector2Int((int)tile.coordinate.x, (int)tile.coordinate.y);
        occupation = tile.occupation;
    }

    public SaveDataTile(){} // JsonUtility 역직렬화용 기본 생성자
}
