using UnityEngine;
using UnityEngine.AI;

namespace _Dev.Christopher.Script
{
    public class BrainProto : MonoBehaviour
    {
        NavMeshAgent myNavMeshAgent;
        GameObject[] players;
        GameObject target;
    
        void Start()
        {
        
        
            myNavMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length != 0)
            {
                target = players[0];
                TargetPath();
            }
        }

        void TargetPath()
        {
            myNavMeshAgent.SetDestination(target.transform.position);
        }
    }
}
