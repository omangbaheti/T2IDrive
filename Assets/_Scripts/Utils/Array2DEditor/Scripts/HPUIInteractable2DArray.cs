using System;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace Array2DEditor
{
    [Serializable]
    public class HPUIInteractable2DArray : Array2D<HPUIMeshContinuousInteractable>
    {
        [SerializeField] private CellRowContinuousInteractable[] cells = new CellRowContinuousInteractable[Consts.defaultGridSize];
        protected override CellRow<HPUIMeshContinuousInteractable> GetCellRow(int idx)
        {
            return cells[cells.Length - idx - 1];
        }
    }
    
    [Serializable]
    public class CellRowContinuousInteractable : CellRow<HPUIMeshContinuousInteractable> { }
}
