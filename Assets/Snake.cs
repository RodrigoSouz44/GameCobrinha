using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Snake : MonoBehaviour
{
    [Header("Prefabs")]
    public Transform bodyPrefab;
    public Transform foodPrefab;
    public Transform wallPrefab; // deve ser Transform (igual ao seu código original)

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gameOverText;

    [Header("Configurações do Jogo")]
    public float speed = 16.0f; // velocidade inicial (ajustado)
    public float cellSize = 0.3f;

    private List<Transform> body = new List<Transform>();
    private Transform currentFood;
    private List<Transform> wall = new List<Transform>();

    private Vector2 direction;
    private Vector2 cellIndex = Vector2.zero;

    private float changeCellTime = 0f;
    private int score = 0;
    private int highScore = 0;
    private bool gameOver = false;
    private bool gameWon = false;

    private Vector3 previousHeadPosition;

    // velocidade automática
    private float timeSinceStart = 0f;
    private float speedIncreaseInterval = 2f;
    private float speedIncreaseAmount = 0.8f;
    private float maxSpeed = 60f;

    // vitória
    private float timeElapsed = 0f;
    private float winTimeSeconds = 60f;
    private int winScore = 30;

    // geração de obstáculos internos
    [Header("Obstáculos internos (aleatórios)")]
    [Tooltip("Quantidade de blocos internos gerados a cada partida")]
    public int obstacleCount = 8;
    [Tooltip("Distância mínima (em células) dos obstáculos para a cabeça/corpo")]
    public int obstacleMinDistanceCells = 2;

    void Start()
    {
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        direction = Vector2.up;
        previousHeadPosition = transform.position;

        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // cria paredes, gera obstáculos e comida
        ClearWalls();
        CreateWalls();
        GenerateObstacles(); // gera obstáculos dentro da área criada
        SpawnFood();

        scoreText.text = "SCORE: 0";
        highScoreText.text = "HIGH SCORE: " + highScore;

        timeSinceStart = 0f;
        timeElapsed = 0f;
    }

    void Update()
    {
        if (gameOver || gameWon)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;
        }

        timeElapsed += Time.deltaTime;

        // aumento de velocidade automático a cada interval
        timeSinceStart += Time.deltaTime;
        if (timeSinceStart >= speedIncreaseInterval)
        {
            timeSinceStart = 0f;
            if (speed < maxSpeed)
            {
                speed += speedIncreaseAmount;
                if (speed > maxSpeed) speed = maxSpeed;
            }
        }

        // condição de vitória
        if (timeElapsed >= winTimeSeconds || score >= winScore)
        {
            OnWin();
            return;
        }

        ChangeDirection();
        Move();
        CheckEatFood();
        CheckCollisions();
    }

    void ChangeDirection()
    {
        if (Input.GetKeyDown(KeyCode.W) && direction != Vector2.down) direction = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.S) && direction != Vector2.up) direction = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.A) && direction != Vector2.right) direction = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.D) && direction != Vector2.left) direction = Vector2.right;
        else if (Input.GetKeyDown(KeyCode.UpArrow) && direction != Vector2.down) direction = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && direction != Vector2.up) direction = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && direction != Vector2.right) direction = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && direction != Vector2.left) direction = Vector2.right;
    }

    void Move()
    {
        if (Time.time > changeCellTime)
        {
            previousHeadPosition = transform.position;

            for (int i = body.Count - 1; i > 0; i--)
                body[i].position = body[i - 1].position;

            if (body.Count > 0)
                body[0].position = transform.position;

            transform.position += (Vector3)direction * cellSize;
            changeCellTime = Time.time + 1f / speed;

            cellIndex = transform.position / cellSize;
        }
    }

    void GrowBody()
    {
        Vector3 pos = (body.Count > 0) ? body[body.Count - 1].position : previousHeadPosition;
        body.Add(Instantiate(bodyPrefab, pos, Quaternion.identity).transform);
    }

    void CheckEatFood()
    {
        if (currentFood == null) return;

        float distance = Vector2.Distance(transform.position, currentFood.position);
        if (distance < cellSize / 2f)
        {
            Destroy(currentFood.gameObject);
            currentFood = null;
            GrowBody();
            UpdateScore();
            SpawnFood();
        }
    }

    void UpdateScore()
    {
        score++;
        if (scoreText != null) scoreText.text = "SCORE: " + score;
        if (score > highScore) highScore = score;
        if (highScoreText != null) highScoreText.text = "HIGH SCORE: " + highScore;
    }

    void SpawnFood()
    {
        if (currentFood != null) return;

        for (int tries = 0; tries < 200; tries++)
        {
            int gx = Random.Range(-22, 23); // grade X (células)
            int gy = Random.Range(-12, 11); // grade Y

            // evita bordas: se quiser permitir mais perto da borda ajuste os limites
            if (gx <= -23 || gx >= 23 || gy <= -13 || gy >= 11) continue;

            Vector2 pos = new Vector2(gx * cellSize, gy * cellSize);

            bool invalid = false;
            foreach (var w in wall)
                if (Vector2.Distance(pos, w.position) < cellSize * 0.9f) { invalid = true; break; }
            if (invalid) continue;

            foreach (var b in body)
                if (Vector2.Distance(pos, b.position) < cellSize * 0.9f) { invalid = true; break; }
            if (invalid) continue;

            if (Vector2.Distance(pos, transform.position) < cellSize * 0.9f) continue;

            currentFood = Instantiate(foodPrefab, pos, Quaternion.identity);
            return;
        }

        // fallback
        Vector3 fallback = transform.position + new Vector3(direction.x * cellSize * 3f, direction.y * cellSize * 3f, 0f);
        currentFood = Instantiate(foodPrefab, fallback, Quaternion.identity);
    }

    void CheckCollisions()
    {
        foreach (var w in wall)
        {
            if (Vector2.Distance(transform.position, w.position) < cellSize / 2f)
            {
                GameOver();
                return;
            }
        }

        for (int i = 0; i < body.Count; i++)
        {
            if (Vector2.Distance(transform.position, body[i].position) < cellSize / 2f)
            {
                GameOver();
                return;
            }
        }
    }

    // --- paredes externas (borda) ---
    void CreateWalls()
    {
        // limpa antes
        ClearWalls();

        int left = -24;
        int top = 11;
        int height = 25;

        float horizontal = left * cellSize;
        float vertical = top * cellSize;

        // horizontais (top e bottom)
        for (int i = 0; i < Mathf.Abs(left * 2) + 1; i++)
        {
            Vector2 topPos = new Vector2(horizontal + i * cellSize, vertical);
            Vector2 bottomPos = new Vector2(horizontal + i * cellSize, vertical - height * cellSize);
            wall.Add(Instantiate(wallPrefab, topPos, Quaternion.identity).transform);
            wall.Add(Instantiate(wallPrefab, bottomPos, Quaternion.identity).transform);
        }

        // verticais (left e right)
        for (int i = 0; i < height + 1; i++)
        {
            Vector2 leftPos = new Vector2(left * cellSize, vertical - i * cellSize);
            Vector2 rightPos = new Vector2(-left * cellSize, vertical - i * cellSize);
            wall.Add(Instantiate(wallPrefab, leftPos, Quaternion.identity).transform);
            wall.Add(Instantiate(wallPrefab, rightPos, Quaternion.identity).transform);
        }
    }

    // limpa todas as paredes (borda + internas)
    void ClearWalls()
    {
        for (int i = wall.Count - 1; i >= 0; i--)
        {
            if (wall[i] != null) Destroy(wall[i].gameObject);
        }
        wall.Clear();
    }

    // gera obstáculos internos em posições da grade (multiplicadas por cellSize)
    void GenerateObstacles()
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("GenerateObstacles: wallPrefab não está definido no Inspector.");
            return;
        }

        int tries = 0;
        int created = 0;

        // limites em células (ajuste conforme seu CreateWalls)
        int minX = -22;
        int maxX = 22;
        int minY = -12;
        int maxY = 10;

        while (created < obstacleCount && tries < obstacleCount * 20)
        {
            tries++;
            int gx = Random.Range(minX, maxX + 1);
            int gy = Random.Range(minY, maxY + 1);

            // evita borda imediata (não criar em x == minX/maxX ou y == minY/maxY)
            if (gx <= minX + 0 || gx >= maxX - 0 || gy <= minY + 0 || gy >= maxY - 0) continue;

            Vector2 pos = new Vector2(gx * cellSize, gy * cellSize);

            // evita gerar em cima da cabeça (distância em células)
            if (Vector2.Distance(pos, transform.position) < obstacleMinDistanceCells * cellSize) continue;

            // evita colidir com corpo existente ou parede existente
            bool invalid = false;
            foreach (var b in body) if (Vector2.Distance(pos, b.position) < cellSize * 0.9f) { invalid = true; break; }
            if (invalid) continue;

            foreach (var w in wall) if (Vector2.Distance(pos, w.position) < cellSize * 0.9f) { invalid = true; break; }
            if (invalid) continue;

            // evita próximo à comida atual
            if (currentFood != null && Vector2.Distance(pos, currentFood.position) < cellSize * 1.2f) continue;

            Transform t = Instantiate(wallPrefab, (Vector3)pos, Quaternion.identity).transform;
            wall.Add(t);
            created++;
        }

        Debug.Log($"GenerateObstacles: tentou {tries} vezes e criou {created} obstáculos.");
    }

    void GameOver()
    {
        gameOver = true;
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER\nPressione R para reiniciar";
        }
    }

    void OnWin()
    {
        gameWon = true;
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "VOCÊ VENCEU!\nPressione R para reiniciar";
        }
    }

    void Restart()
    {
        gameOver = false;
        gameWon = false;
        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        if (score > highScore) highScore = score;
        PlayerPrefs.SetInt("HighScore", highScore);
        if (highScoreText != null) highScoreText.text = "HIGH SCORE: " + highScore;

        score = 0;
        if (scoreText != null) scoreText.text = "SCORE: 0";

        // limpa corpo
        foreach (var b in body) if (b != null) Destroy(b.gameObject);
        body.Clear();

        // limpa paredes e recria
        ClearWalls();
        CreateWalls();
        GenerateObstacles();

        if (currentFood != null) Destroy(currentFood.gameObject);
        currentFood = null;

        transform.position = Vector3.zero;
        direction = Vector2.up;

        changeCellTime = 0f;
        previousHeadPosition = transform.position;

        // reset velocidade e timers
        speed = 16f;
        timeSinceStart = 0f;
        timeElapsed = 0f;

        SpawnFood();
    }
}
