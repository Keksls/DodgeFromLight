using UnityEngine;
using UnityEngine.UI;

public class UI_WorkerNotifier : MonoBehaviour
{
    public Text text;
    public GameObject Worker;
    public bool IsMain = true;

    private void Awake()
    {
        Hide();
        if (IsMain)
            DodgeFromLight.UI_WorkerNotifier = this;
    }

    public void Show(string msg)
    {
        if (!Worker)
            return;
        SetText(msg);
        Worker.SetActive(true);
    }

    public void SetText(string msg)
    {
        if (text)
            text.text = msg;
    }

    public void Hide()
    {
        if (Worker)
            Worker.SetActive(false);
    }
}