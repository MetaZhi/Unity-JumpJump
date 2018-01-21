using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public float Factor;

    public float MaxDistance = 5;
    public GameObject Stage;

    public Transform Camera;

    public Text ScoreText;

    public GameObject Particle;

    public Transform Head;
    public Transform Body;

    public Text SingleScoreText;

    private Rigidbody _rigidbody;
    private float _startTime;
    private GameObject _currentStage;
    private Collider _lastCollisionCollider;
    private Vector3 _cameraRelativePosition;
    private int _score;
    private bool _isUpdateScoreAnimation;

    Vector3 _direction = new Vector3(1, 0, 0);
    private float _scoreAnimationStartTime;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = new Vector3(0, 0, 0);

        _currentStage = Stage;
        _lastCollisionCollider = _currentStage.GetComponent<Collider>();
        SpawnStage();

        _cameraRelativePosition = Camera.position - transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _startTime = Time.time;
            Particle.SetActive(true);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            var elapse = Time.time - _startTime;
            OnJump(elapse);
            Particle.SetActive(false);

            Body.transform.DOScale(0.1f, 0.2f);
            Head.transform.DOLocalMoveY(0.29f, 0.2f);

            _currentStage.transform.DOLocalMoveY(0.25f, 0.2f);
            _currentStage.transform.DOScale(new Vector3(1, 0.5f, 1), 0.2f);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            Body.transform.localScale += new Vector3(1, -1, 1) * 0.05f * Time.deltaTime;
            Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;

            _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
            _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
        }

        if (_isUpdateScoreAnimation)
        {
            UpdateScoreAnimation();
        }
    }

    void OnJump(float elapse)
    {
        _rigidbody.AddForce((new Vector3(0, 1, 0) + _direction) * elapse * Factor, ForceMode.Impulse);
    }

    void SpawnStage()
    {
        var stage = Instantiate(Stage);
        stage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, MaxDistance);

        var randomScale = Random.Range(0.5f, 1);
        stage.transform.localScale = new Vector3(randomScale, 0.5f, randomScale);

        // 重载函数 或 重载方法
        stage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1));
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name.Contains("Stage") && collision.collider != _lastCollisionCollider)
        {
            _lastCollisionCollider = collision.collider;
            _currentStage = collision.gameObject;
            RandomDirection();
            SpawnStage();
            MoveCamera();
            ShowScoreAnimation();

            _score++;
            ScoreText.text = _score.ToString();
        }

        if (collision.gameObject.name == "Ground")
        {
            //本局游戏结束，重新开始
            SceneManager.LoadScene(0);
        }
    }

    private void ShowScoreAnimation()
    {
        _isUpdateScoreAnimation = true;
        _scoreAnimationStartTime = Time.time;
    }

    void UpdateScoreAnimation()
    {
        if (Time.time - _scoreAnimationStartTime > 1)
            _isUpdateScoreAnimation = false;

        var playerScreenPos =
            RectTransformUtility.WorldToScreenPoint(Camera.GetComponent<Camera>(), transform.position);
        SingleScoreText.transform.position = playerScreenPos +
                                       Vector2.Lerp(Vector2.zero, new Vector2(0, 200),
                                           Time.time - _scoreAnimationStartTime);

        SingleScoreText.color = Color.Lerp(Color.black, new Color(0, 0, 0, 0), Time.time - _scoreAnimationStartTime);
    }

    void RandomDirection()
    {
        var seed = Random.Range(0, 2);
        if (seed == 0)
        {
            _direction = new Vector3(1, 0, 0);
        }
        else
        {
            _direction = new Vector3(0, 0, 1);
        }
    }

    void MoveCamera()
    {
        Camera.DOMove(transform.position + _cameraRelativePosition, 1);
    }
}