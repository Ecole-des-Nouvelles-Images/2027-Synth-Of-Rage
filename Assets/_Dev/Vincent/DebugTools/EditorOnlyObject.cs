using System;
using UnityEngine;

namespace _Dev.Vincent.DebugTools
{
    public class EditorOnlyObject : MonoBehaviour
    {
        private void Awake()
        {
            DestroyImmediate(this.gameObject);
        }
    }
}
