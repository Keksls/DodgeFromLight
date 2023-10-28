using UnityEngine;

public class Needle : CellTrap
{
    public Animation Animation;
    public string UpClip;
    public string DownClip;
    int nbTurns = 0;
    int mod = 2;
    public bool IsOn = false;

    public override void Awake()
    {
        IsOn = false;
        Events.TurnStart -= Events_TurnStart;
        Events.TurnStart += Events_TurnStart;
    }

    public void Initialize(int arg)
    {
        if (DodgeFromLight.GameManager != null)
            nbTurns = DodgeFromLight.GameManager.NbTurn;
        if (arg == 0)
            mod = 0;
        else
            mod = 1;
        if (nbTurns % 2 == mod)
        {
            On();
        }
        else
        {
            Off();
        }
    }

    private void Events_TurnStart()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurns++;
        if (nbTurns % 2 == mod)
        {
            On();
        }
        else
        {
            Off();
        }
    }

    private void On()
    {
        IsOn = true;
        Animation.Play(UpClip);
    }

    private void Off()
    {
        IsOn = false;
        Animation.Play(DownClip);
    }

    private void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart;
    }
}
