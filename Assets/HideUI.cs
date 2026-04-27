using System;
using UnityEngine;
using UnityEngine.UI;

public class HideUI : MonoBehaviour
{
   [SerializeField] GameObject ui;

   [SerializeField] private Button button;

   private void OnEnable()
   {
      button.onClick.AddListener(hideUI); 
   }

   private void OnDisable()
   {
      button.onClick.RemoveListener(hideUI); 
   }
   public void hideUI()
   {
       ui.SetActive(!ui.activeSelf);
   }

   // Update is called once per frame
    void Update()
    {
        
    }
}
