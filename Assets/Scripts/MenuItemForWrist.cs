using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MenuItemForWrist : MonoBehaviour
{
    public GameObject bubble;
    bool bubbleAnimating = false;
    GameManager gm;
    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }
    public void OnHintBubblePoked()
    {
        if (bubbleAnimating) { return; }
        MeshRenderer rend = bubble.GetComponent<MeshRenderer>();
        bubbleAnimating = true;
        // Play pop sound
        rend.material.DOColor(Color.clear, .1f).OnComplete(() =>
        {
            rend.material.DOColor(gm.smallBubbleDefaultColor, .6f).OnComplete(() =>
            {
                bubbleAnimating = false;
            });
        });
    }
}
