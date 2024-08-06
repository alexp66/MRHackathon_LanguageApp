using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;
using Unity.XR.Oculus;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Oculus.Voice.Dictation;
using System.Text.RegularExpressions;
using Oculus.Interaction.HandGrab;
using DG.Tweening;
using Nova;
using System.Linq;
using TMPro;
using Meta.WitAi.Data;
using Oculus.Interaction;

public class GameManager : SerializedMonoBehaviour
{
    #region REFERENCES

    public OVRSceneManager sceneManager;
    [HideInInspector] public AudioSource source;
    [SerializeField] List<WordScriptableObject> allWordsInBuildWithSOs = new List<WordScriptableObject>();
    [SerializeField] List<WordScriptableObject> allUnplacedRandomizeWords = new List<WordScriptableObject>();
    Dictionary<string, string> builtInWordLibrary = new Dictionary<string, string>();
    [SerializeField] Dictionary<string, WordScriptableObject> inputEngWord_outputSO = new Dictionary<string, WordScriptableObject>();
    [SerializeField] Dictionary<Guid, string> inputAnchorGUID_outputAssociatedWord = new Dictionary<Guid, string>();
    Dictionary<Guid, bool> inputAnchorGUID_outputWhetherModelIsVisible = new Dictionary<Guid, bool>();
    Dictionary<Guid, (Vector3, Quaternion, Vector3)> inputAnchorGUID_outputModelsLastPose = new Dictionary<Guid, (Vector3, Quaternion, Vector3)>();
    public Dictionary<Guid, WordPrefab> inputAnchorGUID_outputWordPrefab = new Dictionary<Guid, WordPrefab>();
    [SerializeField] Dictionary<Guid, GameObject> inputAnchorGUID_outputRuntimeModelInstance = new Dictionary<Guid, GameObject>();
    Dictionary<string, List<Guid>> inputRoom_outputListOfSavedAnchors = new Dictionary<string, List<Guid>>();
    [SerializeField] Dictionary<string, float[]> inputWord_outputUserRecording = new Dictionary<string, float[]>();
    public Dictionary<string, Vector3> inputWordWithModel_outputPreferredCardLinkLocalPos = new Dictionary<string, Vector3>();
    bool scenePermissionWasGranted = false;
    Action<OVRSpatialAnchor.UnboundAnchor, bool> onLoadAnchor;
    string savedAnchorsFilePath;
    string anchorWordAssociationFilePath;
    string modelVisibilityFilePath;
    string modelPoseFilePath;
    string roomAnchorsFilePath;
    string userRecordingsFilePath;
    string isFirstTimeStartFilePath;
    [SerializeField] List<Guid> allAnchorsInSessionGuids = new List<Guid>();
    [SerializeField] List<OVRSpatialAnchor> allAnchorsInSession = new List<OVRSpatialAnchor>();
    bool hasPinched = false;

    public GameObject leftGrabInteractor;
    public GameObject rightGrabInteractor;

    [FoldoutGroup("Prefabs")]
    public GameObject flashcardPrefabWithSpatialAnchor; // Child this to the icon prefab? 
    [FoldoutGroup("Prefabs")]
    public GameObject flashcardPrefabWithNOSpatialAnchor;
    [FoldoutGroup("Prefabs")]
    public GameObject flashcardPrefabForRandomize;


    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject bedroomSO;
    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject livingRoomSO;
    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject kitchenSO;

    [FoldoutGroup("General References")]
    public HandGrabInteractor grabInteractor_RH;
    [FoldoutGroup("General References")]
    public HandGrabInteractor grabInteractor_LH;
    [FoldoutGroup("General References")]
    public Color smallBubbleDefaultColor;
    [FoldoutGroup("General References")]
    public Color bigBubbleDefaultColor;
    [FoldoutGroup("General References")]
    public Color bubbleTextDefaultColor;
    [FoldoutGroup("General References")]
    public WristMenu wristMenu;
    //[FoldoutGroup("General References")]
    //public MainPanel mainPanel;
    [FoldoutGroup("General References")]
    public GameObject allBubbles;
    [FoldoutGroup("General References")]
    public GameObject bigBubble;
    [FoldoutGroup("General References")]
    public GameObject smallBubble;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI bubblesAllPrimaryText;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI bubblesAllSecondaryText;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI bigBubblePrimaryText;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI bigBubbleSecondaryText;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI smallBubblePrimaryText;
    [FoldoutGroup("General References")]
    public TextMeshProUGUI smallBubbleSecondaryText;
    [FoldoutGroup("General References")]
    public Transform head;
    [FoldoutGroup("General References")]
    public OVRHand rightHand;
    [FoldoutGroup("General References")]
    public OVRHand leftHand;
    [FoldoutGroup("General References")]
    public Transform rightHandFingertip;
    [FoldoutGroup("General References")]
    public Transform leftHandFingertip;
    [FoldoutGroup("General References")]
    public TextAsset translationFile;
    [FoldoutGroup("General References")]
    public OVRPassthroughLayer passthroughLayer1;
    [FoldoutGroup("General References")]
    public OVRPassthroughLayer passthroughLayer2;
    [FoldoutGroup("General References")]
    public AppDictationExperience appDictationExperience;
    [FoldoutGroup("General References")]
    public GameObject cardSpawnPoint;
    //[FoldoutGroup("General References")]
    //public OVRVirtualKeyboard ovrKeyboard;
    [FoldoutGroup("General References")]
    public LineRenderer fingerTipLineRenderer;
    [FoldoutGroup("General References")]
    [SerializeField] Color fingerRayHighlightColor = Color.green;
    [FoldoutGroup("General References")]
    [SerializeField] LayerMask targetLayer;
    [FoldoutGroup("General References")]
    public Transform smallBubbleFollowAnchor;
    [FoldoutGroup("General References")]
    public GameObject confettiObject;
    [FoldoutGroup("General References")]
    public Transform wristButtonsAdoptionParent;

    [FoldoutGroup("Audio")]
    public List<AudioClip> popSoundEffects = new List<AudioClip>();
    [FoldoutGroup("Audio")]
    public AudioClip deleteSound;
    [FoldoutGroup("Audio")]
    public AudioClip genericSelectionSoundForTrash;
    [FoldoutGroup("Audio")]
    public AudioClip cardPlacementSoundEffect;
    [FoldoutGroup("Audio")]
    public AudioClip wrongGuessSoundEffect;
    [FoldoutGroup("Audio")]
    public List<AudioClip> guessSuccessSoundEffects = new List<AudioClip>();


    [FoldoutGroup("Physics Settings")]
    public float followSpeed = .75f;

    [FoldoutGroup("Head Follow")]
    [SerializeField] private Vector3 mainPanelOffset;
    [FoldoutGroup("Head Follow")]
    [SerializeField] private Vector3 hintPanelOffset;
    [FoldoutGroup("Head Follow")]
    public Vector3 wristButtonsOffset;
    [FoldoutGroup("Head Follow")]
    public float wristButtonsSmoothSpeed;
    [FoldoutGroup("Head Follow")]
    [SerializeField] private float smoothAmount = 1;
    [FoldoutGroup("Head Follow")]
    public float confettiSmoothAmount;
    [FoldoutGroup("Head Follow")]
    public Transform posInFrontOfHeadForStartup;
    [FoldoutGroup("Head Follow")]
    public Vector3 confettiOffset = new Vector3(0, 1, 0);
    [FoldoutGroup("Head Follow")]
    public Transform wristTrackingPoint;

    // bool isCreatingFlashcard = false;
    public GameObject prefabCurrentlyEngagingWith;
    [SerializeField] public GameObject currentlyDraggedObject;
    [SerializeField] WordPrefab currentlyDraggedParentPrefab;
    public bool draggingModel = false;
    string currentRoom;
    public string microphoneDeviceName;
    int isFirstTimeStartup = 0;
    bool isLayer1Active = false;
    string wordSearchedViaVoiceSDK;
    public bool pointingEnabled = false;
    bool isFingerPointing = false;
    bool isFingerPointing_RH = false;
    public GameObject currentPointedObject;
    float selectionTimer = 0;
    public bool smallBubblePokeAnimating = false;
    public bool smallBubbleUpdateAnimating = false;
    public float bubbleUpdateAnimTime = 1;
    //AudioClip currentlyRecordingClip;
    public int anchorLoadIncrementor = 0;
    public bool menuIsExpanded = false;

    public enum UserInteractionMode
    {
        AnchorCreation, Practice, Randomize, None
    }
    public enum AnchorVisibilityMode
    {
        AllCollapsed, AllExpanded, IconsGazeActivated, IconsPointActivated, IconsProximityActivated, IconsAndModelsOnly 
    }
    /*{
        Bedroom1, Bedroom2, Bedroom3, GuestRoom, LivingRoom1, LivingRoom2, LivingRoom3, Kitchen, Bathroom1, Bathroom2, Bathroom3, Garage, Basement, Attic, Closet, Pantry
    }*/
    [FoldoutGroup("State")] 
    public float selectionTime = .5f;
    [FoldoutGroup("State")]
    public UserInteractionMode userInteractionMode = UserInteractionMode.None;
    [FoldoutGroup("State")]
    public AnchorVisibilityMode visibilityMode;
    bool permissionAskedForMic = false;

    class PracticeModeData
    {
        public List<Guid> allUUIDsWeStartedWithThisPractice;
        public List<Guid> allUUIDsDrawingFromThisPractice;
        public WordPrefab currentCorrectWordoBeGuessed;
        public List<WordPrefab> otherPrefabsInMultipleChoice;
    }
    PracticeModeData currentPractice;
    #endregion


    private void Awake()
    {
        source = GetComponent<AudioSource>();
        onLoadAnchor = OnLocalized;
               
        //microphoneDeviceName = "Headset Microphone (Oculus Virtual Audio Device)"; // Test that this is right 

    }
    private void Start()
    {
        StartCoroutine(StartupProcotol());
        Vector3 targetPosMainPanel = head.TransformPoint(mainPanelOffset);
        smallBubbleFollowAnchor.position = posInFrontOfHeadForStartup.position;
        smallBubble.transform.position = posInFrontOfHeadForStartup.position;

        //ovrKeyboard.CommitTextEvent.AddListener(HandleKeyboardTextCommit);
        //ovrKeyboard.BackspaceEvent.AddListener(HandleKeyboardTextBackspace);
        //ovrKeyboard.EnterEvent.AddListener(HandleKeyboardTextReturnPressed);
        //targetMicName = Microphone.devices[0];
    }
    private void Update()
    {
        if (userInteractionMode == UserInteractionMode.AnchorCreation) // If they're in creation mode, listen for pinch to create new flashcard/anchor. 
        {
            //CheckPinch_CreationMode(rightHand);
            // Some particle effect on finger
        }
        if(currentlyDraggedObject)
        {
            HandleDragging(currentlyDraggedParentPrefab, draggingModel);
        }
        if (isFingerPointing && pointingEnabled && userInteractionMode == UserInteractionMode.Practice)
        {
            HandleFingerPoint(); 
        }
        if(currentRoom == null)
        {
            // Pop-up UI forcing user to input a current room asap otherwise bugs
            // (Need a flag that startup is done before checking this every frame)
        }

        //HandleDepthAPI();

        HandleHeadFollow(cardSpawnPoint, mainPanelOffset, 130, head, true);
        HandleHeadFollow(smallBubbleFollowAnchor.gameObject, hintPanelOffset, 75, head, true);
        HandleHeadFollow(smallBubble, hintPanelOffset, smoothAmount, smallBubbleFollowAnchor, true);
        HandleHeadFollow(confettiObject, confettiOffset, confettiSmoothAmount, head, false);
        confettiObject.transform.eulerAngles = new Vector3(-90, 0, 0);
        if (menuIsExpanded) {
            HandleHeadFollow(wristButtonsAdoptionParent.gameObject, wristButtonsOffset, wristButtonsSmoothSpeed, head, true);
        }
        else
        {
            HandleHeadFollow(wristButtonsAdoptionParent.gameObject, wristButtonsOffset, 1, head, true);
        }
        //if (leftHand.IsDataHighConfidence && startupComplete) { 
        if (leftHand.IsDataHighConfidence) { 
            wristMenu.transform.position = wristTrackingPoint.position;
            wristMenu.transform.rotation = wristTrackingPoint.rotation;
        }
    }

