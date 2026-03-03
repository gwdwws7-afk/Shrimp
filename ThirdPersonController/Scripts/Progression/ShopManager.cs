using UnityEngine;

namespace ThirdPersonController
{
    public class ShopManager : MonoBehaviour
    {
        public ConsumableCatalog catalog;
        public ConsumableInventory inventory;
        public CurrencyWallet wallet;

        [Header("Pricing")]
        public int levelDifficulty = 1;
        public float priceMultiplier = 1f;

        private void Awake()
        {
            if (catalog == null)
            {
                catalog = Resources.Load<ConsumableCatalog>("ConsumableCatalog") ?? ConsumableCatalog.CreateDefault();
            }

            if (inventory == null)
            {
                inventory = ConsumableInventory.EnsureInstance();
            }

            if (wallet == null)
            {
                wallet = CurrencyWallet.EnsureInstance();
            }
        }

        public int GetPrice(string itemId)
        {
            ConsumableDefinition item = catalog != null ? catalog.GetById(itemId) : null;
            if (item == null)
            {
                return 0;
            }

            return EconomyService.AdjustShopPrice(item.price, levelDifficulty, priceMultiplier);
        }

        public bool Purchase(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return false;
            }

            ConsumableDefinition item = catalog != null ? catalog.GetById(itemId) : null;
            if (item == null)
            {
                return false;
            }

            int price = GetPrice(itemId);
            int totalPrice = price * quantity;
            if (wallet == null || !wallet.SpendCredits(totalPrice))
            {
                return false;
            }

            if (inventory == null)
            {
                return false;
            }

            bool added = inventory.Add(itemId, quantity);
            if (!added)
            {
                wallet.AddCredits(totalPrice);
                return false;
            }

            GameEvents.ShowMessage($"购买 {item.displayName} x{quantity}", 1.4f);
            return true;
        }
    }
}
