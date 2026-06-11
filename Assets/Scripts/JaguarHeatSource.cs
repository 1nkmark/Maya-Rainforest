//////using System.Collections.Generic;
//////using UnityEngine;
//////using HeatmapVisualization; // 引入热力图的命名空间

//////public class JaguarHeatSource : MonoBehaviour
//////{
//////    [Header("把场景里的 Heatmap 物体拖进来")]
//////    public Heatmap heatmapEngine; [Header("把美洲豹/毒蛇拖进来")]
//////    public Transform[] animals; [Header("每个动物散发的热量点数量 (越多越红)")]
//////    public int pointsPerAnimal = 50;

//////    [Header("热量散发半径 (米)")]
//////    public float heatRadius = 0.5f;

//////    //void Start()
//////    //{
//////    //    // 游戏一开始就生成热力图
//////    //    RefreshHeatmap();
//////    //}

//////    void Start()
//////    {
//////        // 改为重复调用，每0.5秒刷新一次美洲豹的位置
//////        InvokeRepeating("RefreshHeatmap", 0.5f, 0.5f);
//////    }

//////    [ContextMenu("手动刷新热力图")]
//////    public void RefreshHeatmap()
//////    {
//////        if (heatmapEngine == null) return;

//////        List<Vector3> heatPoints = new List<Vector3>();

//////        // 遍历所有动物
//////        foreach (Transform animal in animals)
//////        {
//////            if (animal == null) continue;

//////            // 在动物身体周围，随机撒一批点，制造一团“热气”
//////            for (int i = 0; i < pointsPerAnimal; i++)
//////            {
//////                // Random.insideUnitSphere 会在半径为1的球体内随机取点
//////                Vector3 randomPoint = animal.position + Random.insideUnitSphere * heatRadius;
//////                heatPoints.Add(randomPoint);
//////            }
//////        }

//////        // 把计算好的真实动物热源位置，喂给热力图引擎
//////        heatmapEngine.GenerateHeatmap(heatPoints);
//////    }
//////}


////using System.Collections.Generic;
////using UnityEngine;
////using HeatmapVisualization;

////public class JaguarHeatSource : MonoBehaviour
////{
////    [Header("把场景里的 Heatmap 物体拖进来")]
////    public Heatmap heatmapEngine;

////    [Header("把美洲豹/毒蛇拖进来")]
////    public Transform[] animals;

////    [Header("每个动物散发的热量点数量 (越多越红)")]
////    public int pointsPerAnimal = 50;

////    [Header("热量散发半径 (米)")]
////    public float heatRadius = 0.5f;

////    void Start()
////    {
////        // 逻辑不变：每0.5秒刷新一次位置
////        InvokeRepeating("RefreshHeatmap", 0.5f, 0.5f);
////    }

////    [ContextMenu("手动刷新热力图")]
////    public void RefreshHeatmap()
////    {
////        if (heatmapEngine == null) return;

////        List<Vector3> heatPoints = new List<Vector3>();

////        foreach (Transform animal in animals)
////        {
////            if (animal == null) continue;

////            // --- 以下是唯一改动点：从“中心采样”升级为“轮廓采样” ---
////            MeshFilter mf = animal.GetComponentInChildren<MeshFilter>();

////            // 鲁棒性检查：如果有网格，就按轮廓撒点；如果没有，就按原版中心撒点
////            if (mf != null && mf.sharedMesh != null)
////            {
////                Vector3[] vertices = mf.sharedMesh.vertices;
////                for (int i = 0; i < pointsPerAnimal; i++)
////                {
////                    // 随机选一个顶点并转换到世界坐标
////                    Vector3 localVtx = vertices[Random.Range(0, vertices.Length)];
////                    Vector3 worldVtx = animal.TransformPoint(localVtx);

////                    // 加上你原有的 heatRadius 偏移，让热气看起来更自然
////                    heatPoints.Add(worldVtx + Random.insideUnitSphere * heatRadius);
////                }
////            }
////            else
////            {
////                // 兜底逻辑：完全保留你原版的中心撒点逻辑，确保万无一失
////                for (int i = 0; i < pointsPerAnimal; i++)
////                {
////                    heatPoints.Add(animal.position + Random.insideUnitSphere * heatRadius);
////                }
////            }
////            // --- 改动结束 ---
////        }

////        heatmapEngine.GenerateHeatmap(heatPoints);
////    }
////}


//using System.Collections.Generic;
//using UnityEngine;
//using HeatmapVisualization;

//public class JaguarHeatSource : MonoBehaviour
//{
//    public Heatmap heatmapEngine;
//    public Transform[] animals;

//    [Header("视觉精细度")]
//    [Tooltip("每只动物总点数。豹子比较大，建议设为 500-1000")]
//    public int pointsPerAnimal = 500;

//    [Tooltip("热气散发半径，建议 0.1 - 0.2")]
//    public float heatRadius = 0.2f;

//    void Start()
//    {
//        // 逻辑：每0.5秒刷新一次位置，兼顾性能和流畅
//        InvokeRepeating("RefreshHeatmap", 0.5f, 0.5f);
//    }

//    [ContextMenu("手动刷新热力图")]
//    public void RefreshHeatmap()
//    {
//        if (heatmapEngine == null || animals == null) return;

