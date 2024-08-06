using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using Unity.XR.Oculus;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Oculus.Voice.Dictation;
using System.Text.RegularExpressions;
using DG.Tweening;
using Nova;
public class GameManager : MonoBehaviour
{
    #region REFERENCES
    bool isFirstTimeStarting;
    AudioSource source;
    WordScriptableObject[] allWordsInBuildWithSOs;
    Dictionary<string, string> builtInWordLibrary = new Dictionary<string, string>();
    Dictionary<string, WordScriptableObject> inputEngWord_outputSO = new Dictionary<string, WordScriptableObject>();
    Dictionary<Guid, string> inputAnchorGUID_outputAssociatedWord = new Dictionary<Guid, string>();
    Dictionary<Guid, bool> inputAnchorGUID_outputWhetherModelIsVisible = new Dictionary<Guid, bool>();
    Dictionary<Guid, (Vector3, Quaternion, Vector3)> inputAnchorGUID_outputModelsLastPose = new Dictionary<Guid, (Vector3, Quaternion, Vector3)>();
    Dictionary<Guid, WordPrefab> inputAnchorGUID_outputWordPrefab = new Dictionary<Guid, WordPrefab>();
    Dictionary<Guid, GameObject> inputAnchorGUID_outputRuntimeModelInstance = new Dictionary<Guid, GameObject>();
    Dictionary<string, List<Guid>> inputRoom_outputListOfSavedAnchors = new Dictionary<string, List<Guid>>();
    Dictionary<string, float[]> inputWord_outputUserRecording = new Dictionary<string, float[]>();
    bool scenePermissionWasGranted = false;
    Action<OVRSpatialAnchor.UnboundAnchor, bool> onLoadAnchor;
    string savedAnchorsFilePath;
    string anchorWordAssociationFilePath;
    string modelVisibilityFilePath;
    string modelPoseFilePath;
    string roomAnchorsFilePath;
    string userRecordingsFilePath;
    string isFirstTimeFilePath;
    [SerializeField] List<Guid> allAnchorsInSessionGuids = new List<Guid>();
    List<OVRSpatialAnchor> allAnchorsInSession = new List<OVRSpatialAnchor>();
    bool hasPinched = false;

    [FoldoutGroup("Prefabs")]
    public GameObject flashcardPrefabWithSpatialAnchor; // Child this to the icon prefab? 
    [FoldoutGroup("Prefabs")]
    public GameObject flashcardPrefabWithNOSpatialAnchor;
    //[FoldoutGroup("Prefabs")]
    //public GameObject flashcardPrefab;

    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject bedroomSO;
    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject livingRoomSO;
    [FoldoutGroup("Rooms")]
    public RoomWordGameScriptableObject kitchenSO;

    [FoldoutGroup("General References")]
    public Nova.TextBlock mainText;
    [FoldoutGroup("General References")]
    public Nova.UIBlock3D mainTextPanel;
    [FoldoutGroup("General References")]
    public OVRHand rightHand;
    [FoldoutGroup("General References")]
    public OVRHand leftHand;
    [FoldoutGroup("General References")]
    public Transform head;
    [FoldoutGroup("General References")]
    public Transform rightHandFingertip;
    [FoldoutGroup("General References")]
    public Transform leftHandFingertip;
    //[FoldoutGroup("General References")]
    //public TextAsset translationFile;
    [FoldoutGroup("General References")]
    public AppDictationExperience dictationExperience;
    [FoldoutGroup("General References")]
    public OVRPassthroughLayer passthroughLayer1;
    [FoldoutGroup("General References")]
    public OVRPassthroughLayer passthroughLayer2;
    bool isLayer1Active = false;

    // bool isCreatingFlashcard = false;
    GameObject prefabCurrentlyCreating;
    GameObject currentlyDraggedObject;
    WordPrefab currentlyDraggedParentPrefab;
    string currentRoom;
    string microphoneDeviceName;
    float pinchTimer = 0;
    //AudioClip currentlyRecordingClip;

    //public string currentVoiceInput;
    public Nova.TextBlock testTextBlock;
    public Vector3 panelFollowOffset;

