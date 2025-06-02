using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System.Text;
using System;

namespace FFramework
{
    /// <summary>
    /// 对话控制器
    /// TODO:实现自动播放
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        [Header("对话数据")]
        [Tooltip("对话数据文件")] public TextAsset dialogueDataFile;
        [Tooltip("Sprite数据")] public List<DialogueSpriteData> dialogueSpriteDatas = new();
        private Dictionary<string, Sprite> spriteDict = new();
        [Tooltip("是否使用本地化数据")][SerializeField] private bool isUseLocalizedData;
        [Tooltip("本地化数据(如果没有则获取全局本地化数据文件)")][SerializeField] private LocalizationData localizationData;
        private LocalizationManager LocalizationManager => LocalizationManager.Instance;        //本地化管理器
        public LocalizationData LocalizationData => localizationData != null ?
                                localizationData : LocalizationManager.GlobalLocalizationData;
        [Tooltip("当前对话索引值")]
        public int dialogueIndex = 0;
        private string[] dialogueRows;                                                          //对话数据,按行分隔
        private string speaker;                                                                 //说话者
        private string content;                                                                 //对话内容
        private Dictionary<Button, string> branchButtonDict = new();                            //分支选项按钮

        [Header("打字效果设置")]
        private StringBuilder displayedText = new StringBuilder(128);
        [Tooltip("每秒字符数")] public int typingSpeed = 10;
        private Coroutine typingCoroutine;                                                      // 保存对协程的引用

        [Header("UI")]
        [Tooltip("左侧Image")] public Image leftImage;
        [Tooltip("右侧Image")] public Image rightImage;
        [Tooltip("名称文本")] public Text nameText;
        [Tooltip("对话文本显示区域")] public Text dialogueContent;
        [Tooltip("自动播放按钮")] public Button autoplayButton;
        [Tooltip("下一个段对话按钮")] public Button nextButton;

        [Header("分支选项")]
        [Tooltip("分支选项组")] public Transform branchGroup;
        [Tooltip("分支选项预制体")] public GameObject branchButtonPrefab;

        private void Awake()
        {
            if (LocalizationManager != null)
                LocalizationManager.Register(OnLanguageChanged);
        }

        private void OnDestroy()
        {
            if (LocalizationManager != null)
                LocalizationManager.UnRegister(OnLanguageChanged);

            //清理协程
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
        }

        private void Start()
        {
            ReadDialogueDataFile(dialogueDataFile);
            ShowDialogueRow();

            //调用一次数据更新
            OnLanguageChanged(LocalizationManager.LanguageType);
            //下一段对话
            nextButton.onClick.AddListener(() =>
            {
                ShowDialogueRow();
            });
            //自动播放
            autoplayButton.onClick.AddListener(() =>
            {
                //TODO:实现自动播放
                Debug.Log("自动播放");
            });
        }

        #region 数据更新

        /// <summary>
        /// 更新图片
        /// </summary>
        private void UpdateImage(string name, string pos)
        {
            //隐藏所有图片
            leftImage.gameObject.SetActive(false);
            rightImage.gameObject.SetActive(false);
            Sprite sprite = null;
            if (spriteDict.ContainsKey(name))
            {
                sprite = spriteDict[name];
            }
            if (leftImage != null && pos == "Left")
            {
                leftImage.gameObject.SetActive(true);
                leftImage.sprite = sprite;
            }
            if (rightImage != null && pos == "Right")
            {
                rightImage.gameObject.SetActive(true);
                rightImage.sprite = sprite;
            }
        }

        //更新对话文本
        private void OnLanguageChanged(LanguageType type)
        {
            //更新文本
            UpdateText(speaker, content);
            // 更新所有分支按钮的文本
            UpdateAllBranchButtonsText();
        }

