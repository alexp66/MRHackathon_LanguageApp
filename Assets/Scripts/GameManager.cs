using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using Unity.XR.Oculus;
using Sirenix.OdinInspector;
using Sirenix.Serialization;


public class GameManager : MonoBehaviour
{
    AudioSource source;
    SpokenWordScriptableObject[] allWordsInBuild;
    Dictionary<string, SpokenWordScriptableObject> inputEngWord_outputSO = new Dictionary<string, SpokenWordScriptableObject>();
    Dictionary<Guid, string> inputAnchorGUID_outputAssociatedWord = new Dictionary<Guid, string>();
    Dictionary<Guid, bool> inputAnchorGUID_outputWhetherModelIsVisible = new Dictionary<Guid, bool>();
    Dictionary<Guid, (Vector3, Quaternion, Vector3)> inputAnchorGUID_outputModelsLastPose = new Dictionary<Guid, (Vector3, Quaternion, Vector3)>();
    Dictionary<Guid, WordPrefab> inputAnchorGUID_outputRuntimeInstance = new Dictionary<Guid, WordPrefab>();
    Dictionary<Guid, GameObject> inputAnchorGUID_outputRuntimeModelInstance = new Dictionary<Guid, GameObject>();
    bool scenePermissionWasGranted = false;
    Action<OVRSpatialAnchor.UnboundAnchor, bool> onLoadAnchor;
    string savedAnchorsFilePath;
    string anchorWordAssociationFilePath;
    string modelVisibilityFilePath;
    string modelPoseFilePath;
    List<Guid> allAnchorsInSessionGuids = new List<Guid>();
    List<OVRSpatialAnchor> allAnchorsInSession = new List<OVRSpatialAnchor>();

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
    public Transform rightController;
    [FoldoutGroup("General References")]
    public Transform leftController;
   // bool isCreatingFlashcard = false;
    GameObject prefabCurrentlyInteractingWith;



    public enum UserInteractionMode
    {
        AnchorCreation, 
    }
    public enum AnchorVisibilityMode
    {
        AllCollapsed, AllExpanded, IconsGazeActivated, IconsPointActivated, IconsProximityActivated, IconsAndModelsOnly 
    }
    [FoldoutGroup("State")]
    public UserInteractionMode userInteractionMode;
    [FoldoutGroup("State")]
    public AnchorVisibilityMode visibilityMode;


    private void Awake()
    {
        source = GetComponent<AudioSource>();
        onLoadAnchor = OnLocalized;
    }
    private void Start()
    {
        StartCoroutine(StartupProtocol());
    }
    private void Update()
    {

        //HandleDepthAPI();
    }


    #region STARTUP FUNCTIONS
    IEnumerator StartupProtocol()
    {
        yield return StartCoroutine(EnsureFilePathExist());
        allAnchorsInSessionGuids = LoadSavedAnchorsUUIDs();
        inputAnchorGUID_outputAssociatedWord = LoadAnchorWordAssociationDict();
        inputAnchorGUID_outputWhetherModelIsVisible = LoadModelVisibilityDict();
        inputAnchorGUID_outputModelsLastPose = LoadModelPoseDict();

        InitializeRuntimeWordLookupDict();

        yield return StartCoroutine(HandleMixedRealityStartup());

        while (!scenePermissionWasGranted)
        {
            yield return null;
        }

        //yield return StartCoroutine(LoadSpatialAnchors());

        yield break;
    }
    
