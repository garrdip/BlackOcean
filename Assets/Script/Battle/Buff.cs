using ProjectD;
public class Buff
{
    public BuffType type;
    public int value;
    public bool isDebuff = false;
    public TargetObject user;

    public Buff(BuffType typeIn, int valueIn, bool isDebuffIn)
    {
        type = typeIn;
        value = valueIn;
        isDebuff = isDebuffIn;
        user = null;
    }

    public Buff(BuffType typeIn, int valueIn, bool isDebuffIn, TargetObject tar)
    {
        type = typeIn;
        value = valueIn;
        isDebuff = isDebuffIn;
        user = tar;
    }

    public Buff()
    {
        type = BuffType.NONE;
        value = 0;
        isDebuff = false;
        user = null;
    }
}