        //更新文本显示
        private void UpdateText(string speaker, string content)
        {
            if (isUseLocalizedData)
            {
                nameText.text = LocalizationData.GetTypeLanguageContent(LocalizationManager.LanguageType, speaker);
                StartTypingEffect(LocalizationData.GetTypeLanguageContent(LocalizationManager.LanguageType, content));
            }
            else
            {
                nameText.text = speaker;
                dialogueContent.text = content;
            }
        }

        #endregion

        #region 打字效果

        // 启动打字机效果协程
        public void StartTypingEffect(string content)
        {
            // 如果已有协程在运行，先停止它
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            // 启动新协程并保存引用
            typingCoroutine = StartCoroutine(TypeText(content));
        }

        //打字机效果协程
        private IEnumerator TypeText(string content)
        {
            displayedText.Clear();
            foreach (char text in content)
            {
                displayedText.Append(text);
                dialogueContent.text = displayedText.ToString();
                yield return new WaitForSeconds(1f / typingSpeed);
            }
        }

        // 更新特定分支按钮的文本
        private void UpdateBranchButtonText(Button button, string localizationKey)
        {
            if (isUseLocalizedData)
            {
                button.GetComponentInChildren<Text>().text = LocalizationData.GetTypeLanguageContent(LocalizationManager.LanguageType, localizationKey);
            }
            else
            {
                button.GetComponentInChildren<Text>().text = localizationKey;
            }
        }

        // 更新所有分支按钮的文本
        public void UpdateAllBranchButtonsText()
        {
            foreach (var pair in branchButtonDict)
            {
                Button button = pair.Key;
                string key = pair.Value;

                if (button != null)
                {
                    UpdateBranchButtonText(button, key);
                }
            }
        }

        #endregion

        #region 数据读取

        //读取对话数据文件
        private void ReadDialogueDataFile(TextAsset textAsset)
        {
            //清空数据
            dialogueRows = null;
            //按行读取数据
            dialogueRows = textAsset.text.Split('\n');
            //将对话数据添加到字典中
            foreach (var item in dialogueSpriteDatas)
            {
                spriteDict.Add(item.name, item.sprite);
            }
        }

        //显示对话行
        private void ShowDialogueRow()
        {
            for (int i = 0; i < dialogueRows.Length; i++)
            {
                string[] cols = dialogueRows[i].Split(',');

                //是对话数据行
                if (cols[0] == "Dialogue" && int.Parse(cols[1]) == dialogueIndex)
                {
                    this.speaker = cols[2];
                    this.content = cols[5];
                    //更新文本
                    UpdateText(cols[2], cols[5]);
                    //更新图片
                    UpdateImage(cols[2], cols[4]);
                    dialogueIndex = int.Parse(cols[6]);
                    nextButton.gameObject.SetActive(true);
                    //执行事件效果
                    ExecuteAction(cols[7]);
                    //跳出循环
                    break;
                }
                //是分支选项数据行
                else if (cols[0] == "Branch" && int.Parse(cols[1]) == dialogueIndex)
                {
                    nextButton.gameObject.SetActive(false);
                    GenerateBranchButton(i);
                }
                //是结束数据行
                else if (cols[0] == "End" && int.Parse(cols[1]) == dialogueIndex)
                {
                    Debug.Log("对话结束");
                    nextButton.gameObject.SetActive(false);
                }
            }
        }

        //生成分支选项
        //index -> CSV文件行索引
        private void GenerateBranchButton(int index)
        {
            string[] cols = dialogueRows[index].Split(',');
            if (cols[0] == "Branch")
            {
                Button branchButton = Instantiate(branchButtonPrefab, branchGroup).GetComponent<Button>();
                // 保存按钮引用和本地化键到字典中
                string localizationKey = cols[5];
                branchButtonDict[branchButton] = localizationKey;
                // 设置当前语言下的文本
                UpdateBranchButtonText(branchButton, localizationKey);
                //注册点击事件
                branchButton.onClick.AddListener(() =>
                {
                    //跳转到指定对话
                    OnBranchClick(int.Parse(cols[6]));
                    //执行事件效果
                    ExecuteAction(cols[7]);
                });

                //递归调用创建所有分支选项
                GenerateBranchButton(index + 1);
            }
        }

