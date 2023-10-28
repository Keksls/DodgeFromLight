using DFLCommonNetwork.GameEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thunderbird : Ennemy
{
    bool isUp = false;
    int nbTurnUp = 2;
    int nbTurnDown = 2;
    int nbTurn = 0;
    float upY = 1.5f;
    float downY = 0f;
    Transform bird;

    public override void Awake()
    {
        base.Awake();
        bird = transform.GetChild(0);
        Name = "Raised Thunderbird";
        Type = EnnemyType.Thunderbird;
        if (TABase == 0)
            TABase = 1;
        if (PO == 0)
            PO = 1;
        nbTurn = 0;
        Events.TurnStart -= Events_TurnStart;
        Events.TurnStart += Events_TurnStart;
    }

    private new void Events_TurnStart()
    {
        if (DodgeFromLight.TimeStoped > 0)
            return;
        nbTurn++;
        if (isUp)
        {
            if (nbTurn >= nbTurnUp)
                StartCoroutine(GetDown());
        }
        else
        {
            if (nbTurn >= nbTurnDown)
                StartCoroutine(GetUp());
        }
    }

    IEnumerator GetUp()
    {
        isUp = true;
        float enlapsed = 0f;
        float duration = DodgeFromLight.GameManager ? DodgeFromLight.GameManager.EntitiesMovementDuration : 0.2f;
        Vector3 start = new Vector3(0f, downY, 0f);
        Vector3 end = new Vector3(0f, upY, 0f);
        while (enlapsed < duration)
        {
            bird.localPosition = Vector3.Lerp(start, end, enlapsed / duration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        bird.localPosition = end;
        nbTurn = 0;
    }

    IEnumerator GetDown()
    {
        isUp = false;
        float enlapsed = 0f;
        float duration = DodgeFromLight.GameManager ? DodgeFromLight.GameManager.EntitiesMovementDuration : 0.2f;
        Vector3 start = new Vector3(0f, upY, 0f);
        Vector3 end = new Vector3(0f, downY, 0f);
        while (enlapsed < duration)
        {
            bird.localPosition = Vector3.Lerp(start, end, enlapsed / duration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        bird.localPosition = end;
        nbTurn = 0;
    }

    public override void OnDestroy()
    {
        Events.TurnStart -= Events_TurnStart;
    }

    public override List<CellPos> GetBrutPO()
    {
        List<CellPos> result = new List<CellPos>();
        int x = CurrentCell.X;
        int y = CurrentCell.Y;

        if (isUp)
        {
            switch (Orientation)
            {
                case Orientation.Up:
                    // up
                    x = CurrentCell.X;
                    y = CurrentCell.Y + 3;
                    for (int i = 0; i < PO; i++)
                        result.Add(new CellPos(x, y++));

                    // right
                    x = CurrentCell.X + 2;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x++, y));

                    // left
                    x = CurrentCell.X - 2;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x--, y));
                    break;

                case Orientation.Right:
                    // up
                    x = CurrentCell.X + 3;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO; i++)
                        result.Add(new CellPos(x++, y));

                    // right
                    x = CurrentCell.X;
                    y = CurrentCell.Y + 2;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x, y++));

                    // left
                    x = CurrentCell.X;
                    y = CurrentCell.Y - 2;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x, y--));
                    break;

                case Orientation.Down:
                    // up
                    x = CurrentCell.X;
                    y = CurrentCell.Y - 3;
                    for (int i = 0; i < PO; i++)
                        result.Add(new CellPos(x, y--));

                    // right
                    x = CurrentCell.X - 2;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x--, y));

                    // left
                    x = CurrentCell.X + 2;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x++, y));
                    break;

                case Orientation.Left:
                    // up
                    x = CurrentCell.X - 3;
                    y = CurrentCell.Y;
                    for (int i = 0; i < PO; i++)
                        result.Add(new CellPos(x--, y));

                    // right
                    x = CurrentCell.X;
                    y = CurrentCell.Y - 2;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x, y--));

                    // left
                    x = CurrentCell.X;
                    y = CurrentCell.Y + 2;
                    for (int i = 0; i < PO + 1; i++)
                        result.Add(new CellPos(x, y++));
                    break;
            }
        }
        else
        {
            x = CurrentCell.X;
            y = CurrentCell.Y;
            switch (Orientation)
            {
                case Orientation.Up:
                    result.Add(new CellPos(x + 1, y));
                    result.Add(new CellPos(x - 1, y));
                    result.Add(new CellPos(x, y + 1));
                    result.Add(new CellPos(x, y + 2));
                    break;

                case Orientation.Right:
                    result.Add(new CellPos(x + 1, y));
                    result.Add(new CellPos(x + 2, y));
                    result.Add(new CellPos(x, y + 1));
                    result.Add(new CellPos(x, y - 1));
                    break;

                case Orientation.Down:
                    result.Add(new CellPos(x - 1, y));
                    result.Add(new CellPos(x + 1, y));
                    result.Add(new CellPos(x, y - 1));
                    result.Add(new CellPos(x, y - 2));
                    break;

                case Orientation.Left:
                    result.Add(new CellPos(x - 1, y));
                    result.Add(new CellPos(x - 2, y));
                    result.Add(new CellPos(x, y - 1));
                    result.Add(new CellPos(x, y + 1));
                    break;
            }
        }

        return result;
    }
}