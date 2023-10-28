using System.Collections;
using UnityEngine;
using DG.Tweening;
using DFLCommonNetwork.GameEngine;

public class GamePlayer_Controller : Entity
{
    public PlayerCharacter PlayerCharacter;
    public Gear GearA;
    public Gear GearE;
    public int NbGearsEquiped
    {
        get
        {
            int nb = 0;
            if (GearA != null)
                nb++;
            if (GearE != null)
                nb++;
            return nb;
        }
    }
    public AnimationCurve SwitchSwordCurve;
    public float SwitchSwordJumpY = 5f;
    public GameObject ShieldStrikeVFXPrefab;
    public GameObject CastPetVFX;
    public GameObject UncastPetVFX;
    private GameObject currentSwordFX;
    [HideInInspector]
    public GameObject plantedSword = null;
    [HideInInspector]
    public CellPos plantedSwordCell;
    private GameObject petGO = null;
    private bool canPet = false;
    [HideInInspector]
    public bool IsCastingPet = false;
    private bool Jumping = false;

    public override void Awake()
    {
        base.Awake();
        Name = "Player";
        TABase = 1;

        StartMove = () =>
        {
            SetFlapWings(true);
        };
        EndMove = () =>
        {
            SetFlapWings(false);
        };
    }

    private void SetFlapWings(bool flapping)
    {
        if (PlayerCharacter == null)
            return;
        var wings = PlayerCharacter.GetInstantiatesSkin(SkinType.Wings);
        if (wings != null)
            wings.GetComponent<Animator>().SetBool("Flap Fast", flapping);
    }

