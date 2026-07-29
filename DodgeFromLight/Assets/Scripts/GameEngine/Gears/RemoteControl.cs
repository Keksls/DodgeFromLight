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
        GameContext.GameManager._playerController.ThrowGear(Side);
        foreach (Ennemy ennemy in GameContext.Ennemies)
        {
            ennemy.Patrol.Inverse();
            ennemy.SetOrientation(ennemy.GetInversedOrientation(ennemy.Orientation));
        }
        foreach (MovingPlatform platform in GameContext.MovingPlatforms)
        {
            platform.Patrol.Inverse();
            platform.SetOrientation(platform.GetInversedOrientation(platform.Orientation));
        }
        GameContext.GameManager.PlayerMove(Orientation.None);
        return true;
    }
}