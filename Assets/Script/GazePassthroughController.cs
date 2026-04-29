using UnityEngine;

public class GazePassthroughController : MonoBehaviour
{
    [Header("必須コンポーネント")]
    public OVRPassthroughLayer passthroughLayer;
    public GameObject passthroughMesh;
    public Transform eyeGazeTransform;

    [Header("窓の配置パラメータ (UI連動)")]
    public float horizontalOffset = 0f;
    public float verticalOffset = -0.35f;
    public float distance = 0.5f;

    [Header("視線判定パラメータ (UI連動)")]
    [Tooltip("Quad に視線が重なってからパススルーが開くまでの滞留時間 (秒)")]
    public float dwellTime = 0.2f;
    [Tooltip("フェード速度 (1秒あたりの不透明度変化量)")]
    public float fadeSpeed = 5.0f;

    private OVREyeGaze _eyeGaze;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private float _dwellTimer = 0f;
    private float _currentOpacity = 0f;
    private bool _windowOpen = false;

    private float _cachedH, _cachedV, _cachedD;

    private static readonly int InvertedAlpha = Shader.PropertyToID("_InvertedAlpha");

    void Start()
    {
        if (eyeGazeTransform != null)
            _eyeGaze = eyeGazeTransform.GetComponent<OVREyeGaze>();

        if (passthroughLayer != null)
            passthroughLayer.textureOpacity = 1f;

        if (passthroughMesh != null)
        {
            _meshRenderer = passthroughMesh.GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
            {
                _propBlock = new MaterialPropertyBlock();
                _meshRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(InvertedAlpha, 0f);
                _meshRenderer.SetPropertyBlock(_propBlock);
            }

            _cachedH = horizontalOffset;
            _cachedV = verticalOffset;
            _cachedD = distance;
            passthroughMesh.transform.localPosition = new Vector3(_cachedH, _cachedV, _cachedD);
        }

        if (passthroughLayer != null)
            passthroughLayer.hidden = true;
    }

    void Update()
    {
        if (eyeGazeTransform == null || _meshRenderer == null || passthroughMesh == null) return;

        if (_eyeGaze != null && !_eyeGaze.EyeTrackingEnabled)
        {
            Debug.LogWarning("[GazePassthroughController] Eye tracking unavailable. Check Quest Pro permissions.");
            return;
        }

        if (horizontalOffset != _cachedH || verticalOffset != _cachedV || distance != _cachedD)
        {
            _cachedH = horizontalOffset;
            _cachedV = verticalOffset;
            _cachedD = distance;
            passthroughMesh.transform.localPosition = new Vector3(_cachedH, _cachedV, _cachedD);
        }

        bool gazeOnQuad = IsGazeOnQuad();

        if (!_windowOpen)
        {
            _dwellTimer = gazeOnQuad ? _dwellTimer + Time.deltaTime : 0f;
            if (_dwellTimer >= dwellTime)
                _windowOpen = true;
        }
        else
        {
            if (!gazeOnQuad)
            {
                _windowOpen = false;
                _dwellTimer = 0f;
            }
        }

        float target = _windowOpen ? 1.0f : 0.0f;
        _currentOpacity = Mathf.MoveTowards(_currentOpacity, target, fadeSpeed * Time.deltaTime);

        bool shouldShow = _currentOpacity > 0.001f;

        if (_meshRenderer.enabled != shouldShow)
            _meshRenderer.enabled = shouldShow;

        if (passthroughLayer != null && passthroughLayer.hidden == shouldShow)
            passthroughLayer.hidden = !shouldShow;

        if (!shouldShow) return;

        _propBlock.SetFloat(InvertedAlpha, _currentOpacity);
        _meshRenderer.SetPropertyBlock(_propBlock);
    }

    // Cast a ray from the eye gaze and check if it hits the passthrough Quad plane.
    // Uses analytic ray-plane intersection — no Collider needed, keeps Quad transparent
    // to OVRInputModule UI raycasts so Canvas sliders remain operable.
    private bool IsGazeOnQuad()
    {
        Transform t = passthroughMesh.transform;
        // Unity Quad mesh normal is local +Z; world normal = transform.forward
        Plane plane = new Plane(t.forward, t.position);
        Ray ray = new Ray(eyeGazeTransform.position, eyeGazeTransform.forward);

        if (!plane.Raycast(ray, out float enter)) return false;
        if (enter < 0f) return false;

        Vector3 hit = ray.GetPoint(enter);
        // InverseTransformPoint accounts for position, rotation, and scale.
        // Unity Quad mesh corners are at ±0.5 in local XY regardless of world scale.
        Vector3 local = t.InverseTransformPoint(hit);
        return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f;
    }

    // --- UIスライダーから呼び出すための関数群 ---
    public void SetVerticalOffset(float value) => verticalOffset = value;
    public void SetDistance(float value) => distance = value;
    public void SetDwellTime(float value) => dwellTime = value;
    public void SetFadeSpeed(float value) => fadeSpeed = value;
}
