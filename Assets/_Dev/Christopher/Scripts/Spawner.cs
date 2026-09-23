using UnityEngine;

namespace _Dev.Christopher.Scripts
{
    public class Spawner : MonoBehaviour
    {

        public GameObject Enemy;
    
        [SerializeField]private Transform spawnPoint;

        public void SpawnEnemy()
        {
            Instantiate(Enemy, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
