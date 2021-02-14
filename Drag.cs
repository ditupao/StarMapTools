using UnityEngine;

namespace StarMapTools
{
    public class Drag : MonoBehaviour
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
            var m = Input.mousePosition-Vector3.right*Screen.width/2-Vector3.up*Screen.height/2;
            m.x *= canvas.sizeDelta.x/ Screen.width;
            m.y *= canvas.sizeDelta.y/ Screen.height;
            //获取标题在游戏窗口内的坐标
            var rp = parent.localPosition+rt.localPosition;
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
