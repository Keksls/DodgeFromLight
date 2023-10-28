using DFLCommonNetwork.GameEngine;
using System.Collections.Generic;
using UnityEngine;

public class PillarController : MonoBehaviour
{
    public LineRenderer[] LRS;
    public Transform Emiter;
    private List<Vector3> targets;

    private void Awake()
    {
        ClearTargets();
    }

    public void SetTargetCells(List<CellPos> cells)
    {
        targets = new List<Vector3>();
        foreach (CellPos cp in cells)
            targets.Add(cp.ToVector3(0f));
    }

    public void ClearTargets()
    {
        targets = null;
        foreach (LineRenderer lr in LRS)
        {
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.zero);
        }
    }

    private void Update()
    {
        if (targets == null) return;

        int i = 0;
        foreach(Vector3 target in targets)
        {
            LRS[i].SetPosition(0, Emiter.position);
            LRS[i].SetPosition(1, target);
            i++;
        }
    }
}