    public enum UserInteractionMode
    {
        AnchorCreation, Practice
    }
    public enum AnchorVisibilityMode
    {
        AllCollapsed, AllExpanded, IconsGazeActivated, IconsPointActivated, IconsProximityActivated, IconsAndModelsOnly 
    }
    /*{
        Bedroom1, Bedroom2, Bedroom3, GuestRoom, LivingRoom1, LivingRoom2, LivingRoom3, Kitchen, Bathroom1, Bathroom2, Bathroom3, Garage, Basement, Attic, Closet, Pantry
    }*/
    [FoldoutGroup("State")]
    public UserInteractionMode userInteractionMode;
    [FoldoutGroup("State")]
    public AnchorVisibilityMode visibilityMode;

    class PracticeModeData
    {
        public List<Guid> allUUIDsWeStartedWithThisPractice;
        public List<Guid> allUUIDsDrawingFromThisPractice;
    }
    PracticeModeData currentPractice;
    #endregion

    [Button]
    public void SetCurrentRoomIfNotExisting(string room)
    {
        currentRoom = room;
        if (!inputRoom_outputListOfSavedAnchors.ContainsKey(room))
        {
            List<Guid> roomList = new List<Guid>();
            inputRoom_outputListOfSavedAnchors.Add(room, roomList);
        }
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        onLoadAnchor = OnLocalized;
        microphoneDeviceName = "Headset Microphone(Oculus Virtual Audio Device)"; // Test that this is right 
    }
    private void Start()
    {
        StartCoroutine(StartupProtocol());
    }
    private void Update()
    {
        if(userInteractionMode == UserInteractionMode.AnchorCreation) // If they're in creation mode, listen for pinch to create new flashcard/anchor. 
        {
            CheckPinch_CreationMode(rightHand);
            // Some particle effect on finger
        }
        if(currentlyDraggedObject)
        {
            HandleDragging(currentlyDraggedParentPrefab);
        }
        /*if(currentRoom == null)
        {
            // Pop-up UI forcing user to input a current room asap otherwise bugs
            // (Need a flag that startup is done before checking this every frame)
        }*/

        KeepPanelInFrontOfFace(mainTextPanel.gameObject, 1, panelFollowOffset);

        //HandleDepthAPI();

        foreach (Guid item in allAnchorsInSessionGuids)
        {
            mainText.Text = item.ToString();
        }
    }
    IEnumerator StartupProtocol()
    {
        yield return StartCoroutine(StartupInitialization());

        //yield return StartCoroutine(StartupRoutine());
    }

    IEnumerator StartupRoutine()
    {
        if (isFirstTimeStarting)
        {
            mainText.Text = "Hey There :)";

            // Show them 3 options but only have Randomize available. Highlight randomize and say pls complete first
            // 



            isFirstTimeStarting = false;
            yield return StartCoroutine(SaveIsFirstTimeStarting());
        }
        else
        {
            // Welcome back
            mainText.Text = "Welcome Back :)";

            RoomRequest();

        }
    }

    #region INITIALIZATION FUNCTIONS

    IEnumerator StartupInitialization()
    {
        yield return StartCoroutine(EnsureFilePathExist());
        allAnchorsInSessionGuids = LoadSavedAnchorsUUIDs();
        inputAnchorGUID_outputAssociatedWord = LoadAnchorWordAssociationDict();
        inputAnchorGUID_outputWhetherModelIsVisible = LoadModelVisibilityDict();
        inputAnchorGUID_outputModelsLastPose = LoadModelPoseDict();
        inputRoom_outputListOfSavedAnchors = LoadRoomAnchorsDict();
        inputWord_outputUserRecording = LoadUserRecordingsDict();
        isFirstTimeStarting = LoadIsFirstTimeStarting();

        InitializeRuntimeWordLookupDict();

        yield return StartCoroutine(HandleMixedRealityStartup());

        while (!scenePermissionWasGranted)
        {
            yield return null;
        }

        yield return StartCoroutine(LoadSpatialAnchors());

        yield break;
    }
    
