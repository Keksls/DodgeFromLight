using UnityEngine;
using TMPro;
using DFLCommonNetwork.GameEngine;

public class BmbeController : MonoBehaviour
{
    public TextMeshProUGUI NbTurnText;
    public int NbTurnLeft;
    public CellPos Cell;
    bool initialized = false;

    public void Initialize(int nbturn, CellPos cell)
    {
        NbTurnLeft = nbturn;
        Cell = cell;
        NbTurnText = GetComponentInChildren<TextMeshProUGUI>();
        NbTurnText.text = NbTurnLeft.ToString();
        Events.TurnEnd -= Events_TurnEnd;
        Events.TurnEnd += Events_TurnEnd;
        Events.StartMap -= Events_StartMap;
        Events.StartMap += Events_StartMap;
        initialized = true;
    }

    private void Events_StartMap()
    {
        NbTurnLeft = DodgeFromLight.CurrentMap.Grid.GetCell(Cell).Arg1;
        NbTurnText.text = NbTurnLeft.ToString();
    }

    private void Events_TurnEnd()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        if (DodgeFromLight.GameManager == null)
            return;
        NbTurnLeft--;
        NbTurnText.text = NbTurnLeft.ToString();
        DodgeFromLight.CurrentMap.Grid.GetCell(Cell).Arg1 = NbTurnLeft;
        if (NbTurnLeft <= 0) // explode bombe
        {
            GameObject vfx = Instantiate(DodgeFromLight.GridController.BombeVFXPrefab);
            vfx.transform.position = transform.position;
            // remove cell
            DodgeFromLight.CurrentMap.Grid.GetCell(Cell).SetType(CellType.NotWalkable);
            DodgeFromLight.CurrentMap.Grid.GetCell(Cell).SetWalkable(false);
            DodgeFromLight.GridController.ReloadCellNextFrame(DodgeFromLight.CurrentMap.Grid.GetCell(Cell));

            // kill player if on same cell
            if(Cell.Equals(DodgeFromLight.GameManager.PlayerController.CurrentCell.GetCellPos()))
            {
                DodgeFromLight.GameManager.GameOver(DodgeFromLight.GameManager.PlayerController.CurrentCell);
            }
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            Events.TurnEnd -= Events_TurnEnd;
            Events.StartMap -= Events_StartMap;
        }
    }
}