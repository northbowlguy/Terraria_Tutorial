using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public int stackLimit = 64;
    public ToolClass tool;

    public Vector2 offset;
    public Vector2 multiplier;

    public GameObject inventoryUI;
    public GameObject inventorySlotPrefab;

    public int inventoryWidth;
    public int inventoryHeight;
    public InventorySlot[,] inventorySlots;
    public GameObject[,] uiSlots;

    public void Start()
    {
        inventorySlots = new InventorySlot[inventoryWidth, inventoryHeight];
        uiSlots = new GameObject[inventoryWidth, inventoryHeight];
        
        SetupUI();
        UpdateInventoryUI();
        Add(new ItemClass(tool));
    }

    void SetupUI()
    {
        for (int x = 0; x < inventoryWidth; x++)
        {
            for (int y = 0; y < inventoryHeight; y++)
            {
                GameObject inventorySlot = Instantiate(inventorySlotPrefab, inventoryUI.transform.GetChild(0).transform);
                inventorySlot.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + offset.x, (y * multiplier.y) + offset.y);
                uiSlots[x, y] = inventorySlot;
                inventorySlots[x, y] = null;
                
            }
        }
    }

    void UpdateInventoryUI()
    {
        for (int x = 0; x < inventoryWidth; x++)
        {
            for (int y = 0; y < inventoryHeight; y++)
            {
                if (inventorySlots[x, y] == null)
                {
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().sprite = null;
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().enabled = false;

                    uiSlots[x, y].transform.GetChild(1).GetComponent<Text>().text = "0";
                    uiSlots[x, y].transform.GetChild(1).GetComponent<Text>().enabled = false;
                }
                else
                {
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().enabled = true;
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().sprite = inventorySlots[x, y].item.sprite;

                    uiSlots[x, y].transform.GetChild(1).GetComponent<Text>().text = inventorySlots[x, y].quantity.ToString();
                    uiSlots[x, y].transform.GetChild(1).GetComponent<Text>().enabled = true;
                }
            }
        }
    }

    public bool Add(ItemClass item)
    {
        Vector2Int itemPos = Contains(item);
        bool added = false;
        if (itemPos != Vector2Int.one * -1)
        {
            if (inventorySlots[itemPos.x, itemPos.y].quantity < stackLimit)
            {
                inventorySlots[itemPos.x, itemPos.y].quantity += 1;
                added = true;
            }
        }

        if (!added)
        {
            for (int y = inventoryHeight - 1; y >= 0; y--)
            {
                if (added)
                {
                    break;
                }
                for (int x = 0; x < inventoryWidth; x++)
                {
                    if ((inventorySlots[x, y] == null))
                    {
                        inventorySlots[x, y] = new InventorySlot { item = item, position = new Vector2Int(x, y), quantity = 1 };
                        added = true;
                        break;
                    }
                    //else
                    //{
                    //    if (inventorySlots[x, y].item.name == item.name)
                    //    {
                    //        if (item.isStackable)
                    //        {
                    //            inventorySlots[x, y].quantity += 1;
                    //            added = true;
                    //            break;
                    //        }

                    //    }
                    //}
                }
            }
        }
        UpdateInventoryUI();
        return added;
    }

    public Vector2Int Contains(ItemClass item)
    {

        for (int y = inventoryHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < inventoryWidth; x++)
            {
                if (inventorySlots[x, y] != null)
                {
                    if (inventorySlots[x, y].item.sprite == item.sprite)
                    {
                        return new Vector2Int(x, y);
                    }                
                }
            }
        }
        return Vector2Int.one * -1;
    }

    public void Remove(ItemClass item)
    {

    }

}
