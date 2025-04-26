using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreddyInSpace : MonoBehaviour
{
    [SerializeField] private PlayerGameSystem playerGameSystem;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private RectTransform spaceBG;

    [SerializeField] private RectTransform player;
    [SerializeField] private RectTransform obstacleParent;
    [SerializeField] private RectTransform pipePrefab;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private Button unPauseScreenButton;

    [SerializeField] private List<RectTransform> obstacles;
    [SerializeField] private List<RectTransform> collectables;

    [SerializeField] private Vector2 startPos;
    [SerializeField] private float thrustAcceleration;
    [SerializeField] private float gravity;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float minY, maxY;
    [SerializeField] private bool rotate;
    private PlayerBehaviour playerBehaviour;

    [SerializeField, Range(0f, 1f)] private float difficulty;

    private float verticalVelocity;

    private void Start()
    {
        unPauseScreenButton.onClick.AddListener(UnpauseGame);
    }

    public IEnumerator WaitToStartGame()
    {
        pauseScreen.SetActive(false);
        playerBehaviour = playerGameSystem.playerComputer.playerBehaviour;
        player.anchoredPosition = Vector2.zero;
        player.localEulerAngles = Vector3.zero;

        tutorialText.enabled = true;

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        player.anchoredPosition = startPos;
        tutorialText.enabled = false;
        StartGame();
    }

    public void StartGame()
    {
        UnpauseGame();
        SpawnPipes();
        StartCoroutine(ScrollBackground());
        StartCoroutine(ScrollObstacles());
    }

    public void PauseGame()
    {
        playerGameSystem.isPlaying.Value = false;
        playerBehaviour.ultraPowerDrain.Value = false;
        pauseScreen.SetActive(true);
    }

    public void UnpauseGame()
    {
        verticalVelocity = 0;
        playerGameSystem.isPlaying.Value = true;
        playerBehaviour.ultraPowerDrain.Value = false;
        pauseScreen.SetActive(false);
    }

    private void SpawnPipes()
    {
        difficulty = (float)GameManager.Instance.gameNight / 7f;
        obstacles = new();
        collectables = new();

        float baseSpacing = Mathf.Lerp(500f, 250f, difficulty); // Easy: 500px apart, Hard: 250px apart
        float xScale = Mathf.Lerp(1f, UnityEngine.Random.Range(2.5f, 3), difficulty);
        float yScale = Mathf.Lerp(1.2f, 0.9f, difficulty);

        float xPosition = -14700f;

        while (xPosition < 14700f)
        {
            RectTransform pipe = InstantiatePipe(xPosition);

            pipe.localScale = new(1, yScale, 1f);

            Color randColor = new(
                Mathf.Clamp01(0.014f + UnityEngine.Random.Range(-0.01f, 0.1f)),
                Mathf.Clamp01(0.051f + UnityEngine.Random.Range(-0.01f, 0.1f)),
                Mathf.Clamp01(0.133f + UnityEngine.Random.Range(-0.01f, 0.1f)),
                1f
            );

            foreach (RectTransform child in pipe)
            {
                if (child.TryGetComponent(out Image img))
                {
                    if (child.CompareTag("Obstacle"))
                    {
                        img.color = randColor;
                        child.localScale = new(xScale, 1, 1f);
                        obstacles.Add(child);
                    }
                    else if (child.CompareTag("Collectable"))
                    {
                        collectables.Add(child);
                    }
                }
            }

            float breatheRoom = UnityEngine.Random.Range(30, 50) * xScale;
            xPosition += baseSpacing + breatheRoom;
        }
    }

    private RectTransform InstantiatePipe(float xPosition)
    {
        RectTransform pipe = Instantiate(pipePrefab, obstacleParent.transform);

        float yPosition = UnityEngine.Random.Range(-150, 140);

        pipe.localPosition = new Vector2(xPosition, yPosition);
        pipe.rotation = pipePrefab.rotation;
        pipe.localEulerAngles = new Vector3(UnityEngine.Random.Range(0, 2) == 0 ? 0f : 180f, 0f, 0f);

        pipe.gameObject.SetActive(true);
        return pipe;
    }

    private IEnumerator ScrollBackground()
    {
        Vector2 startPos = new(500f, 0f);
        Vector2 endPos = new(-500f, 0f);
        float elapsedTime = 0f;

        spaceBG.anchoredPosition = startPos;

        while (GameManager.Instance.isPlaying)
        {
            yield return null;
            if (!playerGameSystem.isPlaying.Value) continue;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / GameManager.MaxGameLength;

            // Smooth the movement (ease-in-out)
            t = Mathf.Lerp(0f, 1f, t);

            spaceBG.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
        }

        spaceBG.anchoredPosition = endPos;
    }

    private IEnumerator ScrollObstacles()
    {
        Vector2 startPos = new(15000, 0f);
        Vector2 endPos = new(-15000, 0f);
        float elapsedTime = 0f;

        obstacleParent.anchoredPosition = startPos;

        while (GameManager.Instance.isPlaying)
        {
            yield return null;
            if (!playerGameSystem.isPlaying.Value) continue;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / GameManager.MaxGameLength;

            // Smooth the movement (ease-in-out)
            t = Mathf.Lerp(0f, 1f, t);

            obstacleParent.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
        }

        obstacleParent.anchoredPosition = endPos;
    }

    private void Update()
    {
        if (!playerGameSystem.isPlaying.Value || !playerGameSystem.IsOwner) return;

        HandlePosition();
        CheckForCollision();
    }

    private void HandlePosition()
    {
        float deltaTime = Time.deltaTime;

        // If holding space, apply upward acceleration
        if (Input.GetKey(KeyCode.Space))
        {
            verticalVelocity += thrustAcceleration * deltaTime;
        }

        verticalVelocity -= gravity * deltaTime;

        // Clamp velocity
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxSpeed, maxSpeed);

        // Move the player
        Vector2 pos = player.anchoredPosition;
        pos.y += verticalVelocity * deltaTime;

        // Clamp position
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        player.anchoredPosition = pos;

        if (!rotate) return;

        // Z Rotation based on vertical velocity
        float maxTiltAngle = 50f;
        float t = verticalVelocity / maxSpeed;
        float targetZRotation = Mathf.Clamp(t * maxTiltAngle, -maxTiltAngle, maxTiltAngle);

        Quaternion currentRot = player.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZRotation);
        player.localRotation = Quaternion.Lerp(currentRot, targetRot, deltaTime * 5f); // smooth tilt
    }

    private void CheckForCollision()
    {
        Rect playerRect = GetWorldRect(player);

        CheckForCollectableCollision(playerRect);
        CheckForObstacleCollision(playerRect);
    }

    private void CheckForObstacleCollision(Rect playerRect)
    {
        bool hasCollidedWithPipe = false;
        foreach (RectTransform obstacle in obstacles)
        {
            Rect obstacleRect = GetWorldRect(obstacle);

            if (playerRect.Overlaps(obstacleRect))
            {
                hasCollidedWithPipe = true;
            }
        }

        bool hasTouchedBoundaries = player.anchoredPosition.y == minY || player.anchoredPosition.y == maxY;
        playerBehaviour.ultraPowerDrain.Value = hasCollidedWithPipe || hasTouchedBoundaries;
    }

    private void CheckForCollectableCollision(Rect playerRect)
    {
        List<RectTransform> collected = new();

        foreach (RectTransform collectable in collectables)
        {
            Rect collectableRect = GetWorldRect(collectable);
            if (playerRect.Overlaps(collectableRect))
            {
                GameAudioManager.Instance.PlaySfxOneShot("ping", true);
                playerBehaviour.GetGameCollectable();
                collected.Add(collectable);
            }
        }

        foreach (RectTransform item in collected)
        {
            collectables.Remove(item);
            Destroy(item.gameObject);
        }
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector2 size = new(
            rt.rect.width * rt.lossyScale.x,
            rt.rect.height * rt.lossyScale.y
        );

        Vector2 position = new(
            rt.position.x - size.x / 2f,
            rt.position.y - size.y / 2f
        );

        return new Rect(position, size);
    }
}

