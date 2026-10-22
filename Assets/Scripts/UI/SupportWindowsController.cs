using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SkullKing.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SkullKing.UI
{
    /// <summary>UI is authored in scenes. Help never pauses the game and feedback never auto-sends.</summary>
    public sealed class SupportWindowsController : MonoBehaviour
    {
        [Header("Help")]
        [SerializeField] private GameObject _helpPanel;
        [SerializeField] private RulebookContent _rulebook;
        [SerializeField] private TMP_Text _pageTitle;
        [SerializeField] private TMP_Text _pageBody;
        [SerializeField] private TMP_Text _pageNumber;
        [SerializeField] private ScrollRect _helpScroll;
        [Header("Feedback (local files only)")]
        [SerializeField] private GameObject _feedbackPanel;
        [SerializeField] private TMP_InputField _feedbackInput;
        [SerializeField] private TMP_Text _categoryText;
        [SerializeField] private TMP_Text _characterCount;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _saveButton;
        [SerializeField] private string[] _categories = { "오류 제보", "규칙 관련", "개선 제안", "기타" };
        private int _page;
        private int _category;
        private string _savedDraft;
        private IllustratedRulebookView _illustratedView;
        private string Folder => Path.Combine(Application.persistentDataPath, "Feedback");

        private void Awake()
        {
            if (_helpPanel != null) _helpPanel.SetActive(false);
            if (_feedbackPanel != null) _feedbackPanel.SetActive(false);
            if (_feedbackInput != null)
            {
                _feedbackInput.characterLimit = FeedbackFileStore.CharacterLimit;
                _feedbackInput.richText = false;
                _feedbackInput.onValueChanged.AddListener(DraftChanged);
            }
        }
        private void OnDestroy()
        {
            if (_feedbackInput != null) _feedbackInput.onValueChanged.RemoveListener(DraftChanged);
        }
        private void Update()
        {
            if ((_helpPanel == null || !_helpPanel.activeSelf) && (_feedbackPanel == null || !_feedbackPanel.activeSelf)) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
        }
        public void ShowHelp()
        {
            Close();
            _page = 0;
            if (_helpPanel != null) _helpPanel.SetActive(true);
            if (_rulebook != null && _rulebook.illustratedLayout && _helpPanel != null)
            {
                if (_illustratedView == null)
                {
                    _illustratedView = gameObject.GetComponent<IllustratedRulebookView>();
                    if (_illustratedView == null) _illustratedView = gameObject.AddComponent<IllustratedRulebookView>();
                }
                _illustratedView.BuildIfNeeded(_helpPanel, _pageTitle, _pageBody, _pageNumber, _helpScroll);
            }
            ShowPage();
        }
        public void ShowFeedback()
        {
            Close();
            if (_feedbackPanel != null) _feedbackPanel.SetActive(true);
            if (_status != null) _status.text = "이 PC에 파일로 저장됩니다. 자동으로 전송되지 않습니다.";
            RefreshDraft();
        }
        public void Close()
        {
            if (_helpPanel != null) _helpPanel.SetActive(false);
            if (_feedbackPanel != null) _feedbackPanel.SetActive(false);
        }
        public void PreviousPage() { _page--; ShowPage(); }
        public void NextPage() { _page++; ShowPage(); }
        private void ShowPage()
        {
            int count = _rulebook != null && _rulebook.pages != null ? _rulebook.pages.Length : 0;
            if (count == 0)
            {
                if (_pageTitle != null) _pageTitle.text = "규칙";
                if (_pageBody != null) _pageBody.text = "Rulebook Content를 Inspector에서 연결해 주세요.";
                return;
            }
            _page = (_page % count + count) % count;
            var page = _rulebook.pages[_page];
            if (_pageTitle != null) _pageTitle.text = page.title;
            if (_pageBody != null) _pageBody.text = page.body;
            if (_pageNumber != null) _pageNumber.text = (_page + 1) + " / " + count;
            if (_illustratedView != null) _illustratedView.Show(page);
            Canvas.ForceUpdateCanvases();
            if (_helpScroll != null && (_illustratedView == null || !_illustratedView.IsReady))
                _helpScroll.verticalNormalizedPosition = 1f;
        }
        public void NextCategory()
        {
            if (_categories == null || _categories.Length == 0) return;
            _category = (_category + 1) % _categories.Length;
            RefreshDraft();
        }
        private string Category => _categories != null && _categories.Length > 0 ? _categories[_category % _categories.Length] : "기타";
        private string DraftKey => Category + "\n" + (_feedbackInput != null ? _feedbackInput.text : "");
        private void DraftChanged(string value) => RefreshDraft();
        private void RefreshDraft()
        {
            string body = _feedbackInput != null ? _feedbackInput.text : "";
            if (_categoryText != null) _categoryText.text = "분류: " + Category + "  (눌러서 변경)";
            if (_characterCount != null) _characterCount.text = body.Length + " / " + FeedbackFileStore.CharacterLimit;
            if (_saveButton != null) _saveButton.interactable = FeedbackFileStore.IsValidBody(body) && _savedDraft != DraftKey;
        }
        private string Report()
        {
            // No Steam ID, account name, card hand, log file, hardware or network information.
            return "CROSSION 피드백\n작성 시각(UTC): " + DateTime.UtcNow.ToString("O") +
                "\n게임 버전: " + Application.version + "\n화면: " + SceneManager.GetActiveScene().name +
                "\n분류: " + Category + "\n\n" + _feedbackInput.text.Trim();
        }
        public void SaveFeedback()
        {
            if (_feedbackInput == null || !FeedbackFileStore.IsValidBody(_feedbackInput.text))
            { if (_status != null) _status.text = "내용을 먼저 작성해 주세요."; return; }
            if (_savedDraft == DraftKey) return;
            try
            {
                string path = FeedbackFileStore.Save(Folder, Report());
                _savedDraft = DraftKey;
                if (_status != null) _status.text = "파일 저장 완료. ‘저장 폴더’에서 확인하고 직접 전달해 주세요.\n" + Path.GetFileName(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { if (_status != null) _status.text = "파일을 저장하지 못했습니다. 내용을 복사해 따로 보관해 주세요."; }
            RefreshDraft();
        }
        public void CopyFeedback()
        {
            if (_feedbackInput == null || !FeedbackFileStore.IsValidBody(_feedbackInput.text)) return;
            GUIUtility.systemCopyBuffer = Report();
            if (_status != null) _status.text = "내용을 복사했습니다. 원하는 곳에 직접 붙여넣어 주세요.";
        }
        public void OpenFolder()
        {
            try { Directory.CreateDirectory(Folder); Application.OpenURL(new Uri(Folder + Path.DirectorySeparatorChar).AbsoluteUri); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { if (_status != null) _status.text = "저장 폴더를 열지 못했습니다. 내용을 복사해 보관해 주세요."; }
        }
    }
}