    void InitializeRuntimeWordLookupDict()
    {
        // Do CSV table thing load into memory Dictionary
        byte[] rawBytes = Resources.Load<TextAsset>("es_en").bytes;
        builtInWordLibrary = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, string>>(rawBytes, DataFormat.Binary);

        allWordsInBuildWithSOs = Resources.LoadAll<WordScriptableObject>("WordAssets");
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

    IEnumerator HandleMixedRealityStartup()
    {
        // First thing is try to load the scene model and have user set one up 

        yield return StartCoroutine(LoadSceneModelOnApplicationStart());

        yield break;
    }

    #region SCENE API 

    IEnumerator LoadSceneModelOnApplicationStart()
    {
        var sceneManager = FindObjectOfType<OVRSceneManager>();

        sceneManager.SceneModelLoadedSuccessfully += SceneModelLoadedSuccessfuly;
        sceneManager.NoSceneModelToLoad += NoSceneModelToLoad;
        sceneManager.NewSceneModelAvailable += UserPerformedNewRoomSetupWhileApplicationPaused;
        sceneManager.LoadSceneModelFailedPermissionNotGranted += UserDeniedPermissionSoLoadSceneFailed;

        sceneManager.LoadSceneModel();

        yield break;
    }
    void SceneModelLoadedSuccessfuly()
    {
        scenePermissionWasGranted = true;
        if (scenePermissionWasGranted) InitializeDepthAPI();  // If users accepts scene model, also ask them to allow Depth API
        //LoadAllAnchors();
    }
    void NoSceneModelToLoad() // User hasn't set up their Space yet
    {
        // Determine whether it was because of lack of permission or because user hasn't done Space Setup yet. 
        var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.SceneCaptureReturnedWithoutError += SuccessfullSceneCapture;
        sceneManager.UnexpectedErrorWithSceneCapture += RestartSceneCaptureBecasueOfError;

        sceneManager.RequestSceneCapture();
    }
    void UserDeniedPermissionSoLoadSceneFailed()
    {
        var sceneManager = FindObjectOfType<OVRSceneManager>();
        // Ask them to reconsider. If they say no, close the app I guesss. 

    }

    void UserPerformedNewRoomSetupWhileApplicationPaused()
    {
        var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.LoadSceneModel();
    }
    void RestartSceneCaptureBecasueOfError()
    {
        var sceneManager = FindObjectOfType<OVRSceneManager>();
        sceneManager.RequestSceneCapture();
    }
    void SuccessfullSceneCapture()
    {
        Debug.Log("Scene capture Successful");

        scenePermissionWasGranted = true;

        var sceneManager = FindObjectOfType<OVRSceneManager>();
        if (scenePermissionWasGranted) InitializeDepthAPI();  // If users accepts scene model, also ask them to allow Depth API
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
    IEnumerator SaveIsFirstTimeStarting()
    {
        byte[] storageBytes = Sirenix.Serialization.SerializationUtility.SerializeValue<bool>(isFirstTimeStarting, DataFormat.Binary);
        File.WriteAllBytes(isFirstTimeFilePath, storageBytes);
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
        Dictionary <string, float[]> fromFolder = Sirenix.Serialization.SerializationUtility.DeserializeValue<Dictionary<string, float[]>>(rawBytes, DataFormat.Binary);
        if (fromFolder != null && fromFolder.Count > 0) { return fromFolder; }
        else { return new Dictionary<string, float[]>(); }
    }
    bool LoadIsFirstTimeStarting()
    {
        byte[] rawBytes = File.ReadAllBytes(isFirstTimeFilePath);
        return Sirenix.Serialization.SerializationUtility.DeserializeValue<bool>(rawBytes, DataFormat.Binary);        
    }
    #endregion


    #region LOADING 
    IEnumerator LoadSpatialAnchors()
    {
        OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions();
        options.StorageLocation = OVRSpace.StorageLocation.Local;
        options.Uuids = allAnchorsInSessionGuids;

        if(allAnchorsInSessionGuids != null && allAnchorsInSessionGuids.Count > 0) // Mixed reality wont load properly without this. 
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
        if (!success) return;

        var pose = unboundAnchor.Pose;
        // INSTANTIATE A PREFAB
        GameObject spatialAnchorGO = Instantiate(flashcardPrefabWithSpatialAnchor, pose.position, pose.rotation);
        OVRSpatialAnchor prefabAnchor = spatialAnchorGO.GetComponent<OVRSpatialAnchor>();
        unboundAnchor.BindTo(prefabAnchor);
        allAnchorsInSession.Add(prefabAnchor); // add the spatial anchor i either addcomponent or it's already on a prefab i instantiate
        inputAnchorGUID_outputWordPrefab.Add(unboundAnchor.Uuid, spatialAnchorGO.GetComponent<WordPrefab>());

        LoadAnchorsWordPrefabs(spatialAnchorGO, unboundAnchor.Uuid, pose);
    }

    void LoadAnchorsWordPrefabs(GameObject loadedAnchorGO, Guid anchorID, Pose anchorPose)
    {
        string associatedWord = inputAnchorGUID_outputAssociatedWord[anchorID];
        WordPrefab wordPrefab = loadedAnchorGO.GetComponent<WordPrefab>();
        wordPrefab.associatedWord = associatedWord;
        bool hasASO = false;
        if (inputEngWord_outputSO.TryGetValue(associatedWord, out WordScriptableObject wordSO)) { hasASO = true;  }
        

        // If a model was active last session, spawn it. 
        
        switch (visibilityMode)
        {
            case AnchorVisibilityMode.AllCollapsed:
                // Spawn NO visuals
                break;
            case AnchorVisibilityMode.AllExpanded:
                // Spawn all flashcards and models. 
                wordPrefab.postIt.SetActive(true);
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true && hasASO) // If the user has activated 3d model for this anchor and we have pose info 
                {
                    SpawnModel(anchorID, anchorPose, wordSO);
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
                    SpawnModel(anchorID, anchorPose, wordSO);
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
        isFirstTimeFilePath = Application.persistentDataPath + "/" + "startupData" + ".bin";
        if (File.Exists(isFirstTimeFilePath) == false)
        {
            File.Create(isFirstTimeFilePath).Close();
        }
        yield break;
    }
    #endregion

    #endregion

    void RoomRequest()
    {
        // Show UI with different room options
        // set currentRoom = a string with whatever they chose.. 
    }

    #region USER MENU ACTIONS
    void OnAnchorVisibilityTypeChange(bool changeAllInScene) // If user changes visibility type, collapse/expand etc everything according to below modes. 
    {
        List<WordPrefab> allWordPrefabsInScene = new List<WordPrefab>();
        if (changeAllInScene) { allWordPrefabsInScene = GetAllRuntimeWordPrefabs(); }
        else { allWordPrefabsInScene.Add(prefabCurrentlyCreating.GetComponent<WordPrefab>()); }

        switch (visibilityMode)
        {
            case AnchorVisibilityMode.AllCollapsed:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(false);
                    wordPrefab.icon.SetActive(false);
                    ExpandOrCollapseModelIfVisible(false, wordPrefab.gameObject.GetComponent<OVRSpatialAnchor>());
                }
                break;
            case AnchorVisibilityMode.AllExpanded:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(true);
                    wordPrefab.icon.SetActive(false);
                    ExpandOrCollapseModelIfVisible(true, wordPrefab.gameObject.GetComponent<OVRSpatialAnchor>());
                }
                break;
            case AnchorVisibilityMode.IconsGazeActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsPointActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsProximityActivated:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(false);
                    wordPrefab.icon.SetActive(true);
                }
                break;
            case AnchorVisibilityMode.IconsAndModelsOnly:
                foreach (WordPrefab wordPrefab in allWordPrefabsInScene)
                {
                    wordPrefab.postIt.SetActive(false);
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
            wordPrefab.postIt.SetActive(true);
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
    void OnGrab(GameObject draggedObj)
    {
        currentlyDraggedObject = draggedObj;
        currentlyDraggedParentPrefab = draggedObj.GetComponentInParent<WordPrefab>();
    }
    void OnRelease()
    {
        if(currentlyDraggedObject && currentlyDraggedParentPrefab)
        {
            UpdateSpatialAnchorInfoWhenUserMovesAndReleasesPrefab(currentlyDraggedParentPrefab.gameObject, currentlyDraggedObject.transform.position, currentlyDraggedObject);
            currentlyDraggedObject = null;
            currentlyDraggedParentPrefab = null;
        }
        else
        {
            currentlyDraggedObject = null;
            currentlyDraggedParentPrefab = null;
        }
    }
    #endregion

    #region DRAGGING
    void HandleDragging(WordPrefab currentlyDraggedParentPrefab)
    {
        if (currentlyDraggedObject.CompareTag("PostIt"))
        {
            MoveModelIfMovingFlashcard(currentlyDraggedParentPrefab, .5f);
        }
        else if (currentlyDraggedObject.CompareTag("Model"))
        {
            MoveFlashcardIfMovingModel(currentlyDraggedParentPrefab, .5f);
        }
        else
        {
            currentlyDraggedParentPrefab = null;
            currentlyDraggedObject = null;
        }
    }
    void MoveFlashcardIfMovingModel(WordPrefab prefabMoving, float yOffset)
    {
        prefabMoving.postIt.transform.position = new Vector3(prefabMoving.model.transform.position.x, yOffset, prefabMoving.model.transform.position.z);
        prefabMoving.icon.transform.position = new Vector3(prefabMoving.model.transform.position.x, yOffset, prefabMoving.model.transform.position.z);
    }
    void MoveModelIfMovingFlashcard(WordPrefab prefabMoving, float yOffset)
    {
        prefabMoving.model.transform.position = new Vector3(prefabMoving.postIt.transform.position.x, yOffset, prefabMoving.postIt.transform.position.z);
        prefabMoving.icon.transform.position = new Vector3(prefabMoving.postIt.transform.position.x, yOffset, prefabMoving.postIt.transform.position.z);
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
            pinchTimer += Time.deltaTime;
            //Debug.Log(pinchTimer);

        }
        
        if (!hasPinched && isIndexFingerPinching && confidence == OVRHand.TrackingConfidence.High && pinchTimer > 1)
        {
            hasPinched = true;
            PinchEvent(rightHandFingertip.position);
        }
        else if (hasPinched && !isIndexFingerPinching)
        {
            hasPinched = false;
            pinchTimer = 0;
            UnPinchEvent();
        }
        else if(!hasPinched && !isIndexFingerPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            pinchTimer = 0;
        }
    }
    void PinchEvent(Vector3 pinchLocation)
    {
        // animate it
        prefabCurrentlyCreating = PreliminaryFlashcardCreation(pinchLocation);

        StartCoroutine(FlashCardCreationConfirmation(prefabCurrentlyCreating.GetComponent<WordPrefab>(), "Poop", "Pee"));
        // Load up Word Search 
    }
    GameObject PreliminaryFlashcardCreation(Vector3 inputLocation)
    {
        GameObject prefab = Instantiate(flashcardPrefabWithNOSpatialAnchor, inputLocation, Quaternion.identity);
        prefab.GetComponent<WordPrefab>().postIt.SetActive(true);
        return prefab;
        // Create the flashcard prefab stuff for word selection
        // Ask user to confirm, once they confirm the word, run the Confirm method below. 
        // Set the Voice search and keyboard buttons, Voice button only enabled if user had allowed permissions. 
    }
    void UnPinchEvent()
    {

    }
    void OnActivateVoiceSearchButtonPressed()
    {
        // Start the voice search as long as the button is pressed, once button depressed call OnStopListening method 
        dictationExperience.Activate();
    }
    void OnActivateVoiceSearchButtonUnPressed()
    {
        dictationExperience.Deactivate();
        // Make sure button visuals indicate unpressed (if button prefab doesnt do it automatically)
    }
    public void MicHasStopedListening(string voiceTextInputted) // Will get here if user either depresses the button or stops talking for 2 seconds. 
    {
        string editedText = Regex.Match(voiceTextInputted, @"^([\w\-]+)").ToString(); // keep only the first word in the dictation
        testTextBlock.Text = editedText;

        WordSearched(prefabCurrentlyCreating.GetComponent<WordPrefab>(), editedText);

        //currentVoiceInput = null;
    }
    IEnumerator FlashCardCreationConfirmation(WordPrefab currentlyInteractingWordPrefab, string chosenEngWord, string translatedWord) // Happens after word searched for 
    {
        yield return StartCoroutine(ConfirmFlashcardPlacementAndCreateSpatialAnchor(prefabCurrentlyCreating, chosenEngWord)); 

        // On confirmation, Display the flashcard info
        FillFlashcard(currentlyInteractingWordPrefab, chosenEngWord, translatedWord);
    }
    void FlashcardDeletion()
    {
        RequestFlashcardDeletion(prefabCurrentlyCreating);
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
            }
            if (wordSO.audio_en != null && wordSO.audio_en.Count > 0)
            {
                // audio button is active
            }
            if (wordSO.audio_es != null && wordSO.audio_es.Count > 0)
            {
                // audio button is active
            }

            StartCoroutine(FlashCardCreationConfirmation(currentlyInteractingWordPrefab, wordSO.word_en.ToLower(), wordSO.word_es));
        }
        // Fallback to the generic key/value list then. 
        else if (builtInWordLibrary.TryGetValue(textSearchedLowerCase, out string translatedWord))
        {
            // Fill the flashcard with both language text, grey out voice/3d model buttons 

            StartCoroutine(FlashCardCreationConfirmation(currentlyInteractingWordPrefab, textSearchedLowerCase, translatedWord));
        }
        else
        {
            Debug.Log("Word Not Found. Try Again");
        }
    }
    #endregion

