using UnityEngine;

public class FallingIngredient : MonoBehaviour
{
    public float destroyY = -6f; // below screen

    void Update()
    {
        // If it falls below the screen, destroy it
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
