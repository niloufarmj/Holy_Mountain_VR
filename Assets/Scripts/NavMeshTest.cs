using UnityEngine;
using UnityEngine.AI;

public class NavMeshTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GetComponent<NavMeshAgent>().destination = hit.point;
            }
        }
    }
}