    public void SetRoom(string room)
    {
        currentRoom = room;
        if (inputRoom_outputListOfSavedAnchors.ContainsKey(room) == false)
        {
            inputRoom_outputListOfSavedAnchors.Add(room, new List<Guid>());
        }
    }
     
    IEnumerator StartupProcotol()
    {
        yield return StartCoroutine(InitializationProtocol());

        allUnplacedRandomizeWords = ReturnAllUnusedRandomizeWords();

        //wristMenu.leftButton.GetComponentInChildren<TextMeshPro>().text = "Randomize " + "(" + allUnplacedRandomizeWords.Count + ")";
        wristMenu.leftButton.GetComponentInChildren<TextMeshPro>().text = "Randomize";

        UpdateSmallHintBubble(bubbleUpdateAnimTime, "Press Wrist Button to begin!", "", false);      

        SetRoom("GenericRoom"); 

        yield break;
    } 

    #region STARTUP BUTTON PRESSES
    public void MicPermissionRequestEvent()
    {
        if (permissionAskedForMic) { return; }
        microphoneDeviceName = Microphone.devices[0]; // Sloppy current method to force permissions dialog on quest
        appDictationExperience.gameObject.SetActive(true);
        appDictationExperience.Activate();
        permissionAskedForMic = true;
    }
    public void CreateButtonPress()
    {
        MicPermissionRequestEvent();
        //appDictationExperience.Deactivate();
        userInteractionMode = GameManager.UserInteractionMode.AnchorCreation;
        CardCreationEvent();
        UpdateSmallHintBubble(1, "Press the voice search icon to add a word!", "", true);
    }
    [Button]
    public void PracticeButtonPress()
    {
        if (inputAnchorGUID_outputWordPrefab.Count == 0) 
        {
            UpdateSmallHintBubble(.2f, "Add at least one flashcard to practice", "", true);
            return;
        }

        userInteractionMode = GameManager.UserInteractionMode.Practice;
        LaunchPracticeMode();
    }

    public void OnConfirmButtonPress(GameObject prefab, string engWord, string translatedWord, bool viaModelButton) 
    {
        if (allUnplacedRandomizeWords.Count == 0) { wristMenu.randomizeButton_PokeInteractable.enabled = false; }

        WordPrefab wordPrefab = prefab.GetComponent<WordPrefab>();
        wordPrefab.isForRandomize = false;
        wordPrefab.skipButtonEventWrapper.gameObject.SetActive(false);

        wordPrefab.deleteButtonEventWrapper.gameObject.SetActive(true);

        ConfirmFlashcardPlacementAndCreateSpatialAnchor(prefab, engWord.ToLower(), translatedWord, viaModelButton);

        if (wordPrefab.associatedWordSO) { allUnplacedRandomizeWords.Remove(wordPrefab.associatedWordSO); }

        /*wordPrefab.modelButtonParent.gameObject.SetActive(true);
        wordPrefab.modelButtonParent.gameObject.transform.localScale = new Vector3(.0001f, .0001f, .0001f);
        wordPrefab.modelButtonParent.gameObject.transform.DOScale(new Vector3(.02f, .02f, .02f), .5f).SetEase(Ease.OutBounce);*/
        source.PlayOneShot(cardPlacementSoundEffect);

        float fake = 0;
        DOTween.To(() => fake, x => fake = x, 1, .5f).OnComplete(() =>
        {
            wordPrefab.OnCenterButtonPressed(true, true);
        });

        OnSmallHintBubblePoked(1.5f);
    }
    [Button]
    public void RandomizeButtonPress()
    {
        userInteractionMode = GameManager.UserInteractionMode.Randomize;
        PullRandomizeCard(null);
    }
    void PullRandomizeCard(WordScriptableObject skippedWord)
    {
        if(allUnplacedRandomizeWords.Count == 0) { return; }
        WordScriptableObject pulledWordSO;
        if (skippedWord) { pulledWordSO = GetNextRandomizedWordIfSkipped(skippedWord); } else { pulledWordSO = allUnplacedRandomizeWords[0]; }
        
        //allUnplacedRandomizeWords.Remove(pulledWordSO);

        //var card = Instantiate(flashcardPrefabForRandomize, mainPanel.transform.position, mainPanel.transform.rotation);
        var card = Instantiate(flashcardPrefabForRandomize, cardSpawnPoint.transform.position, Quaternion.identity);
        card.transform.rotation = Quaternion.LookRotation(card.transform.position - head.position);

        WordPrefab cardPrefab = card.GetComponent<WordPrefab>();
        cardPrefab.associatedWordSO = pulledWordSO;
        FillFlashcard(cardPrefab, pulledWordSO.word_en, pulledWordSO.word_es);
        card.gameObject.GetComponent<WordPrefab>().isForRandomize = true;

        card.transform.localScale = new Vector3(.001f, .001f, .001f);
        card.transform.DOScale(1, .75f);

        UpdateSmallHintBubble(1, "Place the " + pulledWordSO.word_en + " in your room", "", true);

        cardPrefab.modelButtonParent.gameObject.SetActive(true);
        cardPrefab.modelButtonParent.gameObject.transform.localScale = new Vector3(.0001f, .0001f, .0001f);
        cardPrefab.modelButtonParent.gameObject.transform.DOScale(new Vector3(.02f, .02f, .02f), .5f).SetEase(Ease.OutBounce);

    }
    public void SkipButtonPressedForRandomizeWord(WordScriptableObject skippedWord, GameObject skippedWordPrefab)
    {
       // allUnplacedRandomizeWords.Add(skippedWord); // Re-add it back
        PullRandomizeCard(skippedWord);

        skippedWordPrefab.transform.DOScale(new Vector3(.001f, .001f, .001f), .5f).SetEase(Ease.InBounce).OnComplete(() =>
        {
            Destroy(skippedWordPrefab);
        });

    }
    public void RandomizeCardGrabbed()
    {

    }
    public void RandomizeCardUnGrabbed()
    {

    }


