using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileDropController : MonoBehaviour
{
    public ItemClass item;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if(col.gameObject.CompareTag("Player"))
        {
            //adds to players inventory
            if (col.GetComponent<Inventory>().Add(item))
            {
                Destroy(this.gameObject);
            }
        }
    }
}
