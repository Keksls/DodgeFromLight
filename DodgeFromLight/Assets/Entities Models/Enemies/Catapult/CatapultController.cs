using DFLCommonNetwork.GameEngine;
using System.Collections;
using UnityEngine;

public class CatapultController : MonoBehaviour
{
    public Animator Animator;
    public AnimationCurve ThrowCurve;
    public float YThrowUp = 10f;
    public Transform Rock;
    public GameObject VFX;

    public IEnumerator ThrowAnimation(CellPos targetCell)
    {
        float duration = (DodgeFromLight.GameManager != null ? DodgeFromLight.GameManager.EntitiesMovementDuration : 0.15f);
        float enlapsed = 0f;
        Vector3 startPos = Rock.position;
        Vector3 endPos = targetCell.ToVector3(0f);
        PoolableObject rock = PoolManager.Instance.GetPoolable(PoolName.Rock);
        while (enlapsed < duration)
        {
            Vector3 pos = Vector3.Lerp(startPos, endPos, enlapsed / duration);
            pos.y += ThrowCurve.Evaluate(enlapsed / duration) * YThrowUp;
            rock.Transform.position = pos;
            yield return null;
            enlapsed += Time.deltaTime;
        }
        rock.Transform.position = endPos;
        GameObject vfx = Instantiate(VFX);
        vfx.transform.position = rock.Transform.position;
        rock.SetLifeTime(0.1f);
    }

    public void Throw(CellPos targetCell)
    {
        Animator.Play("Throw");
        StartCoroutine(ThrowAnimation(targetCell));
    }

    public void Reload()
    {
        Animator.Play("Reload");
    }
}