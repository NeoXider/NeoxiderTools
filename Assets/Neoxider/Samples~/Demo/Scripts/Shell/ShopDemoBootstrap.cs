using Neo.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Bootstrap for the Shop demo scene: tops the wallet up so purchases are demoable right away,
    ///     adds a "+100" coins button that calls the real <see cref="Money.Add" /> API and shows the
    ///     teaching card. The shop UI itself is the scene-authored <see cref="Neo.Shop.Shop" /> setup.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Shop Demo Bootstrap")]
    public sealed class ShopDemoBootstrap : MonoBehaviour
    {
        private const float DemoStartBalance = 200f;

        private void Start()
        {
            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Shop · Shop + Money",
                "Shop sells ShopItem cards through the Money wallet: ButtonPrice spends, " +
                "TextMoney reflects the reactive balance, purchases persist via SaveProvider.",
                "Click a card price — ButtonPrice -> Money.Spend(price)",
                "Bought items switch to Used / selected state",
                "+100 button calls Money.Add(100) — the same API your game uses",
                "Balance and purchases persist between plays (SaveProvider)");

            // WHY: the wallet persists between sessions and can arrive at 0 — top it up so the first
            // click can always demonstrate a successful purchase.
            if (Money.I != null && Money.I.money < DemoStartBalance)
            {
                Money.I.Add(DemoStartBalance - Money.I.money);
            }

            BuildAddCoinsButton();
        }

        private void BuildAddCoinsButton()
        {
            var canvasGo = new GameObject("[ShopDemo] Coins Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var buttonGo = new GameObject("Add Coins");
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(170f, 56f);

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.24f, 0.75f, 0.42f, 1f);

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            // WHY: Start already tolerates a scene without a Money wallet; the button must not NRE there.
            button.onClick.AddListener(() =>
            {
                if (Money.I != null)
                {
                    Money.I.Add(100f);
                }
                else
                {
                    Debug.LogWarning("ShopDemoBootstrap: no Money wallet in the scene — '+100 coins' has nothing to add.");
                }
            });

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "+100 coins";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
        }
    }
}
