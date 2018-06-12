using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
class GameScore
{
    public int score;
    public string playerName;
}

class QueryRankResult
{
    public List<GameScore> results = null;
}

public class Player : MonoBehaviour
{
    // 小人跳跃时，决定远近的一个参数
    public float Factor;

    // 盒子随机最远的距离
    public float MaxDistance = 5;

    // 第一个盒子物体
    public GameObject Stage;

    // 盒子仓库，可以放上各种盒子的prefab，用于动态生成。
    public GameObject[] BoxTemplates;

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

    public string LeanCloudAppId;
    public string LeanCloudAppKey;

    private Rigidbody _rigidbody;
    private float _startTime;
    private GameObject _currentStage;
    private Vector3 _cameraRelativePosition;
    private int _score;
    private bool _isUpdateScoreAnimation;

    Vector3 _direction = new Vector3(1, 0, 0);
    private float _scoreAnimationStartTime;
    private int _lastReward = 1;
    private LeanCloudRestAPI _leanCloud;
    private bool _enableInput = true;

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

        _leanCloud = new LeanCloudRestAPI(LeanCloudAppId, LeanCloudAppKey);
    }

    // Update is called once per frame
    void Update()
    {
        if (_enableInput)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _startTime = Time.time;
                Particle.SetActive(true);
            }

            if (Input.GetMouseButtonUp(0))
            {
                // 计算总共按下空格的时长
                var elapse = Time.time - _startTime;
                OnJump(elapse);
                Particle.SetActive(false);

                //还原小人的形状
                Body.transform.DOScale(0.1f, 0.2f);
                Head.transform.DOLocalMoveY(0.29f, 0.2f);

                //还原盒子的形状
                _currentStage.transform.DOLocalMoveY(-0.25f, 0.2f);
                _currentStage.transform.DOScaleY(0.5f, 0.2f);

                _enableInput = false;
            }

            // 处理按下空格时小人和盒子的动画
            if (Input.GetMouseButton(0))
            {
                //添加限定，盒子最多缩放一半
                if (_currentStage.transform.localScale.y > 0.3)
                {
                    Body.transform.localScale += new Vector3(1, -1, 1) * 0.05f * Time.deltaTime;
                    Head.transform.localPosition += new Vector3(0, -1, 0) * 0.1f * Time.deltaTime;

                    _currentStage.transform.localScale += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                    _currentStage.transform.localPosition += new Vector3(0, -1, 0) * 0.15f * Time.deltaTime;
                }
            }
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
        _rigidbody.AddForce(new Vector3(0, 5f, 0) + (_direction) * elapse * Factor, ForceMode.Impulse);
        transform.DOLocalRotate(new Vector3(0, 0, -360), 0.6f, RotateMode.LocalAxisAdd);
    }

    /// <summary>
    /// 生成盒子
    /// </summary>
    void SpawnStage()
    {
        GameObject prefab;
        if (BoxTemplates.Length > 0)
        {
            // 从盒子库中随机取盒子进行动态生成
            prefab = BoxTemplates[Random.Range(0, BoxTemplates.Length)];
        }
        else
        {
            prefab = Stage;
        }

        var stage = Instantiate(prefab);
        stage.transform.position = _currentStage.transform.position + _direction * Random.Range(1.1f, MaxDistance);

        var randomScale = Random.Range(0.5f, 1);
        stage.transform.localScale = new Vector3(randomScale, 0.5f, randomScale);

        // 重载函数 或 重载方法
        stage.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1));
    }

    void OnCollisionExit(Collision collision)
    {
        _enableInput = false;
    }

    /// <summary>
    /// 小人刚体与其他物体发生碰撞时自动调用
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.name == "Ground")
        {
            OnGameOver();
        }
        else
        {
            if (_currentStage != collision.gameObject)
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {
                    _currentStage = collision.gameObject;
                    AddScore(contacts);
                    RandomDirection();
                    SpawnStage();
                    MoveCamera();

                    _enableInput = true;
                }
                else // body collides with the box
                {
                    OnGameOver();
                }
            }
            else //still on the same box
            {
                var contacts = collision.contacts;

                //check if player's feet on the stage
                if (contacts.Length == 1 && contacts[0].normal == Vector3.up)
                {
                    _enableInput = true;
                }
                else // body just collides with this box
                {
                    OnGameOver();
                }
            }
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
            if (precision < 0.1)
                _lastReward *= 2;
            else
                _lastReward = 1;

            _score += _lastReward;
            TotalScoreText.text = _score.ToString();
            ShowScoreAnimation();
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
        transform.right = _direction;
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
        GameScore gameScore = new GameScore();
        gameScore.score = _score;
        gameScore.playerName = nickname;

        //异步保存
        StartCoroutine(_leanCloud.Create("GameScore", JsonUtility.ToJson(gameScore, false), ShowRankPanel));
        SaveScorePanel.SetActive(false);
    }

    /// <summary>
    /// 显示排行榜面板
    /// </summary>
    void ShowRankPanel()
    {
        //获取GameScore数据对象，降序排列取前10个数据
        var param = new Dictionary<string, object>();
        param.Add("order", "-score");
        param.Add("limit", 10);
        StartCoroutine(_leanCloud.Query("GameScore", param, t =>
        {
            var results = JsonUtility.FromJson<QueryRankResult>(t);
            var scores = new List<KeyValuePair<string, string>>();

            //将数据转化为字符串
            foreach (var result in results.results)
            {
                scores.Add(
                    new KeyValuePair<string, string>(result.playerName, result.score.ToString()));
            }

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
        }));
    }
}