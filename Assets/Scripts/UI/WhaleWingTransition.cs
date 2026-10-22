using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>Wings materialize around the submitted card; only its local footprint glows during the swap.</summary>
    public sealed class WhaleWingTransition : MonoBehaviour
    {
        [Header("Unity UI references")]
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _leftWing;
        [SerializeField] private Image _rightWing;
        [Tooltip("Legacy cover retained for scene compatibility; stays transparent with the radial bloom.")]
        [SerializeField] private Image _innerCover;
        [Header("Timing (seconds)")]
        [SerializeField, Min(.05f)] private float _closeSeconds = 2.4f;
        [SerializeField, Min(0f)] private float _coveredSeconds = .7f;
        [SerializeField, Min(.05f)] private float _openSeconds = 1.6f;
#pragma warning disable CS0414 // Serialized compatibility with archived full-screen setup tools.
        [SerializeField, HideInInspector] private Vector2 _wingSize = new Vector2(1.65f, 3.2f);
        [SerializeField, HideInInspector] private float _openOffset = 1.25f;
        [Tooltip("Negative offset overlaps the wing roots across the screen center.")]
        [SerializeField, HideInInspector] private float _closedOffset = .15f;
#pragma warning restore CS0414
        [SerializeField] private float _openAngle = 12f;
        [SerializeField] private Color _wingTint = Color.white;
        [SerializeField, HideInInspector] private int _layoutVersion;
        [Header("Card-local effect")]
        [SerializeField] private RectTransform _cardFocusArea;
        [SerializeField] private Graphic _cardGlow;
        [SerializeField] private Vector2 _cardWingSize = new Vector2(1.5f,1.2f);
        [SerializeField] private Vector2 _cardPadding = new Vector2(6,6);
        [SerializeField] private CelestialWingRig _wingRig;
        private readonly List<CanvasGroup> _fadingCards=new List<CanvasGroup>(3);
        private readonly List<float> _originalAlphas=new List<float>(3);
        private readonly Vector3[] _focusCorners = new Vector3[4];
        private bool _warned;
        private bool _playing;

        [ContextMenu("Preview Wings (Play Mode)")]
        private void Preview()
        {
            if (Application.isPlaying && !_playing) StartCoroutine(Play(null));
        }

        private void Awake() => Cancel();
        private bool Ready => _layoutVersion >= 2 && _visualRoot != null && _group != null && _leftWing != null &&
            _rightWing != null && _leftWing.sprite != null && _rightWing.sprite != null && _innerCover != null;

        public IEnumerator Play(Action swapWinner, RectTransform submittedCard = null, RectTransform stackArea = null,
            Vector2? stackPosition = null, RectTransform previousLeader = null, RectTransform nextLeader = null)
        {
            if (_playing) { swapWinner?.Invoke(); yield break; }
            if (!Ready)
            {
                if (!_warned) Debug.LogWarning("[천상강림] 날개 UI 연결을 확인하세요. 판정은 정상 적용합니다.", this);
                _warned = true;
                swapWinner?.Invoke();
                yield break;
            }
            _playing = true;
            _visualRoot.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            ConfigureCardFootprint(submittedCard, stackArea != null ? stackArea : _cardFocusArea, stackPosition ?? Vector2.zero);
            TrackCard(submittedCard);TrackCard(previousLeader);TrackCard(nextLeader);
            _group.alpha = 1f;
            // Gameplay is held by the existing presentation gate, not a screen-sized input blocker.
            _group.blocksRaycasts = false;
            _group.interactable = false;
            KeepOnTop();
            SetClosingPose(0f);
            try
            {
                yield return Animate(_closeSeconds, SetClosingPose);
                SetClosingPose(1f);
                // Cards themselves are faded out: a soft radial glow can hide the swap without a white rectangle.
                swapWinner?.Invoke();
                KeepOnTop();
                float elapsed = 0f;
                while (elapsed < Mathf.Clamp(_coveredSeconds, 0f, 10f))
                { elapsed += Time.unscaledDeltaTime; yield return null; }
                yield return Animate(_openSeconds, SetRevealPose);
            }
            finally { Cancel(); }
        }

        private IEnumerator Animate(float seconds, Action<float> pose)
        {
            seconds = Mathf.Clamp(seconds, .05f, 10f);
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                pose(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            pose(1f);
        }

        private void SetClosingPose(float closed)
        {
            PoseWings(closed, Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.2f,closed)));
            // One slow white swell; not repeated or abrupt strobing.
            float light = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.28f, 1f, closed));
            _innerCover.color = Color.clear;
            SetGlow(light);
            FadeCards(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.66f,1,closed)));
        }

        private void SetRevealPose(float revealed)
        {
            if(_wingRig!=null&&_wingRig.Ready) _wingRig.Pose(_visualRoot.rect.size,revealed,1-revealed,true);
            else PoseWings(1f - revealed, 1f - revealed);
            _innerCover.color = Color.clear;
            SetGlow(1-Mathf.SmoothStep(0,1,revealed));
            FadeCards(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.85f,revealed)));
        }

        private void PoseWings(float closed, float alpha)
        {
            Vector2 card = _visualRoot.rect.size;
            if(_wingRig!=null&&_wingRig.Ready)
            {
                _wingRig.Pose(card,closed,alpha,false);
                KeepOnTop();
                return;
            }
            float offset = Mathf.Lerp(.55f, .12f, closed) * card.x;
            Vector2 size = Vector2.Scale(card, _cardWingSize) * Mathf.Lerp(.96f, 1.02f, closed);
            PoseWing(_leftWing, -offset, 1f, _openAngle * (1f - closed), size, alpha);
            PoseWing(_rightWing, offset, -1f, -_openAngle * (1f - closed), size, alpha);
            _innerCover.transform.SetAsLastSibling();
            KeepOnTop();
        }
        private void SetGlow(float intensity)
        {
            if(_cardGlow==null)return;
            if(!_cardGlow.gameObject.activeSelf)_cardGlow.gameObject.SetActive(true);
            _cardGlow.enabled=true;
            _cardGlow.transform.SetAsLastSibling();
            _cardGlow.color=new Color(1,.97f,.88f,intensity);
            float swell=Mathf.Lerp(.72f,1.12f,Mathf.SmoothStep(0,1,intensity));
            _cardGlow.rectTransform.sizeDelta=Vector2.Scale(_visualRoot.sizeDelta,new Vector2(1.8f,1.5f))*swell;
        }

        private void ConfigureCardFootprint(RectTransform submitted, RectTransform stackArea, Vector2 finalPosition)
        {
            var parent = _visualRoot.parent as RectTransform;
            Vector2 center = Vector2.zero;
            Vector2 size = new Vector2(156,228);
            if (stackArea != null && parent != null)
                center = parent.InverseTransformPoint(stackArea.TransformPoint(finalPosition));
            if (submitted != null && parent != null)
            {
                submitted.GetWorldCorners(_focusCorners);
                Vector2 min = new Vector2(float.PositiveInfinity,float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity,float.NegativeInfinity);
                foreach (var corner in _focusCorners)
                {
                    Vector2 point = parent.InverseTransformPoint(corner);
                    min = Vector2.Min(min,point); max = Vector2.Max(max,point);
                }
                size = max-min;
                // Cover both the showcase pose and the smaller final stack pose during the swap.
                if (stackArea != null)
                {
                    min = Vector2.Min(min,center-size*.5f);
                    max = Vector2.Max(max,center+size*.5f);
                }
                center = (min+max)*.5f;
                size = max-min;
            }
            _visualRoot.anchorMin = _visualRoot.anchorMax = _visualRoot.pivot = new Vector2(.5f,.5f);
            _visualRoot.localScale = Vector3.one;
            _visualRoot.anchoredPosition = center;
            _visualRoot.sizeDelta = size+_cardPadding*2;
            var cover = _innerCover.rectTransform;
            cover.anchorMin = cover.anchorMax = cover.pivot = new Vector2(.5f,.5f);
            cover.anchoredPosition = Vector2.zero;
            cover.sizeDelta = _visualRoot.sizeDelta;
            _innerCover.raycastTarget = false;
            if (_cardGlow != null)
            {
                var glow = _cardGlow.rectTransform;
                glow.anchorMin = glow.anchorMax = glow.pivot = new Vector2(.5f,.5f);
                glow.anchoredPosition = Vector2.zero;
                glow.sizeDelta = Vector2.Scale(_visualRoot.sizeDelta,new Vector2(2.25f,1.65f));
                _cardGlow.raycastTarget = false;
            }
        }

        private void PoseWing(Image wing, float x, float flip, float angle, Vector2 size, float alpha)
        {
            var rect = wing.rectTransform;
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = size;
            rect.localScale = new Vector3(flip, 1f, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            Color tint = _wingTint;
            tint.a *= alpha;
            wing.color = tint;
            wing.raycastTarget = false;
        }

        public void KeepOnTop()
        {
            if (_visualRoot != null && _visualRoot.gameObject.activeSelf)
            {
                transform.SetAsLastSibling();
                _visualRoot.SetAsLastSibling();
            }
        }
        public void Cancel()
        {
            FadeCards(1);
            _fadingCards.Clear();_originalAlphas.Clear();
            _playing = false;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
            }
            if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
        }
        private void OnDisable() => Cancel();
        private void TrackCard(RectTransform card)
        {
            if(card==null)return;
            var group=card.GetComponent<CanvasGroup>();
            if(group==null)group=card.gameObject.AddComponent<CanvasGroup>();
            if(_fadingCards.Contains(group))return;
            _fadingCards.Add(group);_originalAlphas.Add(group.alpha);
        }
        private void FadeCards(float amount)
        {
            for(int i=0;i<_fadingCards.Count;i++)
                if(_fadingCards[i]!=null)_fadingCards[i].alpha=_originalAlphas[i]*amount;
        }
    }
}
