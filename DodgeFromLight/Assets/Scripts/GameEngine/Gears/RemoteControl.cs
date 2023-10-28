public class RemoteControl : Gear
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
        foreach (Ennemy ennemy in DodgeFromLight.Ennemies)
        {
            ennemy.Patrol.Inverse();
            ennemy.SetOrientation(ennemy.GetInversedOrientation(ennemy.Orientation));
        }
        foreach (MovingPlatform platform in DodgeFromLight.MovingPlatforms)
        {
            platform.Patrol.Inverse();
            platform.SetOrientation(platform.GetInversedOrientation(platform.Orientation));
        }
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        return true;
    }
}