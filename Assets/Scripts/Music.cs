using UnityEngine;
using UnityEngine.UI;

public class MusicController : MonoBehaviour
{
    public AudioSource musicSource; // Ссылка на компонент AudioSource
    public Image musicButtonImage; // Ссылка на изображение кнопки
    public Sprite musicOnSprite;   // Спрайт для включенной музыки
    public Sprite musicOffSprite;  // Спрайт для выключенной музыки

    private bool isMusicOn = true; // Текущее состояние музыки

    void Start()
    {
        // Загружаем сохраненное состояние музыки (1 = вкл, 0 = выкл)
        int musicState = PlayerPrefs.GetInt("music", 1);
        isMusicOn = (musicState == 1);

        // Применяем состояние при старте
        ApplyMusicState();
    }

    void Update()
    {
        // Если нужно обновлять состояние в реальном времени (опционально)
        // Этот метод можно оставить пустым или удалить, если не нужен
    }

    // Метод для применения текущего состояния музыки
    private void ApplyMusicState()
    {
        if (isMusicOn)
        {
            musicSource.enabled = true;
            musicButtonImage.sprite = musicOnSprite;
        }
        else
        {
            musicSource.enabled = false;
            musicButtonImage.sprite = musicOffSprite;
        }
    }

    // Метод для переключения музыки (вызывается при нажатии кнопки)
    public void ToggleMusic()
    {
        // Меняем состояние на противоположное
        isMusicOn = !isMusicOn;

        // Сохраняем новое состояние
        PlayerPrefs.SetInt("music", isMusicOn ? 1 : 0);
        PlayerPrefs.Save(); // Не забываем сохранить

        // Применяем изменения
        ApplyMusicState();
    }
}