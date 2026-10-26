# Shop bridges

`Neo.Shop.Bridges` provides the optional `Neo.Tools.ShopInventoryGrantBridge` component. It connects
[Shop](Shop.md) purchase events to [InventoryComponent](../Tools/Inventory/InventoryComponent.md)
without making the two base modules depend on each other. Reference `Neo.Shop.Bridges` in a custom
assembly that uses this component; its namespace remains `Neo.Tools` for compatibility.

## Scene setup

1. Add **Neoxider > Tools > Inventory > ShopInventoryGrantBridge** to an active scene object.
2. Assign **Shop** explicitly. If empty, `Awake` uses [ShopResolver](ShopResolver.md): search the
   object's parent hierarchy first, then the first active Shop in the scene. Assign a reference when
   multiple shops exist. A Shop created later must be supplied through `SetShop`.
3. Assign **Inventory**, or enable **Use Inventory Singleton** to use an already registered
   `InventoryComponent.Instance` when the explicit reference is missing. This bridge does not create
   an inventory or search the scene for one.
4. Add mappings from stable `ShopItemData.Id` strings to `InventoryItemData` assets and positive
   amounts. IDs match exactly, including case. Every matching mapping is processed; duplicate IDs
   can intentionally grant several inventory items. Missing item assets and non-positive amounts
   grant nothing.
5. Optionally connect **On Granted** to UI, audio or other scene feedback. It supplies the inventory
   item asset and the amount actually added, and fires only when that amount is positive.

The bridge subscribes to `Shop.OnPurchasedId` while enabled and active. Disabling the component or its
GameObject pauses automatic grants; enabling it resumes with one subscription to the current Shop.
`SetShop` detaches the previous event even if its source has already been destroyed. Changing shops
while disabled takes effect on the next enable. Purchases received while disabled are not replayed.

Shop emits `OnPurchasedId` for each item in a purchased bundle. No separate bundle listener or
Inspector connection is needed. Do not also connect the same purchase event manually to
`GrantForShopItemId`, since the automatic subscription would grant twice.

## C# API and UnityEvent targets

```csharp
using Neo.Tools;
using ShopController = Neo.Shop.Shop;

public static class InventoryShopSetup
{
    public static void Configure(ShopInventoryGrantBridge bridge, ShopController shop,
        InventoryComponent inventory, InventoryItemData potion)
    {
        bridge.SetInventory(inventory);
        bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping
        {
            ShopItemId = "potion_basic",
            InventoryItem = potion,
            Amount = 3
        });
        bridge.SetShop(shop);
    }
}
```

| API | Behavior |
| --- | --- |
| `Shop` | Currently assigned or resolved purchase source. |
| `Inventory` | Explicit inventory, otherwise the permitted singleton fallback. |
| `Mappings` | Mutable mapping list for runtime configuration. Configure outside grant callbacks. |
| `SetShop(Shop)` | Rebinds the automatic source while active; `null` detaches it. |
| `SetInventory(InventoryComponent)` | Changes the target; assigning a non-null target also disables singleton fallback. Passing `null` later does not re-enable that fallback. |
| `GrantForShopItemId(string)` | Processes matching mappings. Accepts dynamic string UnityEvent arguments; null, empty and unknown IDs grant nothing. |
| `GrantDirect(InventoryItemData, int)` | Calls the inventory API and returns the amount actually added. Returns zero for a null item, non-positive amount, missing inventory or rejected grant. |

Direct calls to either grant method remain available when the bridge is disabled, including explicit
UnityEvent wiring from another system. These calls grant items without performing a purchase or
checking Shop ownership. `OnGranted` uses the same typed event for automatic and explicit grants.

Inventory limits can accept part of a request or reject it completely. `OnGranted` and `GrantDirect`
report the accepted amount. The bridge is a notification adapter: it does not reserve capacity,
refund currency, queue rejected rewards or roll inventory changes back if a later purchase listener
fails. Projects requiring guaranteed delivery must coordinate purchase and inventory changes through
their transaction layer.

## 11.0 purchase notification contract

Shop commits the payment, profile ownership and profile save before dispatching purchase
notifications. The bridge then attempts inventory delivery from `OnPurchasedId`; this remains
best effort. A full inventory can accept zero items while the purchase stays paid and owned.
A partial grant reports only the amount actually accepted.

If the profile commit/save fails, Shop compensates the debit, restores its profile and does not
dispatch purchase delivery events. If a later observer throws, including an `OnGranted` listener
after inventory accepted items, Shop keeps the committed payment, profile and accepted items.
It reports the fault through `Shop.OnNotificationFault` and `Shop.LastNotificationFault` using
`ShopNotificationFault.PurchaseId`, `ItemId`, `Observer` and `Message`. A bridge feedback exception
is reported under the `OnPurchasedId` observer stage. It does not raise `OnPurchaseFailedId` or
trigger a refund. Later bundle item notifications and the bundle-completed notification are still
attempted.

This is an intentional change from earlier versions that could refund a purchase after inventory
delivery had already happened. Move validation that must reject a purchase before `Buy`/`BuyBundle`;
throwing from a notification is no longer a rejection mechanism. Monitor notification faults as
delivery/feedback errors, and check the actual inventory amount before reconciling a reward.
Blindly replaying a faulted purchase or grant can duplicate items already accepted.

UnityEvent invocation still stops at a throwing listener within that event, and a throwing
`OnGranted` callback can stop the remaining mappings for that item ID. Best effort does not guarantee
delivery to every listener or mapping. Nested `Buy`/`BuyBundle` calls during the same Shop purchase
are rejected before another debit; start an intentional subsequent purchase after the original call
returns.

For explicit C#/UnityEvent calls to `GrantDirect` or `GrantForShopItemId`, listener exceptions still
propagate to the caller after any accepted inventory changes. Those direct calls do not produce a
Shop notification-fault receipt unless they run inside Shop's purchase notification dispatch.

See also: [Shop module](README.md), [ShopItemData](ShopItemData.md),
[Inventory module](../Tools/Inventory/README.md).
