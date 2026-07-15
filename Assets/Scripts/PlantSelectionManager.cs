using System;
using UnityEngine;

namespace WeedHoldings
{
    public class PlantSelectionManager : MonoBehaviour
    {
        public static PlantSelectionManager Instance { get; private set; }

        public PlantData selectedPlant { get; private set; }

        public bool HasSelection => selectedPlant != null;

        public event Action OnSelectionChanged;

        void Awake()
        {
            Instance = this;
        }

        public void SelectPlant(PlantData plant)
        {
            selectedPlant = plant;
            OnSelectionChanged?.Invoke();
        }

        // Only clear when explicitly requested (e.g., panel switch)
        public void ClearSelection()
        {
            selectedPlant = null;
            OnSelectionChanged?.Invoke();
        }
    }
}
