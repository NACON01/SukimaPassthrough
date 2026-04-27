using UnityEngine;

/// <summary>
/// Visual laser ray from the right controller toward UI.
/// OVRInputModule handles UI event dispatch; this script is display only.
/// LineRenderer is created automatically at runtime — no inspector setup required.
/// </summary>
public class ControllerLaserPointer : MonoBehaviour
{
    [Tooltip("レイの最大到達距離 (m)")]
    public float maxDistance = 5f;

    [Tooltip("コントローラー未接続時にレーザーを非表示")]
    public bool hideWhenNoController = true;

    private LineRenderer _line;
    private Transform _raySource;

    void Start()
    {
        // LineRenderer を動的生成（YAML に記述不要）
        _line = gameObject.GetComponent<LineRenderer>();
        if (_line == null)
            _line = gameObject.AddComponent<LineRenderer>();

        ConfigureLine();

        // OVRCameraRig から右コントローラーアンカーを自動取得
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
            _raySource = rig.rightControllerAnchor;
        else
            Debug.LogWarning("[ControllerLaserPointer] OVRCameraRig not found in scene.");
    }

    void Update()
    {
        if (_raySource == null) { _line.enabled = false; return; }

        if (hideWhenNoController && !OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
        {
            _line.enabled = false;
            return;
        }

        _line.enabled = true;

        Vector3 origin = _raySource.position;
        Vector3 dir    = _raySource.forward;
        float   dist   = maxDistance;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance))
            dist = hit.distance;

        _line.SetPosition(0, origin);
        _line.SetPosition(1, origin + dir * dist);
    }

    void ConfigureLine()
    {
        _line.useWorldSpace     = true;
        _line.positionCount     = 2;
        _line.startWidth        = 0.004f;
        _line.endWidth          = 0.001f;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows    = false;

        var mat = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.8f, 0.9f, 1f) };
        _line.material = mat;
    }
}
