using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageBoxControl : MonoBehaviour
{
    [SerializeField]
    private GameObject textTemplateTo;
    [SerializeField]
    private GameObject textTemplateFrom;

    public Button sendMessageButton;
    public TMP_InputField inputMessageField;
    public string inputText = "";
    public Button closeMessageBoxButton;

    public Button messageBoxIconButton;
    public Button acceptPassengerButton;
    public float movementSpeed = 200f; // How fast the phone should move up the screen
    public float messageBoxTop = -250f; // Indicates how high the phone should be when it is showing
    public float messageBoxBottom = -800f; // Indicates how high the phone should be when it is NOT showing
    private bool rising; // Whether the phone is being animated to rise up
    private bool showChatIcon;
    private float timeElapsed; // Tracks time elapsed

    void Start ()
    {
        fastCloseMessageBox();
        hideMessageBoxIcon();
        rising = false;
        acceptPassengerButton.onClick.AddListener(showMessageBoxIcon);
        sendMessageButton.onClick.AddListener(delegate{StartCoroutine(SilentPassenger());});
        messageBoxIconButton.onClick.AddListener(openMessageBox);
        closeMessageBoxButton.onClick.AddListener(closeMessageBox);
    }

    void Update ()
    {
        inputText = inputMessageField.text;
        if(Input.GetKeyDown (KeyCode.Return))
        {
            SilentPassenger();
        }

        Vector3 newPosition = transform.localPosition;

        //Change position of phone, update time while rising
        if (rising)
        {
            timeElapsed += Time.deltaTime;
            newPosition += new Vector3(0, movementSpeed * Time.deltaTime, 0);
        }
        else
        {
            newPosition -= new Vector3(0, movementSpeed * Time.deltaTime, 0);
        }

        //Keep message box within range
        newPosition.y = Mathf.Min(newPosition.y, messageBoxTop);
        newPosition.y = Mathf.Max(newPosition.y, messageBoxBottom);

        //If message box reaches top buttons are interactable, if not they're unusable
        if (newPosition.y == messageBoxTop)
        {
            sendMessageButton.interactable = true;
            inputMessageField.interactable = true;
            closeMessageBoxButton.interactable = true;
        }
        else
        {
            sendMessageButton.interactable = false;
            inputMessageField.interactable = false;
            closeMessageBoxButton.interactable = false;
        }

        //Make messageBox disappear if bottom reached, useful for audio too
        if (newPosition.y == messageBoxBottom)
        {
            gameObject.SetActive(false);
            messageBoxIconButton.gameObject.SetActive(showChatIcon);
        }

        transform.localPosition = newPosition;
    }

    public void fastCloseMessageBox()
    {
        gameObject.SetActive(false);
        rising = false;
        transform.localPosition = new Vector3(650, messageBoxBottom, 0);
    }

    public void openMessageBox()
    {
        //Measure time taken to show phone and start animation rising
        timeElapsed = 0f;
        rising = true;
        transform.localPosition = new Vector3(650, messageBoxBottom, 0);
        gameObject.SetActive(true);
        messageBoxIconButton.gameObject.SetActive(false);
    }

    private void closeMessageBox()
    {
        rising = false;
    }

    private void showMessageBoxIcon()
    {
        showChatIcon = true;
        messageBoxIconButton.gameObject.SetActive(true);
    }

    private void hideMessageBoxIcon()
    {
        messageBoxIconButton.gameObject.SetActive(false);
    }

    void PrintMessageTo(string messageText)
    {
        GameObject text = Instantiate(textTemplateTo) as GameObject;
        text.SetActive(true);

        text.GetComponent<MessageBoxTextTo>().SetText(messageText);

        text.transform.SetParent(textTemplateTo.transform.parent, false);
    }

    void PrintMessageFrom(string messageText)
    {
        GameObject text = Instantiate(textTemplateFrom) as GameObject;
        text.SetActive(true);

        text.GetComponent<MessageBoxTextFrom>().SetText(messageText);

        text.transform.SetParent(textTemplateFrom.transform.parent, false);
    }

    IEnumerator SilentPassenger()
    {
        PrintMessageTo(inputText);
        yield return new WaitForSeconds(1);
        PrintMessageFrom("...");
        inputMessageField.text = "";
    }
}
