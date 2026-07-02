# ShopCategorySelector

**Что это:** `Neo.Shop.ShopCategorySelector` — NoCode-«пилюля» категорий со стрелками prev/next: циклически переключает сериализованный список категорий и вызывает `ShopListView.SetCategory(id)`. Дополняет `ShopCategoryButton` (кнопка = категория) для магазинов, где категории листаются, а не открываются табами.

**Как использовать:** повесить на пилюлю, назначить `ShopListView`, кнопки prev/next, `Image` иконки и `TMP_Text` имени; заполнить список категорий (`id` = `ShopItemData.Category`, пустой id = все товары). API: `Next()`, `Prev()`, `Select(id)`, `CurrentCategoryId`.
