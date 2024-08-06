using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using Oculus.Interaction;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;
using Shapes;
using TMPro;
using UnityEngine.UI;
using Oculus.Interaction.HandGrab;
public class WordPrefab : MonoBehaviour
{
    [FoldoutGroup("Colors & Visuals")]
    public Color buttonDefaultColor;
    [FoldoutGroup("Colors & Visuals")]
    public Color buttonHoveredColor;
    [FoldoutGroup("Colors & Visuals")]
    public Color buttonPressedColor;
    [FoldoutGroup("Colors & Visuals")]
    public Image nativePlaybackButtonIcon;
    [FoldoutGroup("Colors & Visuals")]
    public Image deleteButtonIcon;
    [FoldoutGroup("Colors & Visuals")]
    public Image editButtonIcon;
    [FoldoutGroup("Colors & Visuals")]
    public Image recordUserAudioIcon;
    [FoldoutGroup("Colors & Visuals")]
    public Image playbackUserAudioIcon;
    [FoldoutGroup("Colors & Visuals")]
    public Color recordingVoiceIconColor = Color.red;
    [FoldoutGroup("Colors & Visuals")]
    public Color centerBubbleActiveColor;
    [FoldoutGroup("Colors & Visuals")]
    public Color centerBubbleInActiveColor;

    public bool isForRandomize = false;

    public string associatedWord;
    public WordScriptableObject associatedWordSO;
    public GameObject card;
    public GameObject icon;
    public GameObject model;
    //public Rigidbody modelRB;

    //public TextBlock englishText;
    public TextMeshProUGUI englishText;
    public TextMeshProUGUI translatedText;

    public Shapes.Disc selectionDisc;
    public Shapes.Disc smallProgressDiscForAudioRecording;
    [HideInInspector] public float angleRadEndPoint = -4.712389f;
    [HideInInspector] public float angleRadStartPoint = 1.570796f;
    public InteractableUnityEventWrapper voiceInputEventWrapper;
    public InteractableUnityEventWrapper keyboardInputEventWrapper;
    public InteractableUnityEventWrapper preloadedPlaybackEventWrapper;
    public InteractableUnityEventWrapper userAudioEventWrapper;
    public InteractableUnityEventWrapper recordUserAudioEventWrapper;
    public InteractableUnityEventWrapper confirmButtonEventWrapper;
    public GameObject confirmButtonParent;
    public InteractableUnityEventWrapper deleteButtonEventWrapper;
    public InteractableUnityEventWrapper deleteConfirmButtonEventWrapper;
    public InteractableUnityEventWrapper deleteCancellationButtonEventWrapper;
    public InteractableUnityEventWrapper skipButtonEventWrapper;
    public InteractableUnityEventWrapper addModelButtonEventWrapper;
    public GameObject modelButtonParent;
    public TextBlock deleteText;
    public PointableUnityEventWrapper grabbableEventWrapper;
    public SpriteRenderer audioEntryIcon;

    public OVRVirtualKeyboard.ITextHandler ovrTextHandler;
    public Transform modelLinkPoint;
    public GameObject centerSphere;
    public Image centerBubbleImage;
    public bool centerSphereIsAnimating = false;
    
    GameManager gameManager;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        ovrTextHandler = GetComponentInChildren<OVRVirtualKeyboard.ITextHandler>();
        if (voiceInputEventWrapper)
        {
            //oiceInputEventWrapper.WhenSelect.AddListener(gameManager.LaunchVoiceSearch);
            //voiceInputEventWrapper.WhenUnselect.AddListener(gameManager.StopVoiceSearch);
            //voiceInputEventWrapper.WhenSelect.AddListener(OnButtonPress);
        }