//        List<Vector3> heatPoints = new List<Vector3>();

//        foreach (Transform root in animals)
//        {
//            if (root == null) continue;

//            // 【核心改进】：获取豹子身上所有可见的渲染器（皮肤、毛发、零件）
//            Renderer[] allParts = root.GetComponentsInChildren<Renderer>();

//            if (allParts.Length > 0)
//            {
//                // 将点数平摊到豹子全身各个部位
//                int pointsPerPart = pointsPerAnimal / allParts.Length;

//                foreach (Renderer part in allParts)
//                {
//                    // 过滤掉不可见的辅助线或者热力图盒子本身
//                    if (!part.enabled || part.gameObject == heatmapEngine.gameObject) continue;

//                    // 直接获取该零件在世界空间的确切包围盒 (Bounds)
//                    // 这种方法无视层级、无视缩放、无视旋转，永远精准！
//                    Bounds b = part.bounds;

//                    for (int i = 0; i < pointsPerPart; i++)
//                    {
//                        // 在这个零件（比如豹头、豹腿）的范围内随机取点
//                        Vector3 randomPoint = new Vector3(
//                            Random.Range(b.min.x, b.max.x),
//                            Random.Range(b.min.y, b.max.y),
//                            Random.Range(b.min.z, b.max.z)
//                        );
//                        // 加上微小的扰动，让热气看起来更自然
//                        heatPoints.Add(randomPoint + Random.insideUnitSphere * heatRadius);
//                    }
//                }
//            }
//        }

//        // 把计算好的真实点位喂给引擎
//        heatmapEngine.GenerateHeatmap(heatPoints);
//    }


//    // 在 JaguarHeatSource.cs 类里面最后面加上这个函数
//    private void OnDrawGizmosSelected()
//    {
//        // 这个函数让你在选中物体时，能看到蓝色的点，确认采样位置对不对
//        if (heatmapEngine == null) return;

//        // 逻辑：模拟一次采样并画出来
//        foreach (Transform root in animals)
//        {
//            if (root == null) continue;
//            Renderer[] allParts = root.GetComponentsInChildren<Renderer>();
//            foreach (Renderer part in allParts)
//            {
//                Gizmos.color = Color.cyan;
//                // 画出这个零件的包围盒，看看是不是太大了
//                Gizmos.DrawWireCube(part.bounds.center, part.bounds.size);
//            }
//        }
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using HeatmapVisualization;

public class JaguarHeatSource : MonoBehaviour
{
    [Header("核心引用")]
    public Heatmap heatmapEngine;

    [Header("热源目标 (拖入豹子/Cube/圆柱体)")]
    public Transform[] animals;

    [Header("视觉精细度")]
    [Tooltip("每只动物的总采样点数。豹子建议设置 500 以上，Cube 100 即可。")]
    public int pointsPerAnimal = 600;

    [Tooltip("热气散发半径 (米)")]
    public float heatRadius = 0.2f;

    void Start()
    {
        // 每0.5秒刷新一次，既保证动态跟随，又不卡顿
        InvokeRepeating(nameof(RefreshHeatmap), 0.5f, 0.5f);
    }

    [ContextMenu("手动刷新热力图")]
    public void RefreshHeatmap()
    {
        if (heatmapEngine == null || animals == null) return;

        List<Vector3> heatPoints = new List<Vector3>();

        foreach (Transform root in animals)
        {
            if (root == null) continue;

            // 获取该物体下所有的渲染器（包括皮肤网格和普通网格）
            Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>();

            if (allRenderers.Length > 0)
            {
                // 计算平均每个零件分多少点，但保证每个零件至少有 20 个点，防止豹爪不亮
                int pointsPerPart = Mathf.Max(20, pointsPerAnimal / allRenderers.Length);

                foreach (Renderer rend in allRenderers)
                {
                    // 过滤掉关闭的渲染器、辅助线、以及热力图盒子本身
                    if (!rend.enabled || rend.gameObject == heatmapEngine.gameObject) continue;

                    // 获取该零件的世界空间包围盒 (这就是你看到的青色方框)
                    Bounds b = rend.bounds;

                    for (int i = 0; i < pointsPerPart; i++)
                    {
                        // 在这个零件的体积内随机填充点
                        Vector3 randomPoint = new Vector3(
                            Random.Range(b.min.x, b.max.x),
                            Random.Range(b.min.y, b.max.y),
                            Random.Range(b.min.z, b.max.z)
                        );

                        // 加入微小抖动，让热感更厚实
                        heatPoints.Add(randomPoint + Random.insideUnitSphere * heatRadius);
                    }
                }
            }
        }
        // 加这一行拦截
        if (heatPoints == null || heatPoints.Count == 0) return;

        // 将所有坐标点发送给热力图引擎
        heatmapEngine.GenerateHeatmap(heatPoints);
    }

    // 视觉辅助：选中物体时在 Scene 窗口显示采样范围
    private void OnDrawGizmosSelected()
    {
        if (animals == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform root in animals)
        {
            if (root == null) continue;
            Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in allRenderers)
            {
                Gizmos.DrawWireCube(rend.bounds.center, rend.bounds.size);
            }
        }
    }
}