using UnityEngine;
using TMPro;

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

    /// <summary>
    /// Displays the countdown resolved by the deterministic simulation.
    /// </summary>
    public void SetRemainingTurns(int remainingTurns)
    {
        // This presentation update never advances or explodes the logical bomb.
        NbTurnLeft = remainingTurns;
        if (NbTurnText == null)
            NbTurnText = GetComponentInChildren<TextMeshProUGUI>();
        if (NbTurnText != null)
            NbTurnText.text = NbTurnLeft.ToString();
    }

    private void Events_StartMap()
    {
        NbTurnLeft = GameContext.CurrentMap.Grid.GetCell(Cell).Arg1;
        NbTurnText.text = NbTurnLeft.ToString();
    }

    private void Events_TurnEnd()
    {
        if (GameContext.TimeStoped > 0)
            return;
        if (GameContext.GameManager == null)
            return;
        NbTurnLeft--;
        NbTurnText.text = NbTurnLeft.ToString();
        GameContext.CurrentMap.Grid.GetCell(Cell).Arg1 = NbTurnLeft;
        if (NbTurnLeft <= 0) // explode bombe
        {
            GameObject vfx = Instantiate(GameContext.GridController.BombeVFXPrefab);
            vfx.transform.position = transform.position;
            // remove cell
            GameContext.CurrentMap.Grid.GetCell(Cell).SetType(CellType.NotWalkable);
            GameContext.CurrentMap.Grid.GetCell(Cell).SetWalkable(false);
            GameContext.GridController.ReloadCellNextFrame(GameContext.CurrentMap.Grid.GetCell(Cell));

            // kill player if on same cell
            if(Cell.Equals(GameContext.GameManager.PlayerController.CurrentCell.GetCellPos()))
            {
                GameContext.GameManager.GameOver(GameContext.GameManager.PlayerController.CurrentCell);
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
