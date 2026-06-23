using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class FMVGameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private GameObject descriptionUI;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private RawImage videoRawImage;

    [Header("Ending UI")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TextMeshProUGUI endingText;
    [SerializeField] private Button restartButton;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;

    [Header("Start")]
    [SerializeField] private FMVDialogueNode startNode;

    private TextMeshProUGUI dialogueText;
    private FMVDialogueNode currentNode;
    private int affectionScore;

    // UI와 비디오 플레이어 초기 설정
    private void Awake()
    {
        InitializeUI();
        ConfigureVideoPlayer();
    }

    // 시작 노드가 있으면 첫 대화 노드 표시
    private void Start()
    {
        if (startNode != null)
            ShowNode(startNode);
    }

    // 비활성화될 때 비디오 이벤트 해제 및 재생 중지
    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.Stop();
        }
    }

    // 오브젝트 제거 시 재시작 버튼 이벤트 해제.
    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartFromBeginning);
    }

    // 전달받은 대화 노드의 UI, 비디오, 선택지 갱신
    public void ShowNode(FMVDialogueNode node)
    {
        if (node == null)
            return;

        currentNode = node;

        UpdateDialogueUI(node);
        PlayVideo(node.video);

        bool isEndingNode = node.isEnding;
        bool waitForEndingVideo = isEndingNode && node.video != null && videoPlayer != null;
        if (isEndingNode && !waitForEndingVideo)
            ShowEndingResult(node);

        bool waitForVideo = node.video != null && videoPlayer != null;
        if (waitForVideo || isEndingNode)
            HideChoiceButtons();
        else
            UpdateChoiceButtons(node);
    }

    // 선택한 선택지의 호감도 반영 후 다음 노드로 이동
    public void SelectChoice(int index)
    {
        if (currentNode == null || currentNode.choices == null)
            return;

        if (index < 0 || index >= currentNode.choices.Length)
            return;

        FMVDialogueNode.Choice choice = currentNode.choices[index];
        affectionScore += choice.affection;

        FMVDialogueNode nextNode = choice.nextNode;
        if (nextNode != null)
        {
            ShowNode(nextNode);
        }
        else
            Debug.Log("대화 종료");
    }

    // UI 참조 탐색 및 초기 표시 상태와 버튼 이벤트 설정
    private void InitializeUI()
    {
        if (dialogueUI != null)
            dialogueText = dialogueUI.GetComponentInChildren<TextMeshProUGUI>(true);

        if (descriptionUI != null && descriptionText == null)
            descriptionText = descriptionUI.GetComponentInChildren<TextMeshProUGUI>(true);

        if (endingPanel != null && endingText == null)
            endingText = endingPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        if (endingPanel != null && restartButton == null)
            restartButton = endingPanel.GetComponentInChildren<Button>(true);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartFromBeginning);
            restartButton.onClick.AddListener(RestartFromBeginning);
        }

        HideEndingPanel();
    }

    // 비디오 플레이어의 재생, 오디오, 완료 이벤트 설정 구성
    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        if (videoAudioSource == null)
            videoAudioSource = GetComponent<AudioSource>();
        if (videoAudioSource == null)
            videoAudioSource = gameObject.AddComponent<AudioSource>();

        videoAudioSource.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    // 현재 노드의 대화 텍스트와 엔딩 패널 표시 상태 갱신
    private void UpdateDialogueUI(FMVDialogueNode node)
    {
        bool useEndingPanel = node.isEnding && endingPanel != null;

        if (dialogueText != null)
        {
            dialogueText.text = node.text;
            dialogueText.gameObject.SetActive(!useEndingPanel && !string.IsNullOrWhiteSpace(node.text));
        }

        bool showDescription = !node.isEnding && !string.IsNullOrWhiteSpace(node.description);
        if (descriptionText != null)
            descriptionText.text = node.description;

        if (descriptionUI != null)
            descriptionUI.SetActive(showDescription);

        if (!node.isEnding)
            HideEndingPanel();
    }

    // 지정한 비디오 클립 준비 및 표시 여부 설정
    private void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null)
            return;

        videoPlayer.Stop();
        videoPlayer.clip = clip;

        bool hasVideo = clip != null;
        if (videoRawImage != null)
            videoRawImage.gameObject.SetActive(hasVideo);

        if (hasVideo)
            videoPlayer.Prepare();
    }

    // 비디오 준비 완료 시 출력 텍스처 연결 및 재생
    private void OnVideoPrepared(VideoPlayer source)
    {
        if (source == null)
            return;

        if (videoRawImage != null && source.texture != null)
            videoRawImage.texture = source.texture;

        source.Play();
    }

    // 비디오 종료 시 엔딩, 선택지, 다음 노드 흐름 처리
    private void OnVideoFinished(VideoPlayer source)
    {
        if (currentNode == null)
            return;

        if (currentNode.isEnding)
        {
            ShowEndingResult(currentNode);
            return;
        }

        bool hasChoices = currentNode.choices != null && currentNode.choices.Length > 0;
        if (hasChoices)
        {
            UpdateChoiceButtons(currentNode);
            return;
        }

        if (currentNode.nextNode != null)
            ShowNode(currentNode.nextNode);
    }

    // 노드의 선택지 정보를 버튼 텍스트와 클릭 이벤트에 반영
    private void UpdateChoiceButtons(FMVDialogueNode node)
    {
        if (choiceButtons == null)
            return;

        FMVDialogueNode.Choice[] choices = node.choices;

        for (int buttonIndex = 0; buttonIndex < choiceButtons.Length; buttonIndex++)
        {
            Button button = choiceButtons[buttonIndex];
            if (button == null)
                continue;

            bool hasChoice = choices != null && buttonIndex < choices.Length && choices[buttonIndex] != null;
            button.gameObject.SetActive(hasChoice);

            if (!hasChoice)
                continue;

            FMVDialogueNode.Choice choice = choices[buttonIndex];
            button.interactable = choice.enabled;

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonText != null)
                buttonText.text = choice.choiceText;

            int selectedIndex = buttonIndex;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectChoice(selectedIndex));
        }
    }

    // 모든 선택지 버튼 숨김
    private void HideChoiceButtons()
    {
        if (choiceButtons == null)
            return;

        for (int buttonIndex = 0; buttonIndex < choiceButtons.Length; buttonIndex++)
        {
            if (choiceButtons[buttonIndex] != null)
                choiceButtons[buttonIndex].gameObject.SetActive(false);
        }
    }

    // 엔딩 UI 표시 및 최종 대사와 호감도 결과 처리
    private void ShowEndingResult(FMVDialogueNode node)
    {
        HideChoiceButtons();

        if (endingPanel != null)
            endingPanel.SetActive(true);

        if (endingText != null)
            endingText.text = node.text;
        else if (dialogueText != null)
        {
            dialogueText.text = node.text;
            dialogueText.gameObject.SetActive(true);
        }

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        Debug.Log($"호감도 {affectionScore}");
    }

    // 호감도 초기화 후 시작 노드부터 다시 진행
    private void RestartFromBeginning()
    {
        affectionScore = 0;

        HideEndingPanel();

        if (startNode != null)
            ShowNode(startNode);
    }

    // 엔딩 패널 숨김
    private void HideEndingPanel()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);
    }
}