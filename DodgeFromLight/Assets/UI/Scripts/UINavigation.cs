using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UINavigation : MonoBehaviour
{
    public Selectable FirstElement;
    private GameObject current;
    EventSystem system;

    void OnEnable()
    {
        system = EventSystem.current;
        if (FirstElement != null)
        {
            FirstElement.Select();
            InputField inputfield = FirstElement.GetComponent<InputField>();
            if (inputfield != null)
                inputfield.OnPointerClick(new PointerEventData(system));
            system.SetSelectedGameObject(FirstElement.gameObject, new BaseEventData(system));
            current = FirstElement.gameObject;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = current.GetComponent<Selectable>().FindSelectableOnDown();

            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                    inputfield.OnPointerClick(new PointerEventData(system));

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                current = next.gameObject;
            }
        }
    }
}