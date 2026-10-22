using UnityEngine;
using UnityEngine.UI;
using SkullKing.Data;

namespace SkullKing.UI
{
    // Separate UI layer: artwork and game rules remain independent of the frame.
    public sealed class CardFrameView : MonoBehaviour
    {
        private GameObject _proceduralFrame;
        private Image _artworkFrame;
        private Image _trim;
        private Text _type;
        private Text _bottomNumber;
        private GameObject _hoverOutline;
        private bool _isNumberCard;

        public void Build(Text source)
        {
            var root = (RectTransform)transform;
            Place(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _proceduralFrame = new GameObject("ProceduralFrame", typeof(RectTransform));
            _proceduralFrame.transform.SetParent(transform, false);
            Place((RectTransform)_proceduralFrame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Panel("HeaderPlate", new Vector2(.03f, .82f), new Vector2(.97f, .98f), new Color(.94f, .93f, .89f), _proceduralFrame.transform);
            Edge("Left", new Vector2(0, 0), new Vector2(.027f, 1));
            Edge("Right", new Vector2(.973f, 0), Vector2.one);
            Edge("Top", new Vector2(0, .981f), Vector2.one);
            Edge("Bottom", Vector2.zero, new Vector2(1, .019f));
            _trim = Panel("TypePlate", new Vector2(.07f, .04f), new Vector2(.93f, .135f), Color.black, _proceduralFrame.transform);
            var go = new GameObject("CardType", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(_trim.transform, false);
            _type = go.GetComponent<Text>();
            Place(_type.rectTransform, Vector2.zero, Vector2.one, new Vector2(2, 0), new Vector2(-2, 0));
            _type.font = source != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _type.fontSize = 11;
            _type.resizeTextForBestFit = true;
            _type.resizeTextMinSize = 7;
            _type.resizeTextMaxSize = 11;
            _type.alignment = TextAnchor.MiddleCenter;
            _type.color = Color.white;
            _type.raycastTarget = false;

            var frameObject = new GameObject("ArtworkFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameObject.transform.SetParent(transform, false);
            _artworkFrame = frameObject.GetComponent<Image>();
            Place(_artworkFrame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _artworkFrame.color = Color.white;
            _artworkFrame.preserveAspect = false;
            _artworkFrame.type = Image.Type.Simple;
            _artworkFrame.raycastTarget = false;
            _artworkFrame.gameObject.SetActive(false);

            var numberObject = new GameObject("BottomRightNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            numberObject.transform.SetParent(transform, false);
            _bottomNumber = numberObject.GetComponent<Text>();
            Place(_bottomNumber.rectTransform, new Vector2(.76f, .025f), new Vector2(.96f, .18f), Vector2.zero, Vector2.zero);
            _bottomNumber.font = source != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _bottomNumber.fontStyle = FontStyle.Bold;
            _bottomNumber.fontSize = 22;
            _bottomNumber.resizeTextForBestFit = true;
            _bottomNumber.resizeTextMinSize = 10;
            _bottomNumber.resizeTextMaxSize = 22;
            _bottomNumber.alignment = TextAnchor.MiddleCenter;
            _bottomNumber.color = Color.white;
            _bottomNumber.raycastTarget = false;
            _bottomNumber.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            _hoverOutline = new GameObject("HoverOutline", typeof(RectTransform));
            _hoverOutline.transform.SetParent(transform, false);
            Place((RectTransform)_hoverOutline.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Color highlight = new Color(1f, 1f, 1f, .95f);
            Panel("HoverLeft", Vector2.zero, new Vector2(.018f, 1f), highlight, _hoverOutline.transform);
            Panel("HoverRight", new Vector2(.982f, 0f), Vector2.one, highlight, _hoverOutline.transform);
            Panel("HoverTop", new Vector2(0f, .982f), Vector2.one, highlight, _hoverOutline.transform);
            Panel("HoverBottom", Vector2.zero, new Vector2(1f, .018f), highlight, _hoverOutline.transform);
            _hoverOutline.SetActive(false);
        }

        private void Edge(string label, Vector2 min, Vector2 max)
        {
            Panel(label, min, max, new Color(.12f, .13f, .18f), _proceduralFrame.transform);
            // Fine champagne keyline inside each dark edge.
            if (max.x - min.x < .1f) { min.x += .009f; max.x -= .009f; }
            else { min.y += .006f; max.y -= .006f; }
            Panel(label + "Keyline", min, max, new Color(.76f, .69f, .51f), _proceduralFrame.transform);
        }

        private Image Panel(string label, Vector2 min, Vector2 max, Color color, Transform parent)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Place(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
            return image;
        }

        public void Show(CardData card, Color accent)
        {
            gameObject.SetActive(true);
            _isNumberCard = card.IsNumberCard;
            _trim.color = Color.Lerp(accent, Color.black, .3f);
            _bottomNumber.text = card.IsNumberCard ? card.Number.ToString() : string.Empty;
            _bottomNumber.gameObject.SetActive(card.IsNumberCard);
            _type.text = card.IsNumberCard ? card.Suit switch
            {
                CardSuit.Green => "무협 · 숫자",
                CardSuit.Yellow => "판타지 · 숫자",
                CardSuit.Purple => "사이버펑크 · 숫자",
                CardSuit.Black => "공허 · 으뜸패",
                _ => "숫자"
            } : CrossionCardNames.Role(card.Special);
        }

        public void SetArtworkFrame(Sprite frame)
        {
            bool hasFrame = frame != null;
            _artworkFrame.sprite = frame;
            // 프레임 PNG와 카드가 모두 13:19이므로 원본 전체를 그대로 사용합니다.
            // 이렇게 해야 네 모서리 장식과 하단 이름판이 잘리거나 늘어나지 않습니다.
            _artworkFrame.type = Image.Type.Simple;
            _artworkFrame.preserveAspect = false;

            _artworkFrame.gameObject.SetActive(hasFrame);
            _proceduralFrame.SetActive(!hasFrame);

            // 전용 프레임은 하단 이름판과 네 모서리 장식을 자체적으로 포함합니다.
            _bottomNumber.color = hasFrame ? Color.white : Color.black;
            _bottomNumber.transform.SetAsLastSibling();
        }

        public void SetComposedMode(bool isComposed)
        {
            _artworkFrame.sprite = null;
            _artworkFrame.gameObject.SetActive(false);
            _proceduralFrame.SetActive(!isComposed);
            _bottomNumber.gameObject.SetActive(!isComposed && _isNumberCard);
            _bottomNumber.transform.SetAsLastSibling();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_hoverOutline == null) return;
            _hoverOutline.SetActive(highlighted);
            if (highlighted)
                _hoverOutline.transform.SetAsLastSibling();
        }

        internal static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
