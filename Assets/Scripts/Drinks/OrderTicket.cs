using System.Collections.Generic;
using UnityEngine;

/// One paper slip pegged to the line. Sways on its peg so the line feels alive.
public class OrderTicket : MonoBehaviour
{
    public Recipe Recipe;
    public float PlacedAt;
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
