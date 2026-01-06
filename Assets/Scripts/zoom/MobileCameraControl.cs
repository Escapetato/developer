using UnityEngine;
using UnityEngine.EventSystems;

public class MobileCameraControl : MonoBehaviour
{
    private Camera _cam;

    [Header("드래그 이동 설정")]
    public float panSpeed = 0.5f;
    
    [Header("확대/축소 설정")]
    public float zoomSpeed = 0.01f;
    public float minZoom = 7f;
    public float maxZoom = 15f;

    [Header("이동 범위 제한 (농장 크기에 맞게 조절)")]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Update()
{
    // UI 위에서는 카메라 조작 안 함
    if (IsPointerOverUI()) return;

    // 1. [노트북 터치패드 & 마우스 휠] 확대/축소 테스트
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (scroll != 0)
    {
        // 터치패드 감도에 따라 scroll 속도를 조절하세요 (예: 5f ~ 10f)
        _cam.orthographicSize -= scroll * 5f; 
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
    }

    // 2. [모바일] 실제 터치 입력 (빌드 시 작동)
    if (Input.touchCount == 2)
    {
        HandleZoom();
    }
    // 3. [노트북/모바일 공통] 드래그 이동
    // Input.GetMouseButton(0)를 쓰면 마우스 클릭과 한 손가락 터치 둘 다 대응됩니다.
    else if (Input.GetMouseButton(0))
    {
        HandlePanForEditor(); 
    }
}

// 에디터와 모바일 모두에서 작동하는 드래그 로직
void HandlePanForEditor()
{
    // 마우스의 움직임(델타값)을 가져옵니다.
    float moveX = Input.GetAxis("Mouse X") * panSpeed * _cam.orthographicSize;
    float moveY = Input.GetAxis("Mouse Y") * panSpeed * _cam.orthographicSize;

    transform.Translate(-moveX * Time.deltaTime, -moveY * Time.deltaTime, 0);

    // 범위 제한 (Clamp)
    Vector3 pos = transform.position;
    pos.x = Mathf.Clamp(pos.x, minX, maxX);
    pos.y = Mathf.Clamp(pos.y, minY, maxY);
    transform.position = pos;
}

    // 드래그 이동 로직
    void HandlePan()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            // 터치 이동량만큼 카메라를 반대 방향으로 이동
            Vector2 touchDelta = touch.deltaPosition;
            Vector3 move = new Vector3(-touchDelta.x * panSpeed * Time.deltaTime, -touchDelta.y * panSpeed * Time.deltaTime, 0);
            
            transform.Translate(move, Space.World);
            
            // 이동 범위 제한
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }
    }

    // 확대/축소 로직
    void HandleZoom()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        // 이전 프레임의 위치
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        // 거리 계산
        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        // 차이만큼 줌 조절
        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

        _cam.orthographicSize += deltaMagnitudeDiff * zoomSpeed;
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
    }

    // UI 위에 터치가 있는지 확인하는 함수
    bool IsPointerOverUI()
    {
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return false;
    }
}