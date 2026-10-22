using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>Only animates the Unity-authored visual child, never the button's hit area.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class CouncilButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private RectTransform _visual;
        [SerializeField] private Outline _rim;
        [SerializeField] private Color _hoverRim = new Color(.35f, 1f, .84f, .65f);
        [SerializeField] private float _hoverScale = 1.025f;
        [SerializeField] private float _pressedScale = .975f;
        [SerializeField] private float _response = 14f;
        private Button _button;
        private bool _hover, _selected, _pressed;
        private Vector3 _restScale;
        private bool _captured;
        private float _light;

        private void Awake() { _button = GetComponent<Button>(); Capture(); }
        private void Capture()
        {
            if (_captured || _visual == null) return;
            _restScale = _visual.localScale;
            _captured = true;
        }
        private void Update()
        {
            Capture();
            bool enabledButton = _button != null && _button.IsInteractable();
            bool lit = enabledButton && (_hover || _selected);
            float scale = enabledButton && _pressed ? _pressedScale : lit ? _hoverScale : 1;
            float step = 1 - Mathf.Exp(-_response * Time.unscaledDeltaTime);
            if (_visual != null) _visual.localScale = Vector3.Lerp(_visual.localScale, _restScale * scale, step);
            _light = Mathf.Lerp(_light, lit ? 1 : 0, step);
            if (_rim != null)
            {
                Color color = _hoverRim;
                color.a *= _light;
                _rim.effectColor = color;
            }
        }
        private void OnDisable()
        {
            _hover = _selected = _pressed = false;
            _light = 0;
            if (_visual != null && _captured) _visual.localScale = _restScale;
            if (_rim != null) _rim.effectColor = Color.clear;
        }
        public void OnPointerEnter(PointerEventData data) => _hover = true;
        public void OnPointerExit(PointerEventData data) { _hover = false; _pressed = false; }
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) _pressed = true; }
        public void OnPointerUp(PointerEventData data) => _pressed = false;
        public void OnSelect(BaseEventData data) => _selected = true;
        public void OnDeselect(BaseEventData data) { _selected = false; _pressed = false; }
    }
}
