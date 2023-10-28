using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CarouselView : MonoBehaviour
{
    public Transform HeaderContainer;
    public GameObject HeaderItemPrefab;
    public GameObject Body;
    private RectTransform[] images;
    public float ImgWidth = 152f;

    private bool canSwipe;
    private float lerpTimer;
    private float lerpPosition;
    private float mousePositionStartX;
    private float mousePositionEndX;
    private float dragAmount;
    private float screenPosition;
    private float lastScreenPosition;

    public float image_gap = 30;
    public int swipeThrustHold = 30;
    private int m_currentIndex;
    public int CurrentIndex { get { return m_currentIndex; } }

    #region mono
    void Start()
    {
        images = new RectTransform[DodgeFromLight.Databases.GlossaryData.GlossaryItems.Length];
        int index = 0;
        foreach (GlossaryDataItem data in DodgeFromLight.Databases.GlossaryData.GlossaryItems)
        {
            GameObject item = Instantiate(HeaderItemPrefab);
            item.transform.SetParent(HeaderContainer, false);
            item.transform.GetChild(0).GetChild(1).GetComponent<Image>().sprite = data.Image;
            images[index] = item.GetComponent<RectTransform>();

            item.GetComponent<Button>().onClick.RemoveAllListeners();
            int i = index;
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                GoToIndexSmooth(i);
            });
            index++;
        }

        for (int i = 1; i < images.Length; i++)
        {
            images[i].anchoredPosition = new Vector2(((ImgWidth + image_gap) * i), 0);
        }
        SetIndex(0);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCarouselView();
    }
    #endregion

    #region private methods
    void UpdateCarouselView()
    {
        lerpTimer = lerpTimer + Time.deltaTime;

        if (lerpTimer < 0.333f)
        {
            screenPosition = Mathf.Lerp(lastScreenPosition, lerpPosition * -1, lerpTimer * 3);
            lastScreenPosition = screenPosition;
        }

       /* if (Input.GetMouseButtonDown(0))
        {
            canSwipe = true;
            mousePositionStartX = Input.mousePosition.x;
        }


        if (Input.GetMouseButton(0))
        {
            if (canSwipe)
            {
                mousePositionEndX = Input.mousePosition.x;
                dragAmount = mousePositionEndX - mousePositionStartX;
                screenPosition = lastScreenPosition + dragAmount;
            }
        }*/

        if (Mathf.Abs(dragAmount) > swipeThrustHold && canSwipe)
        {
            canSwipe = false;
            lastScreenPosition = screenPosition;
            if (m_currentIndex < images.Length)
                OnSwipeComplete();
            else if (m_currentIndex == images.Length && dragAmount < 0)
                lerpTimer = 0;
            else if (m_currentIndex == images.Length && dragAmount > 0)
                OnSwipeComplete();
        }

        for (int i = 0; i < images.Length; i++)
        {
            images[i].anchoredPosition = new Vector2(screenPosition + ((ImgWidth + image_gap) * i), 0);
            if (i == m_currentIndex)
            {
                images[i].localScale = Vector3.Lerp(images[i].localScale, new Vector3(1.1f, 1.1f, 1.1f), Time.deltaTime * 5);
            }
            else
            {
                images[i].localScale = Vector3.Lerp(images[i].localScale, new Vector3(0.8f, 0.8f, 0.8f), Time.deltaTime * 5);
            }
        }
    }

    void OnSwipeComplete()
    {
        lastScreenPosition = screenPosition;

        if (dragAmount > 0)
        {
            if (dragAmount >= swipeThrustHold)
            {
                if (m_currentIndex == 0)
                {
                    lerpTimer = 0; lerpPosition = 0;
                }
                else
                {
                    SetIndex(m_currentIndex - 1);
                    lerpTimer = 0;
                    if (m_currentIndex < 0)
                        SetIndex(0);
                    lerpPosition = (ImgWidth + image_gap) * m_currentIndex;
                }
            }
            else
            {
                lerpTimer = 0;
            }
        }
        else if (dragAmount < 0)
        {
            if (Mathf.Abs(dragAmount) >= swipeThrustHold)
            {
                if (m_currentIndex == images.Length - 1)
                {
                    lerpTimer = 0;
                    lerpPosition = (ImgWidth + image_gap) * m_currentIndex;
                }
                else
                {
                    lerpTimer = 0;
                    SetIndex(m_currentIndex + 1);
                    lerpPosition = (ImgWidth + image_gap) * m_currentIndex;
                }
            }
            else
            {
                lerpTimer = 0;
            }
        }
        dragAmount = 0;
    }
    #endregion

    private void SetIndex(int index)
    {
        m_currentIndex = index;

        // bind body data
        GlossaryDataItem data = DodgeFromLight.Databases.GlossaryData.GlossaryItems[index];
        Body.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Image>().sprite = data.Image;
        Body.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = data.Name;
        Body.transform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = data.Description;
    }

    #region public methods
    public void GoToIndex(int value)
    {
        SetIndex(value);
        lerpTimer = 0;
        lerpPosition = (ImgWidth + image_gap) * m_currentIndex;
        screenPosition = lerpPosition * -1;
        lastScreenPosition = screenPosition;
        for (int i = 0; i < images.Length; i++)
        {
            images[i].anchoredPosition = new Vector2(screenPosition + ((ImgWidth + image_gap) * i), 0);
        }
    }

    public void GoToIndexSmooth(int value)
    {
        SetIndex(value);
        lerpTimer = 0;
        lerpPosition = (ImgWidth + image_gap) * m_currentIndex;
    }
    #endregion
}