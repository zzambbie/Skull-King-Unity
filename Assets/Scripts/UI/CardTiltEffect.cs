using UnityEngine;
using UnityEngine.EventSystems;

public class CardTiltEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("틸트 설정")]
    [SerializeField] private float _maxTiltAngle = 15f;    // 최대 기울기 각도
    [SerializeField] private float _tiltSpeed = 8f;         // 보간 속도
    [SerializeField] private float _hoverScale = 1.15f;     // 호버 시 확대
    [SerializeField] private float _hoverLiftY = 30f;       // 호버 시 위로 올라감

    private RectTransform _rect;
    private Vector3 _targetRotation;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    private bool _isHovered;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalScale = _rect.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _originalPosition = _rect.localPosition;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _targetRotation = Vector3.zero;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!_isHovered) return;

        // 카드 중심점 기준 마우스 오프셋 계산
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect, eventData.position, eventData.pressEventCamera, out localPoint);

        // -1~1 범위로 정규화
        float normalizedX = localPoint.x / (_rect.rect.width * 0.5f);
        float normalizedY = localPoint.y / (_rect.rect.height * 0.5f);

        // X축은 마우스 Y에, Y축은 마우스 X에 반비례 (자연스러운 기울기)
        _targetRotation = new Vector3(
            -normalizedY * _maxTiltAngle,  // 위아래 → X축 회전
             normalizedX * _maxTiltAngle,  // 좌우 → Y축 회전
             0
        );
    }

    private void Update()
    {
        // 회전 보간
        Quaternion target = Quaternion.Euler(_targetRotation);
        _rect.localRotation = Quaternion.Slerp(
            _rect.localRotation, target, Time.deltaTime * _tiltSpeed);

        // 스케일 보간
        Vector3 targetScale = _isHovered
            ? _originalScale * _hoverScale
            : _originalScale;
        _rect.localScale = Vector3.Lerp(
            _rect.localScale, targetScale, Time.deltaTime * _tiltSpeed);

        // 위치 보간 (호버 시 위로)
        if (_isHovered)
        {
            Vector3 liftPos = _originalPosition + Vector3.up * _hoverLiftY;
            _rect.localPosition = Vector3.Lerp(
                _rect.localPosition, liftPos, Time.deltaTime * _tiltSpeed);
        }
        else if (_rect.localPosition != _originalPosition)
        {
            _rect.localPosition = Vector3.Lerp(
                _rect.localPosition, _originalPosition, Time.deltaTime * _tiltSpeed);
        }
    }
}
