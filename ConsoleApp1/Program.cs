using System;
class Program
{
    // Метод обчислює середній бал за допомогою циклу
   static double  CalculateSessionMean(int[] examScores)
    {
        // Створюємо змінну для суми всіх оцінок
        int totalPoints = 0;
        // Додамо кожну оцінку до загальної суми
        for (int i = 0; i < examScores.Length; i++)
        {
            totalPoints = totalPoints + examScores[i];
        }
        // Повертаємо середнє значення оцінок
        return (double)totalPoints / examScores.Length;
    }
    static void Main(string[] args)
    {
        // Константа містить найбільший можливий бал
        const int HIGHEST_ALLOWED_SCORE = 100;
        // Змінна для збереження імені студента
        string learnerName;
        // Зчитуємо ім'я студента з клавіатури
        Console.Write("Введіть ім'я студента: ");
        learnerName = Console.ReadLine();
        // Створюємо одновимірний масив оцінок за сесію
        int[] examScores = new int[5];
        // Заповнюємо масив оцінками
        for (int i = 0; i < examScores.Length; i++)
        {
            Console.Write("Введіть оцінку за предмет " + (i + 1) + ": "); examScores[i] =
            int.Parse(Console.ReadLine());
        }
        // Виводимо всі оцінки з масиву
        Console.WriteLine();
        Console.WriteLine("Оцінки студента:");
        for (int i = 0; i < examScores.Length; i++)
        {
            Console.WriteLine("Предмет " + (i + 1) + ": " + examScores[i]);
        }
        // Викликаємо власний метод для обчислення середнього балу double
        double sessionMean = CalculateSessionMean(examScores);
        // Перевіряємо, чи склав студент сесію
        bool passedSession;
        if (sessionMean >= 60)
        {
            passedSession = true;
        }
        else
        {
            passedSession = false;
        }
        // Виводимо результати роботи програми
        Console.WriteLine();
        Console.WriteLine("Студент: " + learnerName);
        Console.WriteLine("Максимальний бал: " + HIGHEST_ALLOWED_SCORE);
        Console.WriteLine("Середній бал: " + sessionMean);
        if (passedSession)
        {
            Console.WriteLine("Студент склав сесію.");
        }
        else
        {
            Console.WriteLine("Студент не склав сесію.");
        }
    }
}