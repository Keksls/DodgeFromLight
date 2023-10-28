public class Collector : Awakening
{
    public Collector()
    {
        RefreshEffect();
    }

    public override void RefreshEffect()
    {
        switch(DodgeFromLight.GameManager._playerController.NbGearsEquiped)
        {
            case 0:
                PO = 0;
                PM = 0;
                break;

            case 1:
                PO = 1;
                PM = 0;
                break;

            case 2:
                PO = 1;
                PM = 1;
                break;
        }
    }
}