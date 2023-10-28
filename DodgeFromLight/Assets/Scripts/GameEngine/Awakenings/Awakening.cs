public abstract class Awakening
{
    public abstract void RefreshEffect();
    public int PO;
    public int PM;

    public static Awakening GetAwakening(AwakeningType type)
    {
        switch (type)
        {
            case AwakeningType.Collector:
                return new Collector();
            case AwakeningType.Concentrate:
                return new Concentrate();
            case AwakeningType.Determined:
                return new Determinated();
            case AwakeningType.Hurry:
                return new Hurry();
            case AwakeningType.Split:
                return new Split();
            default:
                return null;
        }
    }

    public void DoEffect(Ennemy ennemy)
    {
        if (PO != 0)
            ennemy.AddAlteration(new Alteration(AlterationType.PO, PO, 1));
        if (PM != 0)
            ennemy.AddAlteration(new Alteration(AlterationType.PM, PM, 1));
    }
}

public enum AwakeningType
{
    Collector,
    Concentrate,
    Determined,
    Hurry,
    Split
}