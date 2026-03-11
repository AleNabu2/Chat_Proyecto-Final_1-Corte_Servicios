using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject connectionPanel;
    public GameObject chatPanel;

    public TMP_Text connectionStatusText;
    public TMP_Text messagesText;
    public TMP_InputField messageInput;
    public ChatManager chatManager;
    public ScrollRect scrollRect;

    public TMP_Text textMessagePrefab;
    public RawImage imagePrefab;
    public Button pdfButtonPrefab; 

    public Transform chatContent;
    public ChatManager ChatManager;

    void Start()
    {
        chatPanel.SetActive(false);
    }

    public void ShowChat(string role, ChatProtocol protocol)
    {
        connectionPanel.SetActive(false);
        chatPanel.SetActive(true);

        connectionStatusText.text = $"Connected as {role}\nProtocol: {protocol}";

    }

    public void SendMessage()
    {
        string message = messageInput.text;

        if (!string.IsNullOrEmpty(message))
        {
            chatManager.SendChatMessage(message);

            AddMessage("Me: " + message);

            messageInput.text = "";
        }
    }

    public void AddMessage(string message)
    {
        TMP_Text msg = Instantiate(textMessagePrefab, chatContent);
        msg.text = message;

    }

    public void AddImage(Texture2D texture)
{
        RawImage img = Instantiate(imagePrefab, chatContent);
    img.texture = texture;
    }

    public void AddPdf(byte[] pdfBytes, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, pdfBytes);

        Button pdfButton = Instantiate(pdfButtonPrefab, chatContent);

        pdfButton.GetComponentInChildren<TMP_Text>().text = fileName;

        pdfButton.onClick.AddListener(() =>
        {
            Application.OpenURL("file://" + path);
        });

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void BackToConnection()
    {
        chatManager.CloseConnection();

        chatPanel.SetActive(false);
        connectionPanel.SetActive(true);
    }

}

