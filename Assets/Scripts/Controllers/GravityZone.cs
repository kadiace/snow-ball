using UnityEngine;

public class GravityZone : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
            return;

        player.GravityDir = transform.up.normalized;
    }
}
