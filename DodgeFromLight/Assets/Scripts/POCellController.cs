using System.Collections.Generic;
using UnityEngine;

public class POCellController : MonoBehaviour
{
    public GameObject SquareContainer;
    public SpriteRenderer SquareUp;
    public SpriteRenderer SquareDown;

    public GameObject SplittedContainer;
    public SpriteRenderer SplittedUp1;
    public SpriteRenderer SplittedUp2;
    public SpriteRenderer SplittedUp3;
    public SpriteRenderer SplittedUp4;
    public SpriteRenderer SplittedUp5;

    public SpriteRenderer SplittedDown1;
    public SpriteRenderer SplittedDown2;
    public SpriteRenderer SplittedDown3;
    public SpriteRenderer SplittedDown4;
    public SpriteRenderer SplittedDown5;

    public void SetMaterials(List<Material> UpMaterials, List<Material> DownMaterials)
    {
        switch (UpMaterials.Count)
        {
            case 1:
                SplittedContainer.SetActive(false);
                SquareContainer.SetActive(true);
                SquareUp.sharedMaterial = UpMaterials[0];
                SquareDown.sharedMaterial = DownMaterials[0];
                break;

            case 2:
                SplittedContainer.SetActive(true);
                SquareContainer.SetActive(false);
                // Down
                SplittedDown1.sharedMaterial = DownMaterials[0];
                SplittedDown2.sharedMaterial = DownMaterials[1];
                SplittedDown3.sharedMaterial = DownMaterials[0];
                SplittedDown4.sharedMaterial = DownMaterials[1];
                SplittedDown5.enabled = false;

                // Up
                SplittedUp1.sharedMaterial = UpMaterials[0];
                SplittedUp2.sharedMaterial = UpMaterials[1];
                SplittedUp3.sharedMaterial = UpMaterials[0];
                SplittedUp4.sharedMaterial = UpMaterials[1];
                SplittedUp5.enabled = false;
                break;

            case 3:
                SplittedContainer.SetActive(true);
                SquareContainer.SetActive(false);
                // Down
                SplittedDown1.sharedMaterial = DownMaterials[0];
                SplittedDown2.sharedMaterial = DownMaterials[1];
                SplittedDown3.sharedMaterial = DownMaterials[0];
                SplittedDown4.sharedMaterial = DownMaterials[1];
                SplittedDown5.enabled = true;
                SplittedDown5.sharedMaterial = DownMaterials[2];

                // Up
                SplittedUp1.sharedMaterial = UpMaterials[0];
                SplittedUp2.sharedMaterial = UpMaterials[1];
                SplittedUp3.sharedMaterial = UpMaterials[0];
                SplittedUp4.sharedMaterial = UpMaterials[1];
                SplittedUp5.enabled = true;
                SplittedUp5.sharedMaterial = UpMaterials[2];
                break;

            case 4:
                SplittedContainer.SetActive(true);
                SquareContainer.SetActive(false);
                // Down
                SplittedDown1.sharedMaterial = DownMaterials[0];
                SplittedDown2.sharedMaterial = DownMaterials[1];
                SplittedDown3.sharedMaterial = DownMaterials[2];
                SplittedDown4.sharedMaterial = DownMaterials[3];
                SplittedDown5.enabled = false;

                // Up
                SplittedUp1.sharedMaterial = UpMaterials[0];
                SplittedUp2.sharedMaterial = UpMaterials[1];
                SplittedUp3.sharedMaterial = UpMaterials[2];
                SplittedUp4.sharedMaterial = UpMaterials[3];
                SplittedUp5.enabled = false;
                break;

            default:
            case 5:
                SplittedContainer.SetActive(true);
                SquareContainer.SetActive(false);
                // Down
                SplittedDown1.sharedMaterial = DownMaterials[0];
                SplittedDown2.sharedMaterial = DownMaterials[1];
                SplittedDown3.sharedMaterial = DownMaterials[2];
                SplittedDown4.sharedMaterial = DownMaterials[3];
                SplittedDown5.enabled = true;
                SplittedDown5.sharedMaterial = DownMaterials[4];

                // Up
                SplittedUp1.sharedMaterial = UpMaterials[0];
                SplittedUp2.sharedMaterial = UpMaterials[1];
                SplittedUp3.sharedMaterial = UpMaterials[2];
                SplittedUp4.sharedMaterial = UpMaterials[3];
                SplittedUp5.enabled = true;
                SplittedUp5.sharedMaterial = UpMaterials[4];
                break;
        }
    }
}