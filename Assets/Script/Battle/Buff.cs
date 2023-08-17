using ProjectD;
public class Buff
{
    public BuffType type;
    public int value;
    public bool isDebuff;
    public bool isInfinity;
    public bool isDecrease;
    public TargetObject user;

    public Buff()
    {

    }

    public Buff(BuffType typeIn, int valueIn, bool isDebuffIn, bool isInfinityIn, bool isDecreaseIn, TargetObject tarIn)
    {
        type = typeIn;
        value = valueIn;
        isDebuff = isDebuffIn;
        isInfinity = isInfinityIn;
        isDecrease = isDecreaseIn;
        user = tarIn;
    }
    
}
