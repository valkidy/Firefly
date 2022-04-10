using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    public class Utility
    {
        public static Vector3 DFNoise(Vector3 p, float frequency, float timestampOffset)
        {
            p *= frequency;

            uint frameCount = 6u * (uint)(p.sqrMagnitude + timestampOffset);

            Vector3 grad1 = new Vector3(
                Random.Value01(frameCount),
                Random.Value01(frameCount + 1),
                Random.Value01(frameCount + 2)
            );

            p.z += 100;

            frameCount = 6u * (uint)(p.sqrMagnitude + timestampOffset);

            Vector3 grad2 = new Vector3(
                Random.Value01(frameCount + 3),
                Random.Value01(frameCount + 4),
                Random.Value01(frameCount + 5)
            );

            return Vector3.Cross(grad1.normalized, grad2.normalized);
        }        
    }    
}