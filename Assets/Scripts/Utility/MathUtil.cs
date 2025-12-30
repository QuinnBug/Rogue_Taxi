using UnityEngine;

public class MathUtil 
{
    public static float ClampedDegrees(float value)
    {
        value = value % 360;
        if (value < 0) { value += 360; }

        return value;
    }

    public static float FreeDegrees(float value)
    {
        value = value % 360;
        if (value <= -180) { value += 360; }
        if (value > 180) { value -= 360; }

        return value;
    }

    public static float ClampRotation(float rot, float min, float max) 
    {
        rot = Mathf.Clamp(FreeDegrees(rot), min, max);

        return rot;
    }
}
