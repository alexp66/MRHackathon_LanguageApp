using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Oculus.Interaction;

public class WristMenu : MonoBehaviour
{

    public bool menuIsOpen = false;
    public float animateTIme = .3f;
    float collapsedScale = .0001f;
    float openScale = .04f;
     public bool wristIsAnimating = false;

    [FoldoutGroup("Visuals")]
    public Color defaultButtonColors;
    [FoldoutGroup("Visuals")]
    public Color selectedButtonColors;

    [FoldoutGroup("Transforms")]
    public Transform closedPositions;
    [FoldoutGroup("Transforms")]
    public Transform leftOpenPos;
    [FoldoutGroup("Transforms")]
    public Transform centerOpenPos;
    [FoldoutGroup("Transforms")]
    public Transform rightOpenPos;

    [FoldoutGroup("General References")]
    public List<GameObject> wristButtons = new List<GameObject>();
    [FoldoutGroup("General References")]
    public GameObject leftButton;
    [FoldoutGroup("General References")]
    public GameObject centerButton;
    [FoldoutGroup("General References")]
    public GameObject rightButton;
    [FoldoutGroup("General References")]
    public GameManager gameManager;
    //[FoldoutGroup("General References")]
    //public Vector3 buttonOriginalLocalZPos;
    [FoldoutGroup("General References")]
    public PokeInteractable createButton_PokeInteractable;
    [FoldoutGroup("General References")]
    public PokeInteractable randomizeButton_PokeInteractable;
    [FoldoutGroup("General References")]
    public PokeInteractable practiceButton_PokeInteractable;
    [FoldoutGroup("General References")]
    public GameObject leftBubble;
    [FoldoutGroup("General References")]
    public GameObject centerBubble;
    [FoldoutGroup("General References")]
    public GameObject rightBubble;
    [FoldoutGroup("General References")]
    public MeshRenderer leftButtonMeshRenderer;
    [FoldoutGroup("General References")]
    public MeshRenderer centerButtonMeshRenderer;
    [FoldoutGroup("General References")]
    public MeshRenderer rightButtonMeshRenderer;
    [FoldoutGroup("General References")]
    public Transform transformsRefParent;

    [FoldoutGroup("Other Parent Transforms")]
    public Transform leftOpenPosOther;
    [FoldoutGroup("Other Parent Transforms")]
    public Transform centerOpenPosOther;
    [FoldoutGroup("Other Parent Transforms")]
    public Transform rightOpenPosOther;




    private void Start()
    {
        leftButton.transform.localPosition = closedPositions.localPosition;
        centerButton.transform.localPosition = closedPositions.localPosition;
        rightButton.transform.localPosition = closedPositions.localPosition;

        CloseAllHintBubbles();
    }

