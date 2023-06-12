using ProjectD;
public class Buff
{
    public BuffType type;
    public int value;
    public bool isDebuff = false;

    public Buff(BuffType typeIn, int valueIn, bool isDebuffIn)
    {
        type = typeIn;
        value = valueIn;
        isDebuff = isDebuffIn;
    }

    public Buff()
    {
        type = BuffType.NONE;
        value = 0;
        isDebuff = false;
    }
}
