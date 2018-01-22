using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using LeanCloud;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // 小人跳跃时，决定远近的一个参数
    public float Factor;
    // 盒子随机最远的距离
    public float MaxDistance = 5;
    // 第一个盒子物体
    public GameObject Stage;
    // 摄像机
    public Transform Camera;
    // 左上角总分的UI组件
    public Text ScoreText;
    // 粒子效果
    public GameObject Particle;
    // 小人头部
    public Transform Head;
    // 小人身体
    public Transform Body;
    // 飘分的UI组件
    public Text SingleScoreText;
    // 保存分数面板
    public GameObject SaveScorePanel;
    // 名字输入框
    public InputField NameField;
    // 保存按钮
    public Button SaveButton;

    // 排行榜面板
    public GameObject RankPanel;
    // 排行数据的一条Text
    public GameObject RankItem;

    // 重新开始按钮
    public Button RestartButton;

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

        SaveButton.onClick.AddListener(OnClickSaveButton);
        RestartButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(0);
        });

        MainThreadDispatcher.Initialize();
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

            //还原小人的形状
            Body.transform.DOScale(0.1f, 0.2f);
            Head.transform.DOLocalMoveY(0.29f, 0.2f);

            //还原盒子的形状
            _currentStage.transform.DOLocalMoveY(0.25f, 0.2f);
            _currentStage.transform.DOScale(new Vector3(1, 0.5f, 1), 0.2f);
        }

        // 处理按下空格时小人和盒子的动画
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

    /// <summary>
    /// 跳跃
    /// </summary>
    /// <param name="elapse"></param>
    void OnJump(float elapse)
    {
        _rigidbody.AddForce((new Vector3(0, 1, 0) + _direction) * elapse * Factor, ForceMode.Impulse);
    }

    /// <summary>
    /// 生成盒子
    /// </summary>
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

    /// <summary>
    /// 小人刚体与其他物体发生碰撞时自动调用
    /// </summary>
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

            //加分
            _score++;
            ScoreText.text = _score.ToString();
        }

        if (collision.gameObject.name == "Ground")
        {
            //本局游戏结束，显示上传分数panel
            SaveScorePanel.SetActive(true);
        }
    }

    /// <summary>
    /// 显示飘分动画
    /// </summary>
    private void ShowScoreAnimation()
    {
        _isUpdateScoreAnimation = true;
        _scoreAnimationStartTime = Time.time;
    }

    /// <summary>
    /// 更新飘分动画
    /// </summary>
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

    /// <summary>
    /// 随机方向
    /// </summary>
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

    /// <summary>
    /// 移动摄像机
    /// </summary>
    void MoveCamera()
    {
        Camera.DOMove(transform.position + _cameraRelativePosition, 1);
    }

    /// <summary>
    /// 处理点击上传分数按钮
    /// </summary>
    void OnClickSaveButton()
    {
        var nickname = NameField.text;

        //创建一个GameScore分数对象
        AVObject gameScore = new AVObject("GameScore");
        gameScore["score"] = _score;
        gameScore["playerName"] = nickname;

        //异步保存
        gameScore.SaveAsync().ContinueWith(_ =>
        {
            ShowRankPanel();
        });
        SaveScorePanel.SetActive(false);
    }

    /// <summary>
    /// 显示排行榜面板
    /// </summary>
    void ShowRankPanel()
    {
        //获取GameScore数据对象，降序排列取前10个数据
        AVQuery<AVObject> query = new AVQuery<AVObject>("GameScore").OrderByDescending("score").Limit(10);
        query.FindAsync().ContinueWith(t =>
        {
            var results = t.Result;
            var scores = new List<string>();

            //将数据转化为字符串
            foreach (var result in results)
            {
                var score = result["playerName"] + ":" + result["score"];
                scores.Add(score);
            }

            //由于当前是在子线程，对Unity中的物体操作需要在主线程操作，用以下方法转到主线程
            MainThreadDispatcher.Send(_ =>
            {
                foreach (var score in scores)
                {
                    var item = Instantiate(RankItem);
                    item.SetActive(true);
                    item.GetComponent<Text>().text = score;
                    item.transform.SetParent(RankItem.transform.parent);
                }
                RankPanel.SetActive(true);
            }, null);
        });
    }
}