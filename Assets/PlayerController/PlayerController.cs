using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Inventory inventory;
    public bool inventoryShowing = false;

    public float moveSpeed;
    public float jumpForce;
    public bool onGround;

    private Rigidbody2D rb;
    private Animator anim;

    private float horizontal;
    public bool hit;
    public bool place;

    public Vector2 spawnPos;
    public Vector2Int mousePos;
    public int playerRange;
    public TerrainGeneration terrainGenerator;
    public TileClass selectedTile;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        inventory = GetComponent<Inventory>();
    }

    public void Spawn()
    {
        GetComponent<Transform>().position = spawnPos;

    }

    //public void OnTriggerStay2D(Collider2D col)
    //{
    //    if (col.CompareTag("Ground"))
    //    {
    //        onGround = true;
    //    }
    //}

    //public void OnTriggerExit2D(Collider2D col)
    //{
    //    if (col.CompareTag("Ground"))
    //    {
    //        onGround = false;
    //    }
    //}
    private void FixedUpdate()
    {
        //do stuff
        
        float jump = Input.GetAxisRaw("Jump");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        


        if (horizontal > 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        if (vertical > 0.1f || jump > 0.1f)
        {
            if (onGround)
            {
                movement.y = jumpForce;
            }
        }

        rb.velocity = movement;
    }
    public void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        hit = Input.GetMouseButtonDown(0);
        place = Input.GetMouseButton(1);
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            inventoryShowing = !inventoryShowing;
        }

        if (Vector2.Distance(transform.position, mousePos) <= playerRange &&
            Vector2.Distance(transform.position, mousePos) > 1f)
        {
            if (place)
            {
                terrainGenerator.CheckTile(selectedTile, mousePos.x, mousePos.y, false);
            }
        }

        if (Vector2.Distance(transform.position, mousePos) <= playerRange)
        {
            if(hit)
            {
                terrainGenerator.RemoveTile(mousePos.x, mousePos.y);
            }
        }

        //set mouse pos
        mousePos.x = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - 0.5f);
        mousePos.y = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).y - 0.5f);

        inventory.inventoryUI.SetActive(inventoryShowing);

        anim.SetFloat("horizontal", horizontal);
        anim.SetBool("hit", hit || place);

    }
}
