using System.Collections;

[System.Serializable]
public class Interval
{
    public float min;
    public float max;

    public Interval(float _min, float _max)
    {
        min = _min;
        max = _max;
    }

    public float Size
    {
        get { return max - min; }
        private set { }
    }

    public float Lerp(float t)
    {
        if (t < 0) return min;
        if (t > 1) return max;

        return min + Size * t;
    }
}
