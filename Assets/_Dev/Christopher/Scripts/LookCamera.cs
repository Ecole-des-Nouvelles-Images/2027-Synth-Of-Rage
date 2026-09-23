using System;
using UnityEngine;

namespace _Dev.Christopher.Script
{
    public class LookCamera : MonoBehaviour
    {
        private Camera _maincamera;
        private void Awake()
        {
            _maincamera = Camera.main;
        }

        private void LateUpdate() {
            transform.LookAt(transform.position + _maincamera.transform.rotation * Vector3.forward,
                _maincamera.transform.rotation * Vector3.up);
        }
    }
}
