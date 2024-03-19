using ProjectD;

[System.Serializable]
public class Buff
{
    public BuffType type;
    public int value;
    public bool isDebuff; // 디버프 여부
    public bool isInfinity; // 영구 버프(디버프) 증가 감소하지 않음.
    public bool isDecrease; // 턴이 지남에 따라 감소하는 버프(디버프)
    public bool isSeparate; // 사람마다 버프 분리 여부
    public uint user; // 버프를 건 사용자

    public Buff()
    {

    }

    public Buff(Buff copy)
    {
        type = copy.type;
        value = copy.value;
        isDebuff = copy.isDebuff;
        isInfinity = copy.isInfinity;
        isDecrease = copy.isDecrease;
        isSeparate = copy.isSeparate;
        user = copy.user;
    }

    public Buff(BuffType typeIn, int valueIn, bool isDebuffIn, bool isInfinityIn, bool isDecreaseIn, bool isSeparateIn, TargetObject tarIn)
    {
        type = typeIn;
        value = valueIn;
        isDebuff = isDebuffIn;
        isInfinity = isInfinityIn;
        isDecrease = isDecreaseIn;
        isSeparate = isSeparateIn;
        user = tarIn.netId;
    }
    
}
