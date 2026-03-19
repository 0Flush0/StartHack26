using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Selectable : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock block;

    private static readonly int IsSelectedID = Shader.PropertyToID("_IsSelected");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void SetSelected(bool selected)
    {
        rend.GetPropertyBlock(block);
        block.SetFloat(IsSelectedID, selected ? 1f : 0f);
        rend.SetPropertyBlock(block);
    }
}