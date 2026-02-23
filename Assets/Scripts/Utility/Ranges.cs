using UnityEngine;


[System.Serializable]
public struct Range<T> where T : struct
{
    public T min;
    public T max;

    public Range(T _min, T _max)
    {
        min = _min;
        max = _max;

        if ((dynamic)min > max)
        {
            min = _max;
            max = _min;
        }
    }

    public bool Contains(T test)
    {
        return (dynamic)test >= min && (dynamic)test <= max;
    }

    internal T RandomValue()
    {
        return UnityEngine.Random.Range((dynamic)min, (dynamic)max);
    }

    public T Clamp(T value) 
    {
        return Mathf.Clamp((dynamic)value, (dynamic)min, (dynamic)max);
    }
}