        //根据id跳转到指定对话
        private void OnBranchClick(int id)
        {
            dialogueIndex = id;
            ShowDialogueRow();

            //TODO:缓存分支选项
            foreach (Transform child in branchGroup)
            {
                branchButtonDict.Clear();
                Destroy(child.gameObject);
            }
        }

        #endregion

        #region 效果执行

        //执行Action
        private void ExecuteAction(string action)
        {
            if (!string.IsNullOrWhiteSpace(action))
            {
                action = Regex.Replace(action, @"[\r\n]", "");
                FindEffectAction(action).Execute();
            }
        }

        //获取效果事件
        private IBranchAction FindEffectAction(string action)
        {
            //使用缓存检查类型是否已找到过
            string fullTypeName = $"FFramework.{action}";
            Type type = null;
            if (!DialogueActionCache.typeCache.TryGetValue(fullTypeName, out type))
            {
                // 只在首次查找时使用反射
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(fullTypeName);
                    if (type != null)
                    {
                        // 验证类型是否实现了IBranchAction接口
                        if (!typeof(IBranchAction).IsAssignableFrom(type))
                        {
                            Debug.LogError($"Class {action} does not implement IBranchAction interface");
                            return null;
                        }

                        // 缓存找到的有效类型
                        DialogueActionCache.typeCache[fullTypeName] = type;
                        break;
                    }
                }

                if (type == null)
                {
                    Debug.LogError($"Class not found: <color=yellow>{action}</color>, make sure <color=yellow>IBranchAction interface</color> is implemented");
                    //缓存未找到的结果，避免再次查找
                    DialogueActionCache.typeCache[fullTypeName] = null;
                    return null;
                }
            }
            //之前已查找过但未找到
            else if (type == null)
            {
                Debug.LogError($"Class not found: <color=yellow>{action}</color>, make sure <color=yellow>IBranchAction interface</color> is implemented");
                return null;
            }

            //创建实例并执行
            IBranchAction branchAction = null;
            // 检查缓存中是否已有该类型的实例
            if (!DialogueActionCache.typeInstanceCache.TryGetValue(type, out branchAction))
            {
                branchAction = (IBranchAction)Activator.CreateInstance(type);
                DialogueActionCache.typeInstanceCache[type] = branchAction;
            }

            //执行操作
            return branchAction;
        }

        #endregion

#if UNITY_EDITOR

        [Button("添加Sprite数据")]
        private void AddDialogueData()
        {
            string[] newDialogueRows;
            newDialogueRows = dialogueDataFile.text.Split('\n');
            dialogueSpriteDatas.Clear();

            // 如果对话行数据为空，则直接返回
            if (newDialogueRows == null || newDialogueRows.Length == 0)
                return;

            // 按行读取数据
            for (int i = 0; i < newDialogueRows.Length; i++)
            {
                // 跳过空行或格式不正确的行
                if (string.IsNullOrWhiteSpace(newDialogueRows[i]))
                    continue;

                string[] cols = newDialogueRows[i].Split(',');
                // 确保数组长度足够
                if (cols.Length < 3)
                    continue;

                string name = cols[2];

                // 如果列表中没有才添加
                bool exists = false;
                foreach (var item in dialogueSpriteDatas)
                {
                    if (item.name == name)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists && cols[0] == "Dialogue")
                {
                    DialogueSpriteData newData = new DialogueSpriteData(name, null);
                    dialogueSpriteDatas.Add(newData);
                }
            }
        }

#endif

        [Serializable]
        public class DialogueSpriteData
        {
            public string name;
            public Sprite sprite;

            public DialogueSpriteData(string name, Sprite sprite)
            {
                this.name = name;
                this.sprite = sprite;
            }
        }
    }
}