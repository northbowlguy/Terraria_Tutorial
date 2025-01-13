using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToolClass", menuName = "Tool Class")]
public class ToolClass : ScriptableObject
{
    public string name;
    public Sprite sprite;
    public ItemClass.ToolType toolType;
}