    public void OnSpawnModelButtonPress() 
    {
        if(prefabCurrentlyEngagingWith.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anc) == false)
        {
            WordPrefab prefab = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>();
            OnConfirmButtonPress(prefabCurrentlyEngagingWith, prefab.associatedWordSO.word_en.ToLower(), prefab.associatedWordSO.word_es.ToLower(), true);
            return;
        }
        OVRSpatialAnchor anchor = prefabCurrentlyEngagingWith.GetComponent<OVRSpatialAnchor>();
        WordPrefab wordPrefab = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>();
        Pose pose;
        pose.position = prefabCurrentlyEngagingWith.transform.position;
        pose.rotation = prefabCurrentlyEngagingWith.transform.rotation;
        string wordToSearch;
        if(wordPrefab.associatedWordSO != null) { wordToSearch = wordPrefab.associatedWordSO.word_en.ToLower(); }
        else { wordToSearch = wordPrefab.associatedWord; }
        if (inputWordWithModel_outputPreferredCardLinkLocalPos.ContainsKey(wordToSearch)){ wordPrefab.modelLinkPoint.localPosition = inputWordWithModel_outputPreferredCardLinkLocalPos[wordToSearch]; }
        GameObject model = SpawnModel(anchor.Uuid, wordPrefab.modelLinkPoint, wordPrefab.associatedWordSO);
        model.GetComponent<Model>().associatedWordPrefab = wordPrefab;
        wordPrefab.model = model;
        wordPrefab.SetAllModelEvents(model);
    }

    #endregion



    #region INITIALIZATION FUNCTIONS

    IEnumerator InitializationProtocol()
    {
        yield return StartCoroutine(EnsureFilePathExist());
        allAnchorsInSessionGuids = LoadSavedAnchorsUUIDs();
        inputAnchorGUID_outputAssociatedWord = LoadAnchorWordAssociationDict();
        inputAnchorGUID_outputWhetherModelIsVisible = LoadModelVisibilityDict();
        inputAnchorGUID_outputModelsLastPose = LoadModelPoseDict();
        inputRoom_outputListOfSavedAnchors = LoadRoomAnchorsDict();
        inputWord_outputUserRecording = LoadUserRecordingsDict();
        isFirstTimeStartup = LoadIsFirstTimeStartup();

        InitializeRuntimeWordLookupDict();

        //yield return StartCoroutine(HandleMixedRealityStartup());
        yield return StartCoroutine(LoadSceneModelOnApplicationStart());

        /*while (!scenePermissionWasGranted)
        {
            yield return null;
        }*/


        //yield return StartCoroutine(ConsolidateObjects()); // don't use this 
        yield break;
    }

    void InitializeRuntimeWordLookupDict()
    {
        List<WordPrefab> helper = new List<WordPrefab>();
        // Do CSV table thing load into memory Dictionary
        byte[] rawBytes = Resources.Load<TextAsset>("es_en").bytes;
        builtInWordLibrary = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, string>>(rawBytes, DataFormat.Binary);

        allWordsInBuildWithSOs = Resources.LoadAll<WordScriptableObject>("WordAssets").ToList();
        foreach (WordScriptableObject wordSO in allWordsInBuildWithSOs) // Fill runtime dictionary with SO's via refrence to the word string in english
        {
            inputEngWord_outputSO.Add(wordSO.word_en.ToLower(), wordSO);
            
            if(inputWord_outputUserRecording.TryGetValue(wordSO.word_en.ToLower(), out float[] existingRecording))
            {
                AudioClip clip = AudioClip.Create(wordSO.word_es, existingRecording.Length, 2, 44100, false);
                clip.SetData(existingRecording, 0);
                wordSO.userAudio_es = clip;
            }
        }


    }
    IEnumerator ConsolidateObjects()
    {
        List<Guid> itemsToDelete = new List<Guid>();
        foreach (Guid item in allAnchorsInSessionGuids)
        {
            if(inputAnchorGUID_outputWordPrefab.ContainsKey(item) == false)
            {
                //if (allAnchorsInSession.Contains(item)) { allAnchorsInSession.Remove(item); } //  OK realized this list isn't even used by the game anyway
                itemsToDelete.Add(item);
            }
        }

        foreach (Guid item in itemsToDelete)
        {
            allAnchorsInSessionGuids.Remove(item);
        }

        yield break;
    }

    /*IEnumerator HandleMixedRealityStartup()
    {
        // First thing is try to load the scene model and have user set one up 

        yield return StartCoroutine(LoadSceneModelOnApplicationStart());

        yield break;
    }*/

    #region SCENE API 

    IEnumerator LoadSceneModelOnApplicationStart()
    {
        //var sceneManager = FindObjectOfType<OVRSceneManager>();

        sceneManager.SceneModelLoadedSuccessfully += SceneModelLoadedSuccessfuly;
        sceneManager.NoSceneModelToLoad += NoSceneModelToLoad;
        //sceneManager.NewSceneModelAvailable += UserPerformedNewRoomSetupWhileApplicationPaused;
        // sceneManager.LoadSceneModelFailedPermissionNotGranted += UserDeniedPermissionSoLoadSceneFailed;

        sceneManager.LoadSceneModel();

        yield break;
    }
    void SceneModelLoadedSuccessfuly()
    {
        scenePermissionWasGranted = true;
        StartCoroutine(LoadSpatialAnchors());
        //if (scenePermissionWasGranted) InitializeDepthAPI();  // If users accepts scene model, also ask them to allow Depth API
        //LoadAllAnchors();
    }
    void NoSceneModelToLoad() // User hasn't set up their Space yet
    {
        //Debug.Log("No model to load");

        // Determine whether it was because of lack of permission or because user hasn't done Space Setup yet. 
        //var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.SceneCaptureReturnedWithoutError += SuccessfullSceneCapture;
        //sceneManager.UnexpectedErrorWithSceneCapture += RestartSceneCaptureBecasueOfError;

        sceneManager.RequestSceneCapture();
    }
    void UserDeniedPermissionSoLoadSceneFailed()
    {
        //Debug.Log("user denied");

        //var sceneManager = FindObjectOfType<OVRSceneManager>();
        // Ask them to reconsider. If they say no, close the app I guesss. 
    }

    void UserPerformedNewRoomSetupWhileApplicationPaused()
    {
        //Debug.Log("CACA");

        //var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.LoadSceneModel();
    }
    void RestartSceneCaptureBecasueOfError()
    {
        //Debug.Log("errrrrrrrrrrrrr");

        //var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.RequestSceneCapture();
    }
    void SuccessfullSceneCapture()
    {
        //Debug.Log("Scene capture Successful");

        scenePermissionWasGranted = true;
        StartCoroutine(LoadSpatialAnchors());

        //var sceneManager = FindObjectOfType<OVRSceneManager>();
        //if (scenePermissionWasGranted) InitializeDepthAPI();  // If users accepts scene model, also ask them to allow Depth API
        //LoadAllAnchors();

    }
    #endregion

    #region DEPTH API 
    uint id;
    void HandleDepthAPI() // something for update loop. not we sure need it.. just saw it in documentation
    {
        Utils.GetEnvironmentDepthTextureId(ref id);
    }

    void InitializeDepthAPI() // Only call this if we know they gave permission for Scene 
    {
        if (Utils.GetEnvironmentDepthSupported()) // If this is a Quest 3 and thus has depth support
        {
            Utils.EnvironmentDepthCreateParams edcp;
            edcp.removeHands = false;
            Utils.SetupEnvironmentDepth(edcp);
        }
    }
    #endregion
     

    #region SAVING AND LOADING 

    #region SERIALIZATION/DESERIALIZATION
    IEnumerator SaveEverything()
    {
        yield return StartCoroutine(SaveAllActiveAnchorUUIDs());
        yield return StartCoroutine(SaveAllAnchorWordAssociations());
        yield return StartCoroutine(SaveModelVisibilityDict());
        yield return StartCoroutine(SaveModelPoseDict());
        yield return StartCoroutine(SaveRoomAnchorsDict());
    }
    IEnumerator SaveAllActiveAnchorUUIDs()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<List<Guid>>(allAnchorsInSessionGuids, DataFormat.Binary);
        File.WriteAllBytes(savedAnchorsFilePath, storageBytes);
        yield break;
    }
    IEnumerator SaveAllAnchorWordAssociations()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<Guid, string>>(inputAnchorGUID_outputAssociatedWord, DataFormat.Binary);
        File.WriteAllBytes(anchorWordAssociationFilePath, storageBytes);
        yield break;
    }
    IEnumerator SaveModelVisibilityDict()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<Guid, bool>>(inputAnchorGUID_outputWhetherModelIsVisible, DataFormat.Binary);
        File.WriteAllBytes(modelVisibilityFilePath, storageBytes);
        yield break;
    }
    IEnumerator SaveModelPoseDict()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<Guid, (Vector3, Quaternion, Vector3)>>(inputAnchorGUID_outputModelsLastPose, DataFormat.Binary);
        File.WriteAllBytes(modelPoseFilePath, storageBytes);
        yield break;
    }
    IEnumerator SaveRoomAnchorsDict()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<string, List<Guid>>>(inputRoom_outputListOfSavedAnchors, DataFormat.Binary);
        File.WriteAllBytes(roomAnchorsFilePath, storageBytes);
        yield break;
    }
    IEnumerator SaveUserRecordingsDict()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<Dictionary<string, float[]>>(inputWord_outputUserRecording, DataFormat.Binary);
        File.WriteAllBytes(userRecordingsFilePath, storageBytes);

        yield break;
    }
    IEnumerator SaveIsFirstTimeStartup()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<int>(isFirstTimeStartup, DataFormat.Binary);
        File.WriteAllBytes(isFirstTimeStartFilePath, storageBytes);
        yield break;
    }
    List<Guid> LoadSavedAnchorsUUIDs()
    {
        byte[] rawBytes = File.ReadAllBytes(savedAnchorsFilePath);
        List<Guid> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<List<Guid>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new List<Guid>(); }
    }
    Dictionary<Guid, string> LoadAnchorWordAssociationDict()
    {
        byte[] rawBytes = File.ReadAllBytes(anchorWordAssociationFilePath);
        Dictionary<Guid, string> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<Guid, string>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<Guid, string>(); }

    }
    Dictionary<Guid, bool> LoadModelVisibilityDict()
    {
        byte[] rawBytes = File.ReadAllBytes(modelVisibilityFilePath);
        Dictionary<Guid, bool> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<Guid, bool>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<Guid, bool>(); }
    }
    Dictionary<Guid, (Vector3, Quaternion, Vector3)> LoadModelPoseDict()
    {
        byte[] rawBytes = File.ReadAllBytes(modelPoseFilePath);
        Dictionary < Guid, (Vector3, Quaternion, Vector3)> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<Guid, (Vector3, Quaternion, Vector3)>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<Guid, (Vector3, Quaternion, Vector3)>(); }
    }
    Dictionary<string, List<Guid>> LoadRoomAnchorsDict()
    {
        byte[] rawBytes = File.ReadAllBytes(roomAnchorsFilePath);
        Dictionary <string, List<Guid>> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, List<Guid>>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<string, List<Guid>>(); }
    }
    Dictionary<string, float[]> LoadUserRecordingsDict()
    {
        byte[] rawBytes = File.ReadAllBytes(userRecordingsFilePath);
        Dictionary<string, float[]> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, float[]>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<string, float[]>(); }
    }
    int LoadIsFirstTimeStartup()
    {
        byte[] rawBytes = File.ReadAllBytes(isFirstTimeStartFilePath);
        return Sirenix.Serialization.SerializationUtility.DeserializeValue<int>(rawBytes, DataFormat.Binary);
    }
    #endregion 


    #region LOADING 
    IEnumerator LoadSpatialAnchors()
    {
        OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions();
        options.StorageLocation = OVRSpace.StorageLocation.Local;
        options.Uuids = allAnchorsInSessionGuids;

        if(allAnchorsInSessionGuids != null && allAnchorsInSessionGuids.Count > 0)
        {
            OVRSpatialAnchor.LoadUnboundAnchors(options, anchors =>
            {
                if (anchors == null)
                {
                    return;
                }
                foreach (var anchor in anchors)
                {
                    if (anchor.Localized)
                    {
                        OnLocalized(anchor, true);
                    }
                    else if (!anchor.Localizing)
                    {
                        anchor.Localize(onLoadAnchor);
                    }
                }
            });
        }
        
        yield break;
    }

    void OnLocalized(OVRSpatialAnchor.UnboundAnchor unboundAnchor, bool success)
    {
        if (!success) { return; }

        var pose = unboundAnchor.Pose;
        // INSTANTIATE A PREFAB
        GameObject spatialAnchorGO = Instantiate(flashcardPrefabWithSpatialAnchor, pose.position, pose.rotation);
        spatialAnchorGO.transform.localScale = new Vector3(.0001f, .0001f, .0001f);
        spatialAnchorGO.transform.DOScale(Vector3.one, .6f).SetEase(Ease.InBounce);
        OVRSpatialAnchor prefabAnchor = spatialAnchorGO.GetComponent<OVRSpatialAnchor>();
        unboundAnchor.BindTo(prefabAnchor);
        allAnchorsInSession.Add(prefabAnchor); // add the spatial anchor i either addcomponent or it's already on a prefab i instantiate
        inputAnchorGUID_outputWordPrefab.Add(unboundAnchor.Uuid, spatialAnchorGO.GetComponent<WordPrefab>());

        LoadAnchorsWordPrefabs(spatialAnchorGO, unboundAnchor.Uuid, pose);

        anchorLoadIncrementor++;     
    }

    void LoadAnchorsWordPrefabs(GameObject loadedAnchorGO, Guid anchorID, Pose anchorPose)
    {
        string associatedWord = inputAnchorGUID_outputAssociatedWord[anchorID];
        WordPrefab wordPrefab = loadedAnchorGO.GetComponent<WordPrefab>();
        wordPrefab.centerSphere.transform.localRotation = Quaternion.identity;
        wordPrefab.centerSphere.transform.localEulerAngles = new Vector3(180, 0, 0);
        wordPrefab.associatedWord = associatedWord;
        wordPrefab.voiceInputEventWrapper.gameObject.SetActive(false);
        if (wordPrefab.keyboardInputEventWrapper) { wordPrefab.keyboardInputEventWrapper.gameObject.SetActive(false); } 
        bool hasASO = false;
        if (inputEngWord_outputSO.TryGetValue(associatedWord, out WordScriptableObject wordSO)) 
        { 
            hasASO = true;
            wordPrefab.preloadedPlaybackEventWrapper.gameObject.SetActive(true);
            wordPrefab.nativePlaybackButtonIcon.gameObject.SetActive(true);
            wordPrefab.editButtonIcon.gameObject.SetActive(false);
            wordPrefab.voiceInputEventWrapper.gameObject.SetActive(false);
            wordPrefab.associatedWordSO = wordSO;
            wordPrefab.associatedWord = wordSO.word_en.ToLower();
            wordPrefab.englishText.text = wordSO.word_en;
            wordPrefab.translatedText.text = wordSO.word_es;
        }
        else
        {
            wordPrefab.preloadedPlaybackEventWrapper.gameObject.SetActive(false);
            wordPrefab.nativePlaybackButtonIcon.gameObject.SetActive(false);
            wordPrefab.editButtonIcon.gameObject.SetActive(true);
            wordPrefab.voiceInputEventWrapper.gameObject.SetActive(true);
            wordPrefab.englishText.text = wordPrefab.associatedWord;
            wordPrefab.translatedText.text = builtInWordLibrary[wordPrefab.associatedWord];
        }

        FontAdjustment(wordPrefab.englishText);
        FontAdjustment(wordPrefab.translatedText);

        // If a model was active last session, spawn it. 
        if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true && hasASO) // If the user has activated 3d model for this anchor and we have pose info 
        {
            wordPrefab.modelLinkPoint.localPosition = inputWordWithModel_outputPreferredCardLinkLocalPos[wordPrefab.associatedWord.ToLower()];
            wordPrefab.model = SpawnModel(anchorID, wordPrefab.modelLinkPoint, wordSO);
            wordPrefab.model.GetComponent<Model>().associatedWordPrefab = wordPrefab;
            wordPrefab.SetAllModelEvents(wordPrefab.model);
        }

        return;
        // Returning so we don't do below - outside the scope of what this project ended up as

        switch (visibilityMode)
        {
            case AnchorVisibilityMode.AllCollapsed:
                // Spawn NO visuals
                break;
            case AnchorVisibilityMode.AllExpanded:
                // Spawn all flashcards and models. 
                wordPrefab.card.SetActive(true);
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true && hasASO) // If the user has activated 3d model for this anchor and we have pose info 
                {
                    wordPrefab.model = SpawnModel(anchorID, wordPrefab.modelLinkPoint, wordSO);
                    wordPrefab.model.GetComponent<Model>().associatedWordPrefab = wordPrefab;
                }
                break;
            case AnchorVisibilityMode.IconsGazeActivated:
                wordPrefab.icon.SetActive(true);
                // Spawn only the icon
                break;
            case AnchorVisibilityMode.IconsPointActivated:
                wordPrefab.icon.SetActive(true);
                // Spawn only the icon
                break;
            case AnchorVisibilityMode.IconsProximityActivated:
                wordPrefab.icon.SetActive(true);
                // Spawn only the icon
                break;
            case AnchorVisibilityMode.IconsAndModelsOnly:
                // Spawn the icons
                wordPrefab.icon.SetActive(true);
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true && hasASO) // If the user has activated 3d model for this anchor and we have pose info 
                {
                    wordPrefab.model = SpawnModel(anchorID, wordPrefab.modelLinkPoint, wordSO);
                    wordPrefab.model.GetComponent<Model>().associatedWordPrefab = wordPrefab;
                }
                break;
            default:
                break;
        }
    }

    
    #endregion

    IEnumerator EnsureFilePathExist()
    {
        savedAnchorsFilePath = Application.persistentDataPath + "/" + "SavedAnchors" + ".bin";
        if (File.Exists(savedAnchorsFilePath) == false)
        {
            File.Create(savedAnchorsFilePath).Close();
        }

        anchorWordAssociationFilePath = Application.persistentDataPath + "/" + "AnchorWordDictionary" + ".bin";
        if (File.Exists(anchorWordAssociationFilePath) == false)
        {
            File.Create(anchorWordAssociationFilePath).Close();
        }
        modelVisibilityFilePath = Application.persistentDataPath + "/" + "ModelVisibilityDictionary" + ".bin";
        if (File.Exists(modelVisibilityFilePath) == false)
        {
            File.Create(modelVisibilityFilePath).Close();
        }
        modelPoseFilePath = Application.persistentDataPath + "/" + "ModelPoseDictionary" + ".bin";
        if (File.Exists(modelPoseFilePath) == false)
        {
            File.Create(modelPoseFilePath).Close();
        }
        roomAnchorsFilePath = Application.persistentDataPath + "/" + "RoomAnchorsDictionary" + ".bin";
        if (File.Exists(roomAnchorsFilePath) == false)
        {
            File.Create(roomAnchorsFilePath).Close();
        }
        userRecordingsFilePath = Application.persistentDataPath + "/" + "UserRecordingsDictionary" + ".bin";
        if (File.Exists(userRecordingsFilePath) == false)
        {
            File.Create(userRecordingsFilePath).Close();
        }
        isFirstTimeStartFilePath = Application.persistentDataPath + "/" + "other" + ".bin";
        if (File.Exists(isFirstTimeStartFilePath) == false)
        {
            File.Create(isFirstTimeStartFilePath).Close();
        }

        yield break;
    }
    #endregion

    #endregion

   

    #region USER MENU ACTIONS
    void OnAnchorVisibilityTypeChange(bool changeAllInScene) // If user changes visibility type, collapse/expand etc everything according to below modes. 
    {
        List<WordPrefab> allWordPrefabsInScene = new List<WordPrefab>();
        if (changeAllInScene) { allWordPrefabsInScene = GetAllRuntimeWordPrefabs(); }
        else { allWordPrefabsInScene.Add(prefabCurrentlyEngagingWith.GetComponent<WordPrefab>()); }

        switch (visibilityMode)
        {
            case AnchorVisibilityMode.AllCollapsed:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(false);
                    wordPrefab.icon.SetActive(false);
                    ExpandOrCollapseModelIfVisible(false, wordPrefab.gameObject.GetComponent<OVRSpatialAnchor>());
                }
                break;
            case AnchorVisibilityMode.AllExpanded:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(true);
                    wordPrefab.icon.SetActive(false);
                    ExpandOrCollapseModelIfVisible(true, wordPrefab.gameObject.GetComponent<OVRSpatialAnchor>());
                }
                break;
            case AnchorVisibilityMode.IconsGazeActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsPointActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsProximityActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsAndModelsOnly:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.card.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                    ExpandOrCollapseModelIfVisible(true, wordPrefab.gameObject.GetComponent<OVRSpatialAnchor>());
                }
                break;
        }
    }
    IEnumerator OnUserInteractionModeChange(UserInteractionMode newInteractionMode, UserInteractionMode oldInteractionMode)
    {
        switch (oldInteractionMode)
        {
            case UserInteractionMode.AnchorCreation:
                // Shut down any creation stuff
                break;
            case UserInteractionMode.Practice:
                // Shut down the practice game, reset everything back to baseline including visuals
                EndPracticeMode();
                break;
        }
        // Wait for completion of above tasks before continuing...
        switch (newInteractionMode)
        {
            case UserInteractionMode.AnchorCreation:
                // Subscribe to certain events
                // Visual partical effect on thumb/fingers?
                break;
            case UserInteractionMode.Practice:
                // Launch Practice Mode
                LaunchPracticeMode();
                break;
        }

        yield break;
    }
    void ExpandOneAnchorsFlashcardAndModel(GameObject prefabInteractingWith, bool expandFlashcard, bool expandModel)
    {
        OVRSpatialAnchor anchor = prefabInteractingWith.GetComponent<OVRSpatialAnchor>();
        WordPrefab wordPrefab = anchor.gameObject.GetComponent<WordPrefab>();
        if (expandFlashcard)
        {
            wordPrefab.card.SetActive(true);
        }
        if (expandModel)
        {
            inputAnchorGUID_outputRuntimeModelInstance[anchor.Uuid].SetActive(true);
        }
    }
    void ExpandOrCollapseModelIfVisible(bool expandOrCollapse, OVRSpatialAnchor anchor)
    {
        if (!inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchor.Uuid))
        { return; }

        if (expandOrCollapse)
        {
            inputAnchorGUID_outputRuntimeModelInstance[anchor.Uuid].SetActive(true);
        }
        else
        {
            inputAnchorGUID_outputRuntimeModelInstance[anchor.Uuid].SetActive(false);
        }
    }
    #endregion

    #region GRAB/RELEASE
    public void OnHandGrab(GameObject draggedObj)
    {
        currentlyDraggedObject = draggedObj;
        currentlyDraggedParentPrefab = draggedObj.GetComponentInParent<WordPrefab>();
    }
    public void OnHandRelease()
    {
        if(currentlyDraggedObject && currentlyDraggedParentPrefab) 
        {
            if(!currentlyDraggedParentPrefab.isForRandomize && currentlyDraggedObject.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor anchor)) // If we've alread;y set it up with a word, save it 
            {
                StartCoroutine(UpdateSpatialAnchorInfoWhenUserMovesAndReleasesPrefab(currentlyDraggedParentPrefab.gameObject, currentlyDraggedObject.transform.position, currentlyDraggedObject));
            }
            else if (currentlyDraggedParentPrefab.isForRandomize)
            {
                currentlyDraggedParentPrefab.confirmButtonParent.gameObject.SetActive(true);
                currentlyDraggedParentPrefab.confirmButtonParent.gameObject.transform.localScale = Vector3.zero;
                currentlyDraggedParentPrefab.confirmButtonParent.gameObject.transform.DOScale(new Vector3(.02f, .02f, .02f), .5f).SetEase(Ease.OutBounce);
                // Ask for confirmation, if given, then create the spatial anchor, flash the confirm button
            }
            currentlyDraggedObject = null;
            currentlyDraggedParentPrefab = null;
        }
        else
        {
            currentlyDraggedObject = null;
            currentlyDraggedParentPrefab = null;
        }
    }
    public void OnDistanceGrabbed()
    {
        // Remember the last position it was 
        // Or, send the flaschard only but leave the anchor (anchor not movable anyhow)
        // Disable all other distance grabbing if it isn't already (test how it works)

    }
    public void OnDistanceReleased()
    {
        // Send it back to the original position
    }
    public void OnHovered()
    {
        // If practice mode, prime it for selection
    }
    public void OnUnHovered() { }

    #endregion

    #region DRAGGING
    void HandleDragging(WordPrefab currentlyDraggedParentPrefab, bool isModel)
    {
        if (!isModel && currentlyDraggedParentPrefab.model != null) // if it's the flashcard
        {
            ProcessMovement(currentlyDraggedParentPrefab.modelLinkPoint, currentlyDraggedParentPrefab.model, followSpeed);
            //MoveModelIfMovingFlashcard(currentlyDraggedParentPrefab, .25f);
        }
        else // is the model 
        {
            ProcessMovement(currentlyDraggedParentPrefab.model.GetComponent<Model>().flashcardLinkPoint, currentlyDraggedParentPrefab.gameObject, followSpeed);

            currentlyDraggedParentPrefab.gameObject.transform.rotation = Quaternion.LookRotation(currentlyDraggedParentPrefab.gameObject.transform.position - head.position);

            //MoveFlashcardIfMovingModel(currentlyDraggedParentPrefab, .25f);
        }
    }
    protected void ProcessMovement(Transform movementAnchor, GameObject movementObject, float speed)
    {
        //movementObject.transform.DOMove(movementAnchor.position, );
        var step = speed * Time.deltaTime;
        movementObject.transform.position = Vector3.MoveTowards(movementObject.transform.position, movementAnchor.position, step);

        #region old physics stuff
        /*Vector3 direction = movementAnchor.position - movementObject.transform.position;

        //calc a target vel proportional to distance (clamped to maxVel)
        Vector3 targetVel = Vector3.ClampMagnitude(toVel * direction, maxVel);

        // calculate the velocity error
        Vector3 error = targetVel - movementObjRB.velocity;

        // calc a force proportional to the error (clamped to maxForce)
        Vector3 force = Vector3.ClampMagnitude(gain * error, maxForce);

        movementObjRB.AddForce(force);*/
        #endregion
    }


    #endregion

    #region CREATION MODE
    void CheckPinch_CreationMode(OVRHand hand)
    {
        float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        bool isIndexFingerPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        OVRHand.TrackingConfidence confidence = hand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (!hasPinched && isIndexFingerPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            hasPinched = true;
            //CardCreationEvent(rightHandFingertip.position);
        }
        else if (hasPinched && !isIndexFingerPinching)
        {
            hasPinched = false;
            //UnPinchEvent();
        }
    }
    public void CardCreationEvent(Vector3 pinchLocation)
    {
        //prefabCurrentlyEngagingWith = PreliminaryFlashcardCreation(pinchLocation);

        // Load up Word Search 
        
        //FlashCardCreationConfirmation(prefabCurrentlyCreating.GetComponent<WordPrefab>(), "Hi", "Ho");
    }
    void CardCreationEvent()
    {
        prefabCurrentlyEngagingWith = PreliminaryFlashcardCreation(cardSpawnPoint.transform.position);

        // Load up Word Search 
        
        //FlashCardCreationConfirmation(prefabCurrentlyCreating.GetComponent<WordPrefab>(), "Hi", "Ho");
    }
    public void LaunchKeyboard() 
    {

    }
    public void LaunchVoiceSearch() 
    {
        if (userInteractionMode == UserInteractionMode.None || userInteractionMode == UserInteractionMode.Randomize)
        {
            MicPermissionRequestEvent();
        }

        appDictationExperience.Deactivate(); // helps force initialize the first time after allowing mic perimssions i guess. 
        appDictationExperience.Activate();
    }
    public void StopVoiceSearch()
    {
        appDictationExperience.Deactivate();
    }
    public void ListeningText()
    { 
        //prefabCurrentlyCreating.GetComponent<WordPrefab>().englishText.Text = "Listening..";
    }
    
    public void LogVoiceSearchedWord(string rawVoiceInput)
    {
        if(userInteractionMode == UserInteractionMode.Practice) { return; }
        if(rawVoiceInput == null) { UpdateSmallHintBubble(.75f, "Sorry, not found :( Try a different one!", "", true); return; }
        WordPrefab wordPrefab = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>();
        wordSearchedViaVoiceSDK = GetVoiceWord(rawVoiceInput);
        wordPrefab.centerSphere.transform.localRotation = Quaternion.identity; // make sure english side is facing forward
        wordPrefab.englishText.text = wordSearchedViaVoiceSDK;
        FontAdjustment(prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText);
        WordSearched(prefabCurrentlyEngagingWith.GetComponent<WordPrefab>(), wordSearchedViaVoiceSDK);
    }
    string GetVoiceWord(string rawVoiceInput)
    {
        var result = Regex.Match(rawVoiceInput, @"^([\w\-]+)");
        return result.ToString();
    }
    void UnPinchEvent()
    {

    }

    void WordSearched(WordPrefab currentlyInteractingWordPrefab, string textSearched)
    {
        // force text search to lowercase
        string textSearchedLowerCase = textSearched.ToLower();
        // If our manual Scriptable Objects contain the word with associtaed voice and/or model, use that
        if (inputEngWord_outputSO.TryGetValue(textSearchedLowerCase, out WordScriptableObject wordSO))
        {
            currentlyInteractingWordPrefab.associatedWordSO = wordSO;

            // Fill flashcard with both language text, manage buttons for voice, request 3d model if exsists, etc 
            if (wordSO.model != null)
            {
                //Request model button active
                currentlyInteractingWordPrefab.modelButtonParent.SetActive(true);
                currentlyInteractingWordPrefab.modelButtonParent.gameObject.transform.localScale = new Vector3(.0001f, .0001f, .0001f);
                currentlyInteractingWordPrefab.modelButtonParent.gameObject.transform.DOScale(new Vector3(.02f, .02f, .02f), .5f).SetEase(Ease.OutBounce); 
            }
            /*if (wordSO.audio_en != null && wordSO.audio_en.Count > 0)
            {
                // audio button is active
            }*/
            if (wordSO.audio_es != null && wordSO.audio_es.Count > 0)
            {
                currentlyInteractingWordPrefab.preloadedPlaybackEventWrapper.gameObject.SetActive(true);
                currentlyInteractingWordPrefab.nativePlaybackButtonIcon.gameObject.SetActive(true);
                RectTransform rt = currentlyInteractingWordPrefab.nativePlaybackButtonIcon.gameObject.GetComponent<RectTransform>();
                rt.localScale = new Vector3(.0001f, .0001f, .0001f);
                rt.DOScale(new Vector3(4.88f, 4.88f, 4.88f), .5f).SetEase(Ease.OutBounce);
            }
            currentlyInteractingWordPrefab.translatedText.text = wordSO.word_es;
            FontAdjustment(currentlyInteractingWordPrefab.translatedText);
            OnSmallHintBubblePoked(1);
            ConfirmFlashcardPlacementAndCreateSpatialAnchor(currentlyInteractingWordPrefab.gameObject, wordSO.word_en.ToLower(), wordSO.word_es, false);
            source.PlayOneShot(cardPlacementSoundEffect);
            float fake = 0;
            DOTween.To(() => fake, x => fake = x, 1, .5f).OnComplete(() =>
            {
                currentlyInteractingWordPrefab.OnCenterButtonPressed(true, false);
            });
        }
        // Fallback to the generic key/value list then. 
        else if (builtInWordLibrary.TryGetValue(textSearchedLowerCase, out string translatedWord)) // if in the built-in dictionary via csv file 
        {
            // Fill the flashcard with both language text, grey out voice/3d model buttons 
            currentlyInteractingWordPrefab.translatedText.text = translatedWord;
            OnSmallHintBubblePoked(1);
            ConfirmFlashcardPlacementAndCreateSpatialAnchor(currentlyInteractingWordPrefab.gameObject, textSearchedLowerCase, translatedWord, false);
            source.PlayOneShot(cardPlacementSoundEffect);
            float fake = 0;
            DOTween.To(() => fake, x => fake = x, 1, .5f).OnComplete(() =>
            {
                currentlyInteractingWordPrefab.OnCenterButtonPressed(true, false);
            });
        }
        else
        {
            UpdateSmallHintBubble(.75f, "Sorry, not found :( Try a different one!", "", true);
            prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text = "";
        }
    }
    #endregion

    #region PRACTICE MODE
    void LaunchPracticeMode()
    {
        grabInteractor_RH.enabled = false;
        grabInteractor_LH.enabled = false;

        currentPractice = new PracticeModeData();
        currentPractice.allUUIDsDrawingFromThisPractice = new List<Guid>();
        currentPractice.allUUIDsWeStartedWithThisPractice = new List<Guid>();
        //currentPractice.allUUIDsWeStartedWithThisPractice = inputRoom_outputListOfSavedAnchors[currentRoom];
        currentPractice.allUUIDsWeStartedWithThisPractice = GetSafeListOfPracticeWords();
        foreach (Guid id in currentPractice.allUUIDsWeStartedWithThisPractice)
        {
            currentPractice.allUUIDsDrawingFromThisPractice.Add(id);

            inputAnchorGUID_outputWordPrefab[id].centerSphere.transform.localRotation = Quaternion.identity;
            //inputAnchorGUID_outputWordPrefab[id].englishText.text = "?";
           // inputAnchorGUID_outputWordPrefab[id].translatedText.text = "?";
            //inputAnchorGUID_outputWordPrefab[id].englishText.fontSize = 21;
            //inputAnchorGUID_outputWordPrefab[id].translatedText.fontSize = 21;
        }

        ReadyForNewPracticeWord(currentPractice);
    }
    List<Guid> GetSafeListOfPracticeWords()
    {
        List<Guid> toReturn = new List<Guid>();
        foreach (Guid id in allAnchorsInSessionGuids)
        {
            if (inputAnchorGUID_outputWordPrefab.ContainsKey(id))
            {
                toReturn.Add(id);
            }
        }
        return toReturn;
    }
    public void EndPracticeMode()
    {

        foreach (Guid id in currentPractice.allUUIDsWeStartedWithThisPractice)
        {
            WordPrefab prefabInQuestion = inputAnchorGUID_outputWordPrefab[id];

            if (inputAnchorGUID_outputWordPrefab.ContainsKey(id))
            {
                inputAnchorGUID_outputWordPrefab[id].centerSphere.transform.localRotation = Quaternion.identity;
                RegainWordText(prefabInQuestion);
                inputAnchorGUID_outputWordPrefab[id].englishText.color = Color.clear;
                inputAnchorGUID_outputWordPrefab[id].translatedText.color = Color.clear;
                inputAnchorGUID_outputWordPrefab[id].englishText.DOColor(Color.black, .5f);
                inputAnchorGUID_outputWordPrefab[id].centerBubbleImage.color = Color.white;
                inputAnchorGUID_outputWordPrefab[id].translatedText.DOColor(Color.black, .5f).OnComplete(() =>
                {
                    inputAnchorGUID_outputWordPrefab[id].OnCenterButtonPressed(false, false);
                });
            }
        }
        currentPractice = null;
        pointingEnabled = false;
        userInteractionMode = UserInteractionMode.None;
        grabInteractor_RH.enabled = true;
        grabInteractor_LH.enabled = true;
        // Reset the visuals, bring main panel back up. 
    }

    void ReadyForNewPracticeWord(PracticeModeData practice)
    {

        if (practice.allUUIDsDrawingFromThisPractice.Count == 0)
        {
            UpdateSmallHintBubble(bubbleUpdateAnimTime, "That's all of them!", "", true);
            EndPracticeMode();
            return;
        }

        Guid nextWordID = GetRandomWordFromRoomList(practice);
        if(MakeSureNextWordExistsInScene(nextWordID, out bool didSwitchGuid, out Guid newGuid) == false) { return; }
        if (didSwitchGuid) { nextWordID = newGuid; }

        WordPrefab wordPrefab = inputAnchorGUID_outputWordPrefab[nextWordID];
        practice.currentCorrectWordoBeGuessed = wordPrefab;
        // Display guessing UI 

        // Highlight some items around the room somehow
        //practice.otherPrefabsInMultipleChoice = FindSomeMultipleChoiceWordsExcludingCorrectOne(nextWordID, 3, practice.allUUIDsWeStartedWithThisPractice);

        UpdateSmallHintBubble(bubbleUpdateAnimTime, "Point at  " + TranslateAWord(wordPrefab.associatedWord), "", true);
        //smallBubblePrimaryText.text = "Find the " + wordPrefab.associatedWord;

        StartCoroutine(PointerCooldown());
       //pointingEnabled = true;
    }
    bool MakeSureNextWordExistsInScene(Guid nextWordID, out bool didSwitchGuid, out Guid alternativeGuid)
    {
        if (inputAnchorGUID_outputWordPrefab.ContainsKey(nextWordID) == false) 
        {
            currentPractice.allUUIDsDrawingFromThisPractice.Remove(nextWordID);
            if (currentPractice.allUUIDsDrawingFromThisPractice.Count == 0) // if we'vee NOW run outta words
            {
                UpdateSmallHintBubble(bubbleUpdateAnimTime, "That's all of them!", "", true);
                EndPracticeMode();
                alternativeGuid = nextWordID;
                didSwitchGuid = false;
                return false;
            }
            else // throw a new word back 
            {
                alternativeGuid = GetRandomWordFromRoomList(currentPractice);
                didSwitchGuid = true;
                return true;
            }          
        }
        else
        {
            alternativeGuid = nextWordID;
            didSwitchGuid = false;
            return true;
        }
    }
    IEnumerator PointerCooldown()
    {
        yield return new WaitForSeconds(1.5f);
        pointingEnabled = true;
    }
    Guid GetRandomWordFromRoomList(PracticeModeData practice)
    { 
        int pickedWordIndex = UnityEngine.Random.Range(0, practice.allUUIDsDrawingFromThisPractice.Count);
        Guid pickedWordID = practice.allUUIDsDrawingFromThisPractice[pickedWordIndex];
        practice.allUUIDsDrawingFromThisPractice.RemoveAt(pickedWordIndex);
        return pickedWordID;
    }
    List<WordPrefab> FindSomeMultipleChoiceWordsExcludingCorrectOne(Guid correctWordID, int numberToSource, List<Guid> listToSortFrom)
    {
        if(listToSortFrom.Count <= 1) { return null; }
        List<WordPrefab> toReturn = new List<WordPrefab>();
        if((listToSortFrom.Count - 1) < numberToSource) // Subtract 1 because we wanna exclude one to account for the 'correct word' 
        {
            numberToSource = listToSortFrom.Count - 1;
        }
        while (toReturn.Count < numberToSource)
        {
            int pickedWordIndex = UnityEngine.Random.Range(0, listToSortFrom.Count);
            Guid pickedWordID = listToSortFrom[pickedWordIndex];
            if(pickedWordID != correctWordID) { toReturn.Add(inputAnchorGUID_outputWordPrefab[pickedWordID]); }
        }
        return toReturn;
    }

    public void WordGuess(WordPrefab guessedPrefabWord)
    {
        if(userInteractionMode != UserInteractionMode.Practice) { return; }
        if(guessedPrefabWord == currentPractice.currentCorrectWordoBeGuessed)
        {
            confettiObject.GetComponent<ParticleSystem>().Play();
            source.PlayOneShot(GetRandomSuccessSound());
            RegainWordText(guessedPrefabWord);
            ReadyForNewPracticeWord(currentPractice);
            guessedPrefabWord.centerBubbleImage.DOColor(guessedPrefabWord.centerBubbleActiveColor, .1f).SetLoops(6, LoopType.Yoyo).OnComplete(() => {
                guessedPrefabWord.centerBubbleImage.color = guessedPrefabWord.centerBubbleInActiveColor;
            });
            
        }
        else
        {
            UpdateSmallHintBubble(.4f, "Try Again! Point at " + TranslateAWord(currentPractice.currentCorrectWordoBeGuessed.associatedWord), "", true);
            source.PlayOneShot(wrongGuessSoundEffect);
            /*guessedPrefabWord.centerBubbleImage.DOColor(Color.red, .1f).SetLoops(6, LoopType.Yoyo).OnComplete(() => {
                guessedPrefabWord.centerBubbleImage.color = guessedPrefabWord.centerBubbleInActiveColor;
            });*/
            pointingEnabled = true;
        }
    }
    void RegainWordText(WordPrefab wp)
    {

        if (wp.associatedWordSO != null)
        {
            wp.englishText.text = wp.associatedWordSO.word_en;
            wp.translatedText.text = wp.associatedWordSO.word_es;
        }
        else
        {
            wp.englishText.text = wp.associatedWord;
            wp.translatedText.text = builtInWordLibrary[wp.associatedWord.ToLower()];
        }
        FontAdjustment(wp.englishText);
        FontAdjustment(wp.translatedText);
        
    }
    void FontAdjustment(TextMeshProUGUI text)
    {
        if(text.text.Length <= 5)
        {
            text.fontSize = 5.4f;
        }
        else if (text.text.Length > 5 && text.text.Length <= 8)
        {
            text.fontSize = 3.3f;
        }
        else if (text.text.Length > 8)
        {
            text.fontSize = 2.33f;
        }        
    }
    #endregion

    #region 3D MODEL LOADING/DELETING
    GameObject SpawnModel(Guid anchorID, Transform modelAttachPos, WordScriptableObject associatedWordSO)
    {
        inputAnchorGUID_outputWhetherModelIsVisible[anchorID] = true;

        (Vector3, Quaternion, Vector3) pose;
        if (inputAnchorGUID_outputModelsLastPose.ContainsKey(anchorID))
        {
            pose = inputAnchorGUID_outputModelsLastPose[anchorID];
        }
        else
        {
            //pose = (anchorPose.position, anchorPose.rotation, associatedWordSO.defaultModelScale);
            pose = (modelAttachPos.position, modelAttachPos.rotation, associatedWordSO.model.transform.localScale);
            inputAnchorGUID_outputModelsLastPose.Add(anchorID, pose);
        }

        GameObject model = Instantiate(associatedWordSO.model, pose.Item1, pose.Item2);
        model.transform.localScale = new Vector3(.0001f, .0001f, .0001f);
        model.transform.DOScale(1, .6f).SetEase(Ease.InBounce);

        //model.transform.localScale = pose.Item3;

        inputAnchorGUID_outputRuntimeModelInstance.Add(anchorID, model);

        StartCoroutine(SaveEverything());
        return model;
    }
    void DeleteWordModel(Guid anchorID)
    {
        inputAnchorGUID_outputRuntimeModelInstance[anchorID].transform.DOScale(new Vector3(.0001f, .0001f, .0001f), .5f).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            Destroy(inputAnchorGUID_outputRuntimeModelInstance[anchorID]); // Destroy any model
            inputAnchorGUID_outputWhetherModelIsVisible[anchorID] = false;
            inputAnchorGUID_outputModelsLastPose.Remove(anchorID);
            inputAnchorGUID_outputRuntimeModelInstance.Remove(anchorID);

            inputAnchorGUID_outputWordPrefab[anchorID].HandleModelDeletion();
            StartCoroutine(SaveEverything());
        });
        // Leaves only the icon/flashcard behind. 
    }
    #endregion

    #region CREATE/DELETE SPATIAL ANCHOR
    GameObject PreliminaryFlashcardCreation(Vector3 inputLocation)
    {
        GameObject prefab = Instantiate(flashcardPrefabWithNOSpatialAnchor, inputLocation, Quaternion.identity);
        prefab.transform.rotation = Quaternion.LookRotation(prefab.transform.position - head.position);
        //prefab.transform.LookAt(head.transform);
        prefab.transform.localScale = new Vector3(.001f, .001f, .001f);
        prefab.transform.DOScale(1, .75f).SetEase(Ease.InBounce);
        prefab.GetComponent<WordPrefab>().card.SetActive(true);
        return prefab;
        // Create the flashcard prefab stuff for word selection
        // Ask user to confirm, once they confirm the word, run the Confirm method below. 
    }

    async void ConfirmFlashcardPlacementAndCreateSpatialAnchor(GameObject prefabToCreateAnchorOn, string chosenWord, string translatedWord, bool viaModelButton) // Saving Anchors created at runtime 
    {
        if(prefabToCreateAnchorOn.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor existingAnchor))
        {

            allAnchorsInSession.Remove(existingAnchor);
            allAnchorsInSessionGuids.Remove(existingAnchor.Uuid);
            inputAnchorGUID_outputAssociatedWord.Remove(existingAnchor.Uuid);
            inputAnchorGUID_outputWordPrefab.Remove(existingAnchor.Uuid);
            inputRoom_outputListOfSavedAnchors[currentRoom].Remove(existingAnchor.Uuid);

            existingAnchor.Erase((anchorr, successs) =>
            {
                if (successs)
                {
                    Destroy(existingAnchor);
                }
            });

            while (existingAnchor != null)
            {
                await Task.Yield();
            }
        }

        OVRSpatialAnchor anchorToSave = prefabToCreateAnchorOn.AddComponent<OVRSpatialAnchor>();

        while (!anchorToSave.Created && !anchorToSave.Localized) // keep checking for a valid and localized spatial anchor state
        {
            await Task.Yield();
        }

        var result = await anchorToSave.SaveAsync();

        // If save is successfull, save to all our lists. 
        if (result)
        {
            WordPrefab wordPrefab = prefabToCreateAnchorOn.GetComponent<WordPrefab>();
            wordPrefab.associatedWord = chosenWord;
            allAnchorsInSession.Add(anchorToSave);
            allAnchorsInSessionGuids.Add(anchorToSave.Uuid);
            inputAnchorGUID_outputAssociatedWord.Add(anchorToSave.Uuid, chosenWord);
            inputAnchorGUID_outputWordPrefab.Add(anchorToSave.Uuid, wordPrefab);
            inputRoom_outputListOfSavedAnchors[currentRoom].Add(anchorToSave.Uuid);

            prefabCurrentlyEngagingWith = null;

            // Save our app's lists to device
            StartCoroutine(SaveEverything());
            // On confirmation, Display the flashcard info
            FillFlashcard(wordPrefab, chosenWord, translatedWord);

            if (viaModelButton) {
                wordPrefab.modelLinkPoint.localPosition = inputWordWithModel_outputPreferredCardLinkLocalPos[wordPrefab.associatedWordSO.word_en.ToLower()];
                wordPrefab.model = SpawnModel(anchorToSave.Uuid, wordPrefab.modelLinkPoint, wordPrefab.associatedWordSO);

                wordPrefab.model.GetComponent<Model>().associatedWordPrefab = wordPrefab;
                wordPrefab.SetAllModelEvents(wordPrefab.model);
            }
        }
        else
        {
            Debug.Log("Save Failed");
        }
    }

    public void RequestFlashcardDeletion(GameObject prefabToDelete)
    {
        OVRSpatialAnchor anchorToDelete = prefabToDelete.GetComponent<OVRSpatialAnchor>();

        if (!anchorToDelete)  // If the flashcard doesn't have an anchor yet, meaning it was never confirmed, just delete the visausl and such 
        {
            //prefabToDelete.transform.DOScale(new Vector3(.001f, .001f, .001f), .5f).SetEase(Ease.OutBounce).OnComplete(() =>
            //{
                Destroy(prefabToDelete);
                return;
            //});
        }

        allAnchorsInSession.Remove(anchorToDelete);

        Guid anchorID = anchorToDelete.Uuid;
        anchorToDelete.Erase((anchor, success) =>
        {
            if (success)
            {
                // Delete the flashcard prefab instance
                if (anchorToDelete)
                {
                    Destroy(inputAnchorGUID_outputWordPrefab[anchorID]);
                }

                prefabToDelete.transform.DOScale(new Vector3(.001f, .001f, .001f), .5f).SetEase(Ease.OutBounce).OnComplete(() =>
                {
                    Destroy(prefabToDelete);

                    allAnchorsInSessionGuids.Remove(anchorID);
                    inputAnchorGUID_outputAssociatedWord.Remove(anchorID);
                    inputAnchorGUID_outputWordPrefab.Remove(anchorID);
                    inputRoom_outputListOfSavedAnchors[currentRoom].Remove(anchorID);

                    if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID)) { inputAnchorGUID_outputWhetherModelIsVisible.Remove(anchorID); }
                    if (inputAnchorGUID_outputModelsLastPose.ContainsKey(anchorID)) { inputAnchorGUID_outputModelsLastPose.Remove(anchorID); }
                    if (inputAnchorGUID_outputRuntimeModelInstance.ContainsKey(anchorID)) { DeleteWordModel(anchorID); }

                    // Save our app's lists to device
                    StartCoroutine(SaveEverything());
                });             
            }
        });
    }
    IEnumerator UpdateSpatialAnchorInfoWhenUserMovesAndReleasesPrefab(GameObject parentPrefabJustMoved, Vector3 positionJustMovedTo, GameObject prefabJustMoved)
    {
        HandGrabInteractable handGrabComponent = parentPrefabJustMoved.GetComponentInChildren<HandGrabInteractable>();
        handGrabComponent.enabled = false;

        // PER META DOCS - "Anchors cannot be moved. If the content must be moved, delete the old anchor and create a new one."
        parentPrefabJustMoved.transform.position = prefabJustMoved.transform.position; // worldspace. test this works as expected. 
        WordPrefab wordPrefab = parentPrefabJustMoved.GetComponent<WordPrefab>();
        OVRSpatialAnchor oldAnchor = parentPrefabJustMoved.GetComponent<OVRSpatialAnchor>();
        Guid oldAnchorsUUID = oldAnchor.Uuid;
        string associatedWord = inputAnchorGUID_outputAssociatedWord[oldAnchorsUUID];

        allAnchorsInSession.Remove(oldAnchor);
        allAnchorsInSessionGuids.Remove(oldAnchor.Uuid);
        inputAnchorGUID_outputAssociatedWord.Remove(oldAnchor.Uuid);
        inputAnchorGUID_outputWordPrefab.Remove(oldAnchor.Uuid);
        inputRoom_outputListOfSavedAnchors[currentRoom].Remove(oldAnchor.Uuid);

        oldAnchor.Erase((anchorr, successs) =>
        {
            if (successs)
            {
                Destroy(oldAnchor);
            }
        });


        while (oldAnchor != null)
        {
            yield return new WaitForEndOfFrame();
        }

        OVRSpatialAnchor newAnchor = parentPrefabJustMoved.AddComponent<OVRSpatialAnchor>();

        while (!newAnchor.Created && !newAnchor.Localized) // keep checking for a valid and localized spatial anchor state
        {
            yield return new WaitForEndOfFrame();
        }
        newAnchor.Save((anchor, success) =>
        {
            if (success)
            {
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputWhetherModelIsVisible.Add(newAnchor.Uuid, inputAnchorGUID_outputWhetherModelIsVisible[oldAnchorsUUID]); inputAnchorGUID_outputWhetherModelIsVisible.Remove(oldAnchorsUUID); }
                if (inputAnchorGUID_outputRuntimeModelInstance.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputRuntimeModelInstance.Add(newAnchor.Uuid, inputAnchorGUID_outputRuntimeModelInstance[oldAnchorsUUID]); inputAnchorGUID_outputRuntimeModelInstance.Remove(oldAnchorsUUID); }
                if (inputAnchorGUID_outputModelsLastPose.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputModelsLastPose.Add(newAnchor.Uuid, (wordPrefab.model.transform.position, wordPrefab.model.transform.rotation, wordPrefab.model.transform.localScale)); inputAnchorGUID_outputModelsLastPose.Remove(oldAnchorsUUID); }


                allAnchorsInSession.Add(newAnchor);
                allAnchorsInSessionGuids.Add(newAnchor.Uuid);
                inputAnchorGUID_outputAssociatedWord.Add(newAnchor.Uuid, associatedWord);
                inputAnchorGUID_outputWordPrefab.Add(newAnchor.Uuid, parentPrefabJustMoved.GetComponent<WordPrefab>());
                inputRoom_outputListOfSavedAnchors[currentRoom].Add(newAnchor.Uuid);

                StartCoroutine(SaveEverything());
            }
        });

        // Save our app's lists to device
        //yield return StartCoroutine(SaveEverything());

        handGrabComponent.enabled = true;

        yield break;
    }

    #endregion

    #region FILL FLASHCARDS

    void FillFlashcard(WordPrefab flashcardPrefab, string chosenEngWord, string translatedWord)
    {
        // FOrce first letter upper case
        string englishToDisplay = char.ToUpper(chosenEngWord[0]) + chosenEngWord.Substring(1);
        string translationToDisplay = char.ToUpper(translatedWord[0]) + translatedWord.Substring(1);

        flashcardPrefab.englishText.text = englishToDisplay;
        flashcardPrefab.translatedText.text = translationToDisplay;
        FontAdjustment(flashcardPrefab.englishText);
        FontAdjustment(flashcardPrefab.translatedText);

        if(flashcardPrefab.associatedWordSO != null)
        {
            if (EnsureUserAudioClipExists(flashcardPrefab, out AudioClip audio))
            {
                // Make the user audio playback button active
                flashcardPrefab.userAudioEventWrapper.gameObject.SetActive(true);
            }
            if (flashcardPrefab.associatedWordSO.audio_es != null)
            {
                // Make es audio playbck button active
                flashcardPrefab.preloadedPlaybackEventWrapper.gameObject.SetActive(true);
            }
           /* if (flashcardPrefab.associatedWordSO.audio_en != null)
            {
                // Make en audio playbck button active (not sure we even will do this)
            }*/
        }       
    }

    #endregion

    #region AUDIO PLAYBACK
    public void PlayPreloadedWordAudio(WordScriptableObject word, List<AudioClip> audioList)
    {
        if(audioList != null && audioList.Count > 0)
        {
           /* if (audioList == word.audio_en)
            {
                word.audio_en_incrementor++;
                if (word.audio_en_incrementor > (audioList.Count - 1)) { word.audio_en_incrementor = 0; }
                source.PlayOneShot(audioList[word.audio_en_incrementor]);
            }*/
            if (audioList == word.audio_es)
            {
                word.audio_es_incrementor++;
                if (word.audio_es_incrementor > (audioList.Count - 1)) { word.audio_es_incrementor = 0; }
                source.PlayOneShot(audioList[word.audio_es_incrementor]);
            }
            /*else if (audioList == word.userAudio_en)
            {
                word.userAudio_en_incrementor++;
                if (word.userAudio_en_incrementor > (audioList.Count - 1)) { word.userAudio_en_incrementor = 0; }
                source.PlayOneShot(audioList[word.userAudio_en_incrementor]);
            }
            else if (audioList == word.userAudio_es)
            {
                word.userAudio_es_incrementor++;
                if (word.userAudio_es_incrementor > (audioList.Count - 1)) { word.userAudio_es_incrementor = 0; }
                source.PlayOneShot(audioList[word.userAudio_es_incrementor]);
            }*/
        }

    }
    public void PlayUserAudio(WordPrefab word)
    {
        if(EnsureUserAudioClipExists(word, out AudioClip clipToPlay))
        {
            source.PlayOneShot(clipToPlay);
        }
    }

    bool EnsureUserAudioClipExists(WordPrefab wordPrefab, out AudioClip clipToReturn)
    {
        WordScriptableObject wordSO;
        if (wordPrefab.associatedWordSO) { wordSO = wordPrefab.associatedWordSO; } else { wordSO = null; }

        if (wordSO && wordSO.userAudio_es != null)
        {
            clipToReturn = wordSO.userAudio_es;
            return true;
        }
        else if (inputWord_outputUserRecording.TryGetValue(wordPrefab.associatedWord.ToLower(), out float[] existingAudio))
        {
            AudioClip newClip = AudioClip.Create(wordSO.word_es, existingAudio.Length, 2, 44100, false);
            newClip.SetData(existingAudio, 0);
            wordSO.userAudio_es = newClip;
            clipToReturn = wordSO.userAudio_es;
            return true;
        }
        else
        {
            clipToReturn = null;
            wordPrefab.userAudioEventWrapper.gameObject.SetActive(false);
            return false;
        }
    }
    #endregion 

    #region VOICE RECORDING

    public IEnumerator RecordSpokenWord_Start(WordPrefab word)
    {
        // Set up a max length of like 3 seconds
        // Record while the button is compressed, if 3 seconds are up uncompress it and stop recording. 
        // Then save the recording to memory as an asset, serialize it as a SO with a Guid and put Guid + Word string in a dictionary
        // Each wrod should have maximum 1 recordings possible for memory reasons 
        if(Microphone.devices.Length == 0 || Microphone.devices[0] == null) { yield break; }
        Microphone.GetDeviceCaps(microphoneDeviceName, out int minFreq, out int maxFreq);

        int recordingLengthInSecs = 2;
        AudioClip recording = Microphone.Start(microphoneDeviceName, false, recordingLengthInSecs, maxFreq);
        
        yield return new WaitForSeconds(recordingLengthInSecs);

        float[] samples = new float[recording.samples * recording.channels];
        recording.GetData(samples, 0);

        word.associatedWordSO.userAudio_es = recording;

        if (inputWord_outputUserRecording.TryGetValue(word.associatedWord.ToLower(), out float[] existingAudio)) { inputWord_outputUserRecording.Remove(word.associatedWord.ToLower()); }
        inputWord_outputUserRecording.Add(word.associatedWord.ToLower(), samples);
        StartCoroutine(SaveUserRecordingsDict());

        word.userAudioEventWrapper.gameObject.SetActive(true);

        yield break;
    }
    /*   void ForceRecordingEnd()
       {
           Microphone.End(microphoneDeviceName);
       }*/
    #endregion

    #region PASSTHROUGH MODIFICATION
    void ToggleDarkPassthrough()
    {

        // Fade the opactiy or camera view to get a more gradual fade. 

        if (isLayer1Active)
        {
            passthroughLayer1.enabled = false;
            passthroughLayer2.enabled = true;
        }
        else
        {
            passthroughLayer1.enabled = true;
            passthroughLayer2.enabled = false;
        }
       // int tempDepth = passthroughLayer1.compositionDepth;
        //passthroughLayer1.compositionDepth = passthroughLayer2.compositionDepth;
        //passthroughLayer2.compositionDepth = tempDepth;

        isLayer1Active = !isLayer1Active;
    }

    #endregion

    #region FINGER POINTING
    public void OnFingerPointGestureDetected_RH(bool yesOrNo)
    {

        if (yesOrNo == true && isFingerPointing == true)  // If the other finger is already pointing, dont do anything if it's now pointing again 
        {
            return;
        }
        if (yesOrNo == false) // Disable the raycast visual
        {
            HandleFingerUnpoint();
        }
        isFingerPointing = yesOrNo;
        isFingerPointing_RH = true;
    }
    public void OnFingerPointGestureDetected_LH(bool yesOrNo)
    {
        if (yesOrNo == true && isFingerPointing == true)  // If the other finger is already pointing, dont do anything if it's now pointing again 
        {
            return;
        }
        if (yesOrNo == false) // Disable the raycast visual
        {
            HandleFingerUnpoint();
        }
        isFingerPointing = yesOrNo;
        isFingerPointing_RH = false;

    }
    void HandleFingerPoint()
    {
        Transform origin;
        Vector3 originDirection;
        if(isFingerPointing_RH) { origin = rightHandFingertip; originDirection = origin.right; } else { origin = leftHandFingertip; originDirection = -origin.right; }
        // Raycast hit detection

        if (Physics.Raycast(origin.position, originDirection, out RaycastHit hit, Mathf.Infinity, targetLayer))
        {
            bool firstHit = false;
            GameObject parentHit = GetWordPrefabOfAPointedObject(hit.transform.gameObject);
            if(currentPointedObject == null) { currentPointedObject = parentHit; firstHit = true; }
            if(currentPointedObject != parentHit || firstHit == true) // new object hit
            {
                DOTween.Kill(999); // kill any previous disc tween 
                WordPrefab prefabOnWayOut = currentPointedObject.GetComponent<WordPrefab>();
                //prefabOnWayOut.selectionDisc.AngRadiansEnd = 1.570796f; // reset old one
                //prefabOnWayOut.selectionDisc.gameObject.SetActive(false); // reset old one
                prefabOnWayOut.centerBubbleImage.color = prefabOnWayOut.centerBubbleInActiveColor; // reset
                //prefabOnWayOut.centerBubbleImage.gameObject.SetActive(false); // reset old one
                selectionTimer = 0;

                currentPointedObject = GetWordPrefabOfAPointedObject(hit.transform.gameObject);
                WordPrefab prefabOnWayIn = currentPointedObject.GetComponentInParent<WordPrefab>();
                //prefabOnWayIn.selectionDisc.gameObject.SetActive(true); // reset old one
                //prefabOnWayIn.selectionDisc.AngRadiansEnd = 1.570796f; // make sure it's beginning position
                //prefabOnWayIn.centerBubbleImage.gameObject.SetActive(true); // reset old one
                prefabOnWayIn.centerBubbleImage.color = prefabOnWayOut.centerBubbleInActiveColor; // make sure it's beginning position
                //DOTween.To(() => prefabOnWayIn.selectionDisc.AngRadiansEnd, x => prefabOnWayIn.selectionDisc.AngRadiansEnd = x, -4.712389f, selectionTime).SetId(999);
                DOTween.To(() => prefabOnWayIn.centerBubbleImage.color, x => prefabOnWayIn.centerBubbleImage.color = x, prefabOnWayIn.centerBubbleActiveColor, selectionTime).SetId(999);
                //if(goToAnimate == null) { return; }                           
            }
            selectionTimer += Time.deltaTime;
            // fill up visual in conjuinction with that selection timer
            // rotate it or something

            if (selectionTimer >= selectionTime)
            {
                pointingEnabled = false; // false until whatever action is completed
                WordPrefab prefab = currentPointedObject.GetComponent<WordPrefab>();
                DOTween.Kill(999); // kill any previous disc tween 
                //prefab.selectionDisc.AngRadiansEnd = 1.570796f; // reset 
                prefab.centerBubbleImage.color = prefab.centerBubbleInActiveColor; // reset 
                //prefab.selectionDisc.gameObject.SetActive(false); // reset old one              
                HandleValidSelection(prefab);
                source.PlayOneShot(GetRandomPopSound());
                //prefab.centerBubbleImage.gameObject.SetActive(false); // reset old one
                currentPointedObject = null;
                selectionTimer = 0;
            }
            //UpdateRayVisualization(origin.position, hit.point, true);
        }
        else
        {
            if (currentPointedObject != null)
            {
                WordPrefab prefab = currentPointedObject.GetComponent<WordPrefab>();
                DOTween.Kill(999); // kill any previous disc tween 
                //prefab.selectionDisc.AngRadiansEnd = 1.570796f; // Reset the disc
                //prefab.selectionDisc.gameObject.SetActive(false); // reset old one
                prefab.centerBubbleImage.color = prefab.centerBubbleInActiveColor; // Reset the disc
                //prefab.centerBubbleImage.gameObject.SetActive(false); // reset old one
                currentPointedObject = null;
                selectionTimer = 0;
                // Make visuals stop and stop the countdown process
            }
            //UpdateRayVisualization(origin.position, origin.position + origin.forward * 1000, false);
        }
    }
    
    void HandleValidSelection(WordPrefab wordPrefab)
    {
        WordGuess(wordPrefab);
        // re-enable pointing when needed
        // do visual stuff 
    }
    void HandleFingerUnpoint()
    {
        if (currentPointedObject != null)
        {
            WordPrefab prefab = currentPointedObject.GetComponent<WordPrefab>();
            DOTween.Kill(999); // kill any previous disc tween 
            prefab.selectionDisc.AngRadiansEnd = 1.570796f; // reset old one
            prefab.selectionDisc.gameObject.SetActive(false); // reset old one
            currentPointedObject = null;
            // Make visuals stop and stop the countdown process
        }
        selectionTimer = 0;
        fingerTipLineRenderer.enabled = false;
    }
    void UpdateRayVisualization(Vector3 startPosition, Vector3 endPosition, bool hitSomething)
    {
        if (fingerTipLineRenderer != null)
        {
            fingerTipLineRenderer.enabled = true;
            fingerTipLineRenderer.SetPosition(0, startPosition);
            fingerTipLineRenderer.SetPosition(1, endPosition);
            fingerTipLineRenderer.material.color = hitSomething ? Color.green : Color.red;
        }
    }
    
    #endregion 

    #region HELPERS
    
    void HandleHeadFollow(GameObject thingToMove, Vector3 offset, float smoothAmount, Transform target, bool followHead)
    {
        Vector3 targetPos = target.TransformPoint(offset);
        thingToMove.transform.position = Vector3.Lerp(thingToMove.transform.position, targetPos, Time.deltaTime * smoothAmount);      

        if (followHead) 
        {
            Quaternion targetRot = Quaternion.Euler(new Vector3(0, target.eulerAngles.y, 0));
            //thingToMove.transform.rotation = Quaternion.Slerp(thingToMove.transform.rotation, targetRot, Time.deltaTime * smoothAmount);
            thingToMove.transform.rotation = Quaternion.LookRotation(thingToMove.transform.position - head.position);      
        }
    }
    List<WordPrefab> GetAllRuntimeWordPrefabs()
    {
        List<WordPrefab> toReturn = new List<WordPrefab>();
        foreach (Guid uuid in allAnchorsInSessionGuids)
        {
            if (inputAnchorGUID_outputWordPrefab.ContainsKey(uuid))
            {
                toReturn.Add(inputAnchorGUID_outputWordPrefab[uuid]);
            }
        }
        return toReturn;
    }

    List<WordScriptableObject> ReturnAllUnusedRandomizeWords()
    {
        List<WordScriptableObject> toReturn = new List<WordScriptableObject>();

        foreach (WordScriptableObject word in allWordsInBuildWithSOs)
        {
            if (inputAnchorGUID_outputAssociatedWord.ContainsValue(word.word_en.ToLower()) == false)
            {
                toReturn.Add(word);
            }
        }
        return toReturn;
    }
    WordScriptableObject GetNextRandomizedWordIfSkipped(WordScriptableObject skippedWord)
    {
        if(allUnplacedRandomizeWords.Count == 1) { return skippedWord; }
        int skippedIndex = allUnplacedRandomizeWords.IndexOf(skippedWord);
        if(allUnplacedRandomizeWords.ElementAtOrDefault(skippedIndex + 1)) { return allUnplacedRandomizeWords[skippedIndex + 1]; }
        else { return allUnplacedRandomizeWords[0]; }
    }

    public void HandleKeyboardTextCommit(string textCommitted)
    {
        if (prefabCurrentlyEngagingWith)
        {
            prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text + textCommitted;
        }
    }
    public void HandleKeyboardTextBackspace()
    {
        if (prefabCurrentlyEngagingWith)
        {
           // YourString = YourString.Remove(YourString.Length - 1);
            prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text.Remove(prefabCurrentlyEngagingWith.GetComponent<WordPrefab>().englishText.text.Length - 1);
        }
    }
    public void HandleKeyboardTextReturnPressed()
    {
        WordPrefab wordPrefab = prefabCurrentlyEngagingWith.GetComponent<WordPrefab>();
        // Only putting up one letter at at time..
        WordSearched(wordPrefab, wordPrefab.englishText.text);
        
    }
    GameObject GetWordPrefabOfAPointedObject(GameObject hit)
    {
        if (hit.transform.CompareTag("PostIt")) { return hit.GetComponentInParent<WordPrefab>().gameObject; }
        else if (hit.transform.CompareTag("Model")) { return hit.transform.gameObject.GetComponent<Model>().associatedWordPrefab.gameObject; }
        else { return null; }
    }
    #endregion

    public void OnSmallHintBubblePoked(float textFadeTime)
    {
        if (smallBubblePokeAnimating || userInteractionMode == UserInteractionMode.Practice) { return; }
        MeshRenderer rend = smallBubble.GetComponent<MeshRenderer>();
        smallBubble.GetComponentInChildren<PokeInteractable>().enabled = false;
        smallBubblePokeAnimating = true;

        source.PlayOneShot(GetRandomPopSound());
        rend.enabled = false;
        /*rend.material.DOColor(Color.clear, .1f).OnComplete(() =>
        {
            rend.material.DOColor(smallBubbleDefaultColor, .6f).OnComplete(() =>
            {
                smallBubblePopAnimating = false;
            });
        });*/
        smallBubblePrimaryText.DOColor(Color.clear, 5).SetId(222).OnComplete(() =>
        {
            smallBubblePokeAnimating = false;
        });
    }

    public void UpdateSmallHintBubble(float animTime, string newTextPrimary, string newTextSecondary, bool playSound)
    {
        if (DOTween.IsTweening(222))
        {
            DOTween.Kill(222);
            smallBubblePokeAnimating = false;
        }
        if (playSound) { source.PlayOneShot(GetRandomPopSound()); }
        float halfTIme = animTime / 2;
        if (smallBubbleUpdateAnimating) { return; }
        smallBubbleUpdateAnimating = true;

        smallBubble.SetActive(true);
        MeshRenderer rend = smallBubble.GetComponent<MeshRenderer>();
        rend.enabled = true;
        smallBubblePrimaryText.text = "";
        smallBubbleSecondaryText.text = "";
        smallBubblePrimaryText.color = Color.clear;
        smallBubbleSecondaryText.color = Color.clear;

        smallBubble.transform.localScale = Vector3.zero;
        smallBubble.transform.DOScale(new Vector3(.1f, .1f, .1f), halfTIme).SetEase(Ease.InOutBounce).OnComplete(() =>
        {
            smallBubblePrimaryText.text = newTextPrimary;
            smallBubbleSecondaryText.text = newTextSecondary;
            smallBubblePrimaryText.DOColor(bubbleTextDefaultColor, halfTIme);
            smallBubbleSecondaryText.DOColor(bubbleTextDefaultColor, halfTIme).OnComplete(() =>
            {
                smallBubbleUpdateAnimating = false;
                smallBubble.GetComponentInChildren<PokeInteractable>().enabled = true;
            });
        });
    }
    public void UpdateTextOfCardBubble(WordPrefab card, float animTime, TextMeshProUGUI newTextEng, TextMeshProUGUI newTextTranslated)
    {
        float halfTIme = animTime / 2;

       // smallBubblePrimaryText.text = newTextPrimary;
        //smallBubbleSecondaryText.text = newTextSecondary;
        newTextEng.DOColor(bubbleTextDefaultColor, halfTIme);
        newTextTranslated.DOColor(bubbleTextDefaultColor, halfTIme);

    }
    string TranslateAWord(string wordToTranslate)
    {
        if (inputEngWord_outputSO.TryGetValue(wordToTranslate, out WordScriptableObject wordSO))
        {
            return wordSO.word_es;
        }
        else if (builtInWordLibrary.TryGetValue(wordToTranslate, out string translatedWord)) // if in the built-in dictionary via csv file 
        {
            // Fill the flashcard with both language text, grey out voice/3d model buttons 
            return translatedWord;
        }
        else
        {
            return null;
        }
    }
    public AudioClip GetRandomPopSound()
    {
        int indexToReturn = UnityEngine.Random.Range(0, popSoundEffects.Count);
        return popSoundEffects[indexToReturn];
    }
    public AudioClip GetRandomSuccessSound()
    {
        int indexToReturn = UnityEngine.Random.Range(0, guessSuccessSoundEffects.Count);
        return guessSuccessSoundEffects[indexToReturn];
    }
}
