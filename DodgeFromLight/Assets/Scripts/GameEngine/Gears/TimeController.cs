public class TimeController : Gear
{
    public int NbTurnLeft = 3;

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
        NbTurnLeft = 3;
        foreach (Ennemy en in DodgeFromLight.Ennemies)
            en.AddAlteration(new Alteration(AlterationType.PM, -99, NbTurnLeft));
        foreach (MovingPlatform platform in DodgeFromLight.MovingPlatforms)
            platform.Freeze(NbTurnLeft);
        DodgeFromLight.GameManager._playerController.ThrowGear(Side);
        DodgeFromLight.TimeStoped = NbTurnLeft;
        return true;
    }
}