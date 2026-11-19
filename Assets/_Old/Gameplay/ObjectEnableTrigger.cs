using System.Collections.Generic;
using UnityEngine;

namespace _Old.Gameplay
{
    public class ObjectEnableTrigger : MonoBehaviour
    {
        public List<GameObject> objectsToEnable;

        private void Awake()
        {
            foreach (var Obj in objectsToEnable)
            {
                Obj.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                foreach (var Obj in objectsToEnable)
                {
                    Obj.SetActive(true);
                }
                Destroy(this.gameObject);
            }
        }
    }
}
