using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;

public class TTS : MonoBehaviour
{
    public AudioSource _audio;

    void Start()
    {
        _audio = gameObject.GetComponent<AudioSource>();
    }

    IEnumerator DownloadAndPlayAudio(string chunk)
    {
        string url = "https://api.voicerss.org/?key=67da22b7c98e4b56a0998572bccdb4b2&hl=en-us&c=MP3&src=" + chunk;
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            _audio.clip = DownloadHandlerAudioClip.GetContent(www);
            _audio.Play();

            // Introducing a shorter delay between each word
            float delay = Mathf.Max(0.5f, _audio.clip.length * 0.1f);
            yield return new WaitForSeconds(delay);
            _audio.Stop();
        }
        else
        {
            Debug.LogError("Audio download failed. Error: " + www.error);
        }
    }

    IEnumerator DownloadAndPlayChunks(string text)
    {
        // Split the text into chunks based on some criteria (e.g., words, characters)
        string[] chunks = text.Split(' ');

        foreach (string chunk in chunks)
        {
            if (!string.IsNullOrEmpty(chunk))
            {
                // Remove any special characters or spaces
                string cleanedChunk = Regex.Replace(chunk, "[^a-zA-Z0-9]", "");

                // Start coroutine to download and play each chunk
                yield return StartCoroutine(DownloadAndPlayAudio(cleanedChunk));
            }
        }
    }

    public void OnPlayAudioButtonClick(string inputTextValue)
    {
        if (!string.IsNullOrEmpty(inputTextValue))
        {
            StartCoroutine(DownloadAndPlayChunks(inputTextValue));
        }
        else
        {
            Debug.Log("No input received to convert to speech");
        }
    }
}
