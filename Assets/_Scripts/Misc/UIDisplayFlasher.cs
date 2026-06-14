using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIDisplayFlasher : MonoBehaviour
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
        Color color = new Color(panelBG.color.r, panelBG.color.b, panelBG.color.g, panelBG.color.a);
        panelBG.color = _flashColor;
        panelBG.DOColor(color, 0.2f);
    }

    public void Flash(Color color)
    {
        
        Color prevColor = new Color(panelBG.color.r, panelBG.color.b, panelBG.color.g, panelBG.color.a);
        panelBG.color = color;
        panelBG.DOColor(prevColor, 0.2f);
    }

    public void SetColor(Color color)
    {
        panelBG.DOColor(color, 0.2f);
    }
}
