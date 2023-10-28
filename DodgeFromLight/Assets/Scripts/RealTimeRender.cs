using UnityEngine;

public class RealTimeRender : MonoBehaviour
{
    public Camera Cam;
    public Transform ModelAnchor;
    public Transform UpCamAnchor;
    public Transform DownCamAnchor;
    public Transform LeftCamAnchor;
    public Transform RightCamAnchor;
    [HideInInspector]
    public RenderTexture Texture;
    [HideInInspector]
    public GameObject EntityModel;
    [HideInInspector]
    public Animator Animator;
    private CamPos UpCamPos;
    private CamPos DownCamPos;
    private CamPos LeftCamPos;
    private CamPos RightCamPos;

    void Awake()
    {
        UpCamPos = new CamPos(UpCamAnchor.localPosition, UpCamAnchor.localRotation);
        DownCamPos = new CamPos(DownCamAnchor.localPosition, DownCamAnchor.localRotation);
        LeftCamPos = new CamPos(LeftCamAnchor.localPosition, LeftCamAnchor.localRotation);
        RightCamPos = new CamPos(RightCamAnchor.localPosition, RightCamAnchor.localRotation);
        Destroy(UpCamAnchor.gameObject);
        Destroy(DownCamAnchor.gameObject);
        Destroy(LeftCamAnchor.gameObject);
        Destroy(RightCamAnchor.gameObject);

        Texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        Texture.Create();
        Cam.targetTexture = Texture;
    }

    public GameObject SpawnEntity(GameObject EntityPrefab)
    {
        EntityModel = Instantiate(EntityPrefab);
        EntityModel.transform.SetParent(ModelAnchor);
        EntityModel.transform.localPosition = Vector3.zero;
        EntityModel.transform.localRotation = Quaternion.identity;
        EntityModel.layer = ModelAnchor.gameObject.layer;
        Animator = EntityModel.GetComponentInChildren<Animator>();
        return EntityModel;
    }

    public void SetOrientation(Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.Up:
                SetCamPos(UpCamPos);
                break;
            case Orientation.Down:
                SetCamPos(DownCamPos);
                break;
            case Orientation.Left:
                SetCamPos(LeftCamPos);
                break;
            case Orientation.Right:
                SetCamPos(RightCamPos);
                break;
        }
    }

    public void SetCamPos(CamPos pos)
    {
        Cam.transform.localPosition = pos.pos;
        Cam.transform.localRotation = pos.rot;
    }
}

public struct CamPos
{
    public Vector3 pos;
    public Quaternion rot;

    public CamPos(Vector3 _pos, Quaternion _rot)
    {
        pos = _pos;
        rot = _rot;
    }
}