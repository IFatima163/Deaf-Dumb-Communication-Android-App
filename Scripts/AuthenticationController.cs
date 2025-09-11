using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class AuthenticationController : MonoBehaviour
{
    public PrefabSpawner prefabSpawner;
    public GameObject loginPanel, signupPanel, profilePanel, forgotPasswordPanel, notificationPannel;
    public InputField loginEmail, loginPassword, signupEmail, signupPassword, signupCPassword, signupUserName, forgotPasswordEmail;
    public Text notif_Title_Text, notif_Message_Text, profileUserName_Text, profileUserEmail_Text; 
    public Toggle rememberMe;

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    bool isSignIn = false;
    [Header("Popup Menu Items")]
    public Text AdminHeader;
    public GameObject addLibraryButton;
    public GameObject editDataButton;
    public GameObject deleteDataButton;
    
    void Start()
    {
        StartCoroutine(InitializeFirebaseCoroutine());
    }

    IEnumerator InitializeFirebaseCoroutine()
    {
        var initTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => initTask.IsCompleted);

        var dependencyStatus = initTask.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available) 
        {
            InitializeFirebase();
        } 
        else 
        {
            UnityEngine.Debug.LogError(System.String.Format(
            "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        }
        
        // Additional code that needs to run after Firebase initialization
    }

    public void OpenLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
    }
    
    public void OpenSignUpPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        profilePanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
    }
    
    public void OpenProfilePanel(string idName)
    {
        Debug.Log("user name on profile page: " + idName);

        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(true);
        forgotPasswordPanel.SetActive(false);
        
        if (IsAdminUser())
        {
            prefabSpawner.isUserAdminOrNot = true;
            Debug.Log(prefabSpawner.isUserAdminOrNot);
            PopUpPanelSecurity();
        }
        else
        {
            prefabSpawner.isUserAdminOrNot = false;
            Debug.Log(prefabSpawner.isUserAdminOrNot);
            PopUpPanelSecurity();
        }
    }
    
    public void OpenForgotPasswordPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
    }

    bool IsAdminUser()
    {
        // Check if the user's display name is "admin"
        return user != null && user.DisplayName == "admin";
    }

    public void PopUpPanelSecurity()
    {
        if (prefabSpawner.isUserAdminOrNot == true)
        {
            Debug.Log("Admin access activated");
            AdminHeader.enabled = true;
            addLibraryButton.SetActive(true);
            editDataButton.SetActive(true);
            deleteDataButton.SetActive(true);
        }
        else
        {
            AdminHeader.enabled = false;
            addLibraryButton.SetActive(false);
            editDataButton.SetActive(false);
            deleteDataButton.SetActive(false);
        }
    }

    public void LoginUser()
    {
        if (string.IsNullOrEmpty(loginEmail.text) && string.IsNullOrEmpty(loginPassword.text))
        {
            ShowNotificationMessage ("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }
        //Do Login
        SignInUser(loginEmail.text, loginPassword.text);
    }

    public void SignUpUser()
    {
        if (string.IsNullOrEmpty(signupEmail.text) && string.IsNullOrEmpty(signupPassword.text) && string.IsNullOrEmpty(signupCPassword.text) && string.IsNullOrEmpty(signupUserName.text))
        {
            ShowNotificationMessage ("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }
        //Do Signup
        CreateUser(signupEmail.text, signupPassword.text, signupUserName.text);
    }

    public void ForgotPassword()
    {
        if (string.IsNullOrEmpty(forgotPasswordEmail.text))
        {
            ShowNotificationMessage ("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }
        ForgotPasswordSubmit(forgotPasswordEmail.text);
    }

    private void ShowNotificationMessage(string title, string message)
    {
        notif_Title_Text.text = "" + title;
        notif_Message_Text.text = "" + message;

        notificationPannel.SetActive(true);
    }

    public void CloseNotificationPanel()
    {
        notif_Title_Text.text = "";
        notif_Message_Text.text = "";

        notificationPannel.SetActive(false);
    }

    public void Logout()
    {
        auth.SignOut();
        profileUserEmail_Text.text = "";
        profileUserName_Text.text = "";
        OpenLoginPanel();
    }

    void CreateUser(string email, string password, string Username)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
        if (task.IsCanceled) {
            Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
            return;
        }
        if (task.IsFaulted) {
            Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
            
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
            {
                Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    var errorCode = (AuthError)firebaseEx.ErrorCode;
                    ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                }
            }
            return;
        }

        // Firebase user has been created.
        Firebase.Auth.AuthResult result = task.Result;
        Debug.LogFormat("Firebase user created successfully: {0} ({1})",
            result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(Username);
        });
        OpenLoginPanel();
    }   

    public void SignInUser(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
        if (task.IsCanceled) {
            Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
            return;
        }
        if (task.IsFaulted) {
            Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
            
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
            {
                Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    var errorCode = (AuthError)firebaseEx.ErrorCode;
                    ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                }
            }            
            return;
        }

        Firebase.Auth.AuthResult result = task.Result;
        Debug.LogFormat("User signed in successfully: {0} ({1})",
            result.User.DisplayName, result.User.UserId);
        
        profileUserName_Text.text = "" + result.User.DisplayName;

        profileUserEmail_Text.text = "" + result.User.Email;
        OpenProfilePanel(profileUserName_Text.text);
        });
    }

    void InitializeFirebase() 
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);  
        Debug.Log("Firebase Initialization Complete");      
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs) 
    {
        if (auth.CurrentUser != user) {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null) {
            Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn) {
            Debug.Log("Signed in " + user.UserId);
            isSignIn = true;
            }
        } 
    }

    void OnDestroy() 
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    void UpdateUserProfile(string UserName)
    {
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        if (user != null) {
        Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile {
            DisplayName = UserName,
            PhotoUrl = new System.Uri("https://via.placeholder.com/150C/O%20https://placeholder.com/"),
        };
        user.UpdateUserProfileAsync(profile).ContinueWith(task => {
            if (task.IsCanceled) {
            Debug.LogError("UpdateUserProfileAsync was canceled.");
            return;
            }
            if (task.IsFaulted) {
            Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
            return;
            }

            Debug.Log("User profile updated successfully.");

            ShowNotificationMessage("Alert", "Account Successfully Created");
        });
        }
    }

    bool isSigned = false;

    void Update()
    {
        if (isSignIn)
        {
            if (!isSigned)
            {
                isSigned = true;
                profileUserName_Text.text = "" + user.DisplayName;
                profileUserEmail_Text.text = "" + user.Email;
                OpenProfilePanel(profileUserName_Text.text);
            }
        }
    }

    private static string GetErrorMessage(AuthError errorCode)
    {
        var message = "";
        switch (errorCode)
        {
            case AuthError.AccountExistsWithDifferentCredentials:
                message = "Account Does Not Exist";
                break;
            case AuthError.MissingPassword:
                message = "Missing Password";
                break;
            case AuthError.WeakPassword:
                message = "Password Is Too Weak";
                break;
            case AuthError.WrongPassword:
                message = "Incorrect Password";
                break;
            case AuthError.EmailAlreadyInUse:
                message = "Email Is Already In Use";
                break;
            case AuthError.InvalidEmail:
                message = "Invalid Email Address";
                break;
            case AuthError.MissingEmail:
                message = "Email Missing";
                break;
            default:
                message = "Invalid Error";
                break;
        }
        return message;
    }

    void ForgotPasswordSubmit(string forgotPasswordEmail)
    {
        auth.SendPasswordResetEmailAsync(forgotPasswordEmail).ContinueWithOnMainThread(task=>{
            if (task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmailAsync was cancelled");
            }

            if (task.IsFaulted)
            {
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        ShowNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                } 
            }

            ShowNotificationMessage("Alert", "Email Sent Successfully");
        });
    }    
}