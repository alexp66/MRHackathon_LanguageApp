using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Oculus.Interaction;
using Nova;

public class MainPanel : MonoBehaviour
{
    GameManager gameManager;

    public Color defaultButtonBorderColor;
    public Color highlightedButtonBorderColor;

    public TextBlock additionalPanel;

    public GameObject createButtonParent;
    public GameObject randomizeButtonParent;
    public GameObject practiceButtonParent;

    public RoundedBoxProperties createButton;
    public RoundedBoxProperties randomizeButton;
    public RoundedBoxProperties practiceButton;

    public PokeInteractable createButton_PokeInteractable;
    public PokeInteractable randomizeButton_PokeInteractable;
    public PokeInteractable practiceButton_PokeInteractable;

    DOTween buttonBorderTween;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void FlashButtonColor(GameObject button)
    {
        //InteractableColorVisual viz = button.gameObject.GetComponent<InteractableColorVisual>();
        //DOTween.To(() => button.BorderColor, x => button.BorderColor = x, highlightedButtonBorderColor, 1).SetLoops(-1, LoopType.Yoyo).SetId(1234);
        DOTween.To(() => button.transform.localScale, x => button.transform.localScale = x, new Vector3(.055f, .055f, .055f), 1).SetLoops(-1, LoopType.Yoyo).SetId(1234);
    }

    public void KillButtonFlashTween(GameObject button)
    {
        DOTween.Kill(1234);
        // DOTween.To(() => button.BorderColor, x => button.BorderColor = x, defaultButtonBorderColor, .5f);
        DOTween.To(() => button.transform.localScale, x => button.transform.localScale = x, new Vector3(.05f, .05f, .05f), .5f);

    }

    public void ToggleAllButtons(bool onOrOff)
    {
        createButton_PokeInteractable.enabled = onOrOff;
        randomizeButton_PokeInteractable.enabled = onOrOff;
        practiceButton_PokeInteractable.enabled = onOrOff;
    }

    public void CreateButtonPress()
    {
        gameManager.userInteractionMode = GameManager.UserInteractionMode.AnchorCreation;
        gameManager.CreateButtonPress();
    }
    public void RandomizeButtonPress()
    {
        gameManager.userInteractionMode = GameManager.UserInteractionMode.Randomize;
        gameManager.RandomizeButtonPress();
    }
    public void PracticeButtonPress()
    {
        gameManager.userInteractionMode = GameManager.UserInteractionMode.Practice;
        gameManager.PracticeButtonPress();
    }
   
}