    void Update()
    {
        if ((DodgeFromLight.GameManager != null && DodgeFromLight.HasEntitiesMoving) || Jumping)
            return;

        if (IsCastingPet)
        {
            if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.CastPet).IsDown())
            {
                DodgeFromLight.GameManager.StartTimer();
                DodgeFromLight.GameManager.PlayerMove(Orientation.None);
                UncastPet(true, false);
            }
            return;
        }

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

    public void ResetPlayer()
    {
        if (plantedSword != null)
        {
            Destroy(plantedSword);
            GameObject weaponGO = PlayerCharacter.GetInstantiatedWeaponGameObject();
            if (weaponGO != null)
                weaponGO.SetActive(true);
            Destroy(currentSwordFX);
        }
        currentSwordFX = null;
        plantedSword = null;

        canPet = true;
        IsCastingPet = false;

        if (petGO != null)
            Destroy(petGO);
        if (PlayerCharacter.GetInstantiatesSkin(SkinType.Pet) != null)
            PlayerCharacter.GetInstantiatesSkin(SkinType.Pet).SetActive(true);
        DodgeFromLight.GameManager._petController = null;
        Jumping = false;

        // set player skin
        PlayerCharacter.SetSave(SaveManager.CurrentSave);
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

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear1).IsDown())
        {
            if (CurrentCell.GearType != GearType.None)
                SetGearA(CurrentCell.GearType);
            else if (GearA != null && GearA.CanUseGear())
                GearA.UseGear();
            DodgeFromLight.GameManager.StartTimer();
        }

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Gear2).IsDown())
        {
            if (CurrentCell.GearType != GearType.None)
                SetGearE(CurrentCell.GearType);
            else if (GearE != null && GearE.CanUseGear())
                GearE.UseGear();
            DodgeFromLight.GameManager.StartTimer();
        }

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.PlantSword).IsDown() &&
            (SaveManager.CurrentSave.GetCurrentPart(SkinType.Sword) != -1 ||
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Wand) != -1 ||
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Hammer) != -1 ||
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Dagger) != -1 ||
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Axe) != -1))
        {
            DodgeFromLight.GameManager.StartTimer();
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);

            DodgeFromLight.StartWaitingAction();
            if (plantedSword == null) // first time plant sword
                StartCoroutine(PlantSword());
            else // switch sword positions
                StartCoroutine(SwitchSwordPositions());
        }

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.DeployShield).IsDown() &&
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Shield) != -1)
        {
            // check if sword on forward cell
            if (plantedSword == null)
                FailStrikeShield();
            else
            {
                Cell forwardCell = DodgeFromLight.CurrentMap.Grid.GetNeighbor(CurrentCell.GetCellPos(), Orientation);
                if (forwardCell != null && forwardCell.GetCellPos().Equals(plantedSwordCell))
                    StrikeShield();
                else
                    FailStrikeShield();
            }
        }

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.CastPet).IsDown() && canPet &&
            SaveManager.CurrentSave.GetCurrentPart(SkinType.Pet) != -1)
        {
            // try cast pet on forward cell
            Cell nextCell = DodgeFromLight.CurrentMap.Grid.GetNextCell(CurrentCell, Orientation, 1);
            if (nextCell != null)
            {
                CastPet(nextCell);
            }
        }

        else if (DodgeFromLight.GameSettingsManager.GetInput(InputSettingsType.Jump).IsDown())
            StartCoroutine(Jump());
    }

    #region Jump
    /// <summary>
    /// Jump the entity
    /// </summary>
    /// <returns></returns>
    internal IEnumerator Jump()
    {
        Jumping = true;
        Play("Jump");
        Transform playerTransform = transform;
        Vector3 pos = playerTransform.position;
        float YOffset = DodgeFromLight.GameManager.GearJumpYOffset;
        float duration = DodgeFromLight.GameManager.EntitiesMovementDuration;
        float enlapsed = 0.0f;
        CellPos playerCell = CurrentCell.GetCellPos();

        // jump up
        CurrentCell.EntityOnCell = null;
        CurrentCell = new Cell(-1, -1);
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        DodgeFromLight.StartWaitingAction();
        while (enlapsed < duration)
        {
            Vector3 p = pos;
            p.y = DodgeFromLight.GameManager.GearJumpCurve.Evaluate((enlapsed / duration) * 0.5f) * YOffset;
            playerTransform.position = p;
            yield return null;
            enlapsed += Time.deltaTime;
        }
        DodgeFromLight.StopWaitingAction();

        yield return null;
        // fall down
        enlapsed = 0.0f;
        CurrentCell.EntityOnCell = this;
        CurrentCell = DodgeFromLight.CurrentMap.Grid.GetCell(playerCell);
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        DodgeFromLight.StartWaitingAction();
        while (enlapsed < duration)
        {
            Vector3 p = pos;
            p.y = DodgeFromLight.GameManager.GearJumpCurve.Evaluate(((enlapsed / duration) * 0.5f) + 0.5f) * YOffset;
            playerTransform.position = p;
            yield return null;
            enlapsed += Time.deltaTime;
        }
        PlaceOnCell(CurrentCell, true);
        DodgeFromLight.StopWaitingAction();
        Jumping = false;
    }
    #endregion

    #region cast Pet
    private void CastPet(Cell cell)
    {
        IsCastingPet = true;
        canPet = false;
        petGO = new GameObject("castingPet");
        GameObject pet = PlayerCharacter.GetInstantiatesSkin(SkinType.Pet);
        DodgeFromLight.GameManager._petController = petGO.AddComponent<CastingPetController>();
        DodgeFromLight.GameManager._petController.CastPet(pet, CurrentCell, Orientation);
        pet.SetActive(false);
        GameObject vfx = Instantiate(CastPetVFX);
        vfx.transform.position = cell.GetCellPos().ToVector3(2f);
        DodgeFromLight.GameManager.StartTimer();
        DodgeFromLight.GameManager.PlayerMove(Orientation);

    }

    public void UncastPet(bool transposition, bool keepPet)
    {
        if (!IsCastingPet)
            return;
        IsCastingPet = false;
        canPet = false;
        CastingPetController pet = DodgeFromLight.GameManager._petController;
        if (transposition)
            PlaceOnCell(pet.CurrentCell, true);
        pet.CurrentCell.EntityOnCell = null;
        pet.CurrentCell.Walkable = true;
        pet.UncastPet();
        DodgeFromLight.GameManager._petController = null;
        DodgeFromLight.GameManager.Follower.Target = transform;
        GameObject vfx = Instantiate(UncastPetVFX);
        vfx.transform.position = pet.CurrentCell.GetCellPos().ToVector3(0.3f);
        if (keepPet)
        {
            canPet = true;
            PlayerCharacter.GetInstantiatesSkin(SkinType.Pet).SetActive(true);
        }
        Destroy(petGO);
    }
    #endregion

    #region Deploy shield
    private void StrikeShield()
    {
        CellPos swordStrikePos = DodgeFromLight.CurrentMap.Grid.GetFarestCell(plantedSwordCell, Orientation, 4, InLineCellType.Push);
        if (!swordStrikePos.Equals(plantedSwordCell))
        {
            DodgeFromLight.GameManager.StartTimer();
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);
            Play("StrikeShield");
            StartCoroutine(PushSword(swordStrikePos));
        }
        else
        {
            Play("StrikeShieldHit");
            DodgeFromLight.GameManager.StartTimer();
            DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        }
    }

    IEnumerator PushSword(CellPos pos)
    {
        DodgeFromLight.StartWaitingAction();
        float enlapsed = 0.0f;
        float duration = DodgeFromLight.GameManager.EntitiesMovementDuration / 2f;
        yield return new WaitForSeconds(0.15f);
        GameObject vfx = Instantiate(ShieldStrikeVFXPrefab);
        vfx.transform.position = plantedSword.transform.position;
        Vector3 Sstart = plantedSword.transform.position;
        Vector3 Send = pos.ToVector3(0f);
        Send.y = plantedSword.transform.position.y;
        Vector3 FXstart = currentSwordFX.transform.position;
        Vector3 FXend = pos.ToVector3(0f);
        FXend.y = currentSwordFX.transform.position.y;
        while (enlapsed < duration)
        {
            plantedSword.transform.position = Vector3.Lerp(Sstart, Send, enlapsed / duration);
            currentSwordFX.transform.position = Vector3.Lerp(FXstart, FXend, enlapsed / duration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        plantedSword.transform.position = Send;
        currentSwordFX.transform.position = FXend;
        plantedSwordCell = pos;
        DodgeFromLight.StopWaitingAction();
    }

    private void FailStrikeShield()
    {
        Play("StrikeShieldHit");
        DodgeFromLight.GameManager.StartTimer();
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
    }
    #endregion

    #region Plant Sword
    public void RecoverSword()
    {
        if (plantedSword != null)
        {
            Destroy(plantedSword);
            GameObject weaponGO = PlayerCharacter.GetInstantiatedWeaponGameObject();
            if (weaponGO != null)
                weaponGO.SetActive(true);
            Destroy(currentSwordFX);
            WeaponDataAsset wda = PlayerCharacter.GetCurrentWeaponData();
            GameObject FX = DodgeFromLight.Databases.WeaponData.GetTranspositionWeaponVFX(wda.Type, wda.SkinID);
            FX.transform.position = transform.position;
        }
        currentSwordFX = null;
        plantedSword = null;
    }

    IEnumerator PlantSword()
    {
        Play("PlantSword");
        WeaponDataAsset wda = PlayerCharacter.GetCurrentWeaponData();
        plantedSword = Instantiate(DodgeFromLight.Databases.SkinsData.GetPrefab(wda.Type, wda.SkinID));
        plantedSwordCell = CurrentCell.GetCellPos();
        plantedSword.transform.localScale = wda.Scale / 6f;
        PlayerCharacter.GetInstantiatedWeaponGameObject().SetActive(false);
        currentSwordFX = DodgeFromLight.Databases.WeaponData.GetPlantedVFX(wda.Type, wda.SkinID);
        PlantedSwordPosition(wda);
        yield return new WaitForSeconds(DodgeFromLight.GameManager.EntitiesMovementDuration);
        DodgeFromLight.StopWaitingAction();
    }

    private void PlantedSwordPosition(WeaponDataAsset weapon)
    {
        WeaponDataAsset wda = DodgeFromLight.Databases.WeaponData.GetWeaponData(weapon.Type, weapon.SkinID);
        SkinDataAsset sda = DodgeFromLight.Databases.SkinsData.GetSkin(wda.Type, wda.SkinID);
        plantedSword.transform.position = plantedSwordCell.ToVector3(wda.YPos / 6f) + sda.localPos;
        plantedSword.transform.localRotation = Quaternion.Euler(wda.Rotation) * sda.localRot;
        currentSwordFX.transform.position = plantedSwordCell.ToVector3(0.025f);
        StopCoroutine(SwordFall(plantedSword.transform, DodgeFromLight.GameManager.EntitiesMovementDuration, 2f));
        StopCoroutine(SwordFall(currentSwordFX.transform, DodgeFromLight.GameManager.EntitiesMovementDuration / 2f, 2f));
        StartCoroutine(SwordFall(plantedSword.transform, DodgeFromLight.GameManager.EntitiesMovementDuration, 4f));
        StartCoroutine(SwordFall(currentSwordFX.transform, DodgeFromLight.GameManager.EntitiesMovementDuration / 2f, 2f));
    }

    IEnumerator SwordFall(Transform _transform, float duration, float StartUp)
    {
        if (_transform != null)
        {
            Vector3 start = _transform.position;
            start.y += StartUp;
            Vector3 end = _transform.position;
            float enlapsed = 0f;
            while (enlapsed < duration)
            {
                if (_transform != null)
                    _transform.position = Vector3.Lerp(start, end, enlapsed / duration);
                yield return null;
                enlapsed += Time.deltaTime;
            }
            if (_transform != null)
                _transform.position = end;
        }
    }

    IEnumerator SwitchSwordPositions()
    {
        Play("Win", true, 0.5f, lockedWaitEnd: false);
        Events.Fire_PlayerStartChangeCell(CurrentCell, DodgeFromLight.CurrentMap.Grid.GetCell(plantedSwordCell));
        float duration = DodgeFromLight.GameManager.EntitiesMovementDuration * 2f;
        float enlapsed = 0f;
        CellPos lastCell = CurrentCell.GetCellPos();
        PlaceOnCell(DodgeFromLight.CurrentMap.Grid.GetCell(plantedSwordCell), false);

        // turn player to look at sword
        // player jump to sword
        Quaternion startRot = transform.rotation;
        Vector3 relativePos = plantedSwordCell.ToVector3(0f) - transform.position;
        Quaternion endRot = Quaternion.LookRotation(relativePos, Vector3.up);
        Vector3 startPos = transform.position;
        Vector3 endPos = plantedSwordCell.ToVector3(0f);
        float rotWeight = Mathf.Abs(endRot.y - startRot.y);
        float inversedRotWeight = Mathf.Abs((360 + endRot.y) - startRot.y);
        if (inversedRotWeight < rotWeight)
            endRot.y += 360;
        while (enlapsed < duration)
        {
            transform.rotation = Quaternion.Lerp(startRot, endRot, enlapsed / (duration * 0.5f));
            Vector3 pos = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            pos.y = SwitchSwordCurve.Evaluate(enlapsed / duration) * SwitchSwordJumpY;
            transform.position = pos;
            enlapsed += Time.deltaTime;
            yield return null;
        }

        // set new player position
        CellPos newPlantedCellPos = LastCell.GetCellPos();
        PlaceOnCell(CurrentCell, true);
        SetOrientation(Orientation);

        // set new sword position
        plantedSwordCell = newPlantedCellPos;
        WeaponDataAsset weapon = PlayerCharacter.GetCurrentWeaponData();
        PlantedSwordPosition(weapon);

        // player jump fall FX
        GameObject FX = DodgeFromLight.Databases.WeaponData.GetTranspositionWeaponVFX(weapon.Type, weapon.SkinID);
        FX.transform.position = transform.position;

        // camera shake
        DodgeFromLight.GameManager.Follower.GetComponent<Camera>().DOShakeRotation(0.25f, 10f, 2, 2f);
        DodgeFromLight.StopWaitingAction();
        Events.Fire_PlayerEndChangeCell(DodgeFromLight.CurrentMap.Grid.GetCell(lastCell), CurrentCell);
    }
    #endregion

    #region Gears
    /// <summary>
    /// Set gear for A slot
    /// </summary>
    /// <param name="type">gear to set</param>
    public void SetGearA(GearType type)
    {
        if (GearA != null)
            GearA.ThrowGear();
        GearA = Gear.GetGear(type);
        if (GearA != null)
            GearA.Initialize(this, GearSide.A);
        else
            Debug.Log("Gear A fail");
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        PickUpGearVFX();
        CurrentCell.SetGear(GearType.None);
        DodgeFromLight.GridController.UnSpawnGear(CurrentCell);
        Events.Fire_SetGear(type);
    }

    /// <summary>
    /// Set gear for slot E
    /// </summary>
    /// <param name="type">gear to set</param>
    public void SetGearE(GearType type)
    {
        if (GearE != null)
            GearE.ThrowGear();
        GearE = Gear.GetGear(type);
        if (GearE != null)
            GearE.Initialize(this, GearSide.E);
        else
            Debug.Log("Gear E fail");
        DodgeFromLight.GameManager.PlayerMove(Orientation.None);
        PickUpGearVFX();
        CurrentCell.SetGear(GearType.None);
        DodgeFromLight.GridController.UnSpawnGear(CurrentCell);
        Events.Fire_SetGear(type);
    }

    public void PickUpGearVFX()
    {
        GameObject vfx = Instantiate(DodgeFromLight.GameManager.GearPickupVFX);
        Vector3 pos = transform.position;
        pos.y = 10f;
        vfx.transform.position = pos;
    }

    public void SaveCheckpointVFX()
    {
        GameObject vfx = Instantiate(DodgeFromLight.GameManager.CheckboxSaveVFX);
        Vector3 pos = transform.position;
        pos.y = 10f;
        vfx.transform.position = pos;
    }

    /// <summary>
    /// Has a gear of type 'type' equiped
    /// </summary>
    /// <param name="type">type of gear to check</param>
    /// <returns>true if gear is  equiped</returns>
    public bool HasGear(GearType type)
    {
        return GearE != null && GearE.Type == type || GearA != null && GearA.Type == type;
    }

    /// <summary>
    /// is a gear now activated
    /// </summary>
    /// <param name="type">type of gear</param>
    /// <returns>true if HAS gear and gear is activated</returns>
    public bool IsGearActivated(GearType type)
    {
        Gear g = GetGear(type);
        return g != null && g.IsActivated();
    }

    /// <summary>
    /// Get a gear equiped on the player if player got the gear equiped
    /// </summary>
    /// <param name="type">type of gear to get</param>
    /// <returns>the gear if it's equiped</returns>
    public Gear GetGear(GearType type)
    {
        if (GearA != null && GearA.Type == type)
            return GearA;

        if (GearE != null && GearE.Type == type)
            return GearE;

        return null;
    }

    /// <summary>
    /// Throw a Gear
    /// </summary>
    /// <param name="side"></param>
    public void ThrowGear(GearSide side)
    {
        switch (side)
        {
            case GearSide.A:
                if (GearA != null)
                {
                    GearA.ThrowGear();
                    GearA = null;
                }
                break;
            case GearSide.E:
                if (GearE != null)
                {
                    GearE.ThrowGear();
                    GearE = null;
                }
                break;
        }
        Events.Fire_SetGear(GearType.None);
    }
    #endregion
}