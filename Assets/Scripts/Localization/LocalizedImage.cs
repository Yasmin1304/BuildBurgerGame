using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    [SerializeField] private Sprite englishSprite;
    [SerializeField] private Sprite arabicSprite;

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

        if (targetImage == null || LanguageManager.Instance == null)
            return;

        Sprite selectedSprite = LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic
            ? arabicSprite
            : englishSprite;

        if (selectedSprite != null)
            targetImage.sprite = selectedSprite;
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
