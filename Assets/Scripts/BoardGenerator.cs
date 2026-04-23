using System.Collections.Generic;
using UnityEngine;
using TMPro; // Bắt buộc dùng nếu bạn dùng TextMeshPro cho Button

public class BoardGenerator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cellPrefab;    
    public Transform contentParent;  
    [Header("Board Settings")]
    public int totalCells = 100;     

    void Start()
    {
        GenerateBalancedBoard();
    }

    public void GenerateBalancedBoard()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        int[] numberPool = CreateBalancedNumberPool(totalCells);

        // 3. Xáo trộn ngẫu nhiên vị trí các số trong mảng
        ShuffleArray(numberPool);

        // 4. Sinh các ô (Prefab) và nhét vào Content của ScrollView
        for (int i = 0; i < numberPool.Length; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, contentParent);
            
            // Tìm TextMeshProUGUI trong Prefab để gán số
            TextMeshProUGUI numberText = newCell.GetComponentInChildren<TextMeshProUGUI>();
            if (numberText != null)
            {
                numberText.text = numberPool[i].ToString();
            }

            // Mẹo: Đổi tên Object trong Hierarchy để dễ debug
            newCell.name = $"Cell_{i}_[{numberPool[i]}]";
        }
    }

    //  THUẬT TOÁN PHÂN BỔ SỐ SỬ DỤNG MẢNG
    private int[] CreateBalancedNumberPool(int total)
    {
        int[] pool = new int[total]; // Khởi tạo mảng với kích thước cố định bằng total
        int baseCount = total / 9; // Số lượng xuất hiện mặc định
        int remainder = total % 9; // Lượng dư cần rải thêm

        // Mảng lưu số lần xuất hiện thực tế của các số (Index 0 = số 1, Index 8 = số 9)
        int[] digitCounts = new int[9];

        // Gán baseCount cho tất cả 9 chữ số
        for (int i = 0; i < 9; i++)
        {
            digitCounts[i] = baseCount;
        }

        // Rải phần dư ngẫu nhiên để tạo ra mảng như ví dụ [5, 4, 5, 5, 4, 5, 5, 4, 5]
        int[] availableIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        ShuffleArray(availableIndices); 

        for (int i = 0; i < remainder; i++)
        {
            digitCounts[availableIndices[i]]++; // Cộng thêm 1 lượt cho số may mắn này
        }

        // Đỏ toàn bộ số lượng đã tính toán vào mảng Pool
        int poolIndex = 0; // Biến vị trí để chèn vào mảng pool
        for (int digitIndex = 0; digitIndex < 9; digitIndex++)
        {
            int currentDigit = digitIndex + 1; // Chữ số từ 1-9
            for (int count = 0; count < digitCounts[digitIndex]; count++)
            {
                pool[poolIndex] = currentDigit;
                poolIndex++;
            }
        }

        return pool;
    }

    // --- THUẬT TOÁN XÁO TRỘN CHO MẢNG (Fisher-Yates Shuffle) ---
    
    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}