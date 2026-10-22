using UnityEngine;
using UnityEngine.EventSystems;

public class CardTiltEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("틸트 설정")]
    [SerializeField] private float _maxTiltAngle = 15f;    // 최대 기울기 각도
    [SerializeField] private float _tiltSpeed = 8f;         // 보간 속도

    private RectTransform _rect;
    private Vector3 _targetRotation;
    private bool _isHovered;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        // 마우스가 나가면 기울기를 원상복구 (0도)
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

        // X축은 마우스 Y에, Y축은 마우스 X에 반비례 (자연스러운 입체 기울기)
        _targetRotation = new Vector3(
            -normalizedY * _maxTiltAngle,
             normalizedX * _maxTiltAngle,
             0
        );
    }

    private void Update()
    {
        // 위치나 크기는 건드리지 않고, 오직 '회전(기울기)'만 부드럽게 적용!
        Quaternion target = Quaternion.Euler(_targetRotation);
        _rect.localRotation = Quaternion.Slerp(
            _rect.localRotation, target, Time.deltaTime * _tiltSpeed);
    }
}