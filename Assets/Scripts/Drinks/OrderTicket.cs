using System.Collections.Generic;
using UnityEngine;

/// One paper slip pegged to the line. Sways on its peg so the line feels alive.
public class OrderTicket : MonoBehaviour
{
    public Recipe Recipe;
    public float SwayAmplitude = 4.5f;
    public float SwaySpeed = 1.1f;

    Transform _paper;
    float _phase;
    float _nudge;

    public void Setup(Recipe r, Transform paper, float phase)
    {
        Recipe = r;
        _paper = paper;
        _phase = phase;
    }

    [Header("Slip text")]
    [Tooltip("Characters that fit across the slip at full text size.")]
    public float CharsAcross = 10f;
    [Tooltip("Lines that fit down the slip at full text size.")]
    public float LinesDown = 6f;

    /// Writes the order on the slip: the drink, then one ingredient per line.
    /// Shrinks the text so long names stay on the paper.
    public void WriteSlip(Recipe r)
    {
        var tm = GetComponentInChildren<TextMesh>();
        if (tm == null || r == null) return;
        var lines = new List<string> { r.Name };
        foreach (var step in r.Steps) lines.Add("- " + Ingredients.Label(step));
        tm.text = string.Join("\n", lines.ToArray());

        int longest = 0;
        foreach (var l in lines) longest = Mathf.Max(longest, l.Length);
        // shortcut: estimates width from character count; measure the mesh if fonts change.
        float fit = Mathf.Min(1f, CharsAcross / Mathf.Max(1, longest), LinesDown / lines.Count);
        tm.transform.localScale = Vector3.one * fit;
    }

    /// Give it a push, for when an order lands or is completed.
    public void Nudge(float amount) { _nudge = amount; }

    void Update()
    {
        if (_paper == null) return;
        _nudge = Mathf.Lerp(_nudge, 0f, Time.deltaTime * 2.2f);
        float a = Mathf.Sin(Time.time * SwaySpeed + _phase) * SwayAmplitude + _nudge;
        _paper.localRotation = Quaternion.Euler(0f, 0f, a);
    }
}
