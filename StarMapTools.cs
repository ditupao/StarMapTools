using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StarMapTools
{
    [BepInPlugin("sky.plugins.dsp.StarMapTools", "StarMapTools", "1.0")]
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
        GalaxyData galaxy;//星图数据
        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(StarMapTools), null);
            //加载资源
            var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("StarMapTools.starmaptools"));
            prefab_StarMapToolsBasePanel = ab.LoadAsset<GameObject>("StarMapToolsBasePanel");
        }
        void Update()
        {
            if (dataLoadOver)
            {
                //根据按键更新showGUI
                if (Input.GetKeyDown(KeyCode.F1) && !keyLock)
                {
                    keyLock = true;
                    showGUI = !showGUI;
                }
                else if(Input.GetKeyUp(KeyCode.F1) && keyLock)
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
                        if (Input.GetKeyDown(KeyCode.F2))
                        {
                            object target = galaxy.StarById(StarList.value + 1);
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
                            info += "矿脉数量信息:" + "\n";
                            for(int i = 0; i <LDB.veins.Length; i++)
                            {
                                var name = LDB.ItemName(LDB.veins.dataArray[i].MiningItem);
                                var count = planet.veinSpotsSketch[i+1];
                                info += "    "+name + ":" + count + "\n";
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
                        info += "矿脉数量信息:" + "\n";
                        for (int i = 0; i < LDB.veins.Length; i++)
                        {
                            var name = LDB.ItemName(LDB.veins.dataArray[i].MiningItem);
                            var count = star.GetResourceSpots(i+1);
                            info += "    " + name + ":" + count + "\n";
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
            else
            {
                GameMain.mainPlayer.uPosition = ((StarData)target).uPosition + VectorLF3.unit_z * (((StarData)target).physicsRadius);
            }
            //不知为何player的localScale有时会变大,此处恢复原比例
            GameMain.mainPlayer.transform.localScale = Vector3.one;
        }
    }
}
