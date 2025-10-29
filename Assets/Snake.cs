using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Snake : MonoBehaviour
{
    [Header("Prefabs")]
    public Transform bodyPrefab;
    public Transform foodPrefab;
    public Transform wallPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gameOverText;

    [Header("Configurações do Jogo")]
    public float speed = 8.0f;
    public float cellSize = 0.3f;

    private List<Transform> body = new List<Transform>();
    private Transform currentFood;
    private List<Transform> wall = new List<Transform>();

    private Vector2 direction;
    private Vector2 cellIndex = Vector2.zero;

    private float changeCellTime = 0;
    private int score = 0;
    private int highScore = 0;
    private bool gameOver = false;

    // NOVO: guarda a posição da cabeça antes do movimento (usado para spawn da nova parte)
    private Vector3 previousHeadPosition;

    void Start()
    {
        gameOverText.gameObject.SetActive(false);
        direction = Vector2.up;

        CreateWalls();
        SpawnFood();

        scoreText.text = "SCORE: 0";
        highScoreText.text = "HIGH SCORE: 0";
    }

    void Update()
    {
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                Restart();
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
            // Salva a posição atual da cabeça ANTES de mover — usada para GrowBody quando não há corpo
            previousHeadPosition = transform.position;

            // Move o corpo para seguir
            for (int i = body.Count - 1; i > 0; i--)
            {
                body[i].position = body[i - 1].position;
            }

            if (body.Count > 0)
                body[0].position = transform.position;

            // Move a cabeça
            transform.position += (Vector3)direction * cellSize;
            changeCellTime = Time.time + 1 / speed;

            cellIndex = transform.position / cellSize;
        }
    }

    void GrowBody()
    {
        Vector2 position;

        if (body.Count > 0)
        {
            // Se já tem corpo, posiciona a nova peça no fim (comportamento normal)
            position = body[body.Count - 1].position;
        }
        else
        {
            // Se NÃO tem corpo, instancia a nova peça na posição QUE A CABEÇA ESTAVA antes de se mover
            // (evita instanciar sobre a cabeça e causar colisão imediata)
            position = previousHeadPosition;
        }

        body.Add(Instantiate(bodyPrefab, position, Quaternion.identity).transform);
    }

    void CheckEatFood()
    {
        if (currentFood == null) return;

        // Detecção suave (com base na distância) — suficiente para grade baseada em cellSize
        float distance = Vector2.Distance(transform.position, currentFood.position);
        if (distance < cellSize / 2f)
        {
            Destroy(currentFood.gameObject);
            currentFood = null;
            GrowBody();
            UpdateScore();
            SpawnFood(); // gera outro sem pausar o jogo
        }
    }

    void UpdateScore()
    {
        score++;
        scoreText.text = "SCORE: " + score;
    }

    void SpawnFood()
    {
        // Garante apenas 1 comida ativa
        if (currentFood != null) return;

        // Tenta posições válidas (evita parede e corpo)
        for (int tries = 0; tries < 100; tries++)
        {
            float x = Random.Range(-23, 23) * cellSize;
            float y = Random.Range(-13, 11) * cellSize;
            Vector2 pos = new Vector2(x, y);

            bool invalid = false;
            foreach (var w in wall)
                if (Vector2.Distance(pos, w.position) < cellSize * 0.9f) { invalid = true; break; }

            if (invalid) continue;

            foreach (var b in body)
            {
                if (Vector2.Distance(pos, b.position) < cellSize * 0.9f) { invalid = true; break; }
            }

            if (invalid) continue;

            // evita spawn exatamente na cabeça
            if (Vector2.Distance(pos, transform.position) < cellSize * 0.9f) continue;

            currentFood = Instantiate(foodPrefab, pos, Quaternion.identity);
            return;
        }

        // Se não achou posição após várias tentativas, tenta spawn próximo (menos ideal, mas evita loop infinito)
        Vector3 fallback = transform.position + new Vector3(direction.x * cellSize * 3f, direction.y * cellSize * 3f, 0);
        currentFood = Instantiate(foodPrefab, fallback, Quaternion.identity);
    }

    void CheckCollisions()
    {
        // Checa colisão com parede
        foreach (var w in wall)
        {
            if (Vector2.Distance(transform.position, w.position) < cellSize / 2f)
            {
                GameOver();
                return;
            }
        }

        // Checa colisão com corpo (pula checagem com o segmento que eventualmente esteja imediatamente atrás da cabeça)
        // Para evitar detecção falsa imediata no momento do crescimento, conferimos com precisão por distância.
        for (int i = 0; i < body.Count; i++)
        {
            if (Vector2.Distance(transform.position, body[i].position) < cellSize / 2f)
            {
                GameOver();
                return;
            }
        }
    }

    void CreateWalls()
    {
        int cellX = -24;
        int cellY = 11;
        int height = 25;

        float horizontal = cellX * cellSize;
        float vertical = cellY * cellSize;

        for (int i = 0; i < (int)Mathf.Abs((horizontal * 2) / cellSize) + 1; ++i)
        {
            Vector2 top = new Vector3(horizontal + cellSize * i, vertical);
            Vector2 bottom = new Vector3(horizontal + cellSize * i, vertical - height * cellSize);
            wall.Add(Instantiate(wallPrefab, top, Quaternion.identity).transform);
            wall.Add(Instantiate(wallPrefab, bottom, Quaternion.identity).transform);
        }

        for (int i = 0; i < height; ++i)
        {
            Vector2 right = new Vector3(horizontal, vertical - cellSize * i);
            Vector2 left = new Vector3(-horizontal, vertical - cellSize * i);
            wall.Add(Instantiate(wallPrefab, right, Quaternion.identity).transform);
            wall.Add(Instantiate(wallPrefab, left, Quaternion.identity).transform);
        }
    }

    void GameOver()
    {
        gameOver = true;
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "GAME OVER\nPressione R para reiniciar";
    }

    void Restart()
    {
        gameOver = false;
        gameOverText.gameObject.SetActive(false);

        if (score > highScore)
            highScore = score;

        highScoreText.text = "HIGH SCORE: " + highScore;
        score = 0;
        scoreText.text = "SCORE: 0";

        // limpa o corpo e comida
        foreach (var b in body) Destroy(b.gameObject);
        body.Clear();

        if (currentFood != null) Destroy(currentFood.gameObject);
        currentFood = null;

        transform.position = Vector3.zero;
        direction = Vector2.up;

        // reseta temporizadores/posição anterior
        changeCellTime = 0;
        previousHeadPosition = transform.position;

        SpawnFood();
    }
}
