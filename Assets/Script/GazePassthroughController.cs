using UnityEngine;

public class GazePassthroughController : MonoBehaviour
{
    [Header("必須コンポーネント")]
    public OVRPassthroughLayer passthroughLayer;
    public GameObject passthroughMesh;
    public Transform eyeGazeTransform;

    [Header("窓の配置パラメータ (UI連動)")]
    public float horizontalOffset = 0f;
    public float verticalOffset = -0.15f;
    public float distance = 0.4f;

    [Header("視線判定パラメータ (UI連動)")]
    [Tooltip("窓を開く視線角度 (度)")]
    public float thresholdAngle = 15.0f;
    [Tooltip("窓を閉じる視線角度 (度) — チラつき防止ヒステリシス")]
    public float closeAngle = 10.0f;
    public float dwellTime = 0.2f;
    [Tooltip("フェード速度 (1秒あたりの不透明度変化量)")]
    public float fadeSpeed = 5.0f;

    private OVREyeGaze _eyeGaze;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private float _dwellTimer = 0f;
    private float _currentOpacity = 0f;
    private bool _windowOpen = false;

    // Phase B - P1: 前フレームの位置値をキャッシュして変化時のみ書き込み
    private float _cachedH, _cachedV, _cachedD;

    private static readonly int InvertedAlpha = Shader.PropertyToID("_InvertedAlpha");

    void Start()
    {
        // Phase B - L7: OVREyeGaze 参照を取得してトラッキング状態を監視できるようにする
        if (eyeGazeTransform != null)
            _eyeGaze = eyeGazeTransform.GetComponent<OVREyeGaze>();

        if (passthroughLayer != null)
            passthroughLayer.textureOpacity = 1f;

        if (passthroughMesh != null)
        {
            _meshRenderer = passthroughMesh.GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
            {
                // Phase B - P2: MaterialPropertyBlock で SRP バッチを阻害しない
                _propBlock = new MaterialPropertyBlock();
                _meshRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(InvertedAlpha, 0f);
                _meshRenderer.SetPropertyBlock(_propBlock);
            }

            // Phase A - L10 fix: Inspector 値を真実として使用。transform.localPosition は読み戻さない
            _cachedH = horizontalOffset;
            _cachedV = verticalOffset;
            _cachedD = distance;
            passthroughMesh.transform.localPosition = new Vector3(_cachedH, _cachedV, _cachedD);
        }

        // Phase B - P3: 起動時は完全に閉じているので hidden にしておく (enabled 切り替えより副作用が少ない)
        if (passthroughLayer != null)
            passthroughLayer.hidden = true;
    }

    void Update()
    {
        if (eyeGazeTransform == null || _meshRenderer == null) return;

        // Phase B - L7/S3: EyeTracking が無効な場合（権限拒否や一時失敗）は早期 return
        if (_eyeGaze != null && !_eyeGaze.EyeTrackingEnabled)
        {
            Debug.LogWarning("[GazePassthroughController] Eye tracking unavailable. Check Quest Pro permissions.");
            return;
        }

        // Phase B - P1: 位置が変化したフレームのみ Transform を更新
        if (horizontalOffset != _cachedH || verticalOffset != _cachedV || distance != _cachedD)
        {
            _cachedH = horizontalOffset;
            _cachedV = verticalOffset;
            _cachedD = distance;
            passthroughMesh.transform.localPosition = new Vector3(_cachedH, _cachedV, _cachedD);
        }

        // Phase B - L8/P4: forward ベクトルから仰角を計算（localEulerAngles より安定）
        // 下を向くと fwd.y < 0 → pitch > 0（正の値が下方向）
        float pitch = -Mathf.Asin(Mathf.Clamp(eyeGazeTransform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        // Phase B - L5: ヒステリシスで閾値付近のチラつきを防止
        if (!_windowOpen)
        {
            _dwellTimer = (pitch >= thresholdAngle) ? _dwellTimer + Time.deltaTime : 0f;
            if (_dwellTimer >= dwellTime)
                _windowOpen = true;
        }
        else
        {
            if (pitch < closeAngle)
            {
                _windowOpen = false;
                _dwellTimer = 0f;
            }
        }

        // Phase B - L6: MoveTowards はフレームレート非依存で確実に収束する
        float target = _windowOpen ? 1.0f : 0.0f;
        _currentOpacity = Mathf.MoveTowards(_currentOpacity, target, fadeSpeed * Time.deltaTime);

        // Phase B - P2: MaterialPropertyBlock で描画
        _propBlock.SetFloat(InvertedAlpha, _currentOpacity);
        _meshRenderer.SetPropertyBlock(_propBlock);

        // Phase B - P3: 完全に閉じている間は hidden でコンポジット処理をスキップ（GPU 節約）
        if (passthroughLayer != null)
        {
            bool shouldShow = _currentOpacity > 0.001f;
            if (passthroughLayer.hidden == shouldShow)
                passthroughLayer.hidden = !shouldShow;
        }
    }

    // --- UIスライダーから呼び出すための関数群 ---
    public void SetVerticalOffset(float value) => verticalOffset = value;
    public void SetDistance(float value) => distance = value;
    public void SetThreshold(float value) => thresholdAngle = value;
    public void SetDwellTime(float value) => dwellTime = value;
    public void SetFadeSpeed(float value) => fadeSpeed = value;
}
