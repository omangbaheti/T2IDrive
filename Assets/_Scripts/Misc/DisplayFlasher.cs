using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DisplayFlasher : MonoBehaviour
{
    [SerializeField] Color _flashColor;
    Color defaultColor;

    private Image panelBG;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panelBG = GetComponent<Image>();
        defaultColor = panelBG.color;
    }

    public void Flash()
    {
        panelBG.color = _flashColor;
        panelBG.DOColor(defaultColor, 0.2f);
    }

    public void Flash(Color color)
    {
        panelBG.color = color;
        panelBG.DOColor(defaultColor, 0.2f);
    }
}
