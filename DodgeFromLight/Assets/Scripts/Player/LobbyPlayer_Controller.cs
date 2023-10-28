using DFLCommonNetwork.GameEngine;
using SlyvekGameEngine.GameEngine.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyPlayer_Controller : MonoBehaviour
{
    public PlayerCharacter Character;
    public Canvas Canvas;
    public float ChangeCellDuration = 0.2f;
    public Grid CurrentGrid;
    public Cell CurrentCell;
    public bool IsWalking = false;
    public Animator Animator;
    public bool AnimatorLocked { get; private set; }
    public Transform OrnamentContainer;
    public GameObject Ornament;
    public GameObject Bubble;
    public GameObject BubbleEmote;
    public Vector2 EmoteSize = new Vector2(128f, 128f);
    public TextMeshProUGUI BubbleText;
    public GameObject BubbleCarret;
    public Orientation Orientation;
    private string NextClipToPlay = "Idle";
    private Camera Camera;
    private bool forceOrnamentEnabled = false;
    private float killBubbleTime = 0.0f;

    private void Start()
    {
        Ornament.SetActive(false);
        Bubble.SetActive(false);
        killBubbleTime = float.MaxValue;
    }

    private void Update()
    {
        if (Camera != null && OrnamentContainer)
            OrnamentContainer.LookAt(Camera.transform);

        if (Time.time > killBubbleTime)
        {
            killBubbleTime = float.MaxValue;
            Bubble.SetActive(false);
        }
    }

    #region Ornament and Bubble
    private void OnMouseEnter()
    {
        if (!forceOrnamentEnabled && !Bubble.activeInHierarchy && !EventSystem.current.IsPointerOverGameObject())
            ShowOrnament();
    }

    private void OnMouseExit()
    {
        if (!forceOrnamentEnabled && !Bubble.activeInHierarchy)
            Ornament.SetActive(false);
    }

    public void StartForceOrnamentEnabled()
    {
        forceOrnamentEnabled = true;
        if (Ornament != null)
            Ornament.SetActive(true);
    }

    public void StopForceOrnamentEnabled()
    {
        forceOrnamentEnabled = false;
        if (Ornament != null)
            Ornament.SetActive(false);
    }

    public void ShowOrnament()
    {
        Ornament.SetActive(true);
        Bubble.SetActive(false);
    }

    public void ShowBubble()
    {
        Ornament.SetActive(false);
        Bubble.SetActive(true);
    }

    public void PlayerSpeak(string text)
    {
        ShowBubble();
        killBubbleTime = Time.time + 5f;
        var tmp = Bubble.GetComponentInChildren<TextMeshProUGUI>(true);
        tmp.gameObject.SetActive(true);
        BubbleEmote.SetActive(false);
        tmp.text = text;

        // get size
        Vector2 size = tmp.GetPreferredValues(tmp.text);
        float maxWidth = 320f;
        float y = size.y;
        float X = size.x;
        float currentWidth = Mathf.Min(X, maxWidth);
        int nbLine = Mathf.CeilToInt(X / maxWidth);
        y = (nbLine * y) + 32f;

        // set size
        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(-16f, y - 16f);

        rect = Bubble.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(currentWidth + 16f, y);
        rect.localPosition = new Vector3(0f, y / 2f, 0f);
        BubbleCarret.transform.localPosition = new Vector3(0f, -(y / 2f) - 22.94f + 16f, 0f);
    }

    public void SetCanvasCamera(Camera cam)
    {
        Camera = cam;
    }

    public void PlayerEmote(EmoteType emoteType, int emoteID)
    {
        Sprite emote = Resources.Load<Sprite>("Emotes/" + emoteType.ToString() + "/" + emoteID);
        if (emote != null)
        {
            ShowBubble();
            killBubbleTime = Time.time + 5f;
            killBubbleTime = Time.time + 5f;
            Bubble.GetComponentInChildren<TextMeshProUGUI>(true).gameObject.SetActive(false);
            BubbleEmote.SetActive(true);
            BubbleEmote.GetComponentInChildren<Image>().sprite = emote;

            RectTransform rect = Bubble.GetComponent<RectTransform>();
            rect.sizeDelta = EmoteSize;
            rect.localPosition = new Vector3(0f, EmoteSize.y / 2f, 0f);
            BubbleCarret.transform.localPosition = new Vector3(0f, -(EmoteSize.y / 2f) - 22.94f + 16f, 0f);
        }
    }

    public void RefreshOrnament(LitePlayerSave client)
    {
        bool active = Ornament.activeInHierarchy;
        // set ornament
        foreach (Transform t in Ornament.transform)
            Destroy(t.gameObject);
        OrnamentContainer.rotation = Quaternion.identity;
        GameObject ornament = DodgeFromLight.Databases.OrnamentData.GetOrnament(client.GetCurrentPart(SkinType.Ornament), client.Name);
        ornament.transform.SetParent(Ornament.transform);
        ornament.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
        ornament.transform.localPosition = Vector3.zero;
        if (!forceOrnamentEnabled)
            Ornament.SetActive(active);
    }

    #endregion

    #region Spawn / Unspawn
    public void SpawnOnGrid(Grid grid, LitePlayerSave client, Camera camera)
    {
        Camera = camera;
        CurrentGrid = grid;
        Character.SetSave(client);
        RefreshOrnament(client);
    }

    public void Unspawn()
    {
        Character.KillPet();
        Destroy(gameObject);
    }
    #endregion

    #region Map Placements
    public void SetOrientation(Orientation dir)
    {
        Orientation = dir;
        transform.localEulerAngles = LobbyManager.Instance.CurrentGrid.GetOrientationVector(Orientation);
    }

    public void PlaceOnCell(CellPos pos)
    {
        transform.position = pos.ToVector3(0f);
        CurrentCell = CurrentGrid.GetCell(pos);
    }

    public void WalkToCell(CellPos pos, Action Callback = null, bool ignoreUnwalkable = false)
    {
        List<CellPos> path = PathFinding.FindPath(CurrentGrid, CurrentCell, CurrentGrid.GetCell(pos), true, ignoreUnwalkable);
        StopAllCoroutines();
        IsWalking = false;
        StartCoroutine(FollowPath(path, Callback));
        //Debug.Log("current " + CurrentCell.ToString() + "  target " + pos + "   path " + path.Count);
    }

    IEnumerator FollowPath(List<CellPos> path, Action Callback = null)
    {
        SetFlapWings(true);
        killBubbleTime = float.MaxValue;
        Bubble.SetActive(false);
        IsWalking = true;
        float enlapsed = 0.0f;
        CellPos lastCell = CurrentCell.GetCellPos();
        for (int i = 0; i < path.Count; i++)
        {
            Play("Walk", smoothDuration:0.025f);
            Vector3 start = lastCell.ToVector3(0f);
            Vector3 end = path[i].ToVector3(0f);
            transform.LookAt(end);
            enlapsed = 0.0f;
            while (enlapsed < ChangeCellDuration)
            {
                transform.position = Vector3.Lerp(start, end, enlapsed / ChangeCellDuration);
                yield return null;
                enlapsed += Time.deltaTime;
            }

            CurrentCell = CurrentGrid.GetCell(path[i]);
            lastCell = CurrentCell.GetCellPos();
        }
        Play("Idle");
        IsWalking = false;
        Callback?.Invoke();
        SetFlapWings(false);
    }

    private void SetFlapWings(bool flapping)
    {
        if (Character == null)
            return;
        var wings = Character.GetInstantiatesSkin(SkinType.Wings);
        if (wings != null)
            wings.GetComponent<Animator>().SetBool("Flap Fast", flapping);
    }
    #endregion

    /// <summary>
    /// Play animation clip
    /// </summary>
    /// <param name="Clip">clip to play</param>
    /// <param name="lockAnimator">wait for end on clip</param>
    public void Play(string Clip, bool lockAnimator = false, float duration = -1f, bool smooth = true, float smoothDuration = 0.1f, bool lockedWaitEnd = true)
    {
        if (Animator == null)
            return;

        if (!AnimatorLocked)
        {
            if (smooth)
                Animator.CrossFade(Clip, smoothDuration);
            else
                Animator.Play(Clip);
            if (lockAnimator)
            {
                AnimatorLocked = true;
                if (duration == -1f)
                    StartCoroutine(UnlockAnimator(AnimationLength(Clip), lockedWaitEnd));
                else
                    StartCoroutine(UnlockAnimator(duration, lockedWaitEnd));
                NextClipToPlay = "Idle";
            }
        }
        else
            NextClipToPlay = Clip;
    }

    private float AnimationLength(string name)
    {
        float time = 0;
        RuntimeAnimatorController ac = Animator.runtimeAnimatorController;

        for (int i = 0; i < ac.animationClips.Length; i++)
            if (ac.animationClips[i].name.ToLower().Contains(name.ToLower()))
                time = ac.animationClips[i].length;

        return time;
    }

    public void LookAtEntity(Entity entity)
    {
        transform.LookAt(entity.transform);
        Vector3 rot = transform.localEulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.localEulerAngles = rot;
    }

    public void LookAtCell(CellPos pos)
    {
        transform.LookAt(pos.ToVector3(0f));
        Vector3 rot = transform.localEulerAngles;
        rot.x = 0f;
        rot.z = 0f;
        transform.localEulerAngles = rot;
    }

    public void UnlockAnimator()
    {
        StopCoroutine("UnlockAnimator");
        AnimatorLocked = false;
        Play(NextClipToPlay);
    }

    IEnumerator UnlockAnimator(float duration, bool lockedWaitEnd)
    {
        if (lockedWaitEnd)
            DodgeFromLight.StartWaitingAction();
        yield return new WaitForSeconds(duration);
        AnimatorLocked = false;
        Play(NextClipToPlay);
        if (lockedWaitEnd)
            DodgeFromLight.StopWaitingAction();
    }
}