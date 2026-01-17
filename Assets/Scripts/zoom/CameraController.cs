using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    private Camera _cam;

    [Header("이동 및 줌 속도")]
    public float zoomSpeed = 0.05f;

    [Header("줌 범위 설정")]
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("배경화면 경계 (Sprite의 실제 끝 좌표)")]
    public float mapMinX = -25f;
    public float mapMaxX = 25f;
    public float mapMinY = -25f;
    public float mapMaxY = 25f;

    private Vector3 lastMousePos;

    void Awake() => _cam = GetComponent<Camera>();

    void Update()
    {
        // 1. UI 위를 클릭 중이면 무시
        if (IsPointerOverUI()) return;

        // 2. 확대/축소 (두 손가락)
        if (Input.touchCount == 2)
        {
            HandleZoom();
        }
        // 3. 화면 이동 (한 손가락 또는 마우스 드래그)
        else
        {
            HandlePan();
        }

        // 4. 마우스 휠 (PC 테스트용)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) ZoomCamera(-scroll * 10f);
    }

    // 카메라 위치 보정은 모든 이동이 끝난 후 LateUpdate에서 하는 것이 가장 정확합니다.
    void LateUpdate()
    {
        transform.position = ClampCamera(transform.position);
    }

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            // 월드 좌표 대신 화면 좌표의 차이(Delta)를 이용해 이동합니다. (더 안정적임)
            Vector3 delta = Input.mousePosition - lastMousePos;
            
            // 현재 줌 크기에 비례하여 이동 속도를 조절
            float moveSpeed = _cam.orthographicSize / Screen.height * 2f;
            
            Vector3 move = new Vector3(-delta.x * moveSpeed * _cam.aspect, -delta.y * moveSpeed, 0);
            transform.position += move;

            lastMousePos = Input.mousePosition;
        }
    }

    void HandleZoom()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 t0Prev = t0.position - t0.deltaPosition;
        Vector2 t1Prev = t1.position - t1.deltaPosition;

        float prevMag = (t0Prev - t1Prev).magnitude;
        float currentMag = (t0.position - t1.position).magnitude;
        float diff = prevMag - currentMag;

        ZoomCamera(diff * zoomSpeed);
    }

    void ZoomCamera(float delta)
    {
        _cam.orthographicSize += delta;

        // 화면비에 따른 최대 줌 제한 (배경보다 커지지 않게)
        float maxPossibleHeight = (mapMaxY - mapMinY) / 2f;
        float maxPossibleWidth = ((mapMaxX - mapMinX) / 2f) / _cam.aspect;
        float absoluteMaxZoom = Mathf.Min(maxZoom, maxPossibleHeight, maxPossibleWidth);

        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, absoluteMaxZoom);
    }

    Vector3 ClampCamera(Vector3 targetPosition)
    {
        float camHeight = _cam.orthographicSize;
        float camWidth = camHeight * _cam.aspect;

        // 카메라가 비추는 영역의 최소/최대 좌표 계산
        float minX = mapMinX + camWidth;
        float maxX = mapMaxX - camWidth;
        float minY = mapMinY + camHeight;
        float maxY = mapMaxY - camHeight;

        // 계산된 경계 내로 카메라 위치를 가둠
        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }

    // 에디터 뷰에서 경계선을 시각적으로 확인
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 center = new Vector3((mapMinX + mapMaxX) / 2, (mapMinY + mapMaxY) / 2, 0);
        Vector3 size = new Vector3(mapMaxX - mapMinX, mapMaxY - mapMinY, 0);
        Gizmos.DrawWireCube(center, size);
    }
}