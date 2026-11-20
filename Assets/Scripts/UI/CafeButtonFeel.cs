using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Makes a painted button feel nice to press: lifts and tilts a touch on hover
/// or keyboard focus, squishes on press, and draws its underline in.
/// Runs on unscaled time so it still works while the cafe is paused.
public class CafeButtonFeel : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    public Image Underline;
    public float HoverScale = 1.05f;
    public float PressScale = 0.95f;
    public float HoverTilt = -1.6f;

    bool _hover, _focus, _down;
    float _lift;
    float _tiltSign = 1f;

    public void OnPointerEnter(PointerEventData e)
    {
        _hover = true;
        // Select on hover too, so mouse and keyboard always agree on what is focused.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }
    public void OnPointerExit(PointerEventData e)  { _hover = false; _down = false; }
    public void OnPointerDown(PointerEventData e)  { _down = true; }
    public void OnPointerUp(PointerEventData e)    { _down = false; }
    public void OnSelect(BaseEventData e)          { _focus = true; _tiltSign = Random.value < 0.5f ? -1f : 1f; }
    public void OnDeselect(BaseEventData e)        { _focus = false; }

    void OnDisable()
    {
        _hover = _focus = _down = false;
        _lift = 0f;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        if (Underline != null) Underline.fillAmount = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool on = _hover || _focus;
        _lift = Mathf.MoveTowards(_lift, on ? 1f : 0f, dt * 8f);
        float e = _lift * _lift * (3f - 2f * _lift);

        float s = Mathf.Lerp(1f, HoverScale, e);
        if (_down) s = PressScale;
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(s, s, 1f), 1f - Mathf.Exp(-22f * dt));
        transform.localRotation = Quaternion.Euler(0f, 0f, HoverTilt * _tiltSign * e);

        if (Underline != null) Underline.fillAmount = e;
    }
}
