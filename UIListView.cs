using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIListView : MonoBehaviour, IDragHandler, IEndDragHandler
{

    public enum AxisType
    {
        horizontal,
        vertical
    }
    //拉动后自动对齐
    public bool autofocus = true;
    [HideInInspector]
    public ScrollRect.MovementType movementType = ScrollRect.MovementType.Elastic;
    public float AFSensitivity = 1000.0f;
    public AxisType axis = AxisType.horizontal;
    public Vector2 cellSize;//item大小
    public Vector2 spacing;//item间隔
    /// <summary> 最大的行数 </summary>
    public int maxColumn = 1;
    public GameObject prefabObject;//预设对象
    [HideInInspector]
    /// <summary> 为了支持滑动 和 Item的拖拽 </summary>
    public bool isUseItemInterface = false;
    /// <summary> 区别滑动和拖拽的夹角度 </summary>
    public float itemInterfaceAngle = 10f;
    /// <summary> 当前选择的Item(为了同时支持滑动 和 Item的拖拽) </summary>
    public UIListViewItemInterface _theSelectItemInterface;

    public UIListViewItemInterface theSelectItemInterface
    {
        get { return _theSelectItemInterface; }
        set
        {
            _theSelectItemInterface = value;
            isSlide = false;
        }
    }

    /// <summary> 显示列表 </summary>
    private List<RectTransform> showList = new List<RectTransform>();
    /// <summary> 显示列表showList的头ID </summary>
    private int showListHead = 0;

    public int GetShowListIndex(int i)
    {
        return (showListHead + i + showList.Count) % showList.Count;
    }

    private int maxLastIndex = 0;
    /// <summary> 当前mask中 最大可显示的行列数 </summary>
    private int showRol;
    /// <summary> 每个item的间隔 </summary>
    private float fixedInterval;
    /// <summary> 外部数据 </summary>
    private IList data = null;
    /// <summary> 用来处理拖动到末尾的一些逻辑(这东西并不会每次都会调用，只有当拖动索引发生改变的时候才会调用) </summary>
    public System.Action onLastCallback;
    /// <summary> 这东西并不会每次调用，只有当拖动索引发生改变时，会回调此函数，此函数会告知你哪些预设对象需要做数据更新处理 </summary>
    public System.Action<GameObject, object, int> setValueCallback;
    /// <summary> 这个玩意儿要是指定的话，即使超出显示范围,或者数据为空时item对象依旧不会被隐藏，由指定回调函数来托管处理,反之不指定将隐藏没使用的预设对象 </summary>
    public System.Action<GameObject> setNullCallback;

    //---------临时变量------------
    private ScrollRect scrollRect;
    private RectTransform dragRect;
    private RectTransform maskRect;
    private int lastIndex;
    private Vector2 halfSpacing;
    private bool onCenter = true;
    private bool dragTrue = false;

    /// <summary> 必须调用的初始化函数 </summary>
    /// <param name="cellSize">Item Size</param>
    /// <param name="spacing">间隔Size</param>
    /// <param name="maxColumn">最大数量</param>
    /// <param name="prefabObject">Item对象</param>
    /// <param name="axis">水平还是竖直滑动</param>
    /// <param name="autofocus">是否自动对齐到格子</param>
    /// <param name="isUseItemInterface">是否使用ItemInterface(为了同时支持滑动 和 Item的拖拽)</param>
    /// <param name="itemInterfaceAngle">滑动与拖拽的分别角度</param>
    /// <param name="movementType">滑动类型</param>
    public void InitListView(Vector2 cellSize, Vector2 spacing, int maxColumn, GameObject prefabObject, AxisType axis = AxisType.vertical, bool autofocus = false, bool isUseItemInterface = false, float itemInterfaceAngle = 70f, ScrollRect.MovementType movementType = ScrollRect.MovementType.Elastic)
    {
        this.cellSize = cellSize;
        this.spacing = spacing;
        this.maxColumn = maxColumn;
        this.axis = axis;
        this.prefabObject = prefabObject;
        this.isUseItemInterface = isUseItemInterface;
        this.itemInterfaceAngle = itemInterfaceAngle;
        this.autofocus = autofocus;
        this.movementType = movementType;
        if (scrollRect != null) scrollRect.movementType = this.movementType;

        halfSpacing = spacing * 0.5f;
        halfSpacing.y = -halfSpacing.y;

        isInitListViewed = true;

        initAfterAwaked();
    }

    void Awake()
    {
        scrollRect = this.gameObject.GetComponent<ScrollRect>();
        maskRect = scrollRect.GetComponent<RectTransform>();
        dragRect = scrollRect.content;
        SetUpLeft(dragRect);
        scrollRect.onValueChanged.AddListener(OnDragPanel);
        scrollRect.movementType = this.movementType;

        isAwaked = true;
        initAfterAwaked();
    }

    bool isAwaked = false;
    bool isInitListViewed = false;

    int tempStartIndex = 0;
    int tempInitItemNum = 0;

    void initAfterAwaked()
    {
        if(isAwaked && isInitListViewed)
        {
            if (data == null)
            {
                ShowData(new List<int>());
            }
            else
            {
                ShowData(data, tempStartIndex, tempInitItemNum);
            }
        }
    }

    //显示数据
    public void ShowData(IList iData, int startIndex = 0,int initItemNum = 0)
    {
        data = iData;
        this.tempStartIndex = startIndex;
        this.tempInitItemNum = initItemNum;

        if (!isAwaked || !isInitListViewed)
        {
            return;
        }

        scrollRect.horizontal = axis == AxisType.horizontal;
        scrollRect.vertical = axis == AxisType.vertical;

        fixedInterval = axis == AxisType.horizontal ? spacing.x + cellSize.x : spacing.y + cellSize.y;

        if (initItemNum <= 0)
        {
            showRol = Mathf.CeilToInt(axis == AxisType.horizontal ? maskRect.sizeDelta.x / fixedInterval : maskRect.sizeDelta.y / fixedInterval);
        }
        else
        {
            showRol = initItemNum;
        }

        UpdateMaxIndex();
        SetIndexFromDataIndex(startIndex);
        InitContentTrans();
        InitPrefabs(showRol + 1);
        InitGridsPosition();
        RefreshShowGrids();
    }

    public void ClearData()
    {
        if (data!=null)
            data.Clear();
        if (showList != null&&showList.Count>0)
        {
            foreach (var obj in showList)
            {
                GameObject.Destroy(obj.gameObject);
            }
        }
        if (showList!=null)
            showList.Clear();
        Reset(0);
    }

    private void InitGridsPosition()
    {
        Vector2 pos = Vector2.zero;
        int row = 0;
        int col = 0;
        //--------------calculate grid pos------------------
        int showListIndex = 0;
        for (int i = 0; i < showList.Count; i++)
        {
            showListIndex = GetShowListIndex(i);
            showList[showListIndex].sizeDelta = cellSize;

            if (axis == AxisType.horizontal)
            {
                row = i % maxColumn;
                col = Mathf.CeilToInt(i / maxColumn) + lastIndex;
                pos.x = col * cellSize.x + col * spacing.x;
                pos.y = row * cellSize.y + row * spacing.y;
                pos.y = -pos.y;
                showList[showListIndex].anchoredPosition = pos + halfSpacing;
            }
            else
            {
                col = i % maxColumn;
                row = Mathf.CeilToInt(i / maxColumn) + lastIndex;
                pos.x = col * cellSize.x + col * spacing.x;
                pos.y = row * spacing.y + row * cellSize.y;
                pos.y = -pos.y;
                showList[showListIndex].anchoredPosition = pos + halfSpacing;
            }
        }
    }

    //初始化预设数量
    private void InitPrefabs(int cacheLine)
    {
        int count = cacheLine * maxColumn - showList.Count;
        for (; count < 0; count++)
        {
            Destroy(showList[0].gameObject);
            showList.RemoveAt(0);
        }
        showListHead = 0;
        for (int i = 0; i < count; i++)
        {
            //if (i >= data.Count && data.Count>0)
            //    break;
            GameObject _prefab = Instantiate(prefabObject) as GameObject;
            _prefab.SetActive(false);
            _prefab.transform.SetParent(dragRect, false);
            //如果使用ItemInterface
            if(isUseItemInterface)
            {
                UIListViewItemInterface tempItemInterface = _prefab.GetComponent<UIListViewItemInterface>();
                if(tempItemInterface!=null)
                {
                    tempItemInterface.SetUIListView(this);
                }
            }
            RectTransform preRect = _prefab.GetComponent<RectTransform>();
            SetUpLeft(preRect);
            showList.Add(preRect);
            //cacheQueue.Enqueue(preRect);
        }
    }

    private void SetUpLeft(RectTransform rectObject)
    {
        Vector2 upLeft = new Vector2(0, 1);
        rectObject.anchorMin = upLeft;
        rectObject.anchorMax = upLeft;
        rectObject.pivot = upLeft;
    }

    //计算Content的大小和位置
    private void InitContentTrans()
    {
        if (data==null)return;
        //初始化拖动面板的属性，让他根据mask的大小居中显示
        int maxRow = Mathf.CeilToInt(data.Count / (float)maxColumn);

        Vector2 size = Vector2.zero;
        Vector2 pos = Vector2.zero;
        if (axis == AxisType.horizontal)
        {
            size.x = maxRow * fixedInterval;
            size.y = maxColumn * (spacing.y + cellSize.y);

            pos.x -= lastIndex * fixedInterval;
            pos.y = maskRect.sizeDelta.y * 0.5f - size.y * 0.5f;
            pos.y = -pos.y;
        }
        else
        {
            size.x = maxColumn * (spacing.x + cellSize.x);
            size.y = maxRow * fixedInterval;

            pos.x = maskRect.sizeDelta.x * 0.5f - size.x * 0.5f;
            pos.y = lastIndex * fixedInterval;
        }
        dragRect.sizeDelta = size;
        dragRect.anchoredPosition = pos;
    }

    public void StopScrollRectScroll()
    {
        scrollRect.velocity = Vector2.zero;//停止物理滑动
    }

    private void UpdateToCenter()
    {
        if (scrollRect.velocity.sqrMagnitude > AFSensitivity)
        {
            return;
        }
        scrollRect.velocity = Vector2.zero;//停止物理滑动
        if (axis == AxisType.horizontal)
        {
            float pos = dragRect.anchoredPosition.x;
            pos = pos % fixedInterval;
            pos = pos * pos - (fixedInterval * 0.5f) * (fixedInterval * 0.5f);
            int newIndex = (pos < 0 ? lastIndex : lastIndex + 1);
            newIndex = Mathf.Clamp(newIndex, 0, maxLastIndex);
            pos = newIndex * -fixedInterval;
            Vector2 newPos = dragRect.anchoredPosition;
            newPos.x = Mathf.Lerp(dragRect.anchoredPosition.x, pos, Time.deltaTime * 10);
            float newVel = dragRect.anchoredPosition.x - newPos.x;
            if (newVel * newVel < 0.001f)
            {
                newPos.x = pos;
                onCenter = true;
            }
            dragRect.anchoredPosition = newPos;
        }
        else
        {
            float pos = dragRect.anchoredPosition.y;
            pos = pos % fixedInterval;
            pos = pos * pos - (fixedInterval * 0.5f) * (fixedInterval * 0.5f);
            int newIndex = (pos < 0 ? lastIndex : lastIndex + 1);
            newIndex = Mathf.Clamp(newIndex, 0, maxLastIndex);
            pos = newIndex * fixedInterval;
            Vector2 newPos = dragRect.anchoredPosition;
            newPos.y = Mathf.Lerp(dragRect.anchoredPosition.y, pos, Time.deltaTime * 10);
            float newVel = dragRect.anchoredPosition.y - newPos.y;
            if (newVel * newVel < 0.001f)
            {
                newPos.y = pos;
                onCenter = true;
            }
            dragRect.anchoredPosition = newPos;
        }

    }

    //检测滑动的回调函数
    private void OnDragPanel(Vector2 delay)
    {
        int _index = GetIndexFromPosition();
        if (_index != lastIndex)
        {
            isSlide = true;
            int count = Mathf.Abs(_index - lastIndex);//根据滑动的索引距离更新次数
            for (int i = 0; i < count; i++)
            {
                float dir = _index > lastIndex ? -1 : 1;//滑动方向  -1是左面 1是右面   
                int _needRepairIndex = dir == 1 ? showList.Count - 1 : 0;//如果是像右面滑动，那么肯定需要修改列表最后面的元素
                int _referenceIndex = dir == 1 ? 0 : showList.Count - 1;//取相反方向的元素对象用来做参考
                float offset = _index > lastIndex ? fixedInterval : -fixedInterval;
                RectTransform referenceObject = showList[GetShowListIndex(_referenceIndex)];//找到参照物对象
                for (int j = 0; j < maxColumn; j++)
                {
                    RectTransform needRepairObject = showList[GetShowListIndex(_needRepairIndex)]; //找到需要做修改处理的对象
                    //showList.Remove(needRepairObject);

                    if (axis == AxisType.horizontal)
                        needRepairObject.anchoredPosition = new Vector2(referenceObject.anchoredPosition.x + offset, needRepairObject.anchoredPosition.y);
                    else
                        needRepairObject.anchoredPosition = new Vector2(needRepairObject.anchoredPosition.x, referenceObject.anchoredPosition.y - offset);

                    if (dir == 1)//往右面滑动
                    {
                        //把最后的元素插入到最前面去
                        //showList.Insert(0, needRepairObject);
                        showListHead = (showListHead - 1) % showList.Count;
                        RefreshIndex(0, (lastIndex - i - 1) * maxColumn + (maxColumn - j - 1));
                    }
                    else
                    {
                        //把前面的元素丢到最后面去
                        //showList.Add(needRepairObject);
                        showListHead = (showListHead + 1) % showList.Count;
                        RefreshIndex(showList.Count - 1, (lastIndex + i + showRol + 1) * maxColumn + j);
                    }
                }
            }
            if (_index == maxLastIndex)//拖动到末尾了
            {
                if (onLastCallback != null)
                    onLastCallback();
            }
        }

        lastIndex = _index;
    }

    private int GetIndexFromPosition()
    {
        int index = Mathf.FloorToInt(axis == AxisType.horizontal ? -dragRect.anchoredPosition.x / fixedInterval : dragRect.anchoredPosition.y / fixedInterval);
        index = Mathf.Clamp(index, 0, maxLastIndex);

        return index;
    }

    private void RefreshIndex(int prefabIndex, int dataIndex)
    {
        GameObject prefabObject = showList[GetShowListIndex(prefabIndex)].gameObject;
        if (dataIndex >= data.Count)
        {
            prefabObject.SetActive(setNullCallback != null);
            if (setNullCallback != null)
            {
                setNullCallback(prefabObject);
            }
            return;
        }
        prefabObject.SetActive(true);
        if (setValueCallback != null)
            setValueCallback(prefabObject, data[dataIndex], dataIndex);
    }

    private void SetIndexFromDataIndex(int startIndex)
    {
        if(data==null)return;
        lastIndex = Mathf.FloorToInt(startIndex / (float)maxColumn);
        if (data.Count >= showRol)
        {
            lastIndex = Mathf.Clamp(lastIndex, 0, maxLastIndex + 2);
        }
        else
        {
            lastIndex = Mathf.Clamp(lastIndex, 0, maxLastIndex);
        }
    }

    private void UpdateMaxIndex()
    {
        if (data==null)return;
        maxLastIndex = Mathf.CeilToInt(data.Count / (float)maxColumn) - showRol;
        maxLastIndex = maxLastIndex < 0 ? 0 : maxLastIndex;
    }
    /// <summary>
    /// 如果data只是数据更新，那么可以调用此函数刷新数据
    /// </summary>
    public void RefreshShowGrids()
    {
        if (showList==null)return;
        for (int i = 0; i < showList.Count; i++)
        {
            RefreshIndex(i, i + lastIndex * maxColumn);
        }
    }
    /// <summary>
    /// 跳转到索引
    /// </summary>
    /// <param name="startIndex">跳转到某个位置(从0开始)</param>
    /// <returns></returns>
    public void GotoIndex(int startIndex)
    {
        SetIndexFromDataIndex(startIndex);
        InitGridsPosition();
        RefreshShowGrids();
    }
    /// <summary>
    /// 得到当前列表显示索引
    /// </summary>
    public int GetLastIndex()
    {
        return lastIndex;
    }
    /// <summary>
    /// 如果data数量已经变动，调用次函数刷新重置
    /// </summary>
    public void Reset(int startIndex = 0)
    {
        UpdateMaxIndex();
        SetIndexFromDataIndex(startIndex);
        InitContentTrans();
        InitGridsPosition();
        RefreshShowGrids();
    }
    /// <summary>
    /// 如果data列表已经改变，调用次函数刷新重置,如果改变轴向，或者改变mask大小，请调用InitListView
    /// </summary>
    public void Reset(IList pList, System.Action<GameObject, object, int> pIC, int startIndex = 0)
    {
        data = pList;
        setValueCallback = pIC;
        Reset(startIndex);
    }

    public Vector2 m_dragStart;
    public Vector2 m_dragEnd;

    [HideInInspector]
    /// <summary> 拖拽Item 而不是滑动 </summary>
    public bool m_DragUp = false;
    [HideInInspector]
    /// <summary> 本次为滑动，而不是拖动 </summary>
    public bool isSlide = false;

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragTrue)
        {
            m_dragStart = eventData.position;
        }
        m_dragEnd = eventData.position;

        if (isUseItemInterface)
        {
            //临时处理增加上拉模式
            if (m_DragUp)
            {
                if (theSelectItemInterface != null)
                {
                    theSelectItemInterface.UIListViewItem_OnDrag(eventData);
                }
            }
            else if (!isSlide)
            {

                #region 若当次拖动 还不是滑动 可判断是否为拖拽

                if (theSelectItemInterface != null && theSelectItemInterface.IsGOActiveTrue())
                {
                    Vector2 dir = m_dragEnd - m_dragStart;
                    float angle = 90f;
                    if (this.axis == AxisType.horizontal)
                    {
                        angle = Vector2.Angle(dir, new Vector2(0, 1));
                    }
                    else if (this.axis == AxisType.vertical)
                    {
                        angle = Vector2.Angle(dir, new Vector2(1, 0));
                    }

                    if (angle < itemInterfaceAngle|| angle>(180- itemInterfaceAngle))//上拉或者下拉<70或者>90+（90-70）=180-70
                    {
                        //拖拽Item 而不是滑动
                        m_DragUp = true;
                        //Debug.Log(gameObject.name + "___拖拽Item 而不是滑动");
                        scrollRect.content = null;
                        if (theSelectItemInterface != null && theSelectItemInterface.IsGOActiveTrue())
                        {
                            theSelectItemInterface.UIListViewItem_OnBeginDrag(eventData);
                        }
                    }
                }

                #endregion

                #region 若当次拖动没有判断为拖拽则判断是否为滑动

                if (!m_DragUp)
                {
                    Vector2 dir = m_dragEnd - m_dragStart;
                    if (this.axis == AxisType.horizontal)
                    {
                        if (Mathf.Abs(dir.x) > Mathf.Abs(cellSize.x))
                        {
                            isSlide = true;
                        }
                    }
                    else if (this.axis == AxisType.vertical)
                    {
                        if (Mathf.Abs(dir.y) > Mathf.Abs(cellSize.y))
                        {
                            isSlide = true;
                        }
                    }
                }

                #endregion

            }

        }

        dragTrue = true;
        onCenter = false;

    }

    public System.Action<PointerEventData> NormalEndDrag;
    public void OnEndDrag(PointerEventData eventData)
    {
        dragTrue = false;
        //isSlide = false;
        if (m_DragUp) 
        {
            //上拉模式结束
            m_DragUp = false;
            scrollRect.content = dragRect;
            if (theSelectItemInterface != null)
            {
                theSelectItemInterface.UIListViewItem_OnEndDrag(eventData);
            }
        }
        else
        {
            if (NormalEndDrag != null)
            {
                NormalEndDrag(eventData);
            }
        }
    }

    void Start()
    {

    }

    void Update()
    {
        if (autofocus && !dragTrue && !onCenter)
        {
            UpdateToCenter();
        }
    }
}
