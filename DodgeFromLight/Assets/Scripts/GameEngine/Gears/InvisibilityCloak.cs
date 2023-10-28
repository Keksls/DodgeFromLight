public class InvisibilityCloak : Gear
{
    public override bool CanUseGear()
    {
        return true;
    }

    public override bool IsActivated()
    {
        return true;
    }

    public override void ThrowGear()
    {
        RelatedEntity.Invisible = false;
    }

    public override bool UseGear()
    {
        RelatedEntity.Invisible = true;
        return true;
    }
}