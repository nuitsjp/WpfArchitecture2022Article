// 明日の日付を表示するプログラム
using System;

class Program
{
    static void Main(string[] args)
    {
        DateTime dt = DateTime.Now;
        Console.WriteLine(dt.AddDays(1).ToString("yyyy/MM/dd"));

        // 来年の表示する
        Console.WriteLine(dt.AddYears(1).ToString("yyyy/MM/dd"));

        // コンソールから数値を入力して、その日数後の日付を表示する
        Console.Write("何日後の日付を表示しますか？：");
        int days = int.Parse(Console.ReadLine());
        Console.WriteLine(dt.AddDays(days).ToString("yyyy/MM/dd"));

        
    }
}