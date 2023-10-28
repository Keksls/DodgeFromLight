public abstract class Gear
{
    public Entity RelatedEntity { internal set; get; }
    public GearType Type { internal set; get; }
    public GearSide Side { internal set; get; }

    public void Initialize(Entity entity, GearSide side)
    {
        RelatedEntity = entity;
        Side = side;
    }

    public abstract bool UseGear();

    public abstract bool CanUseGear();

    public abstract void ThrowGear();

    public abstract bool IsActivated();

    public static Gear GetGear(GearType type)
    {
        Gear gear = null;
        switch (type)
        {
            case GearType.Bomb:
                gear = new Bomb();
                break;
            case GearType.DiscoBall:
                gear = new DiscoBall();
                break;
            case GearType.Grapple:
                gear = new Grapple();
                break;
            case GearType.InvisibilityCloak:
                gear = new InvisibilityCloak();
                break;
            case GearType.Mirror:
                gear = new Mirror();
                break;
            case GearType.PowderOfDarkness:
                gear = new PowderOfDarkness();
                break;
            case GearType.RemoteControl:
                gear = new RemoteControl();
                break;
            case GearType.SpeedBoots:
                gear = new SpeedBoots();
                break;
            case GearType.TimeController:
                gear = new TimeController();
                break;
        }
        gear.Type = type;
        return gear;
    }
}

public enum GearSide
{
    A, 
    E
}

public enum GearType
{
    None,
    Bomb,
    DiscoBall,
    Grapple,
    InvisibilityCloak,
    Mirror,
    PowderOfDarkness,
    RemoteControl,
    SpeedBoots,
    TimeController
}