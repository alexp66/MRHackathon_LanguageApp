using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;

public class HandPointer : MonoBehaviour
{
    public OVRHand rightHand;
    public GameObject CurrentTarget { get; private set; }

    [SerializeField] bool showRaycast = true;
    [SerializeField] Color highlightColor = Color.red;
    [SerializeField] LayerMask targetLayer;
    [SerializeField] LineRenderer lineRenderer;

    Color originalColor;
    Renderer currentRenderer;

    private void Update()
    {
        CheckHandPointer(rightHand); 
    }


    void CheckHandPointer(OVRHand hand)
    {
        if(Physics.Raycast(hand.PointerPose.position, hand.PointerPose.forward, out RaycastHit hit, Mathf.Infinity, targetLayer))
        {
            if(CurrentTarget != hit.transform.gameObject)
            {
                CurrentTarget = hit.transform.gameObject;
                currentRenderer = CurrentTarget.GetComponent<Renderer>();
                originalColor = currentRenderer.material.color;
                currentRenderer.material.color = highlightColor;
            }

            UpdateRayVisualization(hand.PointerPose.position, hit.point, true);
        }
        else
        {
            if(CurrentTarget != null)
            {
                currentRenderer.material.color = originalColor;
                CurrentTarget = null;
            }

            UpdateRayVisualization(hand.PointerPose.position, hand.PointerPose.position + hand.PointerPose.forward * 1000, false);
        }
    }

    void UpdateRayVisualization(Vector3 startPosition, Vector3 endPosition, bool hitSomething)
    {
        if(showRaycast && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.material.color = hitSomething ? Color.green : Color.red;
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

}


