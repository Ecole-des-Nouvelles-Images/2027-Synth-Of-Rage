using System;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    public class HurtboxEvent : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Hurtbox Entered "+other.name);
            }
        }
    }
}
