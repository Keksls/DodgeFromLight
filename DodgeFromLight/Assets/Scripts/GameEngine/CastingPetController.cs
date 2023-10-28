using UnityEngine;

public class CastingPetController : Entity
{
    GameObject pet;
    public bool Dead = false;

    public override void Awake()
    {
        base.Awake();
        Name = "Player";
        Events.TurnEnd -= Events_TurnEnd1;
        Events.TurnEnd += Events_TurnEnd1;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Events.TurnEnd -= Events_TurnEnd1;
    }

    private void Events_TurnEnd1()
    {
        if(CurrentCell.Equals(DodgeFromLight.GameManager._playerController.CurrentCell))
            DodgeFromLight.GameManager._playerController.UncastPet(false, true);
    }

    public void CastPet(GameObject PetPrefab, Cell cell, Orientation orientation)
    {
        pet = Instantiate(PetPrefab);
        pet.GetComponent<Pet>().Initialize(transform);
        pet.GetComponent<Pet>().IsCasted = true;
        PlaceOnCell(cell, true);
        pet.transform.SetParent(null, false);
        pet.transform.localScale = pet.transform.localScale * 1.7f;
        pet.transform.position = transform.position;
        DodgeFromLight.GameManager.Follower.Target = transform;
        SetOrientation(orientation);
    }

    public void UncastPet()
    {
        Destroy(pet);
    }

    void Update()
    {
        if (DodgeFromLight.GameManager != null && DodgeFromLight.HasEntitiesMoving || Dead)
            return;

        Orientation dir = Orientation.None;
        HandleInputs(ref dir);

        if (dir != Orientation.None)
        {
            switch (DodgeFromLight.GameManager.Follower.Orientation)
            {
                case Orientation.Left:
                    switch (dir)
                    {
                        case Orientation.Up:
                            dir = Orientation.Right;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Down;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Up;
                            break;
                    }
                    break;

                case Orientation.Down:
                    switch (dir)
                    {
                        case Orientation.Up:
                            dir = Orientation.Down;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Up;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Right;
                            break;
                    }
                    break;

                case Orientation.Right:
                    switch (dir)
                    {
                        case Orientation.Up:
                            dir = Orientation.Left;
                            break;
                        case Orientation.Right:
                            dir = Orientation.Up;
                            break;
                        case Orientation.Down:
                            dir = Orientation.Right;
                            break;
                        case Orientation.Left:
                            dir = Orientation.Down;
                            break;
                    }
                    break;
            }
            DodgeFromLight.GameManager.StartTimer();
            DodgeFromLight.GameManager.PlayerMove(dir);
        }
    }

    private void HandleInputs(ref Orientation dir)
    {
        if (Input.GetKey(KeyCode.LeftControl))
            return;

        if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Right).IsDown())
            dir = Orientation.Right;
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.left).IsDown())
            dir = Orientation.Left;
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Forward).IsDown())
            dir = Orientation.Up;
        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Backward).IsDown())
            dir = Orientation.Down;
    }
}