    [Button]
    public IEnumerator OnWristButtonPress(int buttonPressed) 
    {
        if (WristOpenCloseAnimation() == false) { yield break; }
        gameManager.source.PlayOneShot(gameManager.GetRandomPopSound());

        yield return new WaitForSeconds(animateTIme);
        switch (buttonPressed)
        {
            case 1:
                gameManager.RandomizeButtonPress();
                break;
            case 2:
                gameManager.CreateButtonPress();              
                break;
            case 3:
                gameManager.PracticeButtonPress();
                break;
        }
        yield break;
    }
    public bool WristOpenCloseAnimation()
    {
        if (wristIsAnimating) { return false; }
        menuIsOpen = !menuIsOpen;
        wristIsAnimating = true;

        //transformsRefParent.parent = this.transform; 
        leftButton.transform.parent = this.transform;
        centerButton.transform.parent = this.transform;
        rightButton.transform.parent = this.transform;

        createButton_PokeInteractable.enabled = false;
        randomizeButton_PokeInteractable.enabled = false;
        practiceButton_PokeInteractable.enabled = false;

        if (menuIsOpen)
        {
            leftButton.transform.parent = gameManager.wristButtonsAdoptionParent;
            centerButton.transform.parent = gameManager.wristButtonsAdoptionParent;
            rightButton.transform.parent = gameManager.wristButtonsAdoptionParent;

            Sequence moveSeq = DOTween.Sequence();
            Sequence rotationSeq = DOTween.Sequence();
            Sequence scaleSeq = DOTween.Sequence();

            /*moveSeq.Append(leftButton.transform.DOLocalMove(leftOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(centerOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(rightOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(leftOpenPos.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(centerOpenPos.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(rightOpenPos.localRotation.eulerAngles, animateTIme));*/

            moveSeq.Append(leftButton.transform.DOLocalMove(leftOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(centerOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(rightOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(leftOpenPosOther.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(centerOpenPosOther.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(rightOpenPosOther.localRotation.eulerAngles, animateTIme));

            scaleSeq.Append(leftButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(centerButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(rightButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));

            moveSeq.AppendCallback(ExpandDone);
            return true;
        }
        else
        {
            leftButton.transform.parent = this.transform;
            centerButton.transform.parent = this.transform;
            rightButton.transform.parent = this.transform;

            Sequence moveSeq = DOTween.Sequence();
            Sequence rotationSeq = DOTween.Sequence();
            Sequence scaleSeq = DOTween.Sequence();

            moveSeq.Append(leftButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(Vector3.zero, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(Vector3.zero, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(Vector3.zero, animateTIme));

            scaleSeq.Append(leftButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(centerButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(rightButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));

            moveSeq.AppendCallback(CollapseDone);
            return true;
        }
    }
    public void WristOpenCloseAnimationForEvent()
    {
        if (wristIsAnimating) { return; }

        //transformsRefParent.parent = this.transform; 
        leftButton.transform.parent = this.transform;
        centerButton.transform.parent = this.transform;
        rightButton.transform.parent = this.transform;

        createButton_PokeInteractable.enabled = false;
        randomizeButton_PokeInteractable.enabled = false;
        practiceButton_PokeInteractable.enabled = false;

        leftButtonMeshRenderer.material.color = defaultButtonColors;
        centerButtonMeshRenderer.material.color = defaultButtonColors;
        rightButtonMeshRenderer.material.color = defaultButtonColors;

        // HANDLE THE COLORS MANUALLY

        gameManager.source.PlayOneShot(gameManager.GetRandomPopSound());

        if(gameManager.userInteractionMode == GameManager.UserInteractionMode.Practice) {
            gameManager.EndPracticeMode();
            gameManager.OnSmallHintBubblePoked(.2f);
        }
        gameManager.userInteractionMode = GameManager.UserInteractionMode.None;
        //gameManager.OnSmallHintBubblePoked();
        menuIsOpen = !menuIsOpen;
        wristIsAnimating = true;

        if (menuIsOpen)
        {
            leftButton.transform.parent = gameManager.wristButtonsAdoptionParent;
            centerButton.transform.parent = gameManager.wristButtonsAdoptionParent;
            rightButton.transform.parent = gameManager.wristButtonsAdoptionParent;

            Sequence moveSeq = DOTween.Sequence();
            Sequence rotationSeq = DOTween.Sequence();
            Sequence scaleSeq = DOTween.Sequence();

            /*moveSeq.Append(leftButton.transform.DOLocalMove(leftOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(centerOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(rightOpenPos.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(leftOpenPos.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(centerOpenPos.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(rightOpenPos.localRotation.eulerAngles, animateTIme));*/
            
            moveSeq.Append(leftButton.transform.DOLocalMove(leftOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(centerOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(rightOpenPosOther.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(leftOpenPosOther.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(centerOpenPosOther.localRotation.eulerAngles, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(rightOpenPosOther.localRotation.eulerAngles, animateTIme));

            scaleSeq.Append(leftButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(centerButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(rightButton.transform.DOScale(openScale, animateTIme).SetEase(Ease.OutBounce));

            moveSeq.AppendCallback(ExpandDone);
        }
        else
        {
            leftButton.transform.parent = this.transform;
            centerButton.transform.parent = this.transform;
            rightButton.transform.parent = this.transform;

            Sequence moveSeq = DOTween.Sequence();
            Sequence rotationSeq = DOTween.Sequence();
            Sequence scaleSeq = DOTween.Sequence();

            moveSeq.Append(leftButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(centerButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));
            moveSeq.Append(rightButton.transform.DOLocalMove(closedPositions.localPosition, animateTIme).SetEase(Ease.OutBounce));

            rotationSeq.Append(leftButton.transform.DOLocalRotate(Vector3.zero, animateTIme));
            rotationSeq.Append(centerButton.transform.DOLocalRotate(Vector3.zero, animateTIme));
            rotationSeq.Append(rightButton.transform.DOLocalRotate(Vector3.zero, animateTIme));

            scaleSeq.Append(leftButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(centerButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));
            scaleSeq.Append(rightButton.transform.DOScale(collapsedScale, animateTIme).SetEase(Ease.OutBounce));

            moveSeq.AppendCallback(CollapseDone);
        }
    }
    void ExpandDone()
    {
        wristIsAnimating = false;
        createButton_PokeInteractable.enabled = true;
        randomizeButton_PokeInteractable.enabled = true;
        practiceButton_PokeInteractable.enabled = true;

        //transformsRefParent.parent = null;
        /*leftButton.transform.parent = gameManager.wristButtonsAdoptionParent;
        centerButton.transform.parent = gameManager.wristButtonsAdoptionParent;
        rightButton.transform.parent = gameManager.wristButtonsAdoptionParent;*/

        gameManager.menuIsExpanded = true;

        // make them follow
    }
    void CollapseDone()
    {
        wristIsAnimating = false;

        leftButtonMeshRenderer.material.color = defaultButtonColors;
        centerButtonMeshRenderer.material.color = defaultButtonColors;
        rightButtonMeshRenderer.material.color = defaultButtonColors;
    }

    public void LeftButtonPress(PointerEvent pointerData) // randomize
    {
        gameManager.menuIsExpanded = false;

        createButton_PokeInteractable.enabled = false;
        randomizeButton_PokeInteractable.enabled = false;
        practiceButton_PokeInteractable.enabled = false;

        FlashButtonColor(leftButtonMeshRenderer);
        centerButtonMeshRenderer.material.color = defaultButtonColors;
        rightButtonMeshRenderer.material.color = defaultButtonColors;

        //Handle the colors manually !!!!

        StartCoroutine(OnWristButtonPress(1));
        //ResetButtonZPositions();
    }
    public void CenterButtonPress(PointerEvent pointerData) // create
    {
        gameManager.menuIsExpanded = false;

        createButton_PokeInteractable.enabled = false;
        randomizeButton_PokeInteractable.enabled = false;
        practiceButton_PokeInteractable.enabled = false;

        leftButtonMeshRenderer.material.color = defaultButtonColors;
        FlashButtonColor(centerButtonMeshRenderer);
        rightButtonMeshRenderer.material.color = defaultButtonColors;

        StartCoroutine(OnWristButtonPress(2));
        //ResetButtonZPositions();
    }
    public void RightButtonPress(PointerEvent pointerData) // practice
    {
        gameManager.menuIsExpanded = false;

        createButton_PokeInteractable.enabled = false;
        randomizeButton_PokeInteractable.enabled = false;
        practiceButton_PokeInteractable.enabled = false;

        leftButtonMeshRenderer.material.color = defaultButtonColors;
        centerButtonMeshRenderer.material.color = defaultButtonColors;
        FlashButtonColor(rightButtonMeshRenderer);

        StartCoroutine(OnWristButtonPress(3));       
        //ResetButtonZPositions();
    }
    void FlashButtonColor(MeshRenderer rend)
    {
        rend.material.DOColor(selectedButtonColors, (animateTIme / 2)).SetLoops(4, LoopType.Yoyo).OnComplete(() =>
        {
            rend.material.color = defaultButtonColors;
        });
    }
    void ResetButtonZPositions()
    {
        /*rightButton.transform.localPosition = new Vector3(centerButton.transform.localPosition.x, centerButton.transform.localPosition.y, buttonLocalZPos);
        centerButton.transform.localPosition = new Vector3(centerButton.transform.localPosition.x, centerButton.transform.localPosition.y, buttonLocalZPos);
        leftButton.transform.localPosition = new Vector3(centerButton.transform.localPosition.x, centerButton.transform.localPosition.y, buttonLocalZPos);*/

        //rightButton.transform.localPosition = buttonOriginalLocalZPos;
        //centerButton.transform.localPosition = buttonOriginalLocalZPos;
        //leftButton.transform.localPosition = buttonOriginalLocalZPos;

    }

    public void ToggleAllButtons(bool onOrOff)
    {
        createButton_PokeInteractable.enabled = onOrOff;
        randomizeButton_PokeInteractable.enabled = onOrOff;
        practiceButton_PokeInteractable.enabled = onOrOff;
    }

    public void RandomizeButtonHover()
    {
        leftBubble.SetActive(true);
        leftBubble.transform.DOScale(Vector3.one, animateTIme);

    }
    public void CreateButtonHover()
    {
        centerBubble.SetActive(true);
        centerBubble.transform.DOScale(Vector3.one, animateTIme);
    }
    public void PracticeButtonHover()
    {
        rightBubble.SetActive(true);
        rightBubble.transform.DOScale(Vector3.one, animateTIme);

    }
    public void RandomizeButtonUNHover()
    {
        leftBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTIme);
        leftBubble.SetActive(false);
    }
    public void CreateButtonUNHover()
    {
        centerBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTIme);
        centerBubble.SetActive(false);
    }
    public void PracticeButtonUNHover()
    {
        rightBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTIme);
        rightBubble.SetActive(false);
    }
    public void CloseAllHintBubbles(float animateTime)
    {
        leftBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTime);
        centerBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTime);
        rightBubble.transform.DOScale(new Vector3(.001f, .001f, .001f), animateTime);
    }
    public void CloseAllHintBubbles()
    {
        leftBubble.transform.localScale = new Vector3(.001f, .001f, .001f);
        centerBubble.transform.localScale = new Vector3(.001f, .001f, .001f);
        rightBubble.transform.localScale = new Vector3(.001f, .001f, .001f);
    }
}
