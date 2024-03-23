using System.Collections;
using System.Collections.Generic;

public static class Tools
{
    public static int RepeatInt(int value, int limit)
    {
        if (value < 0)
        {
            int val = limit - Mathf.Abs(value) % limit;
            return val == limit ? 0 : val;
        }
        else
        {
            return value % limit;
        }
    }
}
