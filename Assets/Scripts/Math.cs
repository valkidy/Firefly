using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    public static class Math
    {
        public static Vector3 Lerp(this Vector3 a, Vector3 b, float t)
        {            
            return new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));
        }
    }
}