    #region PRACTICE MODE

    void LaunchPracticeMode()
    {
        currentPractice = new PracticeModeData();
        currentPractice.allUUIDsDrawingFromThisPractice = new List<Guid>();
        currentPractice.allUUIDsWeStartedWithThisPractice = new List<Guid>();
        currentPractice.allUUIDsWeStartedWithThisPractice = inputRoom_outputListOfSavedAnchors[currentRoom];
        foreach (Guid id in inputRoom_outputListOfSavedAnchors[currentRoom])
        {
            currentPractice.allUUIDsDrawingFromThisPractice.Add(id);
        }

        ReadyForNewPracticeWord(currentPractice);
    }

    void EndPracticeMode()
    {
        currentPractice = null; 
        // Reset the visuals
    }

    void ReadyForNewPracticeWord(PracticeModeData practice)
    {
        if (practice.allUUIDsDrawingFromThisPractice.Count == 0)
        {
            // Tell the user its over
            return;
        }

        Guid nextWordID = GetRandomWordFromRoomList(practice);
        WordPrefab wordPrefab = inputAnchorGUID_outputWordPrefab[nextWordID];
        // Display guessing UI 

        // Highlight some items around the room somehow
        List<WordPrefab> otherItemsToHighlight = FindSomeMultipleChoiceWordsExcludingCorrectOne(nextWordID, 3, practice.allUUIDsWeStartedWithThisPractice);
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
    #endregion

    #region 3D MODEL LOADING/DELETING

    void SpawnModel(Guid anchorID, Pose anchorPose, WordScriptableObject associatedWordSO)
    {
        inputAnchorGUID_outputWhetherModelIsVisible[anchorID] = true;

        (Vector3, Quaternion, Vector3) pose;
        if (inputAnchorGUID_outputModelsLastPose.ContainsKey(anchorID))
        {
            pose = inputAnchorGUID_outputModelsLastPose[anchorID];
        }
        else
        {
            pose = (anchorPose.position, anchorPose.rotation, associatedWordSO.defaultModelScale);
            inputAnchorGUID_outputModelsLastPose.Add(anchorID, pose);
        }

        GameObject model = Instantiate(associatedWordSO.model, pose.Item1, pose.Item2);
        model.transform.localScale = pose.Item3;

        inputAnchorGUID_outputRuntimeModelInstance.Add(anchorID, model);

        StartCoroutine(SaveEverything());
    }
    void DeleteWordModel(Guid anchorID)
    {
        Destroy(inputAnchorGUID_outputRuntimeModelInstance[anchorID]); // Destroy any model

        inputAnchorGUID_outputWhetherModelIsVisible[anchorID] = false;
        inputAnchorGUID_outputModelsLastPose.Remove(anchorID);
        inputAnchorGUID_outputRuntimeModelInstance.Remove(anchorID);

        StartCoroutine(SaveEverything());

        // Leaves only the icon/flashcard behind. 
    }
    #endregion

    #region CREATE/DELETE SPATIAL ANCHOR
    

    IEnumerator ConfirmFlashcardPlacementAndCreateSpatialAnchor(GameObject prefabToCreateAnchorOn, string chosenWord) // Saving Anchors created at runtime 
    {
        OVRSpatialAnchor anchorToSave = prefabToCreateAnchorOn.AddComponent<OVRSpatialAnchor>();

        while (!anchorToSave.Created && !anchorToSave.Localized) // keep checking for a valid and localized spatial anchor state
        {
            yield return new WaitForEndOfFrame();
        }

        OVRSpatialAnchor.SaveOptions options = new OVRSpatialAnchor.SaveOptions();
        options.Storage = OVRSpace.StorageLocation.Local;


        /*if (anchorToSave.SaveAsync(options).IsCompleted)
        {
            // If save is successfull, save to all our lists. 
            WordPrefab newWordPrefab = prefabToCreateAnchorOn.GetComponent<WordPrefab>();
            newWordPrefab.associatedWord = chosenWord;
            allAnchorsInSession.Add(anchorToSave);
            allAnchorsInSessionGuids.Add(anchorToSave.Uuid);
            inputAnchorGUID_outputAssociatedWord.Add(anchorToSave.Uuid, chosenWord);
            inputAnchorGUID_outputWordPrefab.Add(anchorToSave.Uuid, newWordPrefab);
            inputRoom_outputListOfSavedAnchors[currentRoom].Add(anchorToSave.Uuid);

            prefabCurrentlyCreating = null;
            Debug.Log("SAVE SUCCESS");
            // Save our app's lists to device
            StartCoroutine(SaveEverything());
        }*/

        anchorToSave.Save((anchorToSave, success) =>
        {
            if (!success) { Debug.Log("SAVE FAIL"); return; }

            // If save is successfull, save to all our lists. 
            WordPrefab newWordPrefab = prefabToCreateAnchorOn.GetComponent<WordPrefab>();
            newWordPrefab.associatedWord = chosenWord;
            allAnchorsInSession.Add(anchorToSave);
            allAnchorsInSessionGuids.Add(anchorToSave.Uuid);
            inputAnchorGUID_outputAssociatedWord.Add(anchorToSave.Uuid, chosenWord);
            inputAnchorGUID_outputWordPrefab.Add(anchorToSave.Uuid, newWordPrefab);
            inputRoom_outputListOfSavedAnchors[currentRoom].Add(anchorToSave.Uuid);

            prefabCurrentlyCreating = null;
            Debug.Log("SAVE SUCCESS");
            // Save our app's lists to device
            StartCoroutine(SaveEverything());
        });
    }

    void RequestFlashcardDeletion(GameObject prefabToDelete)
    {
        OVRSpatialAnchor anchorToDelete = prefabToDelete.GetComponent<OVRSpatialAnchor>();

        if (!anchorToDelete)  // If the flashcard doesn't have an anchor yet, meaning it was never confirmed, just delete the visausl and such 
        {
            Destroy(prefabToDelete);
            return;
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
            }
        });
    }
    void UpdateSpatialAnchorInfoWhenUserMovesAndReleasesPrefab(GameObject parentPrefabJustMoved, Vector3 positionJustMovedTo, GameObject prefabJustMoved)
    {
        // PER META DOCS - "Anchors cannot be moved. If the content must be moved, delete the old anchor and create a new one."
        parentPrefabJustMoved.transform.position = prefabJustMoved.transform.position; // worldspace. test this works as expected. 
        WordPrefab wordPrefab = parentPrefabJustMoved.GetComponent<WordPrefab>();
        OVRSpatialAnchor oldAnchor = parentPrefabJustMoved.GetComponent<OVRSpatialAnchor>();
        Guid oldAnchorsUUID = oldAnchor.Uuid;
        string associatedWord = inputAnchorGUID_outputAssociatedWord[oldAnchorsUUID];
        OVRSpatialAnchor newAnchor = parentPrefabJustMoved.AddComponent<OVRSpatialAnchor>();

        allAnchorsInSession.Remove(oldAnchor);
        allAnchorsInSessionGuids.Remove(oldAnchor.Uuid);
        inputAnchorGUID_outputAssociatedWord.Remove(oldAnchor.Uuid);
        inputAnchorGUID_outputWordPrefab.Remove(oldAnchor.Uuid);
        inputRoom_outputListOfSavedAnchors[currentRoom].Remove(oldAnchor.Uuid);

        if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputWhetherModelIsVisible.Add(newAnchor.Uuid, inputAnchorGUID_outputWhetherModelIsVisible[oldAnchorsUUID]); inputAnchorGUID_outputWhetherModelIsVisible.Remove(oldAnchorsUUID); }
        if (inputAnchorGUID_outputRuntimeModelInstance.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputRuntimeModelInstance.Add(newAnchor.Uuid, inputAnchorGUID_outputRuntimeModelInstance[oldAnchorsUUID]); inputAnchorGUID_outputRuntimeModelInstance.Remove(oldAnchorsUUID); }
        if (inputAnchorGUID_outputModelsLastPose.ContainsKey(oldAnchorsUUID)) { inputAnchorGUID_outputModelsLastPose.Add(newAnchor.Uuid, (wordPrefab.model.transform.position, wordPrefab.model.transform.rotation, wordPrefab.model.transform.localScale)); inputAnchorGUID_outputModelsLastPose.Remove(oldAnchorsUUID); }     
        
        Destroy(oldAnchor);

        allAnchorsInSession.Add(newAnchor);
        allAnchorsInSessionGuids.Add(newAnchor.Uuid);
        inputAnchorGUID_outputAssociatedWord.Add(newAnchor.Uuid, associatedWord);
        inputAnchorGUID_outputWordPrefab.Add(newAnchor.Uuid, parentPrefabJustMoved.GetComponent<WordPrefab>());
        inputRoom_outputListOfSavedAnchors[currentRoom].Add(newAnchor.Uuid);

        // Save our app's lists to device
        StartCoroutine(SaveEverything());
    }

    #endregion

    #region FILL FLASHCARDS

    void FillFlashcard(WordPrefab flashcardPrefab, string chosenEngWord, string translatedWord)
    {
        // FOrce first letter upper case
        string englishToDisplay = char.ToUpper(chosenEngWord[0]) + chosenEngWord.Substring(1);
        string translationToDisplay = char.ToUpper(translatedWord[0]) + translatedWord.Substring(1);

        flashcardPrefab.englishText.Text = englishToDisplay;
        flashcardPrefab.translatedText.Text = translationToDisplay;

        if(flashcardPrefab.associatedWordSO != null)
        {
            if (EnsureUserAudioClipExists(flashcardPrefab.associatedWordSO, out AudioClip audio))
            {
                // Make the user audio playback button active
            }
            if (flashcardPrefab.associatedWordSO.audio_es != null)
            {
                // Make es audio playbck button active
            }
            if (flashcardPrefab.associatedWordSO.audio_en != null)
            {
                // Make en audio playbck button active (not sure we even will do this)
            }
        }
        
    }

    #endregion

    #region AUDIO PLAYBACK
    void PlayPreloadedWordAudio(WordScriptableObject word, List<AudioClip> audioList)
    {
        if (audioList == word.audio_en)
        {
            word.audio_en_incrementor++;
            if (word.audio_en_incrementor > (audioList.Count - 1)) { word.audio_en_incrementor = 0; }
            source.PlayOneShot(audioList[word.audio_en_incrementor]);
        }
        else if (audioList == word.audio_es)
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
    void PlayUserAudio(WordScriptableObject word)
    {
        if(EnsureUserAudioClipExists(word, out AudioClip clipToPlay))
        {
            source.PlayOneShot(clipToPlay);
        }
    }

    bool EnsureUserAudioClipExists(WordScriptableObject word, out AudioClip clipToReturn)
    {
        if (word.userAudio_es != null)
        {
            clipToReturn = word.userAudio_es;
            return true;
        }
        else if (inputWord_outputUserRecording.TryGetValue(word.word_en.ToLower(), out float[] existingAudio))
        {
            AudioClip newClip = AudioClip.Create(word.word_es, existingAudio.Length, 2, 44100, false);
            newClip.SetData(existingAudio, 0);
            word.userAudio_es = newClip;
            clipToReturn = word.userAudio_es;
            return true;
        }
        else
        {
            clipToReturn = null;
            return false;
        }
    }
    #endregion

    #region VOICE RECORDING

    [Button]
    void MicTest()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }
    }
    IEnumerator RecordSpokenWord_Start(WordPrefab word)
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
        yield return StartCoroutine(SaveUserRecordingsDict());

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

    #region HELPERS

    void KeepPanelInFrontOfFace(GameObject panel, float smoothAmount, Vector3 offset)
    {
        Vector3 targetPos = head.TransformPoint(offset);
        Quaternion targetRot = Quaternion.Euler(new Vector3(0, head.eulerAngles.y, 0));

        panel.transform.position = Vector3.Lerp(panel.transform.position, targetPos, Time.deltaTime * smoothAmount);
        panel.transform.rotation = Quaternion.Slerp(panel.transform.rotation, targetRot, Time.deltaTime * smoothAmount);
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
    #endregion
}
