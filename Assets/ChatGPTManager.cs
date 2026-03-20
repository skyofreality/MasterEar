using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using OpenAI;
using UnityEngine.Events;
// using Oculus.Voice.Dictation;   //  Commented (causing error)

public class ChatGPTManager : MonoBehaviour {
    [TextArea(5, 20)]
    public string personality;

    [TextArea(5, 20)]
    public string scene;

    public int maxResponseWordLimit = 15;

    public List<NPCAction> actions;

    // public AppDictationExperience voiceToText;  //  Commented

    [System.Serializable]
    public struct NPCAction {
        public string actionKeyword;

        [TextArea(2, 5)]
        public string actionDescription;

        public UnityEvent actionEvent;
    }

    public OnResponseEvent OnResponse;

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { }

    //private OpenAIApi openAI = new OpenAIApi();
    //private List<ChatMessage> messages = new List<ChatMessage>();

    public string GetInstructions() {
        string instructions =
        "You are a video game character and will answer to the message the player ask you.\n" +
        "You must reply using only Personality and Scene.\n" +
        "Answer in less than " + maxResponseWordLimit + " words.\n" +
        personality + "\n" +
        scene + "\n" +
        BuildActionInstructions() +
        "Player message:\n";

        return instructions;
    }

    public string BuildActionInstructions() {
        string instructions = "";

        foreach (var item in actions) {
            instructions += "if I imply: " + item.actionDescription +
            " add keyword: " + item.actionKeyword + "\n";
        }

        return instructions;
    }

    /*
public async void AskChatGPT(string newText)
{
    ChatMessage newMessage = new ChatMessage();
    newMessage.Content = GetInstructions() + newText;
    newMessage.Role = "user";

    messages.Add(newMessage);

    CreateChatCompletionRequest request = new CreateChatCompletionRequest();
    request.Messages = messages;
    request.Model = "gpt-3.5-turbo";

    var response = await openAI.CreateChatCompletion(request);

    if (response.Choices != null && response.Choices.Count > 0)
    {
        var chatResponse = response.Choices[0].Message;

        foreach (var item in actions)
        {
            if (chatResponse.Content.Contains(item.actionKeyword))
            {
                string textNoKeyword =
                chatResponse.Content.Replace(item.actionKeyword, "");

                chatResponse.Content = textNoKeyword;

                item.actionEvent.Invoke();
            }
        }

        messages.Add(chatResponse);

        Debug.Log(chatResponse.Content);

        OnResponse.Invoke(chatResponse.Content);
    }
}
*/

    void Start() {
        // voiceToText.DictationEvents.OnFullTranscription.AddListener(AskChatGPT);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            // voiceToText.Activate();
        }
    }
}