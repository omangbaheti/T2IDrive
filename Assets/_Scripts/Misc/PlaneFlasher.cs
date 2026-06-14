using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlaneFlasher : MonoBehaviour
{
    [SerializeField] Color _flashColor;
    [SerializeField] private float waitTime;
    Color defaultColor;

    private HotSwapColor panelBG;
    void Start()
    {
        panelBG = transform.GetComponentInChildren<HotSwapColor>();
        defaultColor = panelBG.CurrentColor;
    }

    public IEnumerator Flash()
    {
        panelBG.SetColor(_flashColor);
        yield return new WaitForSeconds(waitTime);
        panelBG.SetColor(defaultColor);
    }
}
