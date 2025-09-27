using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchTestObject : MonoBehaviour, IInteractable
{
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color clickColor = Color.green;
    [SerializeField] private float scaleMultiplier = 1.2f;
    
    private Renderer objectRenderer;
    private Vector3 originalScale;
    private Coroutine colorResetCoroutine;
    
    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        // Set initial color
        if (objectRenderer != null)
            objectRenderer.material.color = normalColor;
    }
    
    public void OnInteract()
    {
        Debug.Log($"{gameObject.name} was touched/clicked!");
        
        // Visual feedback
        StartCoroutine(InteractAnimation());
    }
    
    private IEnumerator InteractAnimation()
    {
        // Change color
        if (objectRenderer != null)
            objectRenderer.material.color = clickColor;
        
        // Scale animation
        float duration = 0.3f;
        float elapsed = 0f;
        
        // Scale up
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            transform.localScale = Vector3.Lerp(originalScale * scaleMultiplier, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
        
        // Reset color after a delay
        yield return new WaitForSeconds(0.2f);
        if (objectRenderer != null)
            objectRenderer.material.color = normalColor;
    }
    
    private void OnMouseEnter()
    {
        if (objectRenderer != null)
            objectRenderer.material.color = highlightColor;
    }
    
    private void OnMouseExit()
    {
        if (objectRenderer != null)
            objectRenderer.material.color = normalColor;
    }
}