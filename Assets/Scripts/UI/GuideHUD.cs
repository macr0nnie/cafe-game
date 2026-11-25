using UnityEngine;
using UnityEngine.UI;

/// The drink guide on screen: a "what to do next" line at the top, and a "Next"
/// tag over the thing that does it (pinned to the screen edge with an arrow when
/// it is out of view). Text, not colour, carries the meaning. Never moves the camera.
public class GuideHUD : MonoBehaviour
{
    public GuideTracker Guide;
    public Camera Eye;
    [Tooltip("Screen-edge margin for the Next tag, in canvas pixels.")]
    public float EdgeMargin = 90f;

    GameObject _canvasGO;
    RectTransform _canvasRT;
    Text _line;
    Text _tag;
    RectTransform _tagRT;

    void Awake()
    {
        if (Guide == null) Guide = GetComponent<GuideTracker>();
        if (Eye == null) Eye = GetComponentInChildren<Camera>();
        var canvas = CafeUIKit.MakeCanvas(transform, "Guide_Canvas", 101);
        _canvasGO = canvas.gameObject;
        _canvasRT = canvas.GetComponent<RectTransform>();
        _line = CafeHUD.Chip(canvas.transform, "GuideLine", 26, new Vector2(0.5f, 1f), new Vector2(0f, -40f), CafeUIKit.Paper);
        _tag  = CafeHUD.Chip(canvas.transform, "NextTag",   24, new Vector2(0.5f, 0.5f), Vector2.zero, CafeUIKit.Peach);
        _tagRT = (RectTransform)_tag.transform.parent;
    }

    void LateUpdate()
    {
        if (_canvasGO.activeSelf == CafeGame.MenuOpen) _canvasGO.SetActive(!CafeGame.MenuOpen);
        if (CafeGame.MenuOpen || Guide == null) return;

        CafeHUD.SetChip(_line, Guide.Text);

        var target = Guide.NextTarget;
        if (target == null || Eye == null) { CafeHUD.SetChip(_tag, ""); return; }

        // Aim a little above the object's middle so the tag does not sit on top of it.
        Vector3 world = target.position;
        var col = target.GetComponent<Collider>();
        if (col != null) world = col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.08f);

        Vector3 sp = Eye.WorldToViewportPoint(world);
        bool behind = sp.z < 0f;
        if (behind) { sp.x = 1f - sp.x; sp.y = 1f - sp.y; }
        Vector2 size = _canvasRT.rect.size;
        Vector2 p = new Vector2((sp.x - 0.5f) * size.x, (sp.y - 0.5f) * size.y);

        Vector2 half = size * 0.5f - Vector2.one * EdgeMargin;
        bool off = behind || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.y) > half.y;
        string label = "Next";
        if (off)
        {
            // Pin to the screen edge, pointing the way to turn.
            if (behind) p = new Vector2(p.x < 0f ? -half.x : half.x, Mathf.Clamp(p.y, -half.y, half.y));
            else p *= Mathf.Min(half.x / Mathf.Max(1e-3f, Mathf.Abs(p.x)), half.y / Mathf.Max(1e-3f, Mathf.Abs(p.y)));

            if (Mathf.Abs(p.x) >= half.x - 1f) label = p.x < 0f ? "←  Next" : "Next  →";
            else label = p.y > 0f ? "Next  ↑" : "Next  ↓";
        }
        else label = "Next  ↓";

        CafeHUD.SetChip(_tag, label);
        _tagRT.anchoredPosition = p;
    }
}
