using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;

public class PrefabSpawner : MonoBehaviour
{
    DatabaseReference reference;
    [Header("DB Item Add")]
    public InputField libraryNameInput;
    public InputField actionNameInput;
    public InputField actionLaymenTextInput;
    private string actionGesturePath;
    public NativeFilePicker.Permission permission;
    [Header("DB Item Update")]
    public InputField libraryNameEditorInput;
    public InputField newLibraryNameEditorInput;
    public InputField actionNameEditorInput;
    public InputField newActionNameEditorInput;
    public InputField actionLaymenTextEditorInput;   
    [Header("DB Item Delete")]
    public InputField libraryNameToDeleteInput;
    public bool isUserAdminOrNot;
    public InputField actionNameToDeleteInput;
    private string libraryName, actionName, actionTextPopUpInput, selectedGesturePath; //For User Input
    private bool libraryObjectType, actionObjectType; //For Object Name Setting
    [Header("Prefabs")]
    public GameObject conversationLibraryFolder;
    public GameObject individualLibrary;
    public GameObject individualAction;
    public GameObject actionAudioPopUp;
    public GameObject actionTextPopUp;
    public GameObject actionGesturePopUp;
    private GameObject currentIndividualLibrary, currentlyOpenPopup; //to keep track of what is open right now to work with it only
    private string currentIndividualLibraryName; //To Track Current Position
    [Header("Input Panels")]
    public GameObject conversationLibraryDetailInputPanel;
    public GameObject individualActionDetailInputPanel;
    public GameObject editorPanel;
    public GameObject deletionPanel;
    [Header("Prefab Spawner/ Holder")]
    public Transform conversationLibraryFolderHolder;
    public Transform individualLibraryFolderHolder;
    public Transform actionPopUpHolder; 
    private GameObject instantiatedFolder, instantiatedLibrary, instantiatedAction, instantiatedActionPopUp; //for spawned items
    private Dictionary<string, GameObject> folderDictionary = new Dictionary<string, GameObject>(); //to keep track of libraries on home page
    private List<GameObject> individualLibraries = new List<GameObject>(); //to store individual libraries
    private Dictionary<string, GameObject> libraryDictionary = new Dictionary<string, GameObject>(); //to map library names to their corresponding instances
    private List<GameObject> individualActions = new List<GameObject>(); //to store individual actions
    private Dictionary<string, GameObject> actionDictionary = new Dictionary<string, GameObject>(); //to map action names to their corresponding libraries
    // Dictionaries to track spawned popups for each action
    private Dictionary<string, GameObject> textPopups = new Dictionary<string, GameObject>();
    private Dictionary<string, string> textPopupLaymenText = new Dictionary<string, string>();    
    private Dictionary<string, GameObject> audioPopups = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> gesturePopups = new Dictionary<string, GameObject>();
    private Dictionary<string, string> gesturePopUpImagePath = new Dictionary<string, string>();

