using TMPro;
using UnityEngine;

public class AnimateTextGradient : MonoBehaviour
{
    public float speed = 1f;
    public bool AuoInitialize = false;
    TextMeshProUGUI text;
    private float _timePassed;
    private VertexGradient baseGradient;
    private VertexGradient newGradient;
    private bool initialized = false;

    private void Start()
    {
        if (!AuoInitialize)
            return;
        Initialize();
    }

    public void Initialize()
    {
        text = GetComponent<TextMeshProUGUI>();
        baseGradient = text.colorGradient;
        initialized = true;
        _timePassed = 0f;
    }

    void Update()
    {
        if (!initialized)
            return;
        _timePassed += Time.deltaTime * speed;
        float lerpVal = Mathf.PingPong(_timePassed, 1f);
        newGradient = text.colorGradient;
        newGradient.topLeft = Color.Lerp(baseGradient.topLeft, baseGradient.bottomLeft, lerpVal);
        newGradient.topRight = Color.Lerp(baseGradient.topRight, baseGradient.bottomRight, lerpVal);
        newGradient.bottomLeft = Color.Lerp(baseGradient.bottomLeft, baseGradient.topLeft, lerpVal);
        newGradient.bottomRight = Color.Lerp(baseGradient.bottomRight, baseGradient.topRight, lerpVal);
        text.colorGradient = newGradient;
    }
}