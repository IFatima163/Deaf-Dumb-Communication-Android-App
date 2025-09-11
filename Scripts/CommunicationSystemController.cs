using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunicationSystemController : MonoBehaviour
{
    //this script handles turning communication system panels on and off 
    public GameObject popUpMenu, userProfilePanel, conversationLibraryPanel, conversationLibraryDetailInputPanel, inputEditorPanel, objectDeletePanel, individualActionDetailInputPanel;
    
    public void OpenConversationalLibraryHome() 
    {
        userProfilePanel.SetActive(false);
        conversationLibraryPanel.SetActive(true);
        popUpMenu.SetActive(false);
    }    

    public void CloseConversationalLibraryHome()
    {   
        conversationLibraryPanel.SetActive(false);
    }

    public void OpenUserProfilePanel()
    {
        userProfilePanel.SetActive(true);
        conversationLibraryPanel.SetActive(false);
        popUpMenu.SetActive(false);
    }

    public void OpenPopUpMenuPanel()
    {
        popUpMenu.SetActive(true);
    }

    public void ClosePopUpMenuPanel()
    {
        popUpMenu.SetActive(false);
    }    

    public void OpenConversationLibraryDetailInputPanel()
    {
        conversationLibraryDetailInputPanel.SetActive(true);
        popUpMenu.SetActive(false);
    }

    public void CloseConversationLibraryDetailInputPanel()
    {
        conversationLibraryDetailInputPanel.SetActive(false);
    }

    public void OpenInputEditorPanel()
    {
        inputEditorPanel.SetActive(true);
        popUpMenu.SetActive(false);
    }

    public void CloseInputEditorPanel()
    {
        inputEditorPanel.SetActive(false);
    }

    public void OpenObjectDeletePanel()
    {
        objectDeletePanel.SetActive(true);
        popUpMenu.SetActive(false);
    }

    public void CloseObjectDeletePanel()
    {
        objectDeletePanel.SetActive(false);
    }

    public void CloseIndividualActionDetailInputPanel()
    {
        individualActionDetailInputPanel.SetActive(false);
    }
}
