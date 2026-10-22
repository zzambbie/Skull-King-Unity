using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>
    /// A help-only presentation layer. Reuses the authored page text and navigation callbacks;
    /// never changes gameplay, the title menu, settings, feedback or the saved scene hierarchy.
    /// </summary>
    public sealed class IllustratedRulebookView : MonoBehaviour
    {
        private static readonly Vector2 DesignSize = new Vector2(1320, 840);
        private static readonly Color Ivory = new Color(.95f, .93f, .85f);
        private static readonly Color Gold = new Color(.91f, .75f, .42f);
        private RectTransform _window;
        private GameObject _panel;
        private TMP_Text _summary;
        private TMP_Text _caption;
        private TMP_Text _result;
        private Image _art;
        private readonly TMP_Text[] _examples = new TMP_Text[3];
        private readonly GameObject[] _examplePanels = new GameObject[3];
        private Vector2 _lastAvailableSize;

        public bool IsReady => _window != null;

        public void BuildIfNeeded(GameObject panel, TMP_Text title, TMP_Text body,
            TMP_Text pageNumber, ScrollRect oldScroll)
        {
            if (IsReady) { FitWindow(); return; }
            if (panel == null || title == null || body == null) return;
            _window = title.transform.parent as RectTransform;
            if (_window == null) return;
            _panel = panel;
            // Keep the modal and its raycaster above toolbars and all normal gameplay canvases.
            var modalCanvas = panel.GetComponent<Canvas>();
            if (modalCanvas == null) modalCanvas = panel.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 20000;
            if (panel.GetComponent<GraphicRaycaster>() == null) panel.AddComponent<GraphicRaycaster>();
            var dimmer = panel.GetComponent<Image>();
            if (dimmer != null) { dimmer.color = new Color(0, 0, 0, .78f); dimmer.raycastTarget = true; }
            _window.anchorMin = _window.anchorMax = _window.pivot = new Vector2(.5f, .5f);
            _window.anchoredPosition = Vector2.zero;
            _window.sizeDelta = DesignSize;

            // No scroll: the same bound body moves out of the old masked viewport into a fixed page.
            body.transform.SetParent(_window, false);
            var fitter = body.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            if (oldScroll != null) { oldScroll.StopMovement(); oldScroll.gameObject.SetActive(false); }
            Place(body.rectTransform, new Vector2(265, 20), new Vector2(700, 420));
            body.alignment = TextAlignmentOptions.TopLeft;
            body.fontSize = 27;
            body.enableAutoSizing = false;
            body.richText = false;
            body.lineSpacing = 4;
            body.paragraphSpacing = 8;
            body.margin = Vector4.zero;
            body.overflowMode = TextOverflowModes.Overflow;
            body.raycastTarget = false;
            body.color = Ivory;

            Place(title.rectTransform, new Vector2(0, 329), new Vector2(1150, 52));
            title.fontSize = 35;
            title.enableAutoSizing = true;
            title.fontSizeMin = 29;
            title.fontSizeMax = 35;
            title.color = Gold;
            title.alignment = TextAlignmentOptions.Center;
            title.richText = false;

            var heading = _window.Find("Title");
            if (heading != null && heading.TryGetComponent<TMP_Text>(out var headingText))
            {
                Place(headingText.rectTransform, new Vector2(0, 382), new Vector2(830, 34));
                headingText.text = "CROSSION  /  그림으로 배우는 규칙";
                headingText.fontSize = 22;
                headingText.color = Ivory;
            }
            EnsureSurface(_window, new Color(.025f, .045f, .07f), new Color(.055f, .09f, .13f), Gold);

            _summary = Label("RulebookSummary", _window, body.font, new Vector2(0, 272), new Vector2(1190, 55), 29);
            _summary.color = Gold;

            var artRect = Rect("RulebookIllustration", _window, new Vector2(-390, 59), new Vector2(465, 310));
            _art = artRect.gameObject.AddComponent<Image>();
            _art.preserveAspect = true;
            _art.raycastTarget = false;
            _caption = Label("IllustrationCaption", _window, body.font, new Vector2(-390, -143), new Vector2(465, 76), 23);
            _caption.color = new Color(.72f, .82f, .85f);

            for (int i = 0; i < 3; i++)
            {
                var rect = Rect("RuleExample" + i, _window, new Vector2((i - 1) * 390, -241), new Vector2(355, 82));
                EnsureSurface(rect, new Color(.025f, .06f, .085f), new Color(.065f, .11f, .15f), Gold * .72f);
                _examplePanels[i] = rect.gameObject;
                _examples[i] = Label("Caption", rect, body.font, Vector2.zero, new Vector2(325, 68), 25);
                _examples[i].fontStyle = FontStyles.Bold;
            }
            _result = Label("RuleExampleResult", _window, body.font, new Vector2(0, -309), new Vector2(1190, 42), 25);
            _result.color = Gold;

            if (pageNumber != null)
            {
                Place(pageNumber.rectTransform, new Vector2(0, -365), new Vector2(180, 42));
                pageNumber.fontSize = 24;
                pageNumber.color = Gold;
            }
            MoveNavigation("PreviousPage", new Vector2(-260, -365), new Vector2(180, 48));
            MoveNavigation("NextPage", new Vector2(260, -365), new Vector2(180, 48));
            MoveNavigation("CloseRules", new Vector2(580, 382), new Vector2(110, 40));
            var notice = Label("RulebookPlayNotice", _window, body.font, new Vector2(0, -406), new Vector2(1130, 24), 17);
            notice.text = "규칙 창을 열어도 게임은 계속돼요. 멀티플레이에서는 내 차례를 확인해 주세요.";
            notice.color = new Color(.65f, .72f, .76f);
            FitWindow();
        }

        public void Show(RulebookContent.Page page)
        {
            if (!IsReady || page == null) return;
            _summary.text = page.summary ?? "";
            _caption.text = page.illustrationCaption ?? "";
            _art.sprite = page.illustration;
            _art.enabled = page.illustration != null;
            string[] examples = { page.exampleA, page.exampleB, page.exampleC };
            for (int i = 0; i < 3; i++)
            {
                _examplePanels[i].SetActive(!string.IsNullOrEmpty(examples[i]));
                _examples[i].text = examples[i] ?? "";
            }
            _result.text = page.exampleResult ?? "";
            FitWindow();
        }

        private void LateUpdate()
        {
            if (_panel != null && _panel.activeInHierarchy) FitWindow();
        }

        private void FitWindow()
        {
            if (_window == null || !(_window.parent is RectTransform available)) return;
            Vector2 size = available.rect.size;
            if (size.x <= 1 || size.y <= 1 || size == _lastAvailableSize) return;
            _lastAvailableSize = size;
            float scale = Mathf.Min(1f, size.x * .94f / DesignSize.x, size.y * .94f / DesignSize.y);
            _window.localScale = Vector3.one * scale;
        }

        private void MoveNavigation(string name, Vector2 position, Vector2 size)
        {
            var child = _window.Find(name) as RectTransform;
            if (child != null) Place(child, position, size);
        }

        private static void EnsureSurface(RectTransform rect, Color bottom, Color top, Color trim)
        {
            var existing = rect.Find("CouncilSurface");
            if (existing != null && existing.TryGetComponent<CouncilPanelGraphic>(out var existingPlate))
            {
                // A rulebook is read over a busy game/background: keep its text surface opaque.
                existingPlate.color = Color.white;
                existingPlate.Configure(bottom, top, trim, 13, 1.4f);
                return;
            }
            var surface = Rect("RulebookSurface", rect, Vector2.zero, Vector2.zero);
            surface.SetAsFirstSibling();
            surface.anchorMin = Vector2.zero;
            surface.anchorMax = Vector2.one;
            surface.offsetMin = surface.offsetMax = Vector2.zero;
            var plate = surface.gameObject.AddComponent<CouncilPanelGraphic>();
            plate.Configure(bottom, top, trim, 13, 1.4f);
            plate.raycastTarget = false;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 position, Vector2 size, int fontSize)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize - 3;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Ivory;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, position, size);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
