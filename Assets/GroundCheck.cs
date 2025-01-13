using System.Collections;

using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public bool onground;
    public void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
        {
            onground = true;
        }
        transform.parent.GetComponent<PlayerController>().onGround = onground;
    }

    public void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
        {
            onground = false;
        }
        transform.parent.GetComponent<PlayerController>().onGround = onground;
    }
}
