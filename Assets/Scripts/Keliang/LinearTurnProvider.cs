using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class LinearTurnProvider : ContinuousTurnProvider
{
    protected override float GetTurnAmount(Vector2 input)
    {
        return enableTurnLeftRight ? input.x * turnSpeed * Time.deltaTime : 0f;
    }
}
