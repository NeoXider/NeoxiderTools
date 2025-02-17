using System;
using UnityEngine;

namespace Neo
{
    public class RequireInterface : PropertyAttribute
    {
        public readonly Type RequireType;

        public RequireInterface(Type requireType)
        {
            RequireType = requireType;
        }
    }
}