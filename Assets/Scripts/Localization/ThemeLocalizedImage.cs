using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ThemeLocalizedImage : MonoBehaviour
{
    [Header("Burger")]
    [SerializeField] private Sprite burgerEnglishSprite;
    [SerializeField] private Sprite burgerArabicSprite;

    [Header("Letters")]
    [SerializeField] private Sprite lettersEnglishSprite;
    [SerializeField] private Sprite lettersArabicSprite;

    [Header("Numbers")]
    [SerializeField] private Sprite numbersEnglishSprite;
    [SerializeField] private Sprite numbersArabicSprite;

    private Image targetImage;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void Start()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void Refresh()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null)
            return;

        bool isArabic = LanguageManager.Instance != null &&
                         LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        Sprite selectedSprite = GetThemeSprite(SessionData.SelectedGameMode, isArabic);
        if (selectedSprite != null)
            targetImage.sprite = selectedSprite;
    }

    Sprite GetThemeSprite(GameMode mode, bool isArabic)
    {
        switch (mode)
        {
            case GameMode.Letters:
                return isArabic ? lettersArabicSprite : lettersEnglishSprite;

            case GameMode.Numbers:
                return isArabic ? numbersArabicSprite : numbersEnglishSprite;

            case GameMode.Burger:
            default:
                return isArabic ? burgerArabicSprite : burgerEnglishSprite;
        }
    }

    void HandleLanguageChanged(AppLanguage _)
    {
        Refresh();
    }

    void Subscribe()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged += HandleLanguageChanged;
    }

    void Unsubscribe()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= HandleLanguageChanged;
    }
}
