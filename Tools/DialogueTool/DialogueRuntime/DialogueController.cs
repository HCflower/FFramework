using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace DialogueTool
{
    /// <summary>
    /// 对话控制器
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        [Header("对话数据")]
        [Tooltip("对话数据文件")] public TextAsset dialogueDataFile;
        [Tooltip("当前对话索引值")] public int dialogueIndex = 0;
        [Tooltip("对话数据")] public List<DialogueSpriteData> dialogueDatas = new();
        private Dictionary<string, Sprite> spriteDict = new();
        //对话数据,按行分隔
        private string[] dialogueRows;

        [Header("UI")]
        [Tooltip("左侧Image")] public Image leftImage;
        [Tooltip("右侧Image")] public Image rightImage;
        [Tooltip("名称文本")] public Text nameText;
        [Tooltip("对话文本显示区域")] public Text dialogueContent;
        [Tooltip("下一个段对话按钮")] public Button nextButton;

        [Header("分支选项")]
        [Tooltip("分支选项组")] public Transform branchGroup;
        [Tooltip("分支选项预制体")] public GameObject branchButtonPrefab;

        private void Start()
        {
            ReadDialogueDataFile(dialogueDataFile);
            ShowDialogueRow();
            nextButton.onClick.AddListener(ShowDialogueRow);
        }

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
        private void UpdateDialogueText(string name, string content)
        {
            nameText.text = name;
            dialogueContent.text = content;
        }

        //读取对话数据文件
        private void ReadDialogueDataFile(TextAsset textAsset)
        {
            //清空数据
            dialogueRows = null;
            //按行读取数据
            dialogueRows = textAsset.text.Split('\n');
            //将对话数据添加到字典中
            foreach (var item in dialogueDatas)
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
                    UpdateDialogueText(cols[2], cols[5]);
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
                //设置文本
                branchButton.GetComponentInChildren<Text>().text = cols[5];
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

            //TODO:隐藏分支选项
            foreach (Transform child in branchGroup)
            {
                Destroy(child.gameObject);
            }
        }

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
            string fullTypeName = $"DialogueTool.{action}";
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

#if UNITY_EDITOR

        [Button("添加对话数据")]
        private void AddDialogueData()
        {
            string[] newDialogueRows;
            newDialogueRows = dialogueDataFile.text.Split('\n');
            dialogueDatas.Clear();

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
                foreach (var item in dialogueDatas)
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
                    dialogueDatas.Add(newData);
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