using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameWinUI : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button playAgainButton;

    [Header("Xp System")]
    [SerializeField] private RectTransform xpBar;
    [SerializeField] private TMP_Text currentXpLevelText;
    [SerializeField] private TMP_Text totalXpText;
    [SerializeField] private TMP_Text nextLevelXpText;
    [SerializeField] private TMP_Text xpGainedText;
    [SerializeField] private Image xpProgressBar;


    void Start()
    {
        leaveButton.onClick.AddListener(MultiplayerManager.Instance.LeaveGame);
        playAgainButton.onClick.AddListener(() =>
        {
            MiscellaneousGameUI.Instance.waitToPlayAgainUI.Show();
            Hide();
        });
        GameManager.Instance.OnGameWin += Show;

        Hide();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameWin -= Show;
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    private void Show()
    {
        GameAudioManager.Instance.StopAllSfx();
        MiscellaneousGameUI.Instance.debugCanvasUI.Hide();
        canvas.enabled = true;

        StartCoroutine(PlayXpAnimation());
    }

    private IEnumerator PlayXpAnimation()
    {
        leaveButton.gameObject.SetActive(false);
        playAgainButton.gameObject.SetActive(false);

        PlayerData playerData = MultiplayerManager.Instance.GetLocalPlayerData();
        uint oldExperience = playerData.experience;

        SetXpBar(oldExperience);

        uint xpGain = playerData.role == PlayerRoles.None ? 0 : GameManager.Instance.XpGained.Value; // dont give xp to spectators that didnt play

        xpGainedText.text = $"+{xpGain}XP";
        uint newExperience = (uint)Mathf.Min((long)oldExperience + xpGain, XPManager.MaxXp);

        MultiplayerManager.Instance.SetPlayerExperience(newExperience);

        yield return SlideXpBarUp();

        yield return new WaitForSeconds(0.5f);

        yield return SmoothLerpXpBarProgress(newExperience, oldExperience);

        SetXpBar(newExperience);

        leaveButton.gameObject.SetActive(true);
        playAgainButton.gameObject.SetActive(true);
    }

    private IEnumerator SlideXpBarUp()
    {
        float duration = 1f;
        float timer = 0f;
        Vector2 startPos = new(xpBar.anchoredPosition.x, -1500);
        Vector2 endPos = new(xpBar.anchoredPosition.x, 20f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // Slide XP bar into view
            xpBar.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

            yield return null;
        }
    }

    private IEnumerator SmoothLerpXpBarProgress(float newExperience, float oldExperience)
    {
        float duration = 2f;
        float timer = 0f;

        float tickCooldown = 0.1f; // How often the tick plays
        float tickTimer = 0f;

        while (timer < duration)
        {
            float delta = Time.deltaTime;
            timer += delta;
            tickTimer += delta;

            float t = Mathf.Clamp01(timer / duration);

            // Ease-out progression
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            // XP interpolation
            uint currentDisplayedXp = (uint)Mathf.Lerp(oldExperience, newExperience, easedT);
            SetXpBar(currentDisplayedXp);

            // Play tick every tickCooldown seconds
            if (tickTimer >= tickCooldown)
            {
                tickTimer = 0f;

                // Calculate pitch from eased time (fast start, slow end)
                float pitch = Mathf.Lerp(0.8f, 1.5f, easedT);

                AudioSource tick = GameAudioManager.Instance.PlaySfxInterruptable("select 1");
                if (tick != null) tick.pitch = pitch;
            }

            yield return null;
        }

        // Ensure it finishes clean
        SetXpBar((uint)newExperience);
    }

    private void SetXpBar(uint experience)
    {
        uint currentLevel = XPManager.GetLevelFromXp(experience);

        currentXpLevelText.text = currentLevel.ToString();
        totalXpText.text = experience.ToString() + "XP";
        nextLevelXpText.text = XPManager.GetTotalXpForLevel(currentLevel + 1).ToString() + "XP";
        xpProgressBar.fillAmount = XPManager.GetLevelProgress(experience);
    }
}
