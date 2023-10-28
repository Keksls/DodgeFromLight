using UnityEngine;

public class DiscoballController : MonoBehaviour
{
    public Canvas canvas;
    public TMPro.TMP_Text text;
    public DiscoBall Gear;
    public int NbTurn = 5;
    public int NbTurnStun = 2;

    private void Start()
    {
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnEnd += Events_TurnEnd;
    }

    private void OnDestroy()
    {
        Events.TurnEnd -= Events_TurnEnd;
    }

    private void Update()
    {
        text.transform.LookAt(Camera.main.transform.position);
        Vector3 rot = text.transform.eulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        rot.y += 180f;
        text.transform.eulerAngles = rot;
    }

    public void Initialize(DiscoBall gear)
    {
        Gear = gear;
        RefreshCouldown();
        canvas.worldCamera = Camera.main;
        Gear.InitializeDiscoBall(gameObject, NbTurn, NbTurnStun);
    }

    private void Events_TurnEnd()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        Gear.NbTurnLeft--;
        RefreshCouldown();
        if (Gear.NbTurnLeft <= 0)
            Gear.Destroy();
    }

    void RefreshCouldown()
    {
        text.text = Gear.NbTurnLeft.ToString();
    }
}