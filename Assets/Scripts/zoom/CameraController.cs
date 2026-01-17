using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    private Camera _cam;

    [Header("이동 및 줌 속도")]
    public float panSpeed = 0.5f;   // 드래그 이동 속도
    public float zoomSpeed = 0.01f; // 줌 속도
    
    [Header("줌 범위 설정")]
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("농장 경계 제한 (카메라가 나가지 못하게)")]
    public float minX = -20f;
    public float maxX = 20f;
    public float minY = -20f;
    public float maxY = 20f;

    private Vector3 touchStart;

    void Awake() => _cam = GetComponent<Camera>();

    void Update()
    {
        // UI를 만지고 있으면 카메라 조작 안 함
        if (EventSystem.current.IsPointerOverGameObject() || 
            (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))) 
            return;

        // 1. 확대/축소 (두 손가락)
        if (Input.touchCount == 2)
        {
            HandleZoom();
        }
        // 2. 화면 이동 (한 손가락 또는 마우스 드래그)
        else
        {
            HandlePan();
        }

        // 3. 마우스 휠 (노트북 터치패드 줌 테스트용)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            _cam.orthographicSize -= scroll * 10f;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // 화면 드래그 이동 로직
    void HandlePan()
    {
        // 마우스 왼쪽 버튼 클릭 혹은 터치 시작 지점 저장
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = _cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // 버튼을 누른 채 움직이면 카메라 이동
        if (Input.GetMouseButton(0))
        {
            Vector3 direction = touchStart - _cam.ScreenToWorldPoint(Input.mousePosition);
            _cam.transform.position += direction;

            // 농장 밖으로 나가지 않게 제한
            Vector3 pos = _cam.transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            _cam.transform.position = pos;
        }
    }

    // 두 손가락 줌 로직
    void HandleZoom()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

        _cam.orthographicSize += deltaMagnitudeDiff * zoomSpeed;
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
    }
}