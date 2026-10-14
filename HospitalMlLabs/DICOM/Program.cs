using System;
using System.IO;
using System.Text;

class DicomGenerator
{
    // 🌟 執行這個 Main 方法，硬碟裡就會立刻有無中生有的實體 DICOM 檔案！
    static void Main()
    {
        // 1. 設定您在 Program.cs 中指定的實體部署路徑
        string targetDirectory = @"Z:\HospitalMlLabs\data\";
        string targetFilePath = Path.Combine(targetDirectory, "DICOM_Pneumonia_01.dcm");

        try
        {
            // 防禦性設計：如果資料夾不存在，自動在 Windows 硬碟中開通它
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            using var fs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);

            // ----------------------------------------------------------------------
            // A. DICOM 3.0 標準導言區 (Preamble & Prefix)
            // ----------------------------------------------------------------------
            byte[] preamble = new byte[128]; // 128 位元組的空導言
            writer.Write(preamble);

            byte[] prefix = Encoding.ASCII.GetBytes("DICM"); // 4 位元組的黃金識別碼
            writer.Write(prefix);

            // ----------------------------------------------------------------------
            // B. 醫學 Metadata 與影像特徵資料元 (Dataset - Implicit VR Little Endian)
            // ----------------------------------------------------------------------
            // 格式：Group (2 Bytes), Element (2 Bytes), Length (4 Bytes), Value Bytes

            // 1. Samples Per Pixel (0x0028, 0x0002) = 1 (灰階圖)
            WriteDicomElement(writer, 0x0028, 0x0002, BitConverter.GetBytes((short)1));

            // 2. Photometric Interpretation (0x0028, 0x0004) = "MONOCHROME2 " (12字節，補空格對齊)
            WriteDicomElement(writer, 0x0028, 0x0004, Encoding.ASCII.GetBytes("MONOCHROME2 "));

            // 3. Rows 行數 (0x0028, 0x0010) = 224
            WriteDicomElement(writer, 0x0028, 0x0010, BitConverter.GetBytes((short)224));

            // 4. Columns 列數 (0x0028, 0x0011) = 224
            WriteDicomElement(writer, 0x0028, 0x0011, BitConverter.GetBytes((short)224));

            // 5. Bits Allocated 總配置位元 (0x0028, 0x0100) = 8-bit
            WriteDicomElement(writer, 0x0028, 0x0100, BitConverter.GetBytes((short)8));

            // 6. Bits Stored 實體儲存位元 (0x0028, 0x0101) = 8-bit
            WriteDicomElement(writer, 0x0028, 0x0101, BitConverter.GetBytes((short)8));

            // 7. High Bit 最高位元標記 (0x0028, 0x0102) = 7
            WriteDicomElement(writer, 0x0028, 0x0102, BitConverter.GetBytes((short)7));

            // 8. Pixel Representation 像素正負號標記 (0x0028, 0x0103) = 0 (無正負號)
            WriteDicomElement(writer, 0x0028, 0x0103, BitConverter.GetBytes((short)0));

            // 9. 🌟【核心對齊點一】：Window Center 窗位 (0x0028, 0x1050) = "40"
            WriteDicomElement(writer, 0x0028, 0x1050, Encoding.ASCII.GetBytes("40"));

            // 10. 🌟【核心對齊點二】：Window Width 窗寬 (0x0028, 0x1051) = "400"
            WriteDicomElement(writer, 0x0028, 0x1051, Encoding.ASCII.GetBytes("400"));

            // ----------------------------------------------------------------------
            // C. 實體二進位像素流矩陣 (Pixel Data: 0x7FE0, 0x0010)
            // ----------------------------------------------------------------------
            // 建立 224x224 = 50,176 位元組的仿真胸部 X 光片灰階點矩陣
            byte[] pixelData = new byte[224 * 224];
            var rand = new Random();

            for (int y = 0; y < 224; y++)
            {
                for (int x = 0; x < 224; x++)
                {
                    int index = y * 224 + x;
                    byte baseNoise = (byte)rand.Next(20, 35); // 基礎胸腔背景黑影氣體雜訊

                    // 🌟 實體人工注入：仿真左右肺葉區塊的「高密度肺炎白影浸潤特徵（高灰階變異值）」
                    // 這能讓您的大津演算法與 TensorFlow 特徵提取現場發揮百分之百的實體運算功能！
                    if (y >= 50 && y <= 180)
                    {
                        if ((x >= 40 && x <= 100) || (x >= 124 && x <= 184))
                        {
                            // 注入斑駁不均勻的發炎滲出液白影 (灰階值大幅拉高至 110 ~ 160)
                            baseNoise = (byte)(110 + rand.Next(0, 50));
                        }
                    }
                    pixelData[index] = baseNoise;
                }
            }

            // 將這 5 萬多個實體醫學像素流灌入 DICOM 尾端
            WriteDicomElement(writer, 0x7FE0, 0x0010, pixelData);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ 實體醫學影像生成成功！");
            Console.WriteLine($"   -> 實體檔案路徑: {targetFilePath}");
            Console.WriteLine($"   -> 檔案大小: {new FileInfo(targetFilePath).Length / 1024.0:F1} KB (對齊標準 8-bit Raw 規格)");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ 檔案寫入遭作業系統阻斷: {ex.Message}");
            Console.WriteLine("💡 提示：如果本機沒有 Z 槽，請將程式碼第 10 行路徑改為 @\"C:\\HospitalMlLabs\\data\\\" 即可！");
            Console.ResetColor();
        }
        Console.ReadLine();
    }

    /// <summary>
    /// 依據 DICOM 3.0 隱式傳輸規範，在二進位流上強制寫入標準資安資料元
    /// </summary>
    private static void WriteDicomElement(BinaryWriter writer, ushort group, ushort element, byte[] value)
    {
        writer.Write(group);             // 2 Bytes
        writer.Write(element);           // 2 Bytes
        writer.Write(value.Length);      // 4 Bytes (Length)
        writer.Write(value);             // N Bytes (Value)
    }
}