    void InitializeRuntimeWordLookupDict()
    {
        // This just creates a quick runtime lookup dict for words' assets (flashcards, audio, models etc)
        allWordsInBuild = Resources.LoadAll<SpokenWordScriptableObject>("WordAssets");
        foreach (SpokenWordScriptableObject wordSO in allWordsInBuild) // Fill runtime dictionary with SO's via refrence to the word string in english
        {
            inputEngWord_outputSO.Add(wordSO.word_en, wordSO);
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
    /*async void LoadAllAnchors()
    {
        var anchors = new List<OVRAnchor>();
        await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(anchors); // collects all anchors that have component OVRRoomLayout atached

        // no rooms - call Space Setup or check Scene permission
        if (anchors.Count == 0)
            return;

        foreach (OVRAnchor room in anchors) // for each separate room layout
        {
            var roomAnchors = new List<OVRAnchor>();
            if (!room.TryGetComponent(out OVRAnchorContainer container)) // if the room has no anchors in it (unlikely)
                return;
            await container.FetchChildrenAsync(roomAnchors); // fill roomAnchors list with all child OVRAnchors of that room 
            // BUT HOW TO EXCLUDE SCENE ANCHORS IF I JUST WANT SPATIAL ANCHORS. 

            foreach (OVRAnchor anchorInRoom in roomAnchors)
            {
                // get semantic classification for object name. 
                var label = "other";
                if (anchorInRoom.TryGetComponent(out OVRSemanticLabels labels))
                    label = labels.Labels;
                // BASED ON LABEL, SPAWN RELEVANT 3D OBJ IF THERE IS ONE. BUT HAS TO BE A SPATIAL ANCHOR NOT A SCENE ONE!!
                // OK IT SEEMS TO BE READONLY SO MIGHT HAVE TO ASSIGN ANCHOR A SEMANTIC STRING PAIRED WITH ITS UUID ON CREATION TO LOOK IT UP
                // Load3DModelAtAnchorPosByComparingUUIDInDictionaryOfStrings();
            }
        }  
    }*/
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
    #endregion


    #region LOADING 
    IEnumerator LoadSpatialAnchors()
    {
        OVRSpatialAnchor.LoadOptions options = new OVRSpatialAnchor.LoadOptions();
        options.StorageLocation = OVRSpace.StorageLocation.Local;
        options.Uuids = allAnchorsInSessionGuids;

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
        inputAnchorGUID_outputRuntimeInstance.Add(unboundAnchor.Uuid, spatialAnchorGO.GetComponent<WordPrefab>());

        LoadAnchorsWordPrefabs(spatialAnchorGO, unboundAnchor.Uuid, pose);
    }

    void LoadAnchorsWordPrefabs(GameObject loadedAnchorGO, Guid anchorID, Pose anchorPose)
    {
        SpokenWordScriptableObject associatedWordSO = inputEngWord_outputSO[inputAnchorGUID_outputAssociatedWord[anchorID]];
        WordPrefab wordPrefab = loadedAnchorGO.GetComponent<WordPrefab>();

        // If a model was active last session, spawn it. 
        
        switch (visibilityMode)
        {
            case AnchorVisibilityMode.AllCollapsed:
                // Spawn NO visuals
                break;
            case AnchorVisibilityMode.AllExpanded:
                // Spawn all flashcards and models. 
                wordPrefab.postIt.SetActive(true);
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true) // If the user has activated 3d model for this anchor and we have pose info 
                {
                    SpawnModel(anchorID, anchorPose, associatedWordSO);
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
                if (inputAnchorGUID_outputWhetherModelIsVisible.ContainsKey(anchorID) && inputAnchorGUID_outputWhetherModelIsVisible[anchorID] == true) // If the user has activated 3d model for this anchor and we have pose info 
                {
                    SpawnModel(anchorID, anchorPose, associatedWordSO);
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
            File.Create(savedAnchorsFilePath);
        }

        anchorWordAssociationFilePath = Application.persistentDataPath + "/" + "AnchorWordDictionary" + ".bin";
        if (File.Exists(anchorWordAssociationFilePath) == false)
        {
            File.Create(anchorWordAssociationFilePath);
        }
        modelVisibilityFilePath = Application.persistentDataPath + "/" + "ModelVisibilityDictionary" + ".bin";
        if (File.Exists(modelVisibilityFilePath) == false)
        {
            File.Create(modelVisibilityFilePath);
        }
        modelPoseFilePath = Application.persistentDataPath + "/" + "ModelPoseDictionary" + ".bin";
        if (File.Exists(modelPoseFilePath) == false)
        {
            File.Create(modelPoseFilePath);
        }

        yield break;
    }
    #endregion

    #endregion

    
    #region CREATE/DELETE SPATIAL ANCHOR
    GameObject PreliminaryFlashcardCreation(Vector3 inputLocation)
    {
        //isCreatingFlashcard = true;

        GameObject prefab = Instantiate(flashcardPrefabWithNOSpatialAnchor, inputLocation, Quaternion.identity);
        return prefab;
        // Create the flashcard prefab stuff for word selection
        // Ask user to confirm, once they confirm the word, run the Confirm method below. 
    }

    IEnumerator ConfirmFlashcardPlacementAndCreateSpatialAnchor(GameObject prefabToCreateAnchorOn, string chosenWord) // Saving Anchors created at runtime 
    {
        OVRSpatialAnchor anchorToSave = prefabToCreateAnchorOn.AddComponent<OVRSpatialAnchor>();

        while (!anchorToSave.Created && !anchorToSave.Localized) // keep checking for a valid and localized spatial anchor state
        {
            yield return new WaitForEndOfFrame();
        }

        anchorToSave.Save((anchorToSave, success) =>
        {
            if (!success) { return; }

            // If save is successfull, save to all our lists. 
            allAnchorsInSession.Add(anchorToSave);
            allAnchorsInSessionGuids.Add(anchorToSave.Uuid);
            inputAnchorGUID_outputAssociatedWord.Add(anchorToSave.Uuid, chosenWord);
            inputAnchorGUID_outputRuntimeInstance.Add(anchorToSave.Uuid, prefabToCreateAnchorOn.GetComponent<WordPrefab>());

            //prefabToCreateAnchorOn.GetComponentInChildren<Nova.TextBlock>().Text = chosenWord;

            // Save our app's lists to device
            StartCoroutine(SaveEverything());
        });
    }
    

    void DeleteFlashcard(OVRSpatialAnchor anchorToDelete, Guid anchorID)
    {
        if (!anchorToDelete) return;

        anchorToDelete.Erase((anchor, success) =>
        {
            if (success)
            {
                // Delete the flashcard prefab instance
                Destroy(inputAnchorGUID_outputRuntimeInstance[anchorID]);
                inputAnchorGUID_outputRuntimeInstance.Remove(anchorID);

                if (inputAnchorGUID_outputRuntimeModelInstance.ContainsKey(anchorID)) // If it has a model running, delete that too. 
                {
                    DeleteWordModel(anchorID);
                }

                // Save our app's lists to device
                StartCoroutine(SaveEverything());
            }
        });        
    }
    #endregion

    

    #region 3D MODEL LOADING/DELETING
    void MoveAnchorUponModelMoving()
    {
        // PER META DOCS - "Anchors cannot be moved. If the content must be moved, delete the old anchor and create a new one."
        // Once model is un-grabbed, update the spatial anchor's position along with it. and save it. 
    }

    void SpawnModel(Guid anchorID, Pose anchorPose, SpokenWordScriptableObject associatedWordSO)
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

    #region UI LOGIC
    // USE PALM UP GESTURE THING (IN META SAMPLES SOMEWHERE IN UNITY SCENE - LOADED IT IN) TO BRING UP MENU PANEL. 
    // MAKE ICONS/CARDS FACE USER AT ALL TIMES. BILLBOARD TYPE EFFECT? Nah, kinda unnatural.
    void OnAnchorVisibilityTypeChange() // If user changes visibility type, collapse/expand etc everything according to below modes. 
    {
        List<WordPrefab> allWordPrefabsInScene = GetAllRuntimeWordPrefabs();

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
            default:
                break;
        }
    }
    void OnUserInteractionModeChange()
    {
        switch (userInteractionMode)
        {
            case UserInteractionMode.AnchorCreation:
                // Subscribe to certain events
                // Visual partical effect on thumb/fingers?
                break;
            default:
                break;
        }
    }
    void ExpandOneAnchorsFlashcardAndModel(OVRSpatialAnchor anchor, bool expandFlashcard, bool expandModel)
    {
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
    List<WordPrefab> GetAllRuntimeWordPrefabs()
    {
        List<WordPrefab> toReturn = new List<WordPrefab>();
        foreach (Guid uuid in allAnchorsInSessionGuids)
        {
            if (inputAnchorGUID_outputRuntimeInstance.ContainsKey(uuid))
            {
                toReturn.Add(inputAnchorGUID_outputRuntimeInstance[uuid]);
            }
        }
        return toReturn;
    }
    #endregion

    #region AUDIO PLAYBACK
    void PlayWordAudio(SpokenWordScriptableObject word, List<AudioClip> audioList)
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
        else if (audioList == word.userAudio_en)
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
        }
    }
    #endregion
}
