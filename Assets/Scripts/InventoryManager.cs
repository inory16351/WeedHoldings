using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 수확한 식물의 보유량을 관리하는 인벤토리 매니저.
    /// 재배 화면 우측 리스트의 "보유량" 표시에 사용됩니다.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        // plantID -> 보유 수량
        private Dictionary<int, int> inventory = new Dictionary<int, int>();

        public event System.Action OnInventoryChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int GetCount(int plantID)
        {
            inventory.TryGetValue(plantID, out int count);
            return count;
        }

        public void AddItem(int plantID, int amount)
        {
            if (!inventory.ContainsKey(plantID))
                inventory[plantID] = 0;
            inventory[plantID] += amount;
            OnInventoryChanged?.Invoke();
        }

        public bool UseItem(int plantID, int amount)
        {
            if (GetCount(plantID) < amount) return false;
            inventory[plantID] -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public Dictionary<int, int> GetAll() => new Dictionary<int, int>(inventory);
    }
}
