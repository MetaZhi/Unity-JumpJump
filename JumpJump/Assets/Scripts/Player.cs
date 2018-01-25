using System;
using System.Collections.Generic;
using DG.Tweening;
using LeanCloud;
using UniRx;
using UniRx.Toolkit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    // 小人跳跃时，决定远近的一个参数
    public float Factor;

    // 盒子随机最远的距离
    public float MaxDistance = 5;

    // 第一个盒子物体
    public GameObject Stage;

    // 左上角总分的UI组件
    public Text TotalScoreText;

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

    // 排行数据的姓名
    public GameObject RankName;

    // 排行数据的分数
    public GameObject RankScore;

    // 重新开始按钮
    public Button RestartButton;

    private Rigidbody _rigidbody;
    private float _startTime;
    private GameObject _currentStage;
    private Vector3 _cameraRelativePosition;
    private int _score;
    private bool _isUpdateScoreAnimation;

    Vector3 _direction = new Vector3(1, 0, 0);
    private float _scoreAnimationStartTime;
    private int _lastReward = 1;

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.centerOfMass = new Vector3(0, 0, 0);

        _currentStage = Stage;
        SpawnStage();

        _cameraRelativePosition = Camera.main.transform.position - transform.position;

        SaveButton.onClick.AddListener(OnClickSaveButton);
        RestartButton.onClick.AddListener(() => { SceneManager.LoadScene(0); });

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
            // 计算总共按下空格的时长
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

        // 是否显示飘分效果
        if (_isUpdateScoreAnimation)
            UpdateScoreAnimation();
    }

    /// <summary>
    /// 跳跃
    /// </summary>
    /// <param name="elapse"></param>
    void OnJump(float elapse)
    {
        _rigidbody.AddForce((new Vector3(0, 1.5f, 0) + _direction) * elapse * Factor, ForceMode.Impulse);
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
        if (collision.gameObject.name.Contains("Stage") && collision.gameObject != _currentStage)
        {
            var contacts = collision.contacts;
            //检测是否是脚在盒子上
            Debug.Log(contacts[0].normal);
            if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
            {
                _currentStage = collision.gameObject;

                AddScore(collision.contacts);
                RandomDirection();
                SpawnStage();
                MoveCamera();
                ShowScoreAnimation();
            }
        }

        if (collision.gameObject.name == "Ground")
        {
            OnGameOver();
        }
    }

    /// <summary>
    /// 加分，准确度高的分数成倍增加
    /// </summary>
    /// <param name="contacts">小人与盒子的碰撞点</param>
    private void AddScore(ContactPoint[] contacts)
    {
        if (contacts.Length > 0)
        {
            var hitPoint = contacts[0].point;
            hitPoint.y = 0;

            var stagePos = _currentStage.transform.position;
            stagePos.y = 0;

            var precision = Vector3.Distance(hitPoint, stagePos);
            Debug.Log(precision);
            if (precision < 0.1)
            {
                _lastReward *= 2;
            }
            else
            {
                _lastReward = 1;
            }
            _score += _lastReward;
            TotalScoreText.text = _score.ToString();
        }
    }

    private void OnGameOver()
    {
        if (_score > 0)
        {
            //本局游戏结束，如果得分大于0，显示上传分数panel
            SaveScorePanel.SetActive(true);
        }
        else
        {
            //否则直接显示排行榜
            ShowRankPanel();
        }
    }

    /// <summary>
    /// 显示飘分动画
    /// </summary>
    private void ShowScoreAnimation()
    {
        _isUpdateScoreAnimation = true;
        _scoreAnimationStartTime = Time.time;
        SingleScoreText.text = "+" + _lastReward;
    }

    /// <summary>
    /// 更新飘分动画
    /// </summary>
    void UpdateScoreAnimation()
    {
        if (Time.time - _scoreAnimationStartTime > 1)
            _isUpdateScoreAnimation = false;

        var playerScreenPos =
            RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);
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
        _direction = seed == 0 ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
    }

    /// <summary>
    /// 移动摄像机
    /// </summary>
    void MoveCamera()
    {
        Camera.main.transform.DOMove(transform.position + _cameraRelativePosition, 1);
    }

    /// <summary>
    /// 处理点击上传分数按钮
    /// </summary>
    void OnClickSaveButton()
    {
        var nickname = NameField.text;

        if (nickname.Length == 0)
            return;

        //创建一个GameScore分数对象
        AVObject gameScore = new AVObject("GameScore");
        gameScore["score"] = _score;
        gameScore["playerName"] = nickname;

        //异步保存
        gameScore.SaveAsync().ContinueWith(_ => { ShowRankPanel(); });
        SaveScorePanel.SetActive(false);
    }

    /// <summary>
    /// 显示排行榜面板
    /// </summary>
    void ShowRankPanel()
    {
        Debug.Log("ShowRankPanel");
        //获取GameScore数据对象，降序排列取前10个数据
        AVQuery<AVObject> query = new AVQuery<AVObject>("GameScore").OrderByDescending("score").Limit(10);
        query.FindAsync().ContinueWith(t =>
        {
            var results = t.Result;
            var scores = new List<KeyValuePair<string, string>>();

            //将数据转化为字符串
            foreach (var result in results)
            {
                scores.Add(new KeyValuePair<string, string>(result["playerName"].ToString(), result["score"].ToString()));
            }

            //由于当前是在子线程，对Unity中的物体操作需要在主线程操作，用以下方法转到主线程
            MainThreadDispatcher.Send(_ =>
            {
                Debug.Log(scores.Count);
                foreach (var score in scores)
                {
                    var item = Instantiate(RankName);
                    item.SetActive(true);
                    item.GetComponent<Text>().text = score.Key;
                    item.transform.SetParent(RankName.transform.parent);

                    item = Instantiate(RankScore);
                    item.SetActive(true);
                    item.GetComponent<Text>().text = score.Value;
                    item.transform.SetParent(RankScore.transform.parent);
                }
                RankPanel.SetActive(true);
            }, null);
        });
    }
}