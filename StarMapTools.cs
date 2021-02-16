using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StarMapTools
{
    [BepInPlugin("sky.plugins.dsp.StarMapTools", "StarMapTools", "1.3")]
    public class StarMapTools: BaseUnityPlugin
    {
        GameObject prefab_StarMapToolsBasePanel;//资源
        GameObject ui_StarMapToolsBasePanel;
        Dropdown StarList;//恒星下拉列表
        Dropdown PlanetList;//星球下拉列表
        Text TitleText;//标题
        InputField InfoText;//详细信息
        bool dataLoadOver = false;//是否加载完数据
        bool showGUI = false;//是否显示GUI
        bool keyLock = false;//按键锁
        bool loadingStarData = false;//是否在加载数据
        GalaxyData galaxy;//星图数据
        KeyCode switchGUIKey;//开关GUI的快捷键
        KeyCode tpKey;//tp的快捷键
        static StarMapTools self;
        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(StarMapTools), null);
            self = this;
            //加载资源
            switchGUIKey = Config.Bind<KeyCode>("config", "switchGUI", KeyCode.F1, "开关GUI的按键").Value;
            tpKey = Config.Bind<KeyCode>("config", "tp", KeyCode.F2, "传送按键").Value;
            var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("StarMapTools.starmaptools"));
            prefab_StarMapToolsBasePanel = ab.LoadAsset<GameObject>("StarMapToolsBasePanel");
        }
        void Update()
        {
            if (dataLoadOver)
            {
                //根据按键更新showGUI
                if (Input.GetKeyDown(switchGUIKey) && !keyLock)
                {
                    keyLock = true;
                    showGUI = !showGUI;
                }
                else if(Input.GetKeyUp(switchGUIKey) && keyLock)
                {
                    keyLock = false;
                }
                //根据showGUI更新GUI的显示
                if(showGUI && !ui_StarMapToolsBasePanel.activeSelf || !showGUI && ui_StarMapToolsBasePanel.activeSelf)
                {
                    ui_StarMapToolsBasePanel.SetActive(!ui_StarMapToolsBasePanel.activeSelf);
                }
                //核心内容
                if (showGUI)
                {
                    //判断是否处于新建游戏的状态
                    if (UIRoot.instance.galaxySelect.starmap.galaxyData != null)
                    {
                        //更新数据
                        if (galaxy != UIRoot.instance.galaxySelect.starmap.galaxyData)
                        {
                            TitleText.text = "新游戏模式";
                            galaxy = UIRoot.instance.galaxySelect.starmap.galaxyData;
                            StarList.ClearOptions();
                            foreach (StarData star in galaxy.stars)
                            {
                                StarList.options.Add(new Dropdown.OptionData(star.name));
                            }
                            StarList.value = -1;
                            StarList.RefreshShownValue();
                        }
                    }
                    //判断是否处于游戏中
                    else if (GameMain.galaxy != null)
                    {
                        //更新数据
                        if (galaxy != GameMain.galaxy)
                        {
                            TitleText.text = "读档模式";
                            galaxy = GameMain.galaxy;
                            StarList.ClearOptions();
                            foreach (StarData star in galaxy.stars)
                            {
                                StarList.options.Add(new Dropdown.OptionData(star.name));
                            }
                            StarList.value = -1;
                            StarList.RefreshShownValue();
                        }
                        //传送功能
                        if (Input.GetKeyDown(tpKey))
                        {
                            object target = galaxy.StarById(StarList.value + 1);
                            GameMain.data.ArriveStar((StarData)target);
                            if (PlanetList.value > 0)
                            {
                                target = ((StarData)target).planets[PlanetList.value - 1];
                            }
                            StartCoroutine(wait(target));
                        }
                    }
                    //等待数据加载
                    else if (TitleText.text != "等待数据")
                    {
                        TitleText.text = "等待数据";
                        StarList.ClearOptions();
                        PlanetList.ClearOptions();
                        InfoText.text = "";
                        galaxy = null;
                    }
                    if (loadingStarData)
                    {
                        if(galaxy.StarById(StarList.value + 1).loaded)
                        {
                            loadingStarData = false;
                        }
                        PlanetList.value = -1;
                        PlanetList.RefreshShownValue();
                    }
                }
            }
            //加载数据
            else if(UIRoot.instance.overlayCanvas.transform!=null && GameMain.instance!=null)
            {
                //加载UI
                ui_StarMapToolsBasePanel = GameObject.Instantiate(prefab_StarMapToolsBasePanel, UIRoot.instance.overlayCanvas.transform);
                ui_StarMapToolsBasePanel.transform.Find("TitleText").gameObject.AddComponent<Drag>();
                ui_StarMapToolsBasePanel.SetActive(false);
                //获取控件
                TitleText = ui_StarMapToolsBasePanel.transform.Find("TitleText").GetComponent<Text>();
                StarList =ui_StarMapToolsBasePanel.transform.Find("StarList").GetComponent<Dropdown>();
                PlanetList = ui_StarMapToolsBasePanel.transform.Find("PlanetList").GetComponent<Dropdown>();
                InfoText = ui_StarMapToolsBasePanel.transform.Find("InfoText").GetComponent<InputField>();
                //切换恒星事件
                StarList.onValueChanged.AddListener(delegate {
                    PlanetList.ClearOptions();
                    if (StarList.value>=0 && StarList.value < galaxy.starCount)
                    {
                        var star = galaxy.StarById(StarList.value + 1);
                        if (UIRoot.instance.galaxySelect.starmap.galaxyData != null && !star.loaded)
                        {
                            star.Load();
                        }
                        PlanetList.options.Add(new Dropdown.OptionData("恒星"));
                        foreach (PlanetData planet in galaxy.StarById(StarList.value+1).planets)
                        {
                            PlanetList.options.Add(new Dropdown.OptionData(planet.name));
                        }
                        PlanetList.value = -1;
                        PlanetList.RefreshShownValue();
                    }
                });
                //切换星球事件
                PlanetList.onValueChanged.AddListener(delegate {
                    if(PlanetList.value>0 && PlanetList.value <= galaxy.StarById(StarList.value+1).planetCount)
                    {
                        var planet = galaxy.StarById(StarList.value+1).planets[PlanetList.value - 1];
                        var info = planet.name+"的信息:\n";
                        info += "词条:" + planet.singularityString + "\n";//词条
                        info += "类型:" + planet.typeString + "\n";
                        string waterType = "未知";
                        switch (planet.waterItemId)
                        {
                            case -1:
                                waterType = "熔岩";
                                break;
                            case 0:
                                waterType = "无";
                                break;
                            default:
                                waterType = LDB.ItemName(planet.waterItemId);
                                break;
                        }
                        info += "海洋类型:" + waterType + "\n";
                        if(planet.type!= EPlanetType.Gas && planet.veinSpotsSketch!=null)
                        {
                            info += "矿物信息:" + "\n";
                            for(int i = 0; i <LDB.veins.Length; i++)
                            {
                                var name = LDB.ItemName(LDB.veins.dataArray[i].MiningItem);
                                object amount = planet.veinAmounts[i + 1];
                                if (planet.veinSpotsSketch[i+1]==0)
                                {
                                    amount = "无";
                                }
                                else if ((long)amount == 0)
                                {
                                    if (UIRoot.instance.galaxySelect.starmap.galaxyData != null)
                                    {
                                        amount = "正在加载";
                                        loadingStarData = true;
                                    }
                                    else
                                    {
                                        amount = "未加载,靠近后显示";
                                    }
                                }
                                else if (i + 1 == 7)
                                {
                                    amount = (long)amount * (double)VeinData.oilSpeedMultiplier + " /s";
                                }
                                info += "    " + name + ":" + amount + "\n";
                            }
                        }
                        InfoText.text = info;
                    }
                    else if (PlanetList.value == 0)
                    {
                        var star = galaxy.StarById(StarList.value+1);
                        var info = star.name + "星系的信息:\n";
                        info += "恒星类型:" + star.typeString + "\n";
                        info += "星球数量:" + star.planetCount + "\n";
                        info += "光度:" + star.dysonLumino.ToString() + "\n";
                        info += "距离初始星系恒星:" + ((star.uPosition - galaxy.StarById(1).uPosition).magnitude / 2400000.0).ToString()+"光年\n";
                        info += "矿物信息:" + "\n";
                        for (int i = 0; i < LDB.veins.Length; i++)
                        {
                            var name = LDB.ItemName(LDB.veins.dataArray[i].MiningItem);
                            object amount = star.GetResourceAmount(i + 1);
                            if (star.GetResourceSpots(i + 1) == 0)
                            {
                                amount = "无";
                            }
                            else if ((long)amount == 0)
                            {
                                if(UIRoot.instance.galaxySelect.starmap.galaxyData != null)
                                {
                                    amount = "正在加载";
                                    loadingStarData = true;
                                }
                                else
                                {
                                    amount = "未加载,靠近后显示";
                                }
                            }
                            else if (i + 1 == 7)
                            {
                                amount = (long)amount * (double)VeinData.oilSpeedMultiplier + " /s";
                            }
                            info += "    " + name + ":" + amount + "\n";
                        }
                        InfoText.text = info;
                    } 
                });
                dataLoadOver = true;
            }
        }
        IEnumerator wait(object target)
        {
            yield return new WaitForEndOfFrame();//等待帧结束
            //传送
            if (target is PlanetData)
            {
                GameMain.mainPlayer.uPosition =((PlanetData)target).uPosition + VectorLF3.unit_z * (((PlanetData)target).realRadius);
            }
            else if(target is StarData)
            {
                GameMain.mainPlayer.uPosition = ((StarData)target).uPosition + VectorLF3.unit_z * (((StarData)target).physicsRadius);
                loadingStarData = true;
            }else if(target is VectorLF3)
            {
                GameMain.mainPlayer.uPosition = (VectorLF3)target;
            }
            else if(target is string && (string)target == "resize")
            {
                GameMain.mainPlayer.transform.localScale = Vector3.one;
            }
            if (!(target is string) || (string)target != "resize")
            {
                StartCoroutine(wait("resize"));
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), "OnStarClick")]
        private static bool OnStarClick(UIStarmapStar star)
        {
            self.StarList.value = star.star.index;
            self.StarList.RefreshShownValue();
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), "OnPlanetClick")]
        private static bool OnPlanetClick(UIStarmapPlanet planet)
        {
            self.StarList.value = planet.planet.star.index;
            self.StarList.RefreshShownValue();
            self.PlanetList.value = planet.planet.index+1;
            self.PlanetList.RefreshShownValue();
            return true;
        }
    }
    class Drag : MonoBehaviour
    {
        RectTransform rt;
        RectTransform parent;
        RectTransform canvas;
        Vector3 lastPosition;
        bool drag = false;
        void Start()
        {
            rt = GetComponent<RectTransform>();//标题栏的rt
            parent = rt.parent.GetComponent<RectTransform>();//BasePanel的rt
            canvas = parent.parent.GetComponent<RectTransform>();//canvas的rt
        }
        void Update()
        {
            //获取鼠标在游戏窗口的unity坐标
            var m = Input.mousePosition - Vector3.right * Screen.width / 2 - Vector3.up * Screen.height / 2;
            m.x *= canvas.sizeDelta.x / Screen.width;
            m.y *= canvas.sizeDelta.y / Screen.height;
            //获取标题在游戏窗口内的坐标
            var rp = parent.localPosition + rt.localPosition;
            //获取标题的rect
            var re = rt.rect;
            //判断鼠标是否在标题的范围内按下
            if (m.x >= rp.x - re.width / 2 && m.x <= rp.x + re.width / 2 && m.y >= rp.y - re.height / 2 && m.y <= rp.y + re.height / 2 && Input.GetMouseButtonDown(0))
            {
                drag = true;
                lastPosition = m;
            }
            //获取鼠标是否松开
            else if (drag && Input.GetMouseButtonUp(0))
            {
                drag = false;
            }
            //根据鼠标的拖动更新窗口位置
            if (drag)
            {
                parent.localPosition += m - lastPosition;
                lastPosition = m;
            }
        }
    }
}
