using System;
using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    [RequireComponent(typeof(BoxCollider))]
    public class HurtboxEvent : MonoBehaviour
    {
        private SpriteRenderer _myParentSpriteRenderer;
        private bool _flipX = false;

        private void Awake()
        {
            if(_myParentSpriteRenderer == null && transform.parent != null)
                _myParentSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_myParentSpriteRenderer)
            {
                if(_myParentSpriteRenderer.flipX && _flipX == false)
                {
                    transform.Rotate(0,180,0);
                    _flipX = true;
                }
                if (_myParentSpriteRenderer.flipX == false && _flipX)
                {
                    transform.Rotate(0,-180,0);
                    _flipX = false;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Hurtbox Entered "+other.name);
            }
        }

       
    }
}
