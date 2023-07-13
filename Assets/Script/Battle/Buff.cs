using ProjectD;
public class Buff
{
    public BuffType type { get; set; }
    public int value { get; set; }
    public bool isDebuff { get; set; } = false;
    public bool isInfinity { get; set; } = false;
    public bool isDecrease { get; set; } = true;
    public TargetObject user { get; set; }

    public static Builder builder() => new Builder(); // 빌더 클래스 생성 함수

    // 빌더 클래스
    public class Builder
    {
        private Buff buff = new Buff(); // Buff 클래스 디폴트 생성자

        // 필드값이 설정되지 않았을 경우 기본값
        public Builder()
        {
            buff.type = BuffType.NONE;
            buff.value = 0;
            buff.isDebuff = false;
            buff.user = null;
        }
        
        // --------------------------- 필드값 세팅 --------------------------- //
        public Builder SetBuffType(BuffType type)
        {
            buff.type = type;
            return this;
        }

        public Builder SetValue(int value)
        {
            buff.value = value;
            return this;
        }

        public Builder SetIsDebuff(bool isDebuff)
        {
            buff.isDebuff = isDebuff;
            return this;
        }

        public Builder SetIsInfinity(bool isInfinity)
        {
            buff.isInfinity = isInfinity;
            return this;
        }

        public Builder SetIsDecrease(bool isDecrease)
        {
            buff.isDecrease = isDecrease;
            return this;
        }

        public Builder SetUser(TargetObject user)
        {
            buff.user = user;
            return this;
        }
        // --------------------------------------------------------------------- //

        // 최종 완성된 객체 반환
        public Buff Build()
        {
            return buff;
        }
    }
}
