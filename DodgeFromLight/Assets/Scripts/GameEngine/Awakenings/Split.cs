public class Split : Awakening
{
    int nbTurn = 0;

    public Split()
    {
        PO = 1;
    }

    public override void RefreshEffect()
    {
        nbTurn++;
        if (nbTurn == 6)
            nbTurn = 0;
        if (nbTurn < 4)
            PO = 1;
        else if (nbTurn < 6)
            PO = -1;
    }
}