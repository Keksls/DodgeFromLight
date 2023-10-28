using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Notifications : MonoBehaviour
{
    public Transform Container;
    public GameObject NotificationPrefab;

    private void Awake()
    {
        DodgeFromLight.UI_Notifications = this;
    }

    public void Notify(string message, float duration = 5f)
    {
        StartCoroutine(NotifyRoutine(message, duration));
    }

    IEnumerator NotifyRoutine(string message, float duration)
    {
        GameObject notif = Instantiate(NotificationPrefab);
        TextMeshProUGUI text = notif.GetComponentInChildren<TextMeshProUGUI>();
        text.text = message;
        notif.transform.SetParent(Container, false);
        Button button = notif.GetComponentInChildren<Button>();
        float enlapsed = 0f;
        button.onClick.AddListener(() => { enlapsed = duration; });
        while (enlapsed < duration)
        {
            yield return null;
            enlapsed += Time.deltaTime;
        }
        Destroy(notif);
    }
}