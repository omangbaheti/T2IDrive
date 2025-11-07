using DG.Tweening;
using EditorAttributes;
using UnityEngine;

public class Blinds : MonoBehaviour
{
    [SerializeField] private Transform leftBlind;
    [SerializeField] private Transform rightBlind;
    [SerializeField] private Transform frontBlind;

    [SerializeField] private Transform leftHiddenPos;
    [SerializeField] private Transform rightHiddenPos;
    [SerializeField] private Transform frontHiddenPos;
    [SerializeField] private Transform leftActivePos;
    [SerializeField] private Transform rightActivePos;
    [SerializeField] private Transform frontActivePos;
    [SerializeField] private float time = 0.5f;
    private void Start()
    {
        MoveBlindsDown();
    }
    
    [Button]
    public void MoveBlindsUp()
    {
        leftBlind.GetComponent<MeshRenderer>().enabled = true;
        rightBlind.GetComponent<MeshRenderer>().enabled = true;
        frontBlind.GetComponent<MeshRenderer>().enabled = true;
        leftBlind.DOMove(leftActivePos.position, time);
        rightBlind.DOMove(rightActivePos.position, time);
        frontBlind.DOMove(frontActivePos.position, time);
    }

    [Button]
    public void MoveBlindsDown()
    {
        leftBlind.DOMove(leftHiddenPos.position, time).OnComplete(() => leftBlind.GetComponent<MeshRenderer>().enabled = false);
        rightBlind.DOMove(rightHiddenPos.position, time).OnComplete(() => rightBlind.GetComponent<MeshRenderer>().enabled = false);
        frontBlind.DOMove(frontHiddenPos.position, time).OnComplete(() => frontBlind.GetComponent<MeshRenderer>().enabled = false);
    }
}
