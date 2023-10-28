public class SpeedBoots : Gear
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
    }

    public override bool UseGear()
    {
        DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        DodgeFromLight.GameManager._playerController.AddAlteration(new Alteration(AlterationType.PM, 1, 2));
        return true;
    }
}