using UnityEngine;
using UnityEngine.UI;
using Neo.Rpg.UI;
using Neo.Tools;

namespace Neo.Rpg.Demo
{
    /// <summary>
    /// World-space health bar for NPCs. 
    /// Auto-creates a Canvas using the new universal UI components.
    /// </summary>
    public class DemoNpcUI : MonoBehaviour
    {
        private void Start()
        {
            var combatant = GetComponentInParent<RpgCombatant>();
            if (combatant != null)
            {
                CreateWorldSpaceCanvas(combatant);
            }
        }

        private void CreateWorldSpaceCanvas(RpgCombatant combatant)
        {
            var canvasObj = new GameObject("Npc_HpCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasObj.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;
            
            // 1. Add Universal Billboard Component
            var billboard = canvasObj.AddComponent<BillboardUniversal>();
            billboard.SetBillboardMode(BillboardUniversal.BillboardMode.AwayFromCamera);
            billboard.SetIgnoreY(true);

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 20);

            // 2. Add visual layout (Slider & Colors)
            var sliderObj = new GameObject("Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(canvasObj.transform, false);
            sliderObj.layer = LayerMask.NameToLayer("UI");
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.anchoredPosition = Vector2.zero;

            var hpSlider = sliderObj.AddComponent<Slider>();
            hpSlider.interactable = false;
            hpSlider.transition = Selectable.Transition.None;
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;

            var bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(sliderObj.transform, false);
            bgObj.layer = LayerMask.NameToLayer("UI");
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            bgObj.AddComponent<Image>().color = new Color(0.2f, 0.0f, 0.0f, 0.8f);

            var fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            fillAreaObj.layer = LayerMask.NameToLayer("UI");
            var faRect = fillAreaObj.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;
            faRect.anchoredPosition = Vector2.zero;

            var fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            fillObj.layer = LayerMask.NameToLayer("UI");
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            fillObj.AddComponent<Image>().color = Color.red;

            hpSlider.fillRect = fillRect;

            // 3. Add numeric HP text
            var hpTextObj = new GameObject("HpText", typeof(RectTransform));
            hpTextObj.transform.SetParent(canvasObj.transform, false);
            hpTextObj.layer = LayerMask.NameToLayer("UI");
            var hpTextRt = hpTextObj.GetComponent<RectTransform>();
            hpTextRt.anchorMin = Vector2.zero;
            hpTextRt.anchorMax = Vector2.one;
            hpTextRt.sizeDelta = Vector2.zero;
            hpTextRt.anchoredPosition = new Vector2(0, 0);

            var hpText = hpTextObj.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hpText.font == null) hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hpText.fontSize = 14;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            hpText.verticalOverflow = VerticalWrapMode.Overflow;
            // Add a slight outline for better visibility on all backgrounds
            var outline = hpTextObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            // 4. Add Universal HP Bar UI component
            var hpBar = canvasObj.AddComponent<RpgHpBarUI>();
            hpBar.hpSlider = hpSlider;
            hpBar.hpText = hpText;
            hpBar.Bind(combatant); // Connects to the RpgCombatant automatically
        }
    }
}
