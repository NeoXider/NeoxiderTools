# ShopResolver

`Neo.Shop.ShopResolver` centralizes the scene-reference fallback used by shop views. It first searches the context component's parent hierarchy and only then uses `Object.FindFirstObjectByType<T>()`.

```csharp
Shop shop = ShopResolver.Resolve<Shop>(this);
if (ShopResolver.TryResolve(this, out ShopListView listView))
{
    // Use the closest configured view, or the scene fallback.
}
```

Prefer an explicit serialized reference when the relationship is stable. Use this resolver for optional scene wiring where the established Shop fallback policy is desired. A null context returns null and never starts a global search.

Runtime source: `Assets/Neoxider/Scripts/Shop/ShopResolver.cs`.

Focused EditMode coverage: `Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs`.