    void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(InitializeGameData());
    }

    IEnumerator InitializeGameData()
    {
        // Fetch data for all libraries
        yield return StartCoroutine(FetchLibraryData());
    }

    IEnumerator FetchLibraryData()
    {
        DatabaseReference librariesRef = reference.Child("Libraries");

        var libraryTask = librariesRef.GetValueAsync();
        yield return new WaitUntil(() => libraryTask.IsCompleted);

        if (libraryTask.Exception != null)
        {
            Debug.LogError("Error fetching library data: " + libraryTask.Exception);
            yield break;
        }

        DataSnapshot librarySnapshot = libraryTask.Result;

        foreach (var libraryChild in librarySnapshot.Children)
        {
            libraryName = libraryChild.Key;

            // Spawn the library UI
            SpawnConversationLibraryFolderPrefab(libraryName);

            // Fetch data for all actions under the current library
            yield return StartCoroutine(FetchActionData(libraryName));
        }
    }

    IEnumerator FetchActionData(string libraryName)
    {
        DatabaseReference actionsRef = reference.Child("Libraries").Child(libraryName).Child("Actions");

        var actionTask = actionsRef.GetValueAsync();
        yield return new WaitUntil(() => actionTask.IsCompleted);

        if (actionTask.Exception != null)
        {
            Debug.LogError("Error fetching action data: " + actionTask.Exception);
            yield break;
        }

        DataSnapshot actionSnapshot = actionTask.Result;

        foreach (var actionChild in actionSnapshot.Children)
        {
            actionName = actionChild.Key;
            string actionText = actionChild.Child("ActionLaymenText").Value.ToString();
            string actionGesturePath = actionChild.Child("ActionGesturePath").Value.ToString();

            // Spawn the action UI
            GameObject currentLibrary = libraryDictionary[libraryName];
            SpawnIndividualAction(actionName, actionText, actionGesturePath, currentLibrary);
        }
    }

    public void SetObjectName(GameObject instantiatedItem, bool library, bool action)
    {
        if (library)
        {
            // Displaying library name
            Text[] textComponents = instantiatedItem.GetComponentsInChildren<Text>();
            
            foreach (Text textComponent in textComponents)
            {
                if (textComponent.transform.parent == instantiatedItem.transform)
                {
                    // Set the library name in the correct Text component
                    textComponent.text = libraryName;
                    break;
                }
            }
        }
        else if (action)
        {
            Text[] textComponents = instantiatedItem.GetComponentsInChildren<Text>();

            foreach (Text textComponent in textComponents)
            {
                if (textComponent.transform.parent == instantiatedItem.transform)
                {
                    // Set the action name in the correct Text component
                    textComponent.text = actionName;
                    break;
                }
            }
        }
    }

    public void ConversationLibraryFolderSpawner()
    {
        conversationLibraryDetailInputPanel.SetActive(false);
        libraryName = libraryNameInput.text;
        
        if(!string.IsNullOrEmpty(libraryName) && !IsLibraryNameExists(libraryName))
        {
            SpawnConversationLibraryFolderPrefab(libraryName);
            SaveLibrary(libraryName);
        }
        else
        {
            Debug.Log("Library already exists!");
        }
    }
 
    public void SaveLibrary(string currentLibraryName)
    {       
        reference.Child("Libraries").Child(currentLibraryName).SetValueAsync(true).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully added library to the Realtime Database");
            }
            else
            {
                Debug.Log("Failed to add library to the Realtime Database");
            }
        });
    }

    private bool IsLibraryNameExists(string name)
    {
        // Check if the library name already exists
        return libraryDictionary.ContainsKey(name.ToLowerInvariant());
    }

    public void SpawnConversationLibraryFolderPrefab(string libraryName)
    {
        instantiatedFolder = Instantiate(conversationLibraryFolder, conversationLibraryFolderHolder);

        //set button reference
        Button openIndividualLibraryButton = instantiatedFolder.GetComponentInChildren<Button>();
        SpawnIndividualLibraryPrefab(libraryName);
        
        if (openIndividualLibraryButton != null)
        {
            //set conversation library folder button to redirect to the library it created
            openIndividualLibraryButton.onClick.AddListener(() => OpenIndividualLibrary(libraryName));
        }
        else
        {
            Debug.LogError("Button component not found in prefab.");
        }

        folderDictionary[libraryName] = instantiatedFolder;
        libraryObjectType = true;
        actionObjectType = false;
        SetObjectName(instantiatedFolder, libraryObjectType, actionObjectType);
    }

    public void OpenIndividualLibrary(string libraryName)
    {
        if (libraryDictionary.ContainsKey(libraryName))
        {
            foreach (var library in individualLibraries)
            {
                library.SetActive(false);
            }
            
            currentIndividualLibrary = libraryDictionary[libraryName];
            currentIndividualLibraryName = libraryName;
            currentIndividualLibrary.SetActive(true);

            Button addButton = currentIndividualLibrary.transform.Find("AddButton").GetComponent<Button>();
            if (addButton != null)
            {
                addButton.onClick.RemoveAllListeners();  // Remove existing listeners
                if (isUserAdminOrNot)
                {
                    addButton.onClick.AddListener(OnAddButtonClick);  // Add the listener only if the user is an admin
                }

                // Activate/deactivate the button based on the admin status
                addButton.gameObject.SetActive(isUserAdminOrNot);
            }
        }
    }

    void SpawnIndividualLibraryPrefab(string libraryName) //spawning library page of the created general library
    {
        instantiatedLibrary = Instantiate(individualLibrary, individualLibraryFolderHolder);
        
        // Set the instantiatedLibrary size to match the size of its parent
        RectTransform parentRectTransform = individualLibraryFolderHolder.GetComponent<RectTransform>();
        RectTransform libraryRectTransform = instantiatedLibrary.GetComponent<RectTransform>();
        
        instantiatedLibrary.SetActive(false);
        individualLibraries.Add(instantiatedLibrary);
        libraryDictionary[libraryName] = instantiatedLibrary; // Add the individual library to the dictionary
        Transform libraryTransform = instantiatedLibrary.transform; //getting transform of instantiated library to count child

        if (libraryTransform != null)
        {
            int addButtonIndex = 2; // Index 2 corresponds to the 3rd child (0-based index)
            int backButtonIndex = 3; // Index 3 corresponds to the 4th child

            if (addButtonIndex < libraryTransform.childCount)
            {
                Button addButton = libraryTransform.GetChild(addButtonIndex).GetComponent<Button>();

                if (addButton != null)
                {
                    addButton.onClick.AddListener(OnAddButtonClick);
                }

            }

            if (backButtonIndex < libraryTransform.childCount)
            {
                Button backButton = libraryTransform.GetChild(backButtonIndex).GetComponent<Button>();

                if (backButton != null)
                {
                    // Pass the library name to the OnBackButtonClick method
                    backButton.onClick.AddListener(() => OnBackButtonClick(libraryName));
                }
            }
        }

        libraryObjectType = true;
        actionObjectType = false;
        SetObjectName(instantiatedLibrary, libraryObjectType, actionObjectType);
    }

    public void OnAddButtonClick()
    {
        individualActionDetailInputPanel.SetActive(true);
    }

    public void OnBackButtonClick(string libraryName)
    {
        if (libraryDictionary.ContainsKey(libraryName))
        {
            libraryDictionary[libraryName].SetActive(false);
        }
    }

    // for image in gesture
    public void LoadFile()
    {
        if (permission == NativeFilePicker.Permission.Granted)
        {
            Debug.Log("Permission Granted");
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                {
                    Debug.Log("Operation cancelled");
                }
                else
                {
                    Debug.Log("Picked file: ");
                    //Variables.ActiveScene.Set("picked path", path);
                    selectedGesturePath = path;
                    actionGesturePath = path;
                }
            }, "image/*");
        }
        else
        {
            Debug.Log("Permission Not Granted");
            AskPermission();
        }
    }

    public async void AskPermission()
    {
        NativeFilePicker.Permission permissionResult = await NativeFilePicker.RequestPermissionAsync(false);
        permission = permissionResult;

        if (permission == NativeFilePicker.Permission.Granted)
        {
            LoadFile();
        }
    }

    public void IndividualActionSpawner()
    {
        individualActionDetailInputPanel.SetActive(false);
    
        actionName = actionNameInput.text;
        actionTextPopUpInput = actionLaymenTextInput.text;

        if (!string.IsNullOrEmpty(actionName) && !IsActionNameExists(actionName, currentIndividualLibrary))
        {
            if (!string.IsNullOrEmpty(actionTextPopUpInput) && !string.IsNullOrEmpty(selectedGesturePath))
            {
                SpawnIndividualAction(actionName, actionTextPopUpInput, selectedGesturePath, currentIndividualLibrary);
                SaveAction(actionName, actionTextPopUpInput);
            }
            else
            {
                Debug.Log("Action Text or Image Missing");
            }
        }
        else
        {
            Debug.Log("Action already exists!");
        }
    }

    public void SaveAction(string currentActionName, string currentActionLaymenText)
    {
        string currentLibraryName = currentIndividualLibraryName;

        Dictionary<string, object> actionData = new Dictionary<string, object>
        {
            { "ActionLaymenText", currentActionLaymenText },
            { "ActionGesturePath", actionGesturePath }
        };

        reference.Child("Libraries").Child(currentLibraryName).Child("Actions").Child(currentActionName).SetValueAsync(actionData).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully added action to the Realtime Database");
            }
            else
            {
                Debug.Log("Failed to add action to the Realtime Database");
            }
        });
    }

    private bool IsActionNameExists(string name, GameObject library)
    {
        // Check if the action name already exists in the current library
        return actionDictionary.ContainsKey(name.ToLowerInvariant()) || actionDictionary.ContainsValue(library);
    }

    public void SpawnIndividualAction(string actionName, string actionTextPopUpInput, string actionGesturePath, GameObject individualLibrary)
    {
        // Assuming individualActionFolderHolder is a child of individualLibrary
        Transform actionFolderHolder = individualLibrary.transform.Find("individualActionFolderHolder");

        if (actionFolderHolder != null)
        {
            instantiatedAction = Instantiate(individualAction, actionFolderHolder);
            individualActions.Add(instantiatedAction);
            actionDictionary[actionName] = instantiatedAction;
            textPopupLaymenText[actionName] = actionTextPopUpInput;
            gesturePopUpImagePath[actionName] = actionGesturePath;
            Transform actionTransform = instantiatedAction.transform; //getting transform of instantiated action to count child

            if (actionTransform != null)
            {
                int openTextPopUP = 0; // Index 0 corresponds to the 1st child (0-based index)
                int openAudioPopUp = 1; // Index 1 corresponds to the 2nd child
                int openGesturePopUp = 2; // Index 2 corresponds to the 3rd child

                if (openTextPopUP < actionTransform.childCount)
                {
                    Button openTextPopUPButton = actionTransform.GetChild(openTextPopUP).GetComponent<Button>();

                    if (openTextPopUPButton != null)
                    {
                        // Call SpawnTextPopUp and pass the reference of instantiatedAction
                        openTextPopUPButton.onClick.AddListener(() => SpawnTextPopUp(instantiatedAction, actionName));
                    }
                }

                if (openAudioPopUp < actionTransform.childCount)
                {
                    Button openAudioPopUpButton = actionTransform.GetChild(openAudioPopUp).GetComponent<Button>();

                    if (openAudioPopUpButton != null)
                    {
                        openAudioPopUpButton.onClick.AddListener(() => SpawnAudioPopUp(instantiatedAction, actionName)); //spawn prefab
                    }
                }
                
                if (openGesturePopUp < actionTransform.childCount)
                {
                    Button openGesturePopUpButton = actionTransform.GetChild(openGesturePopUp).GetComponent<Button>();

                    if (openGesturePopUpButton != null)
                    {
                        openGesturePopUpButton.onClick.AddListener(() => SpawnGesturePopUp(instantiatedAction, actionName)); //spawn prefab
                    }
                }
            }

            libraryObjectType = false;
            actionObjectType = true;
            SetObjectName(instantiatedAction, libraryObjectType, actionObjectType);
        }
        else
        {
            Debug.LogError("individualActionFolderHolder not found in individualLibrary.");
        }
    }

    public void SpawnTextPopUp(GameObject instantiatedAction, string actionName)
    {
        CloseCurrentlyOpenPopup();

        if (!textPopups.ContainsKey(actionName))
        {
            instantiatedActionPopUp = Instantiate(actionTextPopUp, actionPopUpHolder);
            string actionTextPopUpInput = textPopupLaymenText[actionName];
            // Update text component
            UpdatePopupTitle(instantiatedActionPopUp, actionName);

            // Store in the dictionary
            textPopups[actionName] = instantiatedActionPopUp;
            // Assuming individualActionFolderHolder is a child of individualLibrary
            Transform actionTextPopUpTextValueHolder = instantiatedActionPopUp.transform.Find("ActionTextHolder");

            if (actionTextPopUpTextValueHolder != null)
            {
                Text actionPopUpLaymentText = actionTextPopUpTextValueHolder.GetComponentInChildren<Text>();
                // Check if the Text component is found
                if (actionPopUpLaymentText != null)
                {
                    // Set the text of the Text component
                    actionPopUpLaymentText.text = actionTextPopUpInput;
                }
                else
                {
                    Debug.LogError("Text component not found in the child of the ActionTextHolder.");
                }
            }
            else
            {
                Debug.LogError("Action Text holder not found in Text Popup.");
            }

            AssignPopUpBottomButtons(instantiatedAction, instantiatedActionPopUp, actionName);
            CloseActionPopUp(instantiatedActionPopUp);
        }
        else
        {
            textPopups[actionName].SetActive(true);
        }

        currentlyOpenPopup = instantiatedActionPopUp;
    }

    public void SpawnAudioPopUp(GameObject instantiatedAction, string actionName)
    {
        CloseCurrentlyOpenPopup();

        if (!audioPopups.ContainsKey(actionName))
        {
            instantiatedActionPopUp = Instantiate(actionAudioPopUp, actionPopUpHolder);
             // Update text component
            UpdatePopupTitle(instantiatedActionPopUp, actionName);
            // Store in the dictionary
            audioPopups[actionName] = instantiatedActionPopUp;

            Transform audioPopUpTransform = instantiatedActionPopUp.transform; //getting transform of action popup to count child
            int playActionAudio = 2; // Index 2 corresponds to the 3rd child

            if (playActionAudio < audioPopUpTransform.childCount)
            {
                Button playAudioButton = audioPopUpTransform.GetChild(playActionAudio).GetComponent<Button>();

                if (playAudioButton != null)
                {
                    playAudioButton.onClick.AddListener(() => OnPlayAudioButtonClick(actionName)); //spawn prefab
                }
                else
                {
                    Debug.Log("Play audio button is empty");
                }
            }

            AssignPopUpBottomButtons(instantiatedAction, instantiatedActionPopUp, actionName);
            CloseActionPopUp(instantiatedActionPopUp);
        }
        else
        {
            audioPopups[actionName].SetActive(true);
        }

        currentlyOpenPopup = instantiatedActionPopUp;
    }

    public void OnPlayAudioButtonClick(string actionName)
    {
        Debug.Log(textPopupLaymenText[actionName]);
        TTS ttsScript = FindObjectOfType<TTS>(); // Assumes there's only one TTS script in the scene
        if (ttsScript != null)
        {
            ttsScript.OnPlayAudioButtonClick(textPopupLaymenText[actionName]);
        }
        else
        {
            Debug.LogError("TTS script not found in the scene");
        }
    }

    //to set image into gesture prefab
    public IEnumerator LoadTexture(string actionGesturePath, Action<Sprite> callback)
    {
        WWW www = new WWW(actionGesturePath);
        yield return www;

        if (string.IsNullOrEmpty(www.error))
        {
            // Create a sprite from the loaded texture
            Sprite sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), Vector2.zero);

            // Invoke the callback with the created sprite
            callback?.Invoke(sprite);
        }
        else
        {
            Debug.LogError("Failed to load texture: " + www.error);
            callback?.Invoke(null);
        }
    }

    public void SpawnGesturePopUp(GameObject instantiatedAction, string actionName)
    {
        CloseCurrentlyOpenPopup();

        if (!gesturePopups.ContainsKey(actionName))
        {
            instantiatedActionPopUp = Instantiate(actionGesturePopUp, actionPopUpHolder);
            actionGesturePath = gesturePopUpImagePath[actionName];
            // Update text component
            UpdatePopupTitle(instantiatedActionPopUp, actionName);

            // Store in the dictionary
            gesturePopups[actionName] = instantiatedActionPopUp;

            if (instantiatedActionPopUp != null)
            {
                Image actionGesture = instantiatedActionPopUp.GetComponent<Image>();
                if (actionGesture != null)
                {
                    // Load the texture and set it as a sprite
                    StartCoroutine(LoadTexture(actionGesturePath, (sprite) =>
                    {
                        if (sprite != null)
                        {
                            actionGesture.sprite = sprite;
                        }
                        else
                        {
                            // Handle the case when loading the texture fails
                            Debug.LogError("Failed to set gesture sprite.");
                        }
                    }));
                }
                else
                {
                    Debug.LogError("Image Component not found on the ChildImage GameObject.");
                }
            }
            else
            {
                Debug.LogError("instantiatedActionPopUp not found.");
            }

            AssignPopUpBottomButtons(instantiatedAction, instantiatedActionPopUp, actionName);
            CloseActionPopUp(instantiatedActionPopUp);
        }
        else
        {
            gesturePopups[actionName].SetActive(true);
        }

        currentlyOpenPopup = instantiatedActionPopUp;
    }

    // Method to update text component in the popup
    private void UpdatePopupTitle(GameObject popup, string actionName)
    {
        Transform textTransform = popup.transform.GetChild(popup.transform.childCount - 1);

        if (textTransform != null)
        {
            Text textToUpdate = textTransform.GetComponent<Text>();

            if (textToUpdate != null)
            {
                textToUpdate.text = actionName;
            }
            else
            {
                Debug.LogError("Text Component not found on the last child.");
            }
        }
        else
        {
            Debug.LogError("Last child not found in the popup.");
        }
    }

    public void AssignPopUpBottomButtons(GameObject instantiatedAction, GameObject instantiatedActionPopup, string actionName)
    {
        CloseCurrentlyOpenPopup();
        Transform actionBottomPanel = instantiatedActionPopUp.transform.Find("BottomPanel");

        if (actionBottomPanel != null)
        {
            int openTextPopUp = 0; // Index 0 corresponds to the 1st child
            int openAudioPopUp = 1; // Index 1 corresponds to the 2nd child
            int openGesturePopUp = 2; // Index 2 corresponds to the 3rd child

            if (openTextPopUp < actionBottomPanel.childCount)
            {
                Button openTextPopUpButton = actionBottomPanel.GetChild(openTextPopUp).GetComponent<Button>();

                if (openTextPopUpButton != null)
                {
                    openTextPopUpButton.onClick.AddListener(() => SpawnTextPopUp(instantiatedAction, actionName));
                }
            }
            else
            {
                Debug.Log("Failed to open text popup");
            }

            if (openAudioPopUp < actionBottomPanel.childCount)
            {
                Button openAudioPopUpButton = actionBottomPanel.GetChild(openAudioPopUp).GetComponent<Button>();

                if (openAudioPopUpButton != null)
                {
                    openAudioPopUpButton.onClick.AddListener(() => SpawnAudioPopUp(instantiatedAction, actionName));
                }
            }
            else
            {
                Debug.Log("Failed to open audio popup");
            }

            if (openGesturePopUp < actionBottomPanel.childCount)
            {
                Button openGesturePopUpButton = actionBottomPanel.GetChild(openGesturePopUp).GetComponent<Button>();

                if (openGesturePopUpButton != null)
                {
                    openGesturePopUpButton.onClick.AddListener(() => SpawnGesturePopUp(instantiatedAction, actionName));
                }
            }
            else
            {
                Debug.Log("Failed to open gesture popup");
            }
            currentlyOpenPopup = instantiatedActionPopUp;
        }
        else
        {
            Debug.LogError("Action Bottom Panel Not Found in Action Pop Up");
        }
    }

    private void CloseCurrentlyOpenPopup()
    {
        if (currentlyOpenPopup != null)
        {
            currentlyOpenPopup.SetActive(false);
        }
    }

    public void CloseActionPopUp(GameObject instantiatedActionPopUp)
    {
        Transform actionPopUpTransform = instantiatedActionPopUp.transform; //getting transform of action popup to count child

        int closePopUp = 3; // Index 3 corresponds to the 4th child

        if (closePopUp < actionPopUpTransform.childCount)
        {
            Button closePopUpButton = actionPopUpTransform.GetChild(closePopUp).GetComponent<Button>();

            if (closePopUpButton != null)
            {
                closePopUpButton.onClick.AddListener(() => instantiatedActionPopUp.SetActive(false)); //spawn prefab
            }
        }
    }

    public void UpdateLibraryName()
    {
        editorPanel.SetActive(false);

        string libraryName = libraryNameEditorInput.text;
        string newLibraryName = newLibraryNameEditorInput.text;

        UpdateLibraryNameUI(libraryName, newLibraryName);
        UpdateData(libraryName, "", newLibraryName, "", "", "");
    }

    public void UpdateLibraryNameUI(string oldLibraryName, string newLibraryName)
    {
        UpdateObjectNameUI(folderDictionary[oldLibraryName], newLibraryName); //folder prefab on home
        UpdateObjectNameUI(libraryDictionary[oldLibraryName], newLibraryName); //individual library itself
        UpdateActionReferences(oldLibraryName, newLibraryName);
    }

    public void UpdateObjectNameUI(GameObject instantiatedItem, string newLibraryName)
    {
        if (instantiatedItem != null)
        {
            Text[] textComponents = instantiatedItem.GetComponentsInChildren<Text>();

            foreach (Text textComponent in textComponents)
            {
                if (textComponent.transform.parent == instantiatedItem.transform)
                {
                    // Set the new library name in the correct Text component
                    textComponent.text = newLibraryName;
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("Instantiated item is null.");
        }
    }

    public void UpdateActionReferences(string oldLibraryName, string newLibraryName)
    {
        foreach (var action in individualActions)
        {
            // Check if the action references the old library
            if (action.transform.parent.name == oldLibraryName)
            {
                // Update the reference to the new library
                action.transform.parent.name = newLibraryName;
            }
        }
    }

    public void UpdateActionName()
    {        
        editorPanel.SetActive(false);
        
        string libraryName = libraryNameEditorInput.text;
        string actionName = actionNameEditorInput.text;
        string newActionName = newActionNameEditorInput.text; 

        UpdateActionNameUIAndPopups(actionName, newActionName);
        UpdateData(libraryName, actionName, "", "", "", newActionName);
    }

    public void UpdateActionNameUIAndPopups(string oldActionName, string newActionName)
    {
        if (actionDictionary.ContainsKey(oldActionName))
        {
            // Update the action name in dictionaries and lists
            actionDictionary[newActionName] = actionDictionary[oldActionName];

            // Remove old entry
            actionDictionary.Remove(oldActionName);

            // Update the action name in the UI
            UpdateObjectNameUI(actionDictionary[newActionName], newActionName);

            // Check and update the name in popups if they have been instantiated
//            if (textPopups.ContainsKey(oldActionName))
  //          {
                UpdatePopupTitle(textPopups[oldActionName], newActionName);
    //        }

            // Check and update the name in audio popups if they have been instantiated
      //      if (audioPopups.ContainsKey(oldActionName))
        //    {
                UpdatePopupTitle(audioPopups[oldActionName], newActionName);
          //  }

            // Check and update the name in gesture popups if they have been instantiated
            //if (gesturePopups.ContainsKey(oldActionName))
            //{
                UpdatePopupTitle(gesturePopups[oldActionName], newActionName);
            //}
        }
        else
        {
            Debug.LogError($"Key '{oldActionName}' not found in the action dictionary.");
        }
    }

    public void UpdateLaymenText()
    {        
        editorPanel.SetActive(false);
        
        string libraryName = libraryNameEditorInput.text;
        string actionName = actionNameEditorInput.text;
        string newActionLaymenText = actionLaymenTextEditorInput.text;

        UpdateLaymenTextUI(actionName, newActionLaymenText);
        UpdateData(libraryName, actionName, "", "", newActionLaymenText, "");
    }

    public void UpdateLaymenTextUI(string actionName, string newActionLaymenText)
    {
        if (textPopupLaymenText.ContainsKey(actionName))
        {
            // Remove the previous text popup
            if (textPopups.ContainsKey(actionName))
            {
                Destroy(textPopups[actionName]);
                textPopups.Remove(actionName);
            }

            // Remove the previous audio popup
            if (audioPopups.ContainsKey(actionName))
            {
                Destroy(audioPopups[actionName]);
                audioPopups.Remove(actionName);
            }

            // Update laymen text
            textPopupLaymenText[actionName] = newActionLaymenText;

            // Create a new text popup
            SpawnTextPopUp(actionDictionary[actionName], actionName);

            // Create a new audio popup
            SpawnAudioPopUp(actionDictionary[actionName], actionName);
        }
        else
        {
            Debug.LogError($"Key '{actionName}' not found in the textPopupLaymenText dictionary.");
        }
    }

    public void UpdateGesturePath()
    {        
        editorPanel.SetActive(false);
        
        string libraryName = libraryNameEditorInput.text;
        string actionName = actionNameEditorInput.text;
        LoadFile();

        UpdateGestureUI(actionName, actionGesturePath);
        UpdateData(libraryName, actionName, "", actionGesturePath, "", "");
    }

    public void UpdateGestureUI(string actionName, string newActionGesturePath)
    {
        if (gesturePopups.ContainsKey(actionName))
        {
            // Remove the previous gesture popup
            if (gesturePopups.ContainsKey(actionName))
            {
                Destroy(gesturePopups[actionName]);
                gesturePopups.Remove(actionName);
            }

            // Update gesture path
            gesturePopUpImagePath[actionName] = newActionGesturePath;

            // Create a new gesture popup
            SpawnGesturePopUp(actionDictionary[actionName], actionName);
        }
        else
        {
            Debug.LogError($"Key '{actionName}' not found in the gesturePopups dictionary.");
        }
    }

    private void UpdateData(string libraryName, string actionName, string newLibraryName, string newActionGesturePath, string newActionLaymenText, string newActionName)
    {
        DatabaseReference libraryRef = reference.Child("Libraries").Child(libraryName);
        DatabaseReference actionRef = libraryRef.Child("Actions").Child(actionName);

        if (!string.IsNullOrEmpty(newActionName))
        {
            actionRef.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    string readActionLaymenText = snapshot.Child("ActionLaymenText").Value.ToString();
                    string readActionGesturePath = snapshot.Child("ActionGesturePath").Value.ToString();

                    Dictionary<string, object> actionData = new Dictionary<string, object>
                    {
                        { "ActionLaymenText", readActionLaymenText },
                        { "ActionGesturePath", readActionGesturePath }
                    };

                    libraryRef.Child("Actions").Child(newActionName).SetValueAsync(actionData).ContinueWith(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            Debug.Log("Successfully updated action name in the Realtime Database");

                            // Remove the previous action
                            actionRef.RemoveValueAsync().ContinueWith(removeTask =>
                            {
                                if (removeTask.IsCompleted)
                                {
                                    Debug.Log("Successfully removed the previous action");
                                }
                                else
                                {
                                    Debug.Log("Failed to remove the previous action");
                                }
                            });
                        }
                        else
                        {
                            Debug.Log("Failed to update action name in the Realtime Database");
                        }
                    });
                }
            });
        }

        if (!string.IsNullOrEmpty(newLibraryName))
        {
            //create a libary with the new name
            reference.Child("Libraries").Child(newLibraryName).SetValueAsync(true);
            // Fetch all actions under the current library
            libraryRef.Child("Actions").GetValueAsync().ContinueWith(actionTask =>
            {
                if (actionTask.IsCompleted)
                {
                    DataSnapshot actionSnapshot = actionTask.Result;

                    foreach (var actionChild in actionSnapshot.Children)
                    {
                        string actionName = actionChild.Key;
                        string actionLaymenText = actionChild.Child("ActionLaymenText").Value.ToString();
                        string actionGesturePath = actionChild.Child("ActionGesturePath").Value.ToString();

                        // Create a dictionary with action details
                        Dictionary<string, object> actionData = new Dictionary<string, object>
                        {
                            { "ActionLaymenText", actionLaymenText },
                            { "ActionGesturePath", actionGesturePath }
                        };

                        // Save the action under the new library
                        reference.Child("Libraries").Child(newLibraryName).Child("Actions").Child(actionName).SetValueAsync(actionData);
                    }

                    // Remove the previous library
                    libraryRef.RemoveValueAsync().ContinueWith(removeTask =>
                    {
                        if (removeTask.IsCompleted)
                        {
                            Debug.Log("Successfully removed the previous library");
                        }
                        else
                        {
                            Debug.Log("Failed to remove the previous library");
                        }
                    });
                }
                else
                {
                    Debug.Log("Failed to fetch actions from the Realtime Database");
                }
            });
        }
        
        if (!string.IsNullOrEmpty(newActionLaymenText))
        {
            // Directly set the new action laymen text
            actionRef.Child("ActionLaymenText").SetValueAsync(newActionLaymenText).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Successfully updated action laymen text in the Realtime Database");
                }
                else
                {
                    Debug.Log("Failed to update action laymen text in the Realtime Database");
                }
            });
        }

        if (!string.IsNullOrEmpty(newActionGesturePath))
        {
            // Directly set the new action gesture path
            actionRef.Child("ActionGesturePath").SetValueAsync(newActionGesturePath).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Successfully updated action gesture path in the Realtime Database");
                }
                else
                {
                    Debug.Log("Failed to update action gesture path in the Realtime Database");
                }
            });
        }
    }

    public void DeleteLibrary()
    {        
        deletionPanel.SetActive(false);
        
        string libraryNameToDelete = libraryNameToDeleteInput.text;

        DeleteLibraryUI(libraryNameToDelete);
        DeleteData(libraryNameToDelete, "");
    }

    public void DeleteLibraryUI(string libraryName)
    {
        if (libraryDictionary.ContainsKey(libraryName))
        {
            GameObject libraryToDelete = libraryDictionary[libraryName];

            // Remove individual library from the dictionary and list
            libraryDictionary.Remove(libraryName);
            individualLibraries.Remove(libraryToDelete);

            // Destroy the individual library GameObject
            Destroy(libraryToDelete);

            // Remove the associated instantiatedFolder if it exists
            if (folderDictionary.ContainsKey(libraryName))
            {
                GameObject folderToDelete = folderDictionary[libraryName];
                folderDictionary.Remove(libraryName);
                Destroy(folderToDelete);
            }

            // Remove all actions associated with the library
            foreach (var action in individualActions.ToList())
            {
                if (action.transform.parent == libraryToDelete.transform)
                {
                    DeleteActionUI(action.name);
                }
            }

            Debug.Log("Library deleted: " + libraryName);
        }
        else
        {
            Debug.LogWarning("Library not found: " + libraryName);
        }
    }

    public void DeleteAction()
    {        
        deletionPanel.SetActive(false);

        string libraryName = libraryNameToDeleteInput.text;
        string actionNameToDelete = actionNameToDeleteInput.text;

        DeleteActionUI(actionNameToDelete);
        DeleteData(libraryName, actionNameToDelete);
    }

    public void DeleteActionUI(string actionName)
    {
        if (actionDictionary.ContainsKey(actionName))
        {
            GameObject actionToDelete = actionDictionary[actionName];

            // Remove from dictionaries
            actionDictionary.Remove(actionName);
            textPopups.Remove(actionName);
            audioPopups.Remove(actionName);
            gesturePopups.Remove(actionName);

            // Remove from lists
            individualActions.Remove(actionToDelete);

            // Destroy the GameObject
            Destroy(actionToDelete);

            Debug.Log("Action deleted: " + actionName);
        }
        else
        {
            Debug.LogWarning("Action not found: " + actionName);
        }
    }

    private void DeleteData(string libraryName, string actionName)
    {
        reference.Child("Libraries").Child(libraryName).Child("Actions").Child(actionName).RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Successfully deleted data from the Realtime Database");
            }
            else
            {
                Debug.Log("Failed to delete data from the Realtime Database");
            }
        });
    }
}