        if (confirmButtonParent)
        {
            confirmButtonParent.gameObject.SetActive(false);
        }
        if (addModelButtonEventWrapper)
        {
            //addModelButtonEventWrapper.WhenSelect.AddListener(OnButtonPress); 
        }
    }

    private void OnDestroy() 
    {
        if (voiceInputEventWrapper)
        {
            //voiceInputEventWrapper.WhenSelect.RemoveListener(gameManager.LaunchVoiceSearch);
            //voiceInputEventWrapper.WhenUnselect.RemoveListener(gameManager.StopVoiceSearch);
        }
    }

    public void SetAllModelEvents(GameObject model)
    {
        if (model) 
        {
            PointableUnityEventWrapper modelEventWrapper = model.GetComponent<PointableUnityEventWrapper>();
            modelEventWrapper.WhenSelect.AddListener(OnModelGrabbed);
            modelEventWrapper.WhenUnselect.AddListener(OnModelReleased);
        }
    }
    public void OnVoiceEntryButtonPress()
    {
        if (gameManager.userInteractionMode == GameManager.UserInteractionMode.Practice) { return; }
        voiceInputEventWrapper.GetComponent<PokeInteractable>().enabled = false;
        gameManager.LaunchVoiceSearch();

        smallProgressDiscForAudioRecording.AngRadiansEnd = 1.570796f; // reset old one
        smallProgressDiscForAudioRecording.gameObject.SetActive(true);
        audioEntryIcon.DOColor(recordingVoiceIconColor, .375f).SetLoops(4, LoopType.Yoyo);
        DOTween.To(() => smallProgressDiscForAudioRecording.AngRadiansEnd, x => smallProgressDiscForAudioRecording.AngRadiansEnd = x, -4.712389f, 1.5f).OnComplete(() =>
        {
            gameManager.StopVoiceSearch();
            smallProgressDiscForAudioRecording.AngRadiansEnd = 1.570796f; // reset old one
            smallProgressDiscForAudioRecording.gameObject.SetActive(false);
            voiceInputEventWrapper.GetComponent<PokeInteractable>().enabled = true;
        });


    }
    public void OnButtonPress()
    {
        gameManager.prefabCurrentlyEngagingWith = this.gameObject;
    }

    public void OnGrabbed(PointerEvent pointerEvent)
    {
        GameObject interactor = (GameObject)pointerEvent.Data;
        if(interactor == gameManager.rightGrabInteractor) { gameManager.leftGrabInteractor.GetComponent<HandGrabInteractor>().enabled = false; }
        else if(interactor == gameManager.leftGrabInteractor) { gameManager.rightGrabInteractor.GetComponent<HandGrabInteractor>().enabled = false; }

        if (model != null && model.TryGetComponent<Grabbable>(out Grabbable grabbable))
        {
            model.GetComponentInChildren<HandGrabInteractable>().enabled = false; 
            model.GetComponent<Grabbable>().enabled = false;
        }
        
        if (gameManager.currentlyDraggedObject) { return; } // only allow one item to be dragged at a time. This will still be broken but for demo should suffice avoiding gamebreaking bugs 
        if (interactor.CompareTag("HandGrabInteractor"))
        {
            if (TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anchor))
            {
                anchor.enabled = false;
            }
            gameManager.draggingModel = false;

            gameManager.OnHandGrab(this.gameObject);
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }            
    }
    public void OnModelGrabbed(PointerEvent pointerEvent)
    {
        GetComponent<HandGrabInteractable>().enabled = false; 
        GetComponent<Grabbable>().enabled = false;
        if (gameManager.currentlyDraggedObject) { return; } // only allow one item to be dragged at a time 

        GameObject interactor = (GameObject)pointerEvent.Data;
        if (interactor == gameManager.rightGrabInteractor) { gameManager.leftGrabInteractor.GetComponent<HandGrabInteractor>().enabled = false; }
        else if (interactor == gameManager.leftGrabInteractor) { gameManager.rightGrabInteractor.GetComponent<HandGrabInteractor>().enabled = false; }
        
        if (interactor.CompareTag("HandGrabInteractor"))
        {
            if (TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anchor))
            {
                anchor.enabled = false;
            }
            gameManager.draggingModel = true;
            gameManager.OnHandGrab(this.gameObject);
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }         
    }
    public void OnReleased(PointerEvent pointerEvent)
    {
        GameObject interactor = (GameObject)pointerEvent.Data;
        gameManager.leftGrabInteractor.GetComponent<HandGrabInteractor>().enabled = true;
        gameManager.rightGrabInteractor.GetComponent<HandGrabInteractor>().enabled = true;
       

        if (interactor.CompareTag("HandGrabInteractor"))
        {
            if (model != null && model.TryGetComponent<Grabbable>(out Grabbable grabbable))
            {
                model.GetComponentInChildren<HandGrabInteractable>().enabled = true; // make the card grabbable again  
                model.GetComponent<Grabbable>().enabled = true;
            }
            
            gameManager.OnHandRelease();
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }
    }
    public void OnModelReleased(PointerEvent pointerEvent)
    {
        GameObject interactor = (GameObject)pointerEvent.Data;
        gameManager.leftGrabInteractor.GetComponent<HandGrabInteractor>().enabled = true;
        gameManager.rightGrabInteractor.GetComponent<HandGrabInteractor>().enabled = true;
        if (interactor.CompareTag("HandGrabInteractor"))
        {
            GetComponent<HandGrabInteractable>().enabled = true; // make the card grabbable again  
            GetComponent<Grabbable>().enabled = true;
            gameManager.draggingModel = false;
            gameManager.OnHandRelease();
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }      
    }
    
    public void OnConfirmButtonPress()
    {
        //confirmButtonEventWrapper.gameObject.SetActive(false);
        // Shrink and get rid of that confirm button
        SimulateSpawnModelPress();
        confirmButtonParent.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBounce).OnComplete(() =>
        {
            confirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
            confirmButtonParent.SetActive(false);
            confirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;
            gameManager.OnConfirmButtonPress(this.gameObject, associatedWordSO.word_en, associatedWordSO.word_es, false);
        });        
    }
    public void SimulateConfirmButtonPress()
    {
        if (confirmButtonParent == null || !confirmButtonParent.activeSelf) { return; }
        confirmButtonParent.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBounce).OnComplete(() =>
        {
            confirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
            confirmButtonParent.SetActive(false);
            confirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;
        });
    }
    public void OnDeleteButtonPress()
    {
        if(gameManager.userInteractionMode == GameManager.UserInteractionMode.Practice) { return; }

        deleteButtonEventWrapper.GetComponent<InteractableDebugVisual>().enabled = false;
        deleteButtonIcon.color = buttonDefaultColor;

        gameManager.source.PlayOneShot(gameManager.genericSelectionSoundForTrash);

        deleteButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        deleteButtonEventWrapper.gameObject.SetActive(false);
        deleteButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;

        deleteConfirmButtonEventWrapper.gameObject.SetActive(true);
        deleteCancellationButtonEventWrapper.gameObject.SetActive(true);
        deleteText.gameObject.SetActive(true);
    }
    public void OnDeleteConfirmButtonPress()
    {
        // Shrink and get rid of that delete button
        deleteConfirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        deleteConfirmButtonEventWrapper.gameObject.SetActive(false);
        deleteConfirmButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;

        deleteCancellationButtonEventWrapper.gameObject.SetActive(false);
        deleteText.gameObject.SetActive(false);

        deleteButtonEventWrapper.gameObject.SetActive(true);

        gameManager.source.PlayOneShot(gameManager.deleteSound);
        gameManager.RequestFlashcardDeletion(this.gameObject);
    }
    public void OnDeleteCancellationButtonPress()
    {
        deleteButtonEventWrapper.GetComponent<InteractableDebugVisual>().enabled = true;

        deleteCancellationButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        deleteCancellationButtonEventWrapper.gameObject.SetActive(false);
        deleteCancellationButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;
        deleteConfirmButtonEventWrapper.gameObject.SetActive(false);
        deleteText.gameObject.SetActive(false);

        deleteButtonEventWrapper.gameObject.SetActive(true);
    }
    public void SkipButtonPress()
    {
        skipButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        skipButtonEventWrapper.gameObject.SetActive(false);
        skipButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;

        gameManager.SkipButtonPressedForRandomizeWord(associatedWordSO, this.gameObject);
    }
    public void PlaybackNativeAudioPress()
    {
        if (associatedWordSO && associatedWordSO.audio_es.Count > 0)
        {
            gameManager.PlayPreloadedWordAudio(associatedWordSO, associatedWordSO.audio_es);
        }
    }
    /*public void PlayUserAudioButtonPress()
    {
        gameManager.PlayUserAudio(this);
    }*/
    public void RecordUserAudio()
    {
        StartCoroutine(gameManager.RecordSpokenWord_Start(this));
    }
    public void PlaybackUserAudio()
    {
        gameManager.PlayUserAudio(this);
    }
    public void OnHovered(PointerEvent pointerEvent)
    {
        GameObject interactor = (GameObject)pointerEvent.Data;
        if (interactor.CompareTag("HandGrabInteractor"))
        {
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }
        // if in practice mode, prime it for selection
    }
    public void OnUnhovered(PointerEvent pointerEvent)
    {
        GameObject interactor = (GameObject)pointerEvent.Data;
        if (interactor.CompareTag("HandGrabInteractor"))
        {
        }
        else if (interactor.CompareTag("DistanceGrabInteractor"))
        {
        }
    }

    public void OnKeyboardInputButtonPress()
    {
        /*gameManager.ovrKeyboard.gameObject.SetActive(true);
        gameManager.ovrKeyboard.transform.position = gameManager.cardSpawnPoint.transform.position;
        gameManager.ovrKeyboard.TextHandler = ovrTextHandler;
        gameManager.ovrKeyboard.TextHandler = ovrTextHandler;*/
    }

    public void OnCenterButtonPressed(bool playSound, bool playNativeAudioAfter)
    {
        // Spin the sphere
        if (centerSphereIsAnimating) { return; }
        if (playSound) { gameManager.source.PlayOneShot(gameManager.GetRandomPopSound()); }
        centerSphereIsAnimating = true;
        Vector3 newRotation = new Vector3(centerSphere.transform.localRotation.eulerAngles.x + 180, centerSphere.transform.localRotation.eulerAngles.y, centerSphere.transform.localRotation.eulerAngles.z);
        centerSphere.transform.DOLocalRotate(newRotation, .75f, RotateMode.FastBeyond360).OnComplete(() =>
        {
            centerSphereIsAnimating = false;
            if(playNativeAudioAfter) { PlaybackNativeAudioPress(); }
        });
    }

    public void OnCenterButtonPressed(bool playSound)
    {
        // Spin the sphere
        if (centerSphereIsAnimating) { return; }
        if (playSound) { gameManager.source.PlayOneShot(gameManager.GetRandomPopSound()); }
        centerSphereIsAnimating = true;
        Vector3 newRotation = new Vector3(centerSphere.transform.localRotation.eulerAngles.x + 180, centerSphere.transform.localRotation.eulerAngles.y, centerSphere.transform.localRotation.eulerAngles.z);
        centerSphere.transform.DOLocalRotate(newRotation, .75f, RotateMode.FastBeyond360).OnComplete(() =>
        {
            centerSphereIsAnimating = false;
        });
    }
    public void OnSpawnModelPress()
    {
        modelButtonParent.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBounce);

        if (addModelButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled == false) { return; } // assuming we already added the model then 

        gameManager.source.PlayOneShot(gameManager.GetRandomPopSound());
        addModelButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        addModelButtonEventWrapper.gameObject.SetActive(false);
        //addModelButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;
        modelButtonParent.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBounce);
        SimulateConfirmButtonPress();
        gameManager.OnSpawnModelButtonPress();
    }
    public void SimulateSpawnModelPress()
    {
        addModelButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = false;
        modelButtonParent.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBounce).OnComplete(() =>
        {
            addModelButtonEventWrapper.gameObject.SetActive(false);
            addModelButtonEventWrapper.gameObject.GetComponent<PokeInteractable>().enabled = true;
        });
       
    }
    public void HandleModelDeletion()
    {

    }
}
