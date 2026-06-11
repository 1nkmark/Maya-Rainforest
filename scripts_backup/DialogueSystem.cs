using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string content;
    public AudioClip clip;   // 配音音频
}

public class DialogueSystem : MonoBehaviour
{
    public System.Action OnDialogueStart;
    public System.Action OnDialogueEnd;

    // OnLineStart：某句话开始时触发，传入说话人名字
    private System.Action<string> _onLineStart;
    public event System.Action<string> OnLineStart
    {
        add { _onLineStart -= value; _onLineStart += value; }
        remove { _onLineStart -= value; }
    }

    // ★ 新增 OnLineEnd：某句话结束（显示完+等待结束）后、切到下一句之前触发
    private System.Action<string> _onLineEnd;
    public event System.Action<string> OnLineEnd
    {
        add { _onLineEnd -= value; _onLineEnd += value; }
        remove { _onLineEnd -= value; }
    }

    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;

    public List<DialogueLine> dialogueLines;
    public float autoPlayDelay = 2.0f;
    public bool useRightTrigger = false;

    private int currentLineIndex = 0;
    private bool isWaitingForNext = false;
    private Coroutine displayCoroutine = null;
    private Coroutine waitCoroutine = null;

    private AudioSource audioSource;
    private InputAction skipAction;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        dialoguePanel.SetActive(false);

        skipAction = new InputAction("Skip", binding: "<Keyboard>/space");
        if (useRightTrigger)
            skipAction = new InputAction("Skip", binding: "<XRController>{RightHand}/triggerPressed");

        skipAction.performed += ctx => OnSkipInput();
        skipAction.Enable();

        StartDialogue();
    }

    void OnDisable()
    {
        skipAction?.Disable();
    }

    public void StartDialogue()
    {
        if (dialogueLines.Count == 0) return;
        dialoguePanel.SetActive(true);
        currentLineIndex = 0;
        OnDialogueStart?.Invoke();
        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        isWaitingForNext = false;

        var line = dialogueLines[currentLineIndex];
        speakerText.text = line.speaker;

        _onLineStart?.Invoke(line.speaker);

        if (line.clip != null)
        {
            audioSource.clip = line.clip;
            audioSource.Play();
            displayCoroutine = StartCoroutine(SyncTextWithAudio(line.content));
        }
        else
        {
            dialogueText.text = line.content;
            displayCoroutine = null;
            StartWaitForNext();
        }
    }

    IEnumerator SyncTextWithAudio(string fullText)
    {
        dialogueText.text = "";
        float startTime = Time.time;
        int totalChars = fullText.Length;
        float clipLength = audioSource.clip.length;

        while (audioSource.isPlaying)
        {
            float elapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsed / clipLength);
            int charsToShow = Mathf.Clamp(Mathf.FloorToInt(progress * totalChars), 0, totalChars);
            dialogueText.text = fullText.Substring(0, charsToShow);
            yield return null;
        }

        dialogueText.text = fullText;
        displayCoroutine = null;
        StartWaitForNext();
    }

    void StartWaitForNext()
    {
        if (waitCoroutine != null) StopCoroutine(waitCoroutine);
        waitCoroutine = StartCoroutine(WaitAndNext());
    }

    IEnumerator WaitAndNext()
    {
        isWaitingForNext = true;
        yield return new WaitForSeconds(autoPlayDelay);
        isWaitingForNext = false;
        waitCoroutine = null;

        // ★ 在切换到下一句之前，触发当前句的 OnLineEnd
        _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);

        NextLine();
    }

    void OnSkipInput()
    {
        if (!dialoguePanel.activeSelf) return;

        if (displayCoroutine != null)
        {
            if (audioSource.isPlaying) audioSource.Stop();
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
            dialogueText.text = dialogueLines[currentLineIndex].content;
            if (waitCoroutine != null) StopCoroutine(waitCoroutine);

            // ★ 跳过时也触发 OnLineEnd
            _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);
            NextLine();
        }
        else if (isWaitingForNext)
        {
            if (waitCoroutine != null) StopCoroutine(waitCoroutine);
            waitCoroutine = null;
            isWaitingForNext = false;

            // ★ 跳过等待时也触发 OnLineEnd
            _onLineEnd?.Invoke(dialogueLines[currentLineIndex].speaker);
            NextLine();
        }
    }

    void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Count)
            DisplayCurrentLine();
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        OnDialogueEnd?.Invoke();
        Debug.Log("对话结束");
